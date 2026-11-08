using KingmakerWakingRune.Lodge;

namespace KingmakerWakingRune.Tests
{
    public static class LodgePolicyTests
    {
        private const int V = 1;

        private static LodgeAction Decide(bool inLodge, bool marked, int version, bool setupDone, int sheilas)
        {
            return LodgePolicy.Decide(inLodge, marked, version, V, setupDone, sheilas).Action;
        }

        public static void TestStockAreaWithoutMarkerIsIgnored()
        {
            Assert.Equal(LodgeAction.None, Decide(false, false, 0, false, 0), "stock campaign area");
        }

        public static void TestFirstLodgeEntryCreatesSetupOnce()
        {
            Assert.Equal(LodgeAction.CreateInitialSetup, Decide(true, true, V, false, 0), "fresh session");
        }

        public static void TestReloadAfterSetupOnlyVerifies()
        {
            Assert.Equal(LodgeAction.Restored, Decide(true, true, V, true, 1), "save reload");
        }

        public static void TestReloadNeverRespawnsWhenSheilaMissing()
        {
            Assert.Equal(LodgeAction.Error, Decide(true, true, V, true, 0), "setup recorded, Sheila gone");
        }

        public static void TestDuplicatesAreReportedNotCreated()
        {
            Assert.Equal(LodgeAction.Error, Decide(true, true, V, true, 2), "duplicate Sheila");
            Assert.Equal(LodgeAction.Error, Decide(true, true, V, false, 1), "Sheila before setup");
        }

        public static void TestLodgeWithoutSessionMarkerIsRejected()
        {
            Assert.Equal(LodgeAction.Error, Decide(true, false, 0, false, 0), "marker missing");
        }

        public static void TestUnsupportedSessionVersionIsRejected()
        {
            Assert.Equal(LodgeAction.Error, Decide(true, true, V + 1, false, 0), "future version");
        }

        public static void TestSessionOutsideLodgeIsReported()
        {
            Assert.Equal(LodgeAction.Error, Decide(false, true, V, true, 0), "session elsewhere");
        }
    }
}
