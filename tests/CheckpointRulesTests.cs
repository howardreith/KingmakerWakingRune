using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Tests
{
    /// <summary>The checkpoint completion contract (WR-PLAYTEST-02): only this operation's own terminal result ends it.</summary>
    public static class CheckpointRulesTests
    {
        private static CheckpointFacts Live()
        {
            return new CheckpointFacts { SameWorld = true };
        }

        private static CheckpointStage Assess(CheckpointFacts facts, out CheckpointFailure failure)
        {
            return CheckpointRules.Assess(facts, out failure);
        }

        /// <summary>
        /// The owner's log (0.10.14 with Mounted Combat 0.2.11): the save was current in the loading queue but held back before
        /// writing, while the queue and commit flags read idle. That is a pending save, not a failure.
        /// </summary>
        public static void TestSaveHeldBackBeforeWritingIsPending()
        {
            CheckpointFacts facts = Live();
            facts.Owned = true;
            facts.Current = true;
            Assert.Equal(CheckpointStage.Running, Assess(facts, out CheckpointFailure failure), "still running");
            Assert.Equal(CheckpointFailure.None, failure, "no failure");
            Assert.Equal(true, CheckpointRules.Pending(CheckpointStage.Running), "pending");
        }

        public static void TestSaveBehindAnotherOperationIsQueued()
        {
            CheckpointFacts facts = Live();
            facts.Owned = true;
            Assert.Equal(CheckpointStage.Queued, Assess(facts, out CheckpointFailure _), "queued");
        }

        public static void TestOperationRetiredWithoutCompletingIsRejected()
        {
            CheckpointFacts facts = Live();
            Assert.Equal(CheckpointStage.Failed, Assess(facts, out CheckpointFailure failure), "failed");
            Assert.Equal(CheckpointFailure.Rejected, failure, "the queue let it go without finishing it");
        }

        /// <summary>A routine that finished without its own record fails, whatever other save of the same name exists.</summary>
        public static void TestCompletionWithoutItsOwnRecordFails()
        {
            CheckpointFacts facts = Live();
            facts.Completed = true;
            Assert.Equal(CheckpointStage.Failed, Assess(facts, out CheckpointFailure failure), "failed");
            Assert.Equal(CheckpointFailure.NoRecord, failure, "no record of its own");
        }

        public static void TestRecordStillCommittingIsPending()
        {
            CheckpointFacts facts = Live();
            facts.Completed = true;
            facts.Prepared = true;
            facts.Listed = true;
            facts.HasArchive = true;
            facts.Committing = true;
            Assert.Equal(CheckpointStage.Committing, Assess(facts, out CheckpointFailure _), "committing");
            // The native failure path removes the record before it clears the operation state.
            facts.Listed = false;
            Assert.Equal(CheckpointStage.Committing, Assess(facts, out CheckpointFailure _), "still committing until the state clears");
        }

        public static void TestRecordRemovedByTheWriterIsAWriteFailure()
        {
            CheckpointFacts facts = Live();
            facts.Completed = true;
            facts.Prepared = true;
            facts.Listed = false;
            facts.HasArchive = false;
            Assert.Equal(CheckpointStage.Failed, Assess(facts, out CheckpointFailure failure), "failed");
            Assert.Equal(CheckpointFailure.WriteFailed, failure, "the write reported an error");
            facts.Listed = true;
            Assert.Equal(CheckpointFailure.WriteFailed, Fail(facts), "a record without an archive is not written");
        }

        public static void TestCommittedRecordIsWrittenAndAwaitsReadBack()
        {
            CheckpointFacts facts = Live();
            facts.Completed = true;
            facts.Prepared = true;
            facts.Listed = true;
            facts.HasArchive = true;
            Assert.Equal(CheckpointStage.Written, Assess(facts, out CheckpointFailure failure), "written");
            Assert.Equal(CheckpointFailure.None, failure, "no failure");
            Assert.Equal(false, CheckpointRules.Pending(CheckpointStage.Written), "terminal for the native save");
        }

        /// <summary>A load or new game during the save ends the operation, even if its save later completes.</summary>
        public static void TestAnotherWorldEndsTheOperation()
        {
            CheckpointFacts facts = Live();
            facts.Completed = true;
            facts.Prepared = true;
            facts.Listed = true;
            facts.HasArchive = true;
            facts.SameWorld = false;
            Assert.Equal(CheckpointFailure.WorldChanged, Fail(facts), "the world changed");
            facts.Completed = false;
            facts.Owned = true;
            Assert.Equal(CheckpointFailure.WorldChanged, Fail(facts), "even while still queued");
        }

        public static void TestFailuresAreDescribedWithoutBlamingTheDisk()
        {
            foreach (CheckpointFailure failure in new[] { CheckpointFailure.Rejected, CheckpointFailure.NoRecord, CheckpointFailure.WriteFailed, CheckpointFailure.ReadBack, CheckpointFailure.WorldChanged, CheckpointFailure.TimedOut })
            {
                string text = CheckpointRules.Describe(failure, "before leaving");
                Assert.Equal(false, text.Contains("disk"), $"{failure} does not mention the disk");
                Assert.Equal(true, text.Contains("before leaving"), $"{failure} names the save");
            }
            Assert.Equal(false, CheckpointRules.Describe(CheckpointFailure.Rejected, "x") == CheckpointRules.Describe(CheckpointFailure.TimedOut, "x"), "rejection and timeout are told apart");
            Assert.Equal(false, CheckpointRules.Describe(CheckpointFailure.ReadBack, "x") == CheckpointRules.Describe(CheckpointFailure.WriteFailed, "x"), "read-back and write failure are told apart");
        }

        /// <summary>
        /// WR-PLAYTEST-02 review: the one-save guard is held while the queue holds the operation or its record is being
        /// written, and released once both are over, however it ended. A given-up operation that the queue later retires
        /// without its completion callback (rejected, or a failed write) must release it too, or every later departure,
        /// portal use and retraining is refused for the rest of the session.
        /// </summary>
        public static void TestTheGuardIsReleasedByAnyEnd()
        {
            Assert.Equal(true, CheckpointRules.HoldsSaveGuard(true, false), "queued or current: held");
            Assert.Equal(true, CheckpointRules.HoldsSaveGuard(false, true), "routine over, archive still being written: held");
            Assert.Equal(false, CheckpointRules.HoldsSaveGuard(false, false), "left the queue, nothing writing: released");
            // The given-up operation that was rejected: not owned, never completed, no record. Its assessment is a failure
            // (so it cannot lead to travel), and it no longer holds the guard.
            CheckpointFacts rejected = Live();
            Assert.Equal(CheckpointFailure.Rejected, Fail(rejected), "rejected");
            Assert.Equal(false, CheckpointRules.HoldsSaveGuard(rejected.Owned, rejected.Committing), "and released");
        }

        private static CheckpointFailure Fail(CheckpointFacts facts)
        {
            Assert.Equal(CheckpointStage.Failed, CheckpointRules.Assess(facts, out CheckpointFailure failure), "failed");
            return failure;
        }
    }
}
