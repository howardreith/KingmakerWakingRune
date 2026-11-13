using KingmakerWakingRune.Scenario;

namespace KingmakerWakingRune.Tests
{
    internal static class JournalTests
    {
        public static void TestTheJournalFollowsTheRecordedJourneyToSuccess()
        {
            var f = new JournalFacts();
            var m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Started, m[JournalRules.Prepare], "preparation is the first objective");
            Assert.Equal(JournalMark.None, m[JournalRules.Defenses], "nothing else before departure");
            f.SanctumEntered = true;
            m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Completed, m[JournalRules.Prepare], "arrived");
            Assert.Equal(JournalMark.Started, m[JournalRules.Defenses], "the sanctum's defenses");
            Assert.Equal(JournalMark.None, m[JournalRules.Revival], "the revival not yet known");
            f.DefendersDefeated = true;
            m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Completed, m[JournalRules.Defenses], "the way to the tomb is open");
            Assert.Equal(JournalMark.Started, m[JournalRules.Revival], "confront the revival");
            f.KruneAwake = true;
            m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Completed, m[JournalRules.Revival], "he woke");
            Assert.Equal(JournalMark.Started, m[JournalRules.Krune], "defeat him");
            f.KruneSlain = true;
            m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Completed, m[JournalRules.Krune], "slain");
            Assert.Equal(JournalMark.Started, m[JournalRules.Artifacts], "the artifacts to carry home");
            f.Returned = f.SpearHome = f.RodHome = true;
            m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Completed, m[JournalRules.Artifacts], "both carried home");
            Assert.Equal(JournalMark.Started, m[JournalRules.Report], "report at the lodge");
            f.Debriefed = true;
            Assert.Equal(JournalMark.Completed, JournalRules.Marks(f)[JournalRules.Report], "the report completes the quest");
        }

        public static void TestHeMayWakeBeforeTheDefendersFall()
        {
            var f = new JournalFacts { SanctumEntered = true, KruneAwake = true };
            var m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Completed, m[JournalRules.Defenses], "the tomb reached (or the revival came to them)");
            Assert.Equal(JournalMark.Completed, m[JournalRules.Revival], "confronted");
            Assert.Equal(JournalMark.Started, m[JournalRules.Krune], "now defeat him");
        }

        public static void TestAWithdrawalFailsWhatWasLeftAndTheQuestButNotWhatWasDone()
        {
            var f = new JournalFacts { SanctumEntered = true, DefendersDefeated = true, Returned = true, Debriefed = true, Withdrawn = true };
            var m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Completed, m[JournalRules.Defenses], "the defenses stay overcome");
            Assert.Equal(JournalMark.Failed, m[JournalRules.Revival], "the revival not confronted");
            Assert.Equal(JournalMark.Failed, m[JournalRules.Krune], "the runelord not stopped");
            Assert.Equal(JournalMark.None, m[JournalRules.Artifacts], "no artifacts objective without his death");
            Assert.Equal(JournalMark.Failed, m[JournalRules.Report], "the quest fails with the report");
        }

        public static void TestAnArtifactLeftBehindFailsOnlyThatObjective()
        {
            var f = new JournalFacts { SanctumEntered = true, DefendersDefeated = true, KruneAwake = true, KruneSlain = true, Returned = true, SpearHome = true, Debriefed = true };
            var m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Completed, m[JournalRules.Krune], "slain");
            Assert.Equal(JournalMark.Failed, m[JournalRules.Artifacts], "the rod left behind");
            Assert.Equal(JournalMark.Completed, m[JournalRules.Report], "the quest still completes");
        }

        public static void TestTheJournalOnlyMovesForward()
        {
            Assert.Equal(true, JournalRules.Advances(JournalMark.None, JournalMark.Started), "start");
            Assert.Equal(true, JournalRules.Advances(JournalMark.None, JournalMark.Completed), "straight to done on an old save");
            Assert.Equal(true, JournalRules.Advances(JournalMark.Started, JournalMark.Failed), "fail");
            Assert.Equal(false, JournalRules.Advances(JournalMark.Completed, JournalMark.Started), "never back");
            Assert.Equal(false, JournalRules.Advances(JournalMark.Failed, JournalMark.Completed), "a failure stands");
            Assert.Equal(false, JournalRules.Advances(JournalMark.Started, JournalMark.None), "nor undone");
        }
    }
}
