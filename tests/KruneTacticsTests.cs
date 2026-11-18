using System;
using System.Linq;
using KingmakerWakingRune.Sanctum.Krune;

namespace KingmakerWakingRune.Tests
{
    internal static class KruneTacticsTests
    {
        private static KruneView Full()
        {
            var v = new KruneView();
            foreach (BookEntry e in KruneBook.Plan().Where(e => e.Kind == PrepKind.Native || e.Kind == PrepKind.Local))
            {
                string key = KruneTactics.Key(e.Spell, e.Metamagic);
                v.Ready[key] = v.Ready.TryGetValue(key, out int n) ? n + 1 : 1;
            }
            return v;
        }

        public static void TestHeProtectsHimselfThenDividesThePartyBeforeHisStrongestSpells()
        {
            KruneView v = Full();
            v.FoesWithin30 = 3;
            v.BestCluster = 3;
            v.FoeCasters = 2;
            Assert.Equal("Foresight", KruneTactics.Standard(v).Spell, "protection first");
            KruneChoice swift = KruneTactics.Swift(v);
            Assert.Equal("Quickened Wall of Stone", swift.Key, "a quickened wall divides the foes");
            v.HasForesight = v.HasSpellTurning = v.HasStoneskin = v.HasGlobe = true;
            v.RoundsInCombat = 2;
            Assert.Equal("Summon Monster IX", KruneTactics.Standard(v).Spell, "summons before offense");
            v.SummonsAlive = 3;
            v.WallStanding = true;
            Assert.Equal("Horrid Wilting", KruneTactics.Standard(v).Spell, "then his most potent spell");
        }

        public static void TestHeAnswersWhatHeActuallySees()
        {
            KruneView v = Full();
            v.HasForesight = v.HasStoneskin = v.HasSpellTurning = v.HasGlobe = true;
            v.UnseenFoeSuspected = true;
            Assert.Equal("Quickened Glitterdust", KruneTactics.Swift(v).Key, "an unseen foe: quickened glitterdust");
            Assert.Equal("True Seeing", KruneTactics.Standard(v).Spell, "and true seeing");
            v.HasTrueSeeing = true;
            v.UnseenFoeSuspected = false;
            v.AdjacentFoes = 1;
            Assert.Equal("Shield", KruneTactics.Standard(v).Spell, "a foe at his side: shield");
            v.HasShield = true;
            v.AdjacentFoes = 0;
            v.BestCluster = 2;
            v.FoeHasStrongBuffs = true;
            Assert.Equal(false, KruneTactics.SwiftOptions(v).Concat(KruneTactics.StandardOptions(v)).Any(choice => choice.Spell == "Black Tentacles"), "both printed preparations are unavailable");
        }

        public static void TestHeldSpearNeedsAnUnusedStandardAnAdjacentFoeAndExhaustedMagic()
        {
            Assert.Equal(true, KruneTactics.MayUseHeldSpear(true, false, true), "exhausted caster with a foe in reach");
            Assert.Equal(false, KruneTactics.MayUseHeldSpear(true, true, true), "magic available in melee");
            Assert.Equal(false, KruneTactics.MayUseHeldSpear(true, false, false), "no walking into melee");
            Assert.Equal(false, KruneTactics.MayUseHeldSpear(false, false, true), "standard already spent");
        }

        public static void TestRejectedSwiftSummonCannotStarveStandardAtFrozenGameTime()
        {
            var plans = new FailedKrunePlans();
            const string summon = "SummonMonsterVSingle/Quicken/point/Swift";
            plans.BeginRound(1);
            plans.Reject(summon);
            for (int retry = 0; retry < 1000; retry++)
            {
                plans.BeginRound(1);
                Assert.Equal(false, plans.Allows(summon), "same turn, same command");
            }
            Assert.Equal(true, plans.Allows("SummonMonsterVSingle/point/Standard"), "standard remains available");
            plans.BeginRound(2);
            Assert.Equal(true, plans.Allows(summon), "next round can reconsider");
        }

        public static void TestAWallOfStoneIsARealBreakableObstacle()
        {
            WallRecord wall = WallRules.Layout("Wall-1", 0f, 0f, 0f, 4f, 8, (x, y) => !(Math.Abs(x - 1.5f) < 0.6f));
            wall.RaisedAt = 1d;
            Assert.Equal(1f, Math.Abs(wall.DirX), "across the caster's line to the point");
            Assert.Equal(7, wall.Sections.Count, "no section where a creature stands");
            WallSection hit = WallRules.Hit(wall, -3.5f, 5f, 30, out int dealt);
            Assert.Equal(22, dealt, "hardness 8 once");
            Assert.Equal(38, hit.HitPoints, "60 hit points a section");
            Assert.Equal(true, hit.X < 0f, "the section nearest the attacker");
            Assert.Equal(2, WallRules.Disintegrate(wall, 0f, 4f).Count, "disintegrate takes a 10-foot cube");
            Assert.Equal(true, WallRules.Standing(wall), "the rest still stands");
            foreach (WallSection s in wall.Sections)
            {
                s.HitPoints = 0;
            }
            Assert.Equal(false, WallRules.Standing(wall), "all broken: no wall");
        }

        public static void TestOnlyWhatIsLeftInASlotIsChosen()
        {
            KruneView v = Full();
            v.HasForesight = v.HasSpellTurning = v.HasStoneskin = v.HasGlobe = true;
            v.SummonsAlive = 3;
            v.WallStanding = true;
            v.BestCluster = 3;
            v.Ready["Horrid Wilting"] = 0;
            Assert.Equal("Empowered Cloudkill", KruneTactics.Standard(v).Key, "wilting spent: the empowered cloud");
            v.CloudActive = true;
            v.Ready.Keys.ToList().ForEach(k => v.Ready[k] = 0);
            Assert.Equal(null, KruneTactics.Standard(v), "nothing left: the native fallback acts");
            Assert.Equal(null, KruneTactics.Swift(v), "no quickened spell left");
        }

        public static void TestWhenOverwhelmedHeWithdrawsWithinTheSanctumAndReturnsRecovered()
        {
            KruneView v = Full();
            v.HitPointFraction = 0.3d;
            v.AdjacentFoes = 2;
            Assert.Equal(true, KruneTactics.Overwhelmed(v), "low and pressed");
            Assert.Equal("Phase Door", KruneTactics.Standard(v).Spell, "phase door first");
            Assert.Equal(KruneAim.Self, KruneTactics.Standard(v).Aim, "the passages first; the director then steps through them");
            v.HasPhaseDoor = true;
            Assert.Equal(true, KruneTactics.Standard(v).Spell != "Phase Door", "an open phase door is not cast again");
            Assert.Equal(true, KruneTactics.CanRelocate(v), "an open passage is a way out");
            v.HasPhaseDoor = false;
            Assert.Equal(KrunePhase.Recover, KruneTactics.NextPhase(v), "withdrawn");
            v.Phase = KrunePhase.Recover;
            v.HitPointFraction = 1d;
            v.AdjacentFoes = 3;
            Assert.Equal(KrunePhase.Recover, KruneTactics.NextPhase(v), "still pressed at full health: he keeps withdrawing rather than flickering back");
            v.HitPointFraction = 0.3d;
            v.AdjacentFoes = 0;
            v.HasForesight = v.HasSpellTurning = v.HasStoneskin = true;
            KruneChoice recover = KruneTactics.Standard(v);
            Assert.Equal(KruneAim.SummonPoint, recover.Aim, "a small army while he recovers");
            v.HitPointFraction = 0.7d;
            Assert.Equal(KrunePhase.Fight, KruneTactics.NextPhase(v), "recovered: back to the attack");
            v.Ready["Phase Door"] = 0;
            v.Ready["Quickened Dimension Door"] = 0;
            v.Ready["Dimension Door"] = 0;
            v.Ready["Greater Teleport"] = 0;
            v.Phase = KrunePhase.Fight;
            v.HitPointFraction = 0.2d;
            v.AdjacentFoes = 1;
            Assert.Equal(KrunePhase.Fight, KruneTactics.NextPhase(v), "no way out left: he fights to the death");
        }
    }
}
