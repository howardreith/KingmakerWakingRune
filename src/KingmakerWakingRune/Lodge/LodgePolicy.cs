namespace KingmakerWakingRune.Lodge
{
    internal enum LodgeAction
    {
        /// <summary>Not a Waking Rune session; do nothing.</summary>
        None,
        /// <summary>First entry of a new session: create Sheila and record setup.</summary>
        CreateInitialSetup,
        /// <summary>Setup already happened (save load or re-entry): verify only, never spawn.</summary>
        Restored,
        /// <summary>Inconsistent state; report it, change nothing.</summary>
        Error,
    }

    internal struct LodgeDecision
    {
        public LodgeAction Action;
        public string Reason;

        public LodgeDecision(LodgeAction action, string reason)
        {
            Action = action;
            Reason = reason;
        }
    }

    /// <summary>Pure decision for what an area load means for lodge setup. No game types.</summary>
    internal static class LodgePolicy
    {
        public static LodgeDecision Decide(bool inLodge, bool sessionMarked, int sessionVersion, int supportedVersion, bool setupDone, int sheilaCount)
        {
            if (!inLodge)
            {
                return sessionMarked
                    ? new LodgeDecision(LodgeAction.Error, "Waking Rune session loaded outside the lodge")
                    : new LodgeDecision(LodgeAction.None, "not a Waking Rune area");
            }
            if (!sessionMarked)
            {
                return new LodgeDecision(LodgeAction.Error, "lodge loaded without the Waking Rune session marker");
            }
            if (sessionVersion != supportedVersion)
            {
                return new LodgeDecision(LodgeAction.Error, $"unsupported session version {sessionVersion} (expected {supportedVersion})");
            }
            if (!setupDone)
            {
                return sheilaCount == 0
                    ? new LodgeDecision(LodgeAction.CreateInitialSetup, "first lodge entry")
                    : new LodgeDecision(LodgeAction.Error, $"setup not recorded but {sheilaCount} Sheila instance(s) exist");
            }
            return sheilaCount == 1
                ? new LodgeDecision(LodgeAction.Restored, "setup previously recorded; one Sheila present")
                : new LodgeDecision(LodgeAction.Error, $"setup recorded but {sheilaCount} Sheila instance(s) exist");
        }
    }
}
