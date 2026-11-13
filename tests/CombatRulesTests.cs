using System.Collections.Generic;
using System.Linq;
using KingmakerWakingRune.Sanctum.Krune;

namespace KingmakerWakingRune.Tests
{
    internal static class CombatRulesTests
    {
        public static void TestSolidFogHalvesEachCreaturesOwnSpeed()
        {
            Assert.Equal(15d, SolidFogRules.SpeedIn(30d, false), "30 ft becomes 15 ft, not a fixed 5 ft");
            Assert.Equal(10d, SolidFogRules.SpeedIn(20d, false), "a slower creature keeps half of its own speed");
            Assert.Equal(30d, SolidFogRules.SpeedIn(60d, false), "a hasted 60 ft becomes 30 ft: the multiplier follows haste");
            Assert.Equal(30d, SolidFogRules.SpeedIn(30d, true), "freedom of movement moves normally");
        }

        public static void TestSolidFogPenalizesMeleeOnly()
        {
            Assert.Equal(-2, SolidFogRules.MeleeModifier(AttackDelivery.Melee, false), "melee attack and damage -2");
            Assert.Equal(0, SolidFogRules.MeleeModifier(AttackDelivery.RangedWeapon, false), "no melee penalty on a ranged attack (it fails instead)");
            Assert.Equal(0, SolidFogRules.MeleeModifier(AttackDelivery.RangedTouch, false), "rays are not penalized");
            Assert.Equal(0, SolidFogRules.MeleeModifier(AttackDelivery.Melee, true), "freedom of movement attacks normally");
        }

        public static void TestSolidFogStopsOrdinaryRangedWeaponsButNotRays()
        {
            Assert.Equal(true, SolidFogRules.RangedAttackFails(AttackDelivery.RangedWeapon, true, false, false), "shooting from inside");
            Assert.Equal(true, SolidFogRules.RangedAttackFails(AttackDelivery.RangedWeapon, false, true, false), "shooting into it");
            Assert.Equal(true, SolidFogRules.RangedAttackFails(AttackDelivery.RangedWeapon, false, false, true), "shooting through it");
            Assert.Equal(false, SolidFogRules.RangedAttackFails(AttackDelivery.RangedWeapon, false, false, false), "clear of the fog");
            Assert.Equal(false, SolidFogRules.RangedAttackFails(AttackDelivery.RangedTouch, true, true, true), "a ray goes through");
            Assert.Equal(false, SolidFogRules.RangedAttackFails(AttackDelivery.Melee, true, true, true), "melee is penalized, not stopped");
            Assert.Equal(true, SolidFogRules.SegmentNear(0f, 0f, 10f, 0f, 5f, 2f, 3f), "a line passing 2 m from the centre of a 3 m fog crosses it");
            Assert.Equal(false, SolidFogRules.SegmentNear(0f, 0f, 10f, 0f, 5f, 4f, 3f), "4 m away it does not");
            Assert.Equal(false, SolidFogRules.SegmentNear(0f, 0f, 10f, 0f, 14f, 0f, 3f), "a fog beyond the target is not crossed");
        }

        public static void TestTentaclesMaintainWithTheRoundsCheckAndPlusFive()
        {
            int cmb = TentacleRules.Cmb(17);
            Assert.Equal(22, cmb, "CL 17 + 4 Strength + 1 size");
            Assert.Equal(TentacleResult.Grappled, TentacleRules.Resolve(10, cmb, 32, false, false), "32 needed: 10 + 22 holds");
            Assert.Equal(TentacleResult.Missed, TentacleRules.Resolve(9, cmb, 32, false, false), "31 misses");
            Assert.Equal(TentacleResult.Maintained, TentacleRules.Resolve(5, cmb, 32, true, false), "held: +5 makes 32");
            Assert.Equal(TentacleResult.Released, TentacleRules.Resolve(4, cmb, 32, true, false), "held: a failed check lets go (no damage)");
            Assert.Equal(TentacleResult.Released, TentacleRules.Resolve(1, cmb, 10, true, false), "a natural 1 fails even when held");
            Assert.Equal(TentacleResult.Grappled, TentacleRules.Resolve(20, cmb, 60, false, false), "a natural 20 succeeds");
            Assert.Equal(TentacleResult.Immune, TentacleRules.Resolve(20, cmb, 0, true, true), "freedom of movement: let go, never crushed");
            Assert.Equal(true, TentacleRules.Damages(TentacleResult.Maintained), "a kept hold crushes");
            Assert.Equal(false, TentacleRules.Damages(TentacleResult.Released), "a broken hold does not");
            Assert.Equal(false, TentacleRules.Damages(TentacleResult.Immune), "freedom of movement takes nothing");
        }

        public static void TestTentaclesRollOncePerRound()
        {
            Assert.Equal(false, TentacleRules.NewRoundRoll(100d, 100d), "the cast's own entry roll is that round's roll");
            Assert.Equal(true, TentacleRules.NewRoundRoll(100d, 106d), "the next round rolls again");
            Assert.Equal(true, TentacleRules.NewRoundRoll(100d, 50d), "time ran backwards (a load): roll afresh");
            Assert.Equal(false, TentacleRules.EntryNeedsRoll(100d, 104d), "a creature entering mid-round meets the round's roll");
            Assert.Equal(true, TentacleRules.EntryNeedsRoll(-1d, 104d), "no roll yet: entering starts the round's roll");
        }

        public static void TestTheRodAppliesUpToThreeLegalPaidFeats()
        {
            var all = new List<string> { "Empower", "Reach", "Extend", "Maximize" };
            var none = new List<string>();
            List<RodOption> a = RodRules.Apply(new[] { "Maximize", "Empower", "Extend", "Reach" }, all, none, 10);
            Assert.Equal("Maximize,Empower,Extend", string.Join(",", a.Select(o => o.Feat).ToArray()), "three at most, in the order asked");
            Assert.Equal(6, a.Sum(o => o.Cost), "3 + 2 + 1 charges");
            a = RodRules.Apply(new[] { "Maximize", "Empower" }, all, none, 4);
            Assert.Equal("Maximize", string.Join(",", a.Select(o => o.Feat).ToArray()), "4 charges: Maximize (3), not Empower (2) as well");
            a = RodRules.Apply(new[] { "Maximize", "Empower" }, all, new List<string> { "Empower" }, 10);
            Assert.Equal("Maximize", string.Join(",", a.Select(o => o.Feat).ToArray()), "an already empowered slot is only maximized");
            a = RodRules.Apply(new[] { "Maximize", "Extend" }, new List<string> { "Extend" }, none, 10);
            Assert.Equal("Extend", string.Join(",", a.Select(o => o.Feat).ToArray()), "a spell that cannot be maximized is not");
            a = RodRules.Apply(new[] { "Empower" }, all, none, 1);
            Assert.Equal(0, a.Count, "not enough charges: nothing");
            Assert.Equal(0, RodRules.Apply(new[] { string.Empty }, all, none, 10).Count, "Silent, Still and Widen have no mechanic here");
            Assert.Equal(7, RodRules.Options.Length, "all seven printed feats are listed");
            Assert.Equal(3, RodRules.Options.Count(o => !o.Supported), "Silent, Still and Widen are listed as unsupported");
        }

        public static void TestKruneAsksTheRodForDamageAndExtendsSummonsSparingly()
        {
            Assert.Equal("Maximize,Empower", string.Join(",", RodRules.KruneRequest(true, false, 10, 8).ToArray()), "horrid wilting");
            Assert.Equal("Maximize,Empower", string.Join(",", RodRules.KruneRequest(true, false, 10, 5).ToArray()), "cloudkill");
            Assert.Equal(0, RodRules.KruneRequest(true, false, 10, 2).Count, "acid arrow is not worth the rod");
            Assert.Equal("Extend", string.Join(",", RodRules.KruneRequest(false, true, 6, 5).ToArray()), "a summons while five charges stay in hand");
            Assert.Equal(0, RodRules.KruneRequest(false, true, 5, 9).Count, "not at the cost of the next potent spell");
            Assert.Equal(0, RodRules.KruneRequest(false, false, 10, 9).Count, "nothing for anything else");
        }

        public static void TestTheRodRenewsOnScheduleAndNeverRefundsASpend()
        {
            const double Day = 86400d;
            // Left full across its renewal: the renewal is taken (the schedule moves on), so spending then sticks.
            double at = 0d;
            int charges = RodRules.Renew(10, ref at, 100000d, Day);
            Assert.Equal(10, charges, "full rod across the boundary stays full");
            Assert.Equal(Day, at, "but its schedule moves on to the last due renewal");
            charges -= 1;
            Assert.Equal(9, RodRules.Renew(charges, ref at, 100000d, Day), "the read right after a spend does not refund it");
            Assert.Equal(9, RodRules.Renew(charges, ref at, 100500d, Day), "nor any later read before the next renewal");
            double restartAt = at;
            Assert.Equal(9, RodRules.Renew(9, ref restartAt, 101000d, Day), "a restart reads the saved 9 and date: still 9");
            charges -= 3;
            Assert.Equal(6, RodRules.Renew(charges, ref at, 100000d, Day), "several charges spent, still kept");
            Assert.Equal(10, RodRules.Renew(charges, ref at, 2 * Day + 1d, Day), "renewed at the next due time");
            Assert.Equal(2 * Day, at, "schedule at the renewal just taken");
            // Partly spent across one or more days: renewed once, the schedule advanced by whole days.
            at = 0d;
            Assert.Equal(10, RodRules.Renew(4, ref at, 3.5 * Day, Day), "a partly spent rod renews");
            Assert.Equal(3 * Day, at, "three whole days passed");
            Assert.Equal(4, RodRules.Renew(4, ref at, 3.9 * Day, Day), "not again within the day");
            // No schedule yet, or a date after now: it starts now, nothing refilled.
            at = -1d;
            Assert.Equal(2, RodRules.Renew(2, ref at, 500d, Day), "first schedule: no refill");
            Assert.Equal(500d, at, "starts now");
            at = 900d;
            Assert.Equal(2, RodRules.Renew(2, ref at, 500d, Day), "a date after now: no refill");
        }

        public static void TestTwoCastersCannotBothDrawTheLastCharges()
        {
            var all = new List<string> { "Empower", "Reach", "Extend", "Maximize" };
            var none = new List<string>();
            int charges = 3;
            List<RodOption> first = RodRules.Apply(new[] { "Maximize" }, all, none, RodRules.Available(charges, 0));
            Assert.Equal(1, first.Count, "the first caster takes Maximize with the last 3");
            List<RodOption> second = RodRules.Apply(new[] { "Maximize" }, all, none, RodRules.Available(charges, first.Sum(o => o.Cost)));
            Assert.Equal(0, second.Count, "the second, while the first is pending, gets nothing (the spell casts without it)");
            List<RodOption> third = RodRules.Apply(new[] { "Extend", "Maximize" }, all, none, RodRules.Available(charges, 2));
            Assert.Equal("Extend", string.Join(",", third.Select(o => o.Feat).ToArray()), "one free charge: only Extend");
            Assert.Equal(0, RodRules.Available(1, 5), "never negative");
        }

        public static void TestTheSpearStrikesFromTheSecondAttempt()
        {
            Assert.Equal(false, SpearEgoRules.StrikesFirst(0), "the first attempt is only the Will save");
            Assert.Equal(true, SpearEgoRules.StrikesFirst(1), "the second attempt is struck first");
            Assert.Equal(true, SpearEgoRules.DisarmSucceeds(10, 30), "telekinesis 10 + 20 against CMD 30 disarms");
            Assert.Equal(false, SpearEgoRules.DisarmSucceeds(9, 30), "29 does not");
            Assert.Equal(false, SpearEgoRules.DisarmSucceeds(1, 5), "a natural 1 fails");
        }

        public static void TestTheDanceEndsInHisHandOrDropsOrFallsWithHim()
        {
            Assert.Equal(DanceEnd.Continue, DanceRules.End(10d, 24d, false, true), "within its 4 rounds");
            Assert.Equal(DanceEnd.Grasped, DanceRules.End(24d, 24d, false, true), "he grasps it as it ends");
            Assert.Equal(DanceEnd.Dropped, DanceRules.End(24d, 24d, false, false), "unable to act: it drops in his space");
            Assert.Equal(DanceEnd.FallsWithHim, DanceRules.End(10d, 24d, true, false), "his death ends it at once");
            Assert.Equal(false, DanceRules.CanLoose(30d, 48d), "grasped: not again for 4 rounds");
            Assert.Equal(true, DanceRules.CanLoose(48d, 48d), "after 4 rounds it may dance again");
            Assert.Equal(true, DanceRules.CanLoose(10d, -1d), "never grasped from a dance");
            Assert.Equal(true, DanceRules.OwnMaster(true, false, false, false, true), "his own master: he may take up his spear");
            Assert.Equal(false, DanceRules.OwnMaster(true, false, false, false, false), "dominated or charmed (allegiance changed): not by his will");
            Assert.Equal(false, DanceRules.OwnMaster(true, true, false, false, true), "confused: the confusion owns his actions");
            Assert.Equal(false, DanceRules.OwnMaster(true, false, true, false, true), "frightened");
            Assert.Equal(false, DanceRules.OwnMaster(true, false, false, true, true), "cowering");
            Assert.Equal(false, DanceRules.OwnMaster(false, false, false, false, true), "unable to act");
        }

        public static void TestHeSuspectsOnlyWhereHeLastSawAFoe()
        {
            Assert.Equal(true, SightRules.Suspected(100d, 105d, false), "vanished 5 s ago");
            Assert.Equal(false, SightRules.Suspected(100d, 113d, false), "forgotten after 12 s");
            Assert.Equal(false, SightRules.Suspected(100d, 105d, true), "a visible foe is seen, not suspected");
            Assert.Equal(false, SightRules.Suspected(-1d, 105d, false), "never seen: nothing to suspect");
        }
    }
}
