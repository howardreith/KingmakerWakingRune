namespace KingmakerWakingRune.Sanctum
{
    /// <summary>Where one requested checkpoint save stands.</summary>
    internal enum CheckpointStage
    {
        /// <summary>Waiting behind another operation in the native loading queue.</summary>
        Queued,

        /// <summary>The native loading queue is running it (another mod may still hold it back before writing).</summary>
        Running,

        /// <summary>Its routine has finished and the record it wrote is still being committed to disk.</summary>
        Committing,

        /// <summary>Its routine has finished and its record is committed: read it back before trusting it.</summary>
        Written,

        Failed,
    }

    /// <summary>Why a checkpoint failed. Each is reported in its own words; none of them is taken to mean a full disk.</summary>
    internal enum CheckpointFailure
    {
        None,

        /// <summary>The loading queue let go of the operation without finishing it (another mod or the engine retired it).</summary>
        Rejected,

        /// <summary>The routine finished without creating a record (the game refused to save at that moment).</summary>
        NoRecord,

        /// <summary>The native writer removed the record it created: the write itself reported an error.</summary>
        WriteFailed,

        /// <summary>The record was committed but its archive could not be read back.</summary>
        ReadBack,

        /// <summary>A save was loaded, or a new game started, while the operation was pending.</summary>
        WorldChanged,

        /// <summary>The bounded wait ran out before a terminal result; the stage it was in is reported.</summary>
        TimedOut,
    }

    /// <summary>What can be observed of one checkpoint operation, read fresh each frame.</summary>
    internal struct CheckpointFacts
    {
        /// <summary>The game and session that asked for the save are still loaded.</summary>
        public bool SameWorld;

        /// <summary>The native loading queue still holds this exact operation, as its current process or queued.</summary>
        public bool Owned;

        /// <summary>It is the loading queue's current process.</summary>
        public bool Current;

        /// <summary>Its completion callback ran: the native queue finished it normally (a retired operation never calls back).</summary>
        public bool Completed;

        /// <summary>The native writer created the replacement record for this operation (seen inside its own routine).</summary>
        public bool Prepared;

        /// <summary>That record is still in the game's list of saves.</summary>
        public bool Listed;

        /// <summary>That record still names an archive (a failed write clears it).</summary>
        public bool HasArchive;

        /// <summary>That record is still being serialized or committed.</summary>
        public bool Committing;
    }

    /// <summary>
    /// The completion contract for a scenario checkpoint (WR-PLAYTEST-02). A save is pending until this exact request reaches
    /// a terminal result: global loading or commit flags are never taken as its end, because another mod (Mounted Combat's
    /// deferred save) can hold a queued save back while those flags read idle. Pure, so it is unit-tested.
    /// </summary>
    internal static class CheckpointRules
    {
        public static CheckpointStage Assess(CheckpointFacts facts, out CheckpointFailure failure)
        {
            failure = CheckpointFailure.None;
            if (!facts.SameWorld)
            {
                failure = CheckpointFailure.WorldChanged;
                return CheckpointStage.Failed;
            }
            if (!facts.Completed)
            {
                if (facts.Owned)
                {
                    return facts.Current ? CheckpointStage.Running : CheckpointStage.Queued;
                }
                failure = CheckpointFailure.Rejected;
                return CheckpointStage.Failed;
            }
            if (!facts.Prepared)
            {
                failure = CheckpointFailure.NoRecord;
                return CheckpointStage.Failed;
            }
            if (facts.Committing)
            {
                return CheckpointStage.Committing;
            }
            if (!facts.Listed || !facts.HasArchive)
            {
                failure = CheckpointFailure.WriteFailed;
                return CheckpointStage.Failed;
            }
            return CheckpointStage.Written;
        }

        /// <summary>
        /// Whether an operation still holds the one-save-at-a-time guard: while the native queue holds it, or its record is
        /// still being written. Once both are over it releases the guard whatever its result (success, rejection, write
        /// failure), including an operation that was given up; given up, it never leads to travel either way.
        /// </summary>
        public static bool HoldsSaveGuard(bool owned, bool recordCommitting)
        {
            return owned || recordCommitting;
        }

        /// <summary>A stage that can still change by itself.</summary>
        public static bool Pending(CheckpointStage stage)
        {
            return stage == CheckpointStage.Queued || stage == CheckpointStage.Running || stage == CheckpointStage.Committing;
        }

        /// <summary>The player's words for a failure: what happened and that nothing was spent, never a guess at the disk.</summary>
        public static string Describe(CheckpointFailure failure, string what)
        {
            switch (failure)
            {
                case CheckpointFailure.Rejected:
                    return $"the game did not write the save {what}";
                case CheckpointFailure.NoRecord:
                    return $"the game declined to save {what}";
                case CheckpointFailure.WriteFailed:
                    return $"the game reported an error writing the save {what}";
                case CheckpointFailure.ReadBack:
                    return $"the save {what} was written but could not be read back";
                case CheckpointFailure.WorldChanged:
                    return $"another game was loaded while saving {what}";
                case CheckpointFailure.TimedOut:
                    return $"the save {what} did not finish in time";
                default:
                    return $"the save {what} could not be confirmed";
            }
        }
    }
}
