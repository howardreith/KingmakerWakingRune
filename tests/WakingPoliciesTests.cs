using KingmakerWakingRune.Lodge;

namespace KingmakerWakingRune.Tests
{
    public static class WakingPoliciesTests
    {
        public static void TestFirstInitializationSpawnsMissingMembers()
        {
            Assert.Equal(WakingPolicies.CastAction.Spawn, WakingPolicies.DecideMember(false, 0), "fresh save");
        }

        public static void TestExistingMemberIsKeptDuringFirstInitialization()
        {
            // A WR-01 save already has Sheila; only the missing cast is created.
            Assert.Equal(WakingPolicies.CastAction.Keep, WakingPolicies.DecideMember(false, 1), "already present");
        }

        public static void TestReloadNeverSpawnsAfterSetupRecorded()
        {
            Assert.Equal(WakingPolicies.CastAction.Keep, WakingPolicies.DecideMember(true, 1), "reload");
            Assert.Equal(WakingPolicies.CastAction.Report, WakingPolicies.DecideMember(true, 0), "missing after setup");
        }

        public static void TestDuplicatesAreReportedNotCreated()
        {
            Assert.Equal(WakingPolicies.CastAction.Report, WakingPolicies.DecideMember(false, 2), "duplicate before setup");
            Assert.Equal(WakingPolicies.CastAction.Report, WakingPolicies.DecideMember(true, 2), "duplicate after setup");
        }

        public static void TestGrantsHappenOnlyWhileFlagIsUnset()
        {
            Assert.Equal(true, WakingPolicies.ShouldGrant(false), "first");
            Assert.Equal(false, WakingPolicies.ShouldGrant(true), "repeat load");
        }

        public static void TestProtagonistXpReachesExactlyTheTarget()
        {
            Assert.Equal(115000, WakingPolicies.ProtagonistXpDelta(0, 115000), "fresh hero");
            Assert.Equal(0, WakingPolicies.ProtagonistXpDelta(115000, 115000), "already at target");
            Assert.Equal(0, WakingPolicies.ProtagonistXpDelta(130000, 115000), "never removes earned XP");
        }

        public static void TestTreasuryLandsOnExactAmount()
        {
            Assert.Equal(450000L, 0 + WakingPolicies.TreasuryDelta(0, 450000), "from zero");
            Assert.Equal(449100L, WakingPolicies.TreasuryDelta(900, 450000), "class starting gold absorbed");
            Assert.Equal(-50L, WakingPolicies.TreasuryDelta(450050, 450000), "above target comes down");
            Assert.Equal(50000L, WakingPolicies.TreasurySupplement(true, false, false, 450000, 500000), "old lodge save: the difference once");
            Assert.Equal(0L, WakingPolicies.TreasurySupplement(true, true, false, 450000, 500000), "supplement already applied (reload)");
            Assert.Equal(0L, WakingPolicies.TreasurySupplement(true, false, true, 450000, 500000), "the expedition has left");
            Assert.Equal(0L, WakingPolicies.TreasurySupplement(false, false, false, 450000, 500000), "no old grant: the new game gets the full amount instead");
        }

        public static void TestRecruitStartingGoldIsRefunded()
        {
            Assert.Equal(800L, WakingPolicies.RecruitGoldRefund(450000, 450800), "native starting gold");
            Assert.Equal(0L, WakingPolicies.RecruitGoldRefund(450000, 450000), "no change");
            Assert.Equal(0L, WakingPolicies.RecruitGoldRefund(450000, 449000), "never adds gold");
        }

        public static void TestBriefingOpensOnlyWhenPendingAndFree()
        {
            Assert.Equal(true, WakingPolicies.ShouldOpenBriefing(true, true, false, true, false), "ready");
            Assert.Equal(false, WakingPolicies.ShouldOpenBriefing(false, true, false, true, false), "outside lodge");
            Assert.Equal(false, WakingPolicies.ShouldOpenBriefing(true, false, false, true, false), "not a Waking Rune save");
            Assert.Equal(false, WakingPolicies.ShouldOpenBriefing(true, true, true, true, false), "already complete");
            Assert.Equal(false, WakingPolicies.ShouldOpenBriefing(true, true, false, false, false), "speakers not ready");
            Assert.Equal(false, WakingPolicies.ShouldOpenBriefing(true, true, false, true, true), "another modal owns the UI");
        }
    }
}
