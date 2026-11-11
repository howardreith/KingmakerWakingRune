using System.Linq;
using System.Web.Script.Serialization;
using KingmakerWakingRune.Sanctum.Encounter;
using KingmakerWakingRune.Sanctum.Wards;

namespace KingmakerWakingRune.Tests
{
    /// <summary>Exercises the production WR-05 rule code. The unit, its native rolls and its AI are proven in the running game.</summary>
    public static class KurshuRulesTests
    {
        private const double Round = 6d;

        private static CorpseFacts Outsider(string id, int hitDice)
        {
            return new CorpseFacts { Id = id, Dead = true, Outsider = true, HitDice = hitDice };
        }

        private static KurshuState Fresh()
        {
            var state = new KurshuState();
            KurshuRules.DecideOnInitialize(state, legacyEntered: false);
            return state;
        }

        public static void TestHardModeRemovesWeaknessAndOneNegativeLevelOnly()
        {
            Assert.Equal(30, KurshuProfile.Authored("Armor Class"), "AC: printed 28, weakness's natural armor -2 removed once");
            Assert.Equal(24, KurshuProfile.Authored("Flat-footed AC"), "flat-footed");
            Assert.Equal(17, KurshuProfile.Authored("Touch AC"), "touch unchanged");
            Assert.Equal(13, KurshuProfile.Authored("Natural armor"), "natural armor");
            Assert.Equal(26, KurshuProfile.Authored("Spell resistance"), "SR: printed 24, weakness's -2 removed once");
            Assert.Equal(219, KurshuProfile.Authored("Hit points"), "hp: one negative level's 5, not the lower tier's 50");
            Assert.Equal(18, KurshuProfile.Authored("Fortitude"), "Fortitude");
            Assert.Equal(9, KurshuProfile.Authored("Reflex"), "Reflex");
            Assert.Equal(14, KurshuProfile.Authored("Will"), "Will");
            Assert.Equal(19, KurshuProfile.Authored("Slam attack"), "slam");
            Assert.Equal(14, KurshuProfile.Authored("Tail slap attack"), "tail");
            Assert.Equal(13, KurshuProfile.Authored("Spell-like caster level"), "caster level");
            Assert.Equal(20, KurshuProfile.Authored("Spell-like DC (3rd level)"), "DC is not changed by negative levels");
            Assert.Equal(10, KurshuProfile.Authored("Damage reduction (cold iron and magic)"), "DR unchanged by hard mode");
            Assert.Equal(30, KurshuProfile.Authored("Resist cold, electricity, fire, sonic"), "resistances unchanged by hard mode");
            Assert.Equal(2, KurshuProfile.Authored("Negative levels (divine separation)"), "two negative levels remain, not one or none");
            Assert.Equal(2, KurshuProfile.HardModeNegativeLevels, "the levels the separation fact applies");
            Assert.Equal(10, KurshuProfile.Authored("Initiative"), "initiative kept as printed");
        }

        public static void TestUndrainedBaseIsAuthoredPlusTheTwoLevelsTheSeparationFactTakesAway()
        {
            Assert.Equal(229, KurshuProfile.Undrained("Hit points"), "hp before two levels");
            Assert.Equal(21, KurshuProfile.Undrained("Slam attack"), "slam before two levels");
            Assert.Equal(16, KurshuProfile.Undrained("Tail slap attack"), "tail before two levels");
            Assert.Equal(20, KurshuProfile.Undrained("Fortitude"), "Fortitude before two levels");
            Assert.Equal(15, KurshuProfile.Undrained("Spell-like caster level"), "CL before two levels");
            Assert.Equal(KurshuProfile.UndrainedCasterLevel, KurshuProfile.Undrained("Spell-like caster level"), "one source for the base CL");
            Assert.Equal(30, KurshuProfile.Undrained("Armor Class"), "AC is not lowered by negative levels");
            Assert.Equal(26, KurshuProfile.Undrained("Spell resistance"), "SR is not lowered by negative levels");
        }

        public static void TestFreshArrivalGetsTheFightWithOneLimitedWishAlreadySpent()
        {
            var state = Fresh();
            Assert.Equal(KurshuPolicy.Active, state.Policy, "policy");
            Assert.Equal(2, KurshuRules.Remaining(state, KurshuUses.LimitedWish), "limited wishes left");
            Assert.Equal(3, KurshuRules.Remaining(state, KurshuUses.Fireball), "fireballs");
            Assert.Equal(1, KurshuRules.Remaining(state, KurshuUses.Feed), "feed");
            Assert.Equal(false, KurshuRules.DecideOnInitialize(state, legacyEntered: false), "decided once");
            Assert.Equal(2, KurshuRules.Remaining(state, KurshuUses.LimitedWish), "a second decision spends nothing");
        }

        public static void TestLegacySavesAreNotGivenTheFight()
        {
            var entered = new KurshuState();
            KurshuRules.DecideOnInitialize(entered, legacyEntered: true);
            Assert.Equal(KurshuPolicy.LegacyNotInserted, entered.Policy, "a pre-WR-04 save already inside");

            var wr04 = new SanctumState { Version = 3, Initialized = true, Kurshu = null };
            Assert.Equal(true, wr04.Migrate(), "migrates");
            Assert.Equal(KurshuPolicy.LegacyNotInserted, wr04.Kurshu.Policy, "a WR-04 save inside the sanctum");
            Assert.Equal(false, wr04.Kurshu.Defeated, "not falsely defeated");
            Assert.Equal(-1d, wr04.Ritual.GlobalDeadline, "no clock started by the compatibility choice");
            Assert.Equal(0, wr04.Kurshu.Spent.Count, "nothing spent");

            var fresh = new SanctumState();
            Assert.Equal(false, fresh.Migrate(), "a new state is current");
            fresh.Initialize(100d, legacyEntered: false);
            Assert.Equal(KurshuPolicy.Active, fresh.Kurshu.Policy, "token arrival");
            fresh.Initialize(200d, legacyEntered: true);
            Assert.Equal(KurshuPolicy.Active, fresh.Kurshu.Policy, "a reload never re-decides");
        }

        public static void TestUsesAreFiniteAndARefillIsPutBack()
        {
            var state = Fresh();
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(true, KurshuRules.Spend(state, KurshuUses.Fireball), "fireball " + i);
            }
            Assert.Equal(false, KurshuRules.Spend(state, KurshuUses.Fireball), "a fourth");
            Assert.Equal(0, KurshuRules.Reconcile(state, KurshuUses.Fireball, native: 3), "a refilled native resource is put back to none");
            Assert.Equal(3, KurshuRules.Remaining(state, KurshuUses.HoldPerson), "each ability is tracked on its own");
            Assert.Equal(1, KurshuRules.Reconcile(state, KurshuUses.HoldPerson, native: 1), "a use the game spent is recorded");
            Assert.Equal(1, KurshuRules.Remaining(state, KurshuUses.HoldPerson), "ledger follows");
            Assert.Equal(2, KurshuRules.Reconcile(state, KurshuUses.LimitedWish, native: 3), "the pre-spent wish stays spent");
        }

        public static void TestPowerSurgeBoostsOneSpellThisRoundAndIsNotPermanent()
        {
            var state = Fresh();
            Assert.Equal(0, KurshuRules.SurgeBonus(state, 10d, Round), "no surge");
            Assert.Equal(true, KurshuRules.ArmSurge(state, 10d), "armed");
            Assert.Equal(false, KurshuRules.ArmSurge(state, 11d), "not twice at once");
            Assert.Equal(2, KurshuRules.SurgeBonus(state, 12d, Round), "+2 this round");
            Assert.Equal(true, KurshuRules.ConsumeSurge(state), "the next spell-like ability uses it");
            Assert.Equal(0, KurshuRules.SurgeBonus(state, 12.5d, Round), "the spell after gets nothing");
            Assert.Equal(false, KurshuRules.ConsumeSurge(state), "consumed once");

            Assert.Equal(true, KurshuRules.ArmSurge(state, 20d), "second use");
            Assert.Equal(0, KurshuRules.SurgeBonus(state, 26.5d, Round), "lapsed after its round");
            Assert.Equal(true, KurshuRules.ExpireSurge(state, 26.5d, Round), "expired");
            Assert.Equal(true, KurshuRules.ArmSurge(state, 30d), "third use");
            KurshuRules.ConsumeSurge(state);
            Assert.Equal(false, KurshuRules.ArmSurge(state, 40d), "three per day");
            Assert.Equal(0, KurshuRules.Remaining(state, KurshuUses.PowerSurge), "spent out");
            Assert.Equal(2, state.SurgesConsumed, "consumed");
            Assert.Equal(1, state.SurgesExpired, "expired unused, not refunded");
        }

        public static void TestFeedNeedsARealEligibleCorpseAndHappensOnce()
        {
            var state = Fresh();
            Assert.Equal(FeedVerdict.Summoned, KurshuRules.CanFeed(state, new CorpseFacts { Id = "s", Dead = true, Outsider = true, HitDice = 12, Summoned = true }), "a summon leaves no corpse to eat");
            Assert.Equal(FeedVerdict.NotOutsider, KurshuRules.CanFeed(state, new CorpseFacts { Id = "w", Dead = true, HitDice = 10 }), "an animal");
            Assert.Equal(FeedVerdict.TooFewHitDice, KurshuRules.CanFeed(state, Outsider("small", 7)), "under 8 HD");
            Assert.Equal(FeedVerdict.NotDead, KurshuRules.CanFeed(state, new CorpseFacts { Id = "alive", Outsider = true, HitDice = 10 }), "alive");
            Assert.Equal(FeedVerdict.ExhaustedHusk, KurshuRules.CanFeed(state, new CorpseFacts { Id = "husk", Dead = true, Outsider = true, HitDice = 12, StoneHusk = true }), "an A3 husk");
            Assert.Equal(-1, KurshuRules.Feed(state, Outsider("small", 7), 50d), "an invalid corpse changes nothing");
            Assert.Equal(2, state.SeparationLevels, "levels kept");
            Assert.Equal(1, KurshuRules.Remaining(state, KurshuUses.Feed), "use kept");

            Assert.Equal(FeedVerdict.Eligible, KurshuRules.CanFeed(state, Outsider("deva", 8)), "8 HD outsider");
            Assert.Equal(2, KurshuRules.Feed(state, Outsider("deva", 8), 60d), "removes both separation levels, no more");
            Assert.Equal(0, state.SeparationLevels, "none left");
            Assert.Equal(FeedVerdict.AlreadyFedToday, KurshuRules.CanFeed(state, Outsider("other", 12)), "once per day");
            Assert.Equal(-1, KurshuRules.Feed(state, Outsider("deva", 8), 70d), "the same corpse twice");
            Assert.Equal("deva", state.FedOn, "which corpse");
            Assert.Equal(true, state.ConsumedCorpses.Contains("deva"), "consumed");
        }

        public static void TestFeedRemovesOnlyTheLevelsItsHitDiceAllow()
        {
            var state = Fresh();
            state.SeparationLevels = 2;
            var big = Outsider("big", 9);
            Assert.Equal(2, KurshuRules.Feed(state, big, 1d), "min(levels, HD)");
            var none = Fresh();
            none.SeparationLevels = 0;
            Assert.Equal(FeedVerdict.NothingToRestore, KurshuRules.CanFeed(none, Outsider("x", 10)), "nothing to restore: no gain, no use spent");
        }

        public static void TestControlEffectsAreNotADefeatAndTheClockStartsOnce()
        {
            var state = Fresh();
            var ritual = new RitualState();
            Assert.Equal(0, KurshuRules.ObserveHealth(state, 219, 219, conscious: true, dead: false, now: 1d).Count, "held: conscious, unhurt");
            Assert.Equal(0, KurshuRules.ObserveHealth(state, 219, 219, conscious: false, dead: false, now: 2d).Count, "asleep at full hit points");
            var wound = KurshuRules.ObserveHealth(state, 160, 219, conscious: true, dead: false, now: 3d);
            Assert.Equal(true, wound.SequenceEqual(new[] { HealthEvent.SubstantialWound }), "first substantial wound");
            Assert.Equal(0, KurshuRules.ObserveHealth(state, 100, 219, conscious: true, dead: false, now: 4d).Count, "spoken once");

            var down = KurshuRules.ObserveHealth(state, -5, 219, conscious: false, dead: false, now: 10d);
            Assert.Equal(true, down.SequenceEqual(new[] { HealthEvent.Defeated }), "unconscious under regeneration is a defeat");
            if (down.Contains(HealthEvent.Defeated))
            {
                RitualRules.DefeatKurshu(ritual, 10d);
            }
            double deadline = ritual.GlobalDeadline;
            Assert.Equal(10d + WardProfile.GlobalDeadlineSeconds, deadline, "90 minutes from the defeat");

            Assert.Equal(0, KurshuRules.ObserveHealth(state, 5, 219, conscious: true, dead: false, now: 30d).Count, "regenerated and up again");
            Assert.Equal(0, KurshuRules.ObserveHealth(state, -3, 219, conscious: false, dead: false, now: 40d).Count, "a second fight's defeat is not new");
            Assert.Equal(false, RitualRules.DefeatKurshu(ritual, 40d), "the clock is not restarted");
            Assert.Equal(deadline, ritual.GlobalDeadline, "nor extended");

            var slain = KurshuRules.ObserveHealth(state, -40, 219, conscious: false, dead: true, now: 50d);
            Assert.Equal(true, slain.SequenceEqual(new[] { HealthEvent.Slain }), "permanent death after acid stops regeneration");
            Assert.Equal(0, KurshuRules.ObserveHealth(state, -40, 219, conscious: false, dead: true, now: 51d).Count, "repeated death callbacks");
            Assert.Equal("unconscious, regenerating", state.DefeatedHow, "the first qualifying defeat is the one recorded");
        }

        public static void TestADeathWithoutAnEarlierDefeatIsBoth()
        {
            var state = Fresh();
            var events = KurshuRules.ObserveHealth(state, -30, 219, conscious: false, dead: true, now: 5d);
            Assert.Equal(true, events.SequenceEqual(new[] { HealthEvent.Defeated, HealthEvent.Slain }), "one observation, both events");
        }

        public static void TestStateSurvivesASaveRoundTripWithoutRefreshing()
        {
            var state = Fresh();
            state.EntityId = "kurshu-1";
            KurshuRules.Spend(state, KurshuUses.Fireball);
            KurshuRules.ArmSurge(state, 5d);
            KurshuRules.Feed(state, Outsider("deva", 10), 6d);
            KurshuRules.ObserveHealth(state, -1, 219, conscious: false, dead: false, now: 7d);
            var json = new JavaScriptSerializer();
            KurshuState loaded = json.Deserialize<KurshuState>(json.Serialize(state));
            Assert.Equal("kurshu-1", loaded.EntityId, "same entity, re-bound rather than respawned");
            Assert.Equal(2, KurshuRules.Remaining(loaded, KurshuUses.Fireball), "spent fireball stays spent");
            Assert.Equal(2, KurshuRules.Remaining(loaded, KurshuUses.LimitedWish), "wishes not refilled");
            Assert.Equal(2, KurshuRules.Remaining(loaded, KurshuUses.PowerSurge), "surge use stays spent");
            Assert.Equal(0, KurshuRules.Remaining(loaded, KurshuUses.Feed), "feed stays spent");
            Assert.Equal(0, loaded.SeparationLevels, "levels stay removed");
            Assert.Equal(true, loaded.Defeated, "defeat kept");
            Assert.Equal(0, KurshuRules.ObserveHealth(loaded, -1, 219, conscious: false, dead: false, now: 8d).Count, "no repeat after load");
            Assert.Equal(false, KurshuRules.DecideOnInitialize(loaded, legacyEntered: false), "no re-initialization");
        }

        public static void TestTacticsPreferOutsidersAndUseAreasOnlyWhereTheyPay()
        {
            var outsider = new TacticsTarget { Distance = 10f, Outsider = true, EnemiesWithin20Feet = 1 };
            var human = new TacticsTarget { Distance = 10f, Humanoid = true, EnemiesWithin20Feet = 1 };
            Assert.Equal(true, KurshuTactics.Target(KurshuAim.MagicMissile, outsider) > KurshuTactics.Target(KurshuAim.MagicMissile, human), "outsiders first");
            Assert.Equal(0f, KurshuTactics.Target(KurshuAim.Fireball, human), "no fireball on one");
            var cluster = new TacticsTarget { Distance = 15f, EnemiesWithin20Feet = 3 };
            Assert.Equal(true, KurshuTactics.Target(KurshuAim.Fireball, cluster) > 0f, "a cluster");
            cluster.SelfWithin20Feet = true;
            Assert.Equal(0f, KurshuTactics.Target(KurshuAim.Fireball, cluster), "never on herself");
            Assert.Equal(0f, KurshuTactics.Target(KurshuAim.HoldPerson, outsider), "hold person needs a humanoid");
            human.Held = true;
            Assert.Equal(0f, KurshuTactics.Target(KurshuAim.HoldPerson, human), "not on the held");
            Assert.Equal(0f, KurshuTactics.Actor(KurshuAim.Displacement, new TacticsSelf { Displaced = true, VisibleEnemies = 2 }), "no second displacement");
            Assert.Equal(0f, KurshuTactics.Actor(KurshuAim.MagicMissile, new TacticsSelf { VisibleEnemies = 0 }), "no omniscience: nothing seen, nothing cast");
            Assert.Equal(0f, KurshuTactics.Actor(KurshuAim.PowerSurge, new TacticsSelf { VisibleEnemies = 1, SurgeArmed = true, AnySaveAbilityLeft = true }), "no surge while one is armed");
        }
    }
}
