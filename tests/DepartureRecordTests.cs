using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Tests
{
    /// <summary>The departure record: the expedition is readied once per committed departure, and older records migrate quietly.</summary>
    public static class DepartureRecordTests
    {
        public static void TestFirstUseReadiesOnce()
        {
            Assert.Equal(DepartureReadiness.Refresh, DepartureRecord.Classify(null), "no record: ready now");
            Assert.Equal(DepartureReadiness.Reuse, DepartureRecord.Classify(DepartureRecord.Ready(132594.8)), "retry or checkpoint reload: no second refresh");
            Assert.Equal("ready:132595", DepartureRecord.Ready(132594.8), "marker keeps the game time");
        }

        public static void TestEarlierTwoUseRecordMigrates()
        {
            Assert.Equal(DepartureReadiness.Legacy, DepartureRecord.Classify("a=| wiz cl 11 3:[fly,-]"), "the old selections record is dropped, not replayed");
        }
    }
}
