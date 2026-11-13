using KingmakerWakingRune.Scenario;

namespace KingmakerWakingRune.Tests
{
    internal static class JournalTests
    {
        public static void TestTheJournalFollowsTheRecordedJourneyToSuccess()
        {
            var f = new JournalFacts();
            var m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Started, m[JournalRules.Brief], "the briefing is the first objective");
            Assert.Equal(JournalMark.None, m[JournalRules.Travel], "nothing else before it");
            f.Briefed = true;
            Assert.Equal(JournalMark.Started, JournalRules.Marks(f)[JournalRules.Travel], "then the token");
            f.SanctumEntered = true;
            m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Completed, m[JournalRules.Travel], "arrived");
            Assert.Equal(JournalMark.Started, m[JournalRules.Kurshu], "Kurshu next");
            f.KurshuDefeated = true;
            Assert.Equal(JournalMark.Started, JournalRules.Marks(f)[JournalRules.Defenders], "then the antechamber");
            f.DefendersDefeated = true;
            Assert.Equal(JournalMark.Started, JournalRules.Marks(f)[JournalRules.Krune], "then the runelord");
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

        public static void TestAWithdrawalFailsTheRunelordAndTheQuestButNotWhatWasDone()
        {
            var f = new JournalFacts { Briefed = true, SanctumEntered = true, KurshuDefeated = true, Returned = true, Debriefed = true, Withdrawn = true };
            var m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Completed, m[JournalRules.Kurshu], "Kurshu stays done");
            Assert.Equal(JournalMark.Failed, m[JournalRules.Krune], "the runelord not stopped");
            Assert.Equal(JournalMark.None, m[JournalRules.Artifacts], "no artifacts objective without his death");
            Assert.Equal(JournalMark.Failed, m[JournalRules.Report], "the quest fails with the report");
        }

        public static void TestAnArtifactLeftBehindFailsOnlyThatObjective()
        {
            var f = new JournalFacts { Briefed = true, SanctumEntered = true, KurshuDefeated = true, DefendersDefeated = true, KruneSlain = true, Returned = true, SpearHome = true, Debriefed = true };
            var m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Completed, m[JournalRules.Krune], "slain");
            Assert.Equal(JournalMark.Failed, m[JournalRules.Artifacts], "the rod left behind");
            Assert.Equal(JournalMark.Completed, m[JournalRules.Report], "the quest still completes");
        }

        public static void TestObjectivesOpenWhenTheirEncounterBeginsOutOfOrder()
        {
            var f = new JournalFacts { Briefed = true, SanctumEntered = true, DefendersEngaged = true };
            var m = JournalRules.Marks(f);
            Assert.Equal(JournalMark.Started, m[JournalRules.Kurshu], "Kurshu still open");
            Assert.Equal(JournalMark.Started, m[JournalRules.Defenders], "the antechamber fight begun before Kurshu fell");
            Assert.Equal(JournalMark.None, m[JournalRules.Krune], "the runelord not yet known");
            f.KruneAwake = true;
            Assert.Equal(JournalMark.Started, JournalRules.Marks(f)[JournalRules.Krune], "he woke while the defenders stood");
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
