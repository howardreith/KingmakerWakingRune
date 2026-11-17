using KingmakerWakingRune.Party;

namespace KingmakerWakingRune.Tests
{
    /// <summary>The Coordinator's free retraining: when it may start, and who the native respec accepts.</summary>
    public static class RetrainingRulesTests
    {
        public static void TestRetrainingStartsOnlyWhenNothingElseIsMoving()
        {
            Assert.Equal(null, RetrainingRules.Refusal(true, false, false, false, false, false), "in the lodge and idle");
            Assert.Equal(false, RetrainingRules.Refusal(false, false, false, false, false, false) == null, "only at the lodge");
            Assert.Equal(false, RetrainingRules.Refusal(true, true, false, false, false, false) == null, "not in combat");
            Assert.Equal(false, RetrainingRules.Refusal(true, false, true, false, false, false) == null, "not while leaving or saving");
            Assert.Equal(false, RetrainingRules.Refusal(true, false, false, true, false, false) == null, "not over an open character build");
            Assert.Equal(false, RetrainingRules.Refusal(true, false, false, false, true, false) == null, "not while loading");
            Assert.Equal(false, RetrainingRules.Refusal(true, false, false, false, false, true) == null, "one retraining at a time");
        }

        public static void TestPetsAndTheDeadAreNotRetrainedOnTheirOwn()
        {
            Assert.Equal(true, RetrainingRules.CanRetrain(false, false, 11, 0), "a living level 11 character");
            Assert.Equal(false, RetrainingRules.CanRetrain(false, true, 11, 0), "an animal companion follows its owner's build");
            Assert.Equal(false, RetrainingRules.CanRetrain(true, false, 11, 0), "finally dead");
            Assert.Equal(false, RetrainingRules.CanRetrain(false, false, 3, 3), "at or below the blueprint's class-level limit");
        }
    }
}
