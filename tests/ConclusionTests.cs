using KingmakerWakingRune.Sanctum.Krune;

namespace KingmakerWakingRune.Tests
{
    internal static class ConclusionTests
    {
        public static void TestOnlyAHomecomingWithBothArtifactsIsASuccess()
        {
            var c = new ConclusionState();
            Assert.Equal(ScenarioOutcome.None, ConclusionRules.Decide(c), "nothing decided before the party is home");
            Assert.Equal(true, ConclusionRules.RecordDeath(c, 100d, false), "death recorded");
            Assert.Equal(false, ConclusionRules.RecordDeath(c, 200d, false), "once");
            Assert.Equal(100d, c.KruneDeadAt, "the first time stands");
            Assert.Equal(ScenarioOutcome.None, ConclusionRules.Decide(c), "a death alone is not the end");
            c.BodyConsumed = true;
            Assert.Equal(ScenarioOutcome.None, ConclusionRules.Decide(c), "nor is the body consumed");
            Assert.Equal(true, ConclusionRules.RecordReturn(c, 300d, true, true), "home");
            Assert.Equal(false, ConclusionRules.RecordReturn(c, 400d, false, false), "a reload does not return twice");
            Assert.Equal(ScenarioOutcome.Success, ConclusionRules.Decide(c), "both artifacts");
            Assert.Equal(true, ConclusionRules.MayHandOver(c), "handover allowed once");
            c.HandoverAt = 310d;
            Assert.Equal(false, ConclusionRules.MayHandOver(c), "never twice");
        }

        public static void TestWithdrawalAndMissingArtifactsAreDistinguished()
        {
            var early = new ConclusionState();
            Assert.Equal(true, ConclusionRules.ReturnWarning(early, true, true).Contains("abandons"), "warned before abandoning");
            ConclusionRules.RecordReturn(early, 50d, false, false);
            Assert.Equal(ScenarioOutcome.Withdrawn, ConclusionRules.Decide(early), "home before Krune died");
            Assert.Equal(false, ConclusionRules.MayHandOver(early), "nothing to hand over");

            var partial = new ConclusionState();
            ConclusionRules.RecordDeath(partial, 10d, false);
            Assert.Equal(true, ConclusionRules.ReturnWarning(partial, true, false).Contains("rune-carved rod"), "warned of the rod");
            Assert.Equal(string.Empty, ConclusionRules.ReturnWarning(partial, true, true), "nothing to warn with both");
            ConclusionRules.RecordReturn(partial, 20d, true, false);
            Assert.Equal(ScenarioOutcome.KruneSlainArtifactsMissing, ConclusionRules.Decide(partial), "slain, rod left behind");

            var diagnostic = new ConclusionState();
            ConclusionRules.RecordDeath(diagnostic, 10d, true);
            ConclusionRules.RecordReturn(diagnostic, 20d, true, true);
            Assert.Equal(ScenarioOutcome.DiagnosticLegacy, ConclusionRules.Decide(diagnostic), "a diagnostic kill is never a completed adventure");
        }
    }
}
