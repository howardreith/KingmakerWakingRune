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

        public static void TestTheTailHoldsOneCreatureAndFreedomOfMovementDefeatsTheGrab()
        {
            var state = Fresh();
            Assert.Equal(GrabVerdict.FreedomOfMovement, KurshuRules.CanGrab(state, targetNoLarger: true, targetFreeOfMovement: true), "freedom of movement resists automatically");
            Assert.Equal(GrabVerdict.TooLarge, KurshuRules.CanGrab(state, targetNoLarger: false, targetFreeOfMovement: false), "larger than her");
            Assert.Equal(GrabVerdict.Eligible, KurshuRules.CanGrab(state, targetNoLarger: true, targetFreeOfMovement: false), "medium foe");
            Assert.Equal(true, KurshuRules.Hold(state, "brena", "Brena", 10d), "held");
            Assert.Equal(GrabVerdict.AlreadyHolding, KurshuRules.CanGrab(state, targetNoLarger: true, targetFreeOfMovement: false), "never a second hold");
            Assert.Equal(false, KurshuRules.Hold(state, "dorn", "Dorn", 11d), "a second hold is refused");
            Assert.Equal("brena", state.HeldId, "still the first");
            Assert.Equal(HoldChoice.Release, KurshuRules.DecideHold(state, holdStillStands: false, offensiveSpellsLeft: false, reason: out string why), "freed by freedom of movement after the grab");
            Assert.Equal("the hold no longer stands", why, "reason");
            Assert.Equal(string.Empty, state.HeldId, "nothing held");
            Assert.Equal(HoldChoice.None, KurshuRules.DecideHold(state, holdStillStands: true, offensiveSpellsLeft: false, reason: out why), "nothing to decide");
        }

        public static void TestTheHoldCostsAStandardActionEachTurnOrEnds()
        {
            var state = Fresh();
            KurshuRules.Hold(state, "brena", "Brena", 10d);
            Assert.Equal(HoldChoice.Release, KurshuRules.DecideHold(state, holdStillStands: true, offensiveSpellsLeft: true, reason: out string why), "spells left: she lets go to cast");
            Assert.Equal("she lets go to cast", why, "her tactics");

            KurshuRules.Hold(state, "brena", "Brena", 20d);
            Assert.Equal(HoldChoice.Maintain, KurshuRules.DecideHold(state, holdStillStands: true, offensiveSpellsLeft: false, reason: out why), "nothing left to cast: she squeezes");
            Assert.Equal(true, state.MaintainDue, "the standard action is owed");
            KurshuRules.Maintained(state, success: true);
            Assert.Equal(false, state.MaintainDue, "paid");
            Assert.Equal("brena", state.HeldId, "held on a success");

            Assert.Equal(HoldChoice.Maintain, KurshuRules.DecideHold(state, holdStillStands: true, offensiveSpellsLeft: false, reason: out why), "next turn");
            Assert.Equal(HoldChoice.Release, KurshuRules.DecideHold(state, holdStillStands: true, offensiveSpellsLeft: false, reason: out why), "a maintenance never made ends it");
            Assert.Equal("last turn's maintenance was never made", why, "no free hold");

            KurshuRules.Hold(state, "brena", "Brena", 30d);
            KurshuRules.DecideHold(state, holdStillStands: true, offensiveSpellsLeft: false, reason: out why);
            KurshuRules.Maintained(state, success: false);
            Assert.Equal(string.Empty, state.HeldId, "a failed maintenance ends it");
            Assert.Equal(3, state.Grabs, "grabs counted");
            Assert.Equal(true, state.LastRelease.Contains("maintenance check failed"), "recorded");
        }

        public static void TestTheHoldSurvivesASaveRoundTrip()
        {
            var state = Fresh();
            KurshuRules.Hold(state, "brena", "Brena", 10d);
            KurshuRules.DecideHold(state, holdStillStands: true, offensiveSpellsLeft: false, reason: out string _);
            var json = new JavaScriptSerializer();
            KurshuState loaded = json.Deserialize<KurshuState>(json.Serialize(state));
            Assert.Equal("brena", loaded.HeldId, "same victim");
            Assert.Equal(true, loaded.MaintainDue, "the owed action is not forgiven by a load");
            Assert.Equal(GrabVerdict.AlreadyHolding, KurshuRules.CanGrab(loaded, targetNoLarger: true, targetFreeOfMovement: false), "no second hold after a load");
        }

        public static void TestIntelligenceDrainToZeroIsComatoseUntilRestored()
        {
            Assert.Equal(0, MindDrainRules.Excess(4), "score stays above 0");
            Assert.Equal(0, MindDrainRules.Excess(1), "1 is still awake");
            Assert.Equal(1, MindDrainRules.Excess(0), "exactly 0: comatose, one point held back");
            Assert.Equal(4, MindDrainRules.Excess(-3), "past 0: the rest is held back too");

            // Intelligence 12 with 11 drain shows 1; a 4-point slam would leave -3: drain stays 11, 4 held back (true drain 15).
            Assert.Equal(false, MindDrainRules.Restore(11, 11, 4, out int drain, out int pending), "no restoration yet");
            Assert.Equal(4, pending, "nothing changes");

            Assert.Equal(false, MindDrainRules.Restore(11, 8, 4, out drain, out pending), "3 points restored: still at 0");
            Assert.Equal(1, pending, "the held-back points are paid first");
            Assert.Equal(11, drain, "the visible score stays at 1");

            Assert.Equal(true, MindDrainRules.Restore(11, 5, 4, out drain, out pending), "6 points restored: true drain 9");
            Assert.Equal(0, pending, "nothing held back");
            Assert.Equal(9, drain, "Intelligence 12 - 9 = 3");

            Assert.Equal(true, MindDrainRules.Restore(11, 0, 4, out drain, out pending), "restoration restores every drained point of the score");
            Assert.Equal(0, drain, "all of the true drain is gone");
            Assert.Equal(0, pending, "including the held-back points");

            // The bear in play: Intelligence 2, a 5-point slam: visible drain 1, 4 held back; the scroll of restoration clears it.
            Assert.Equal(true, MindDrainRules.Restore(1, 0, 4, out drain, out pending), "restoration wakes the bear");
            Assert.Equal(0, drain, "Intelligence 2 again");
        }

        /// <summary>The production order of one slam: settle, then the native drain adds its points, then the hold-back.</summary>
        private static MindDrainState Slam(MindDrainState state, int score, int points)
        {
            state = MindDrainRules.Settle(state);
            state.Drain += points;
            return MindDrainRules.HoldBack(state, score - state.Drain);
        }

        private static MindDrainState ComatoseAt13()
        {
            // Intelligence 13, a 14-point drain: visible drain 12 (score 1), 2 held back.
            MindDrainState state = Slam(new MindDrainState(), 13, 14);
            Assert.Equal(true, state.Comatose, "comatose");
            Assert.Equal(12, state.Drain, "score shown 1");
            Assert.Equal(2, state.Pending, "2 held back");
            return state;
        }

        public static void TestASlamRightAfterRestorationIsNotErased()
        {
            // Case A: restoration clears the engine's drain, and the next slam lands before any recovery check.
            MindDrainState state = ComatoseAt13();
            state.Drain = 0;
            state = Slam(state, 13, 3);
            Assert.Equal(false, state.Comatose, "awake");
            Assert.Equal(3, state.Drain, "the new slam's 3 points stand: Intelligence 10");
            Assert.Equal(0, state.Pending, "nothing held back");
            MindDrainState later = MindDrainRules.Settle(MindDrainRules.Settle(state));
            Assert.Equal(3, later.Drain, "further recovery checks do not erase it");
            Assert.Equal(false, later.Comatose, "still awake");
        }

        public static void TestASlamOnAStillComatoseVictimIsHeldBack()
        {
            // Case B: no restoration; the new slam joins the held-back points and the score stays at 1.
            MindDrainState state = Slam(ComatoseAt13(), 13, 3);
            Assert.Equal(true, state.Comatose, "still comatose");
            Assert.Equal(12, state.Drain, "score shown 1");
            Assert.Equal(5, state.Pending, "2 + 3 held back");
            Assert.Equal(12, state.DrainWhenHeldBack, "the reference point is unchanged");
            state.Drain = 0;
            state = MindDrainRules.Settle(state);
            Assert.Equal(false, state.Comatose, "restoration restores every drained point, held-back included");
            Assert.Equal(0, state.Drain, "Intelligence 13");
        }

        public static void TestRestorationSettledBeforeTheSlamGivesTheSameResult()
        {
            // Case C: the periodic check runs between restoration and the slam.
            MindDrainState state = ComatoseAt13();
            state.Drain = 0;
            state = MindDrainRules.Settle(state);
            Assert.Equal(false, state.Comatose, "woken by the check");
            state = Slam(state, 13, 3);
            Assert.Equal(3, state.Drain, "as in case A: Intelligence 10, drain 3");
            Assert.Equal(false, state.Comatose, "awake");
        }

        public static void TestAnImmuneComatoseVictimGainsNothing()
        {
            // Native immunity: the drain rule adds no points, so nothing more is held back.
            MindDrainState state = Slam(ComatoseAt13(), 13, 0);
            Assert.Equal(2, state.Pending, "unchanged");
            Assert.Equal(12, state.Drain, "unchanged");
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
