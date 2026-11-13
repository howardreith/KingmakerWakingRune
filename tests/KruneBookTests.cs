using System.Linq;
using KingmakerWakingRune.Sanctum.Krune;

namespace KingmakerWakingRune.Tests
{
    internal static class KruneBookTests
    {
        public static void TestEveryPrintedPreparationHasOneSlotOfItsOwn()
        {
            Assert.Equal(true, KruneManifest.Prepared.All(p => KruneBook.Known(p.Spell)), "every printed spell is mapped");
            var plan = KruneBook.Plan();
            Assert.Equal(KruneManifest.Prepared.Sum(p => p.Count), plan.Count, "one entry per printed slot");
            for (int level = 0; level <= 9; level++)
            {
                Assert.Equal(KruneManifest.PreparedCount(level), plan.Count(e => e.Level == level), $"level {level}");
                Assert.Equal(KruneManifest.PreparedCount(level), KruneBook.Slots(level), $"book size at level {level}");
            }
        }

        public static void TestMetamagicSitsInTheRaisedSlotWithoutAnExtraCost()
        {
            var plan = KruneBook.Plan();
            BookEntry quickSummon = plan.Single(e => e.Spell == "Summon Monster V" && e.Metamagic == "Quickened");
            Assert.Equal(9, quickSummon.Level, "quickened summon monster V in a 9th-level slot");
            Assert.Equal(5, quickSummon.BaseLevel, "known at 5th");
            BookEntry empowered = plan.Single(e => e.Spell == "Summon Monster VI" && e.Metamagic == "Empowered");
            Assert.Equal(6, empowered.BaseLevel, "empowered summon monster VI known at 6th");
            Assert.Equal(1, plan.Count(e => e.Spell == "Summon Monster IX"), "one summon monster IX");
            Assert.Equal(string.Empty, plan.Single(e => e.Spell == "Summon Monster IX").Metamagic, "not quickened");
            Assert.Equal(2, plan.Count(e => e.Spell == "Wall of Stone"), "quickened at 9th and plain at 5th");
        }

        public static void TestNothingIsFreeOrSilentlyDropped()
        {
            var plan = KruneBook.Plan();
            Assert.Equal(PrepKind.Spent, plan.Single(e => e.Spell == "Mage Armor").Kind, "cast before hibernation");
            Assert.Equal(true, plan.Where(e => e.Kind == PrepKind.Held).All(e => e.Blueprint.StartsWith("KWR_KruneHeld_") && KruneBook.HeldReason(e.Spell).Length > 0), "held slots are named with a reason");
            Assert.Equal(PrepKind.Local, plan.First(e => e.Spell == "Wall of Stone").Kind, "a defining spell is built, not held");
            Assert.Equal(PrepKind.Local, plan.First(e => e.Spell == "Black Tentacles").Kind, "a defining spell is built, not held");
            Assert.Equal(false, plan.Any(e => e.Spell == "Wish"), "wish is known, not prepared");
            Assert.Equal("DimensionDoorCasterOnly", plan.First(e => e.Spell == "Dimension Door").Variant, "the single-creature form");
        }
    }
}
