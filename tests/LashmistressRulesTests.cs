using System.Linq;
using KingmakerWakingRune.Sanctum.Lashmistress;
using KingmakerWakingRune.Sanctum.Wards;

namespace KingmakerWakingRune.Tests
{
    internal static class LashmistressRulesTests
    {
        public static void TestTheAdvancedTemplateIsGivenToExactlyThreeDefenders()
        {
            Assert.Equal(3, DefenderKeys.Advanced.Length, "three defenders");
            Assert.Equal(false, DefenderKeys.IsAdvanced(DefenderKeys.Vandiana), "never Vandiana");
            Assert.Equal(true, DefenderKeys.Advanced.Contains(DefenderKeys.Agarik) && DefenderKeys.Advanced.Contains(DefenderKeys.Jorgas) && DefenderKeys.Advanced.Contains(DefenderKeys.Naga), "Agarik, Jorgas and the naga");
            Assert.Equal(4, DefenderKeys.All.Distinct().Count(), "four identities: Jargas is Jorgas, not a fifth");
        }

        public static void TestTheTemplateRebuildsFromUnbuffedScores()
        {
            // The Blackstrike's printed Str 22 carries Bull's Strength; the rebuild starts from 18, then the potion adds +4.
            Abilities blackstrike = LashmistressProfile.BlackstrikeBase.Advanced();
            Assert.Equal(22, blackstrike.Str, "18 + 4 template");
            Assert.Equal(26, blackstrike.Str + 4, "and Bull's Strength on top, counted once");
            Assert.Equal(18, blackstrike.Con, "Con 14 + 4");
            Assert.Equal(104, LashmistressProfile.AdvancedHitPoints(LashmistressProfile.BlackstrikeBaseHitPoints, LashmistressProfile.BlackstrikeHitDice), "86 + 2 x 9");
            Abilities naga = LashmistressProfile.NagaBase.Advanced();
            Assert.Equal(25, naga.Dex, "21 + 4");
            Assert.Equal(105, LashmistressProfile.AdvancedHitPoints(LashmistressProfile.NagaBaseHitPoints, LashmistressProfile.NagaHitDice), "85 + 2 x 10");
            Assert.Equal(21, LashmistressProfile.AdvancedConDC(LashmistressProfile.NagaPoisonBaseDC, LashmistressProfile.NagaBase.Con), "poison DC 19 follows Con 22");
            Assert.Equal(2, new Abilities(10, 10, 10, 2, 10, 10).Advanced().Int, "an Int of 2 or less is not raised");
        }

        public static void TestTheNagaCastsFromItsOwnSlots()
        {
            // Printed 7/7/5 with Cha 17; the template's Cha 21 adds a first-level slot only.
            Assert.Equal(7, LashmistressProfile.NagaSlots(1, false), "1st printed");
            Assert.Equal(7, LashmistressProfile.NagaSlots(2, false), "2nd printed");
            Assert.Equal(5, LashmistressProfile.NagaSlots(3, false), "3rd printed");
            Assert.Equal(8, LashmistressProfile.NagaSlots(1, true), "1st advanced");
            Assert.Equal(7, LashmistressProfile.NagaSlots(2, true), "2nd advanced");
            Assert.Equal(5, LashmistressProfile.NagaSlots(3, true), "3rd advanced");
        }

        public static void TestVandianaKeepsTwoProgressionsAndHerPrintedLists()
        {
            Assert.Equal(8, LashmistressProfile.VandianaDivineCasterLevel, "cleric 3 + theurge 5");
            Assert.Equal(9, LashmistressProfile.VandianaArcaneCasterLevel, "wizard 4 + theurge 5");
            // Cleric: Wis 18 bonus spells, CL 8 table, plus one domain entry a level.
            Assert.Equal(4, VandianaSpells.Count(VandianaSpells.Cleric, 4), "4th: three + domain");
            Assert.Equal(5, VandianaSpells.Count(VandianaSpells.Cleric, 3), "3rd: four + domain");
            Assert.Equal(5, VandianaSpells.Count(VandianaSpells.Cleric, 2), "2nd: four + domain");
            Assert.Equal(6, VandianaSpells.Count(VandianaSpells.Cleric, 1), "1st: five + domain");
            Assert.Equal(1, VandianaSpells.Cleric.Count(e => e.Domain && e.Level == 4), "one domain entry at 4th");
            // Conjurer: Int 17 (headband included), CL 9, one school slot a level.
            Assert.Equal(2, VandianaSpells.Count(VandianaSpells.Wizard, 5), "5th");
            Assert.Equal(3, VandianaSpells.Count(VandianaSpells.Wizard, 4), "4th: Int 17 gives no 4th-level bonus");
            Assert.Equal(5, VandianaSpells.Count(VandianaSpells.Wizard, 3), "3rd");
            Assert.Equal(6, VandianaSpells.Count(VandianaSpells.Wizard, 2), "2nd");
            Assert.Equal(6, VandianaSpells.Count(VandianaSpells.Wizard, 1), "1st, chill touch and grease twice");
            Assert.Equal(1, LashmistressProfile.BonusSpells(17, 3), "Int 17 gives a 3rd-level bonus spell");
            Assert.Equal(0, LashmistressProfile.BonusSpells(17, 4), "but none at 4th");
            Assert.Equal(1, LashmistressProfile.BonusSpells(18, 4), "Wis 18 gives one at 4th");
            Assert.Equal(5, VandianaSpells.CastBeforeArrival.Length, "false life, mage armor, hound, alarm, glyph");
            foreach (string spent in VandianaSpells.CastBeforeArrival)
            {
                Assert.Equal(1, VandianaSpells.Cleric.Concat(VandianaSpells.Wizard).Count(e => e.Source == spent), spent + " is one prepared entry");
            }
        }

        public static void TestPreparationIsRequestedOnceAndStaysSpent()
        {
            var state = new LashmistressState { Policy = LashmistressPolicy.Active };
            Assert.Equal(1, LashmistressRules.OnAwareness(state, darkvisionPrepared: true, now: 10d).Count(), "darkvision only: the potions wait for the battle");
            Assert.Equal(0, LashmistressRules.OnAwareness(state, true, 20d).Count(), "a second cause prepares nothing again");
            string[] door = LashmistressRules.OnDoorHeard(state, 30d).ToArray();
            Assert.Equal(5, door.Length, "cat's grace, the two invisibilities and the two Bull's Strength potions");
            Assert.Equal(0, LashmistressRules.OnDoorHeard(state, 40d).Count(), "opening the door again refills nothing");
            Assert.Equal(Prep.NagaInvisibilitySelf, LashmistressRules.NextFor(state, DefenderKeys.Naga), "itself first");
            LashmistressRules.Issued(state, Prep.NagaInvisibilitySelf, 31d);
            LashmistressRules.Performed(state, Prep.NagaInvisibilitySelf, "slot spent", 33d);
            Assert.Equal(Prep.NagaInvisibilityAgarik, LashmistressRules.NextFor(state, DefenderKeys.Naga), "then Agarik");

            Assert.Equal(false, state.Preparations.ContainsKey("NagaInvisibilityJorgas"), "no free invisibility for Jorgas");
            var json = new System.Web.Script.Serialization.JavaScriptSerializer();
            var loaded = json.Deserialize<LashmistressState>(json.Serialize(state));
            Assert.Equal(true, loaded.Preparation(Prep.NagaInvisibilitySelf).Performed, "performed survives a reload");
            Assert.Equal(0, LashmistressRules.OnDoorHeard(loaded, 50d).Count(), "and a reload re-requests nothing");
            LashmistressRules.Abandon(loaded, Prep.NagaInvisibilityAgarik, "the naga is dead");
            Assert.Equal(null, LashmistressRules.NextFor(loaded, DefenderKeys.Naga), "an abandoned step is not retried");
        }

        public static void TestCombatAddsBlurOnlyOnce()
        {
            var state = new LashmistressState { Policy = LashmistressPolicy.Active };
            Assert.Equal(4, LashmistressRules.OnCombat(state, 5d).Count(), "bull's strength and blur for each");
            Assert.Equal(0, LashmistressRules.OnCombat(state, 6d).Count(), "once");
            Assert.Equal(Prep.AgarikBullsStrength, LashmistressRules.NextFor(state, DefenderKeys.Agarik), "strength before blur");
        }

        public static void TestScoutingNeverStartsTheCountdown()
        {
            var state = new LashmistressState { Policy = LashmistressPolicy.Active };
            var ritual = new RitualState();
            // Hearing the door prepares the defenders; it is not an engagement.
            LashmistressRules.OnDoorHeard(state, 10d).ToList();
            RitualRules.HearDoor(ritual, 10d);
            Assert.Equal(false, state.Engaged, "the door alone");
            Assert.Equal(false, ritual.ConfrontationStarted, "no clock");
            Assert.Equal(false, LashmistressRules.Engage(state, EngagementCause.None, "a cancelled cursor", 11d), "no cause, no engagement");
            Assert.Equal(true, LashmistressRules.Engage(state, EngagementCause.HostileAct, "an arrow at Agarik", 12d), "a real attack engages");
            Assert.Equal(false, LashmistressRules.Engage(state, EngagementCause.CombatJoined, "a second defender joins", 13d), "once");
            Assert.Equal(EngagementCause.HostileAct, state.EngagedBy, "the first cause is kept");
            var feeder = new FeederState();
            Assert.Equal(true, RitualRules.CommitConfrontation(ritual, feeder, 9, 12d), "the printed clock commits once");
            double deadline = ritual.ConfrontationDeadline;
            Assert.Equal(false, RitualRules.CommitConfrontation(ritual, feeder, 7, 20d), "never rerolled on re-entry or reload");
            Assert.Equal(deadline, ritual.ConfrontationDeadline, "deadline unchanged");
        }

        public static void TestTheEarlierDeadlineRulesAndKillingVandianaCancelsNothing()
        {
            var ritual = new RitualState();
            RitualRules.DefeatKurshu(ritual, 0d);
            RitualRules.CommitConfrontation(ritual, new FeederState(), 7, 100d);
            var state = new LashmistressState { Policy = LashmistressPolicy.Active };
            LashmistressRules.Slain(state, DefenderKeys.Vandiana, 110d);
            Assert.Equal(false, RitualRules.Tick(ritual, 110d), "her death stops no clock");
            Assert.Equal(true, RitualRules.Tick(ritual, 100d + 7 * 6d), "the countdown comes due");
            Assert.Equal(AwakeningReason.ConfrontationDeadline, ritual.Reason, "the earlier clock");
            Assert.Equal(false, RitualRules.Tick(ritual, 99999d), "AwakeningDue once");
        }

        public static void TestTheLastDefenderIsCountedOnce()
        {
            var state = new LashmistressState { Policy = LashmistressPolicy.Active };
            Assert.Equal(false, LashmistressRules.Slain(state, DefenderKeys.Naga, 1d), "one of four");
            Assert.Equal(false, LashmistressRules.Slain(state, DefenderKeys.Agarik, 2d), "two");
            Assert.Equal(false, LashmistressRules.Slain(state, DefenderKeys.Agarik, 3d), "a repeat counts nothing");
            Assert.Equal(false, LashmistressRules.Slain(state, DefenderKeys.Jorgas, 4d), "three");
            Assert.Equal(true, LashmistressRules.Slain(state, DefenderKeys.Vandiana, 5d), "the last");
            Assert.Equal(false, LashmistressRules.Slain(state, DefenderKeys.Vandiana, 6d), "once");
            Assert.Equal(2d, state.Defender(DefenderKeys.Agarik).SlainAt, "the first death time is kept");
        }

        public static void TestAnOlderSaveIsUpgradedOnlyWhenEverythingSaysItIsSafe()
        {
            var safe = new UpgradeFacts { KurshuEncounterActive = true, PartyOutsideAtrium = true };
            var state = new LashmistressState();
            LashmistressRules.MarkForUpgradeCheck(state);
            Assert.Equal(true, LashmistressRules.DecideUpgrade(state, safe), "safe");
            Assert.Equal(LashmistressPolicy.Upgraded, state.Policy, "upgraded");
            foreach (var unsafeFacts in new[]
            {
                new UpgradeFacts { KurshuEncounterActive = true, PartyOutsideAtrium = false },
                new UpgradeFacts { KurshuEncounterActive = true, PartyOutsideAtrium = true, A4DoorEverOpened = true },
                new UpgradeFacts { KurshuEncounterActive = true, PartyOutsideAtrium = true, HoundEverNoticed = true },
                new UpgradeFacts { KurshuEncounterActive = true, PartyOutsideAtrium = true, ConfrontationStarted = true },
                new UpgradeFacts { KurshuEncounterActive = true, PartyOutsideAtrium = true, EmeraldDestroyed = true },
                new UpgradeFacts { KurshuEncounterActive = false, PartyOutsideAtrium = true },
            })
            {
                var older = new LashmistressState();
                LashmistressRules.MarkForUpgradeCheck(older);
                Assert.Equal(false, LashmistressRules.DecideUpgrade(older, unsafeFacts), "unsafe: " + older.UpgradeReason);
                Assert.Equal(LashmistressPolicy.LegacyNotInserted, older.Policy, "kept as it was");
                Assert.Equal(false, older.AllDefeated, "never falsely defeated");
            }
            var fresh = new LashmistressState();
            LashmistressRules.DecideOnInitialize(fresh, legacyEntered: false);
            LashmistressRules.MarkForUpgradeCheck(fresh);
            Assert.Equal(LashmistressPolicy.Active, fresh.Policy, "a fresh arrival is not re-checked");
        }

        public static void TestAlignedBurstsHarmByAlignmentNotEveryone()
        {
            Assert.Equal(4, AlignedBurst.Dice(8), "CL 8: 4d8");
            Assert.Equal(5, AlignedBurst.Dice(14), "at most 5d8");
            Assert.Equal(BlightShare.Full, AlignedBurst.OrdersWrath(lawful: false, chaotic: true), "chaotic: full");
            Assert.Equal(BlightShare.Half, AlignedBurst.OrdersWrath(false, false), "neutral on the axis: half");
            Assert.Equal(BlightShare.Unaffected, AlignedBurst.OrdersWrath(true, false), "lawful: nothing");
            Assert.Equal(BlightShare.Full, AlignedBurst.UnholyBlight(good: true, evil: false), "good: full");
            Assert.Equal(BlightShare.Unaffected, AlignedBurst.UnholyBlight(false, true), "evil: nothing (Vandiana's allies)");
            Assert.Equal(20, AlignedBurst.Damage(20, BlightShare.Full, saved: false), "full");
            Assert.Equal(10, AlignedBurst.Damage(20, BlightShare.Full, saved: true), "saved: half");
            Assert.Equal(10, AlignedBurst.Damage(20, BlightShare.Half, saved: false), "neutral: half");
            Assert.Equal(5, AlignedBurst.Damage(20, BlightShare.Half, saved: true), "neutral, saved: quarter");
            Assert.Equal(true, AlignedBurst.Rider(BlightShare.Full, saved: false), "daze or sicken on a failed save");
            Assert.Equal(false, AlignedBurst.Rider(BlightShare.Half, false), "never on a neutral");
            Assert.Equal(false, AlignedBurst.Rider(BlightShare.Full, true), "never on a save");
        }

        public static void TestHerTacticsKeepAreasOffHerAlliesAndChannelOffTheLiving()
        {
            var crowd = new A4Target { EnemiesWithin10Feet = 3, EnemiesWithin20Feet = 3, Martial = true, Humanoid = true };
            Assert.Equal(true, LashmistressTactics.Target(A4Aim.Confusion, crowd) > 0f, "a cluster of intruders");
            crowd.AlliesWithin10Feet = 1;
            Assert.Equal(0f, LashmistressTactics.Target(A4Aim.Confusion, crowd), "not on top of an ally");
            Assert.Equal(0f, LashmistressTactics.Actor(A4Aim.ChannelNegative, new A4Self { VisibleEnemies = 3, EnemiesWithin30Feet = 3, LivingAlliesWithin30Feet = 1 }), "no Selective Channel: no channel near a living ally");
            Assert.Equal(1f, LashmistressTactics.Actor(A4Aim.ChannelNegative, new A4Self { VisibleEnemies = 3, EnemiesWithin30Feet = 3 }), "alone among intruders she channels");
            Assert.Equal(0f, LashmistressTactics.Actor(A4Aim.Darkness, new A4Self { VisibleEnemies = 2 }), "darkness only when her allies see in it");
            Assert.Equal(0f, LashmistressTactics.Target(A4Aim.HoldPerson, new A4Target { Humanoid = false }), "hold person needs a humanoid");
            Assert.Equal(0f, LashmistressTactics.Target(A4Aim.Haste, new A4Target { IsAlly = true, Hasted = true, AlliesNotHastedWithin15Feet = 2 }), "no second haste on the hasted");
            Assert.Equal(0f, LashmistressTactics.Actor(A4Aim.Melee, new A4Self { VisibleEnemies = 0 }), "nothing without a visible enemy");
        }

        private static LashmistressState Requested(string key)
        {
            var state = new LashmistressState { Policy = LashmistressPolicy.Active };
            LashmistressRules.Request(state, key, "combat", 0d);
            return state;
        }

        public static void TestAPreparationIsSettledWhoeverPerformsIt()
        {
            // Combat requests Blur; the Blackstrike's own brain drinks it. No director command was ever issued.
            LashmistressState state = Requested(Prep.AgarikBlur);
            var seen = new PrepObservation { CanStillHappen = true, ResourceLeft = 1 };
            Assert.Equal(PrepVerdict.None, LashmistressRules.Reconcile(state, Prep.AgarikBlur, seen, false, 1d, 6d), "nothing yet");
            Assert.Equal(1, state.Preparation(Prep.AgarikBlur).ResourceAtRequest, "one potion when first seen");
            seen.ResourceLeft = 0;
            seen.EffectSeen = true;
            Assert.Equal(PrepVerdict.Performed, LashmistressRules.Reconcile(state, Prep.AgarikBlur, seen, false, 7d, 6d), "the brain's potion settles it");
            PrepRecord blur = state.Preparation(Prep.AgarikBlur);
            Assert.Equal(false, blur.Issued, "never issued by the director");
            Assert.Equal(true, blur.PerformedBy.StartsWith("its own action"), "performed by its own action: " + blur.PerformedBy);
            Assert.Equal(0, blur.ResourceAtResolution, "the potion was spent");
            Assert.Equal(null, LashmistressRules.NextFor(state, DefenderKeys.Agarik), "nothing is owed any more");
            // The director's own command, when it succeeded, is credited to the director.
            LashmistressState door = Requested(Prep.AgarikBullsStrength);
            LashmistressRules.Issued(door, Prep.AgarikBullsStrength, 1d);
            LashmistressRules.Reconcile(door, Prep.AgarikBullsStrength, new PrepObservation { CanStillHappen = true, ResourceLeft = 0, EffectSeen = true }, true, 4d, 6d);
            Assert.Equal("the director's command", door.Preparation(Prep.AgarikBullsStrength).PerformedBy, "director credited");
            // Already in effect, nothing spent.
            LashmistressState already = Requested(Prep.VandianaDarkvision);
            LashmistressRules.Reconcile(already, Prep.VandianaDarkvision, new PrepObservation { CanStillHappen = true, ResourceLeft = 1, EffectSeen = true }, false, 1d, 6d);
            Assert.Equal(true, already.Preparation(Prep.VandianaDarkvision).PerformedBy.StartsWith("no one"), "nothing spent");
        }

        public static void TestAnUnavailableOrInterruptedPreparationEndsAndGatesNothing()
        {
            // The potion is gone with no effect (drunk and dispelled, or taken): after a full round it is abandoned for good.
            LashmistressState state = Requested(Prep.JorgasBlur);
            var gone = new PrepObservation { CanStillHappen = true, ResourceLeft = 0 };
            Assert.Equal(PrepVerdict.None, LashmistressRules.Reconcile(state, Prep.JorgasBlur, gone, false, 10d, 6d), "a potion being drunk is given its round");
            Assert.Equal(PrepVerdict.None, LashmistressRules.Reconcile(state, Prep.JorgasBlur, gone, false, 14d, 6d), "still inside the round");
            Assert.Equal(PrepVerdict.Abandoned, LashmistressRules.Reconcile(state, Prep.JorgasBlur, gone, false, 16d, 6d), "a round with nothing: abandoned");
            Assert.Equal(null, LashmistressRules.NextFor(state, DefenderKeys.Jorgas), "it is not owed forever");
            // The director's command still running holds the clock (the potion leaves the stack before the effect lands).
            LashmistressState running = Requested(Prep.JorgasBlur);
            var drinking = new PrepObservation { CanStillHappen = true, ResourceLeft = 0, DirectorCommandRunning = true };
            LashmistressRules.Reconcile(running, Prep.JorgasBlur, drinking, false, 10d, 6d);
            Assert.Equal(PrepVerdict.None, LashmistressRules.Reconcile(running, Prep.JorgasBlur, drinking, false, 30d, 6d), "not while its own command runs");
            // An actor or subject that died ends it at once.
            LashmistressState dead = Requested(Prep.NagaInvisibilityAgarik);
            Assert.Equal(PrepVerdict.Abandoned, LashmistressRules.Reconcile(dead, Prep.NagaInvisibilityAgarik, new PrepObservation { CanStillHappen = false, CannotReason = "Agarik is dead", ResourceLeft = 3 }, false, 1d, 6d), "its subject died");
            Assert.Equal("Agarik is dead", dead.Preparation(Prep.NagaInvisibilityAgarik).Outcome, "with its reason");
            // A resolved preparation is never reopened.
            Assert.Equal(PrepVerdict.None, LashmistressRules.Reconcile(dead, Prep.NagaInvisibilityAgarik, new PrepObservation { CanStillHappen = true, EffectSeen = true }, false, 2d, 6d), "settled once");
        }

        public static void TestTheEncounterYieldsToCharmDominationConfusionAndFear()
        {
            var own = new ControlFacts { Alive = true, OriginalFaction = true, EncounterGroup = true };
            Assert.Equal(null, ControlOwnership.Yield(own), "its own");
            var dominated = own;
            dominated.OriginalFaction = false;
            dominated.PlayerFaction = true;
            dominated.EncounterGroup = false;
            Assert.Equal(true, ControlOwnership.Yield(dominated).Contains("allegiance"), "domination");
            var charmed = own;
            charmed.OriginalFaction = false;
            Assert.Equal(true, ControlOwnership.Yield(charmed) != null, "charm to neutral");
            var confused = own;
            confused.Confused = true;
            Assert.Equal("confused", ControlOwnership.Yield(confused), "confusion");
            var afraid = own;
            afraid.Frightened = true;
            Assert.Equal("frightened", ControlOwnership.Yield(afraid), "fear");
            var record = new DefenderRecord { Key = DefenderKeys.Agarik };
            Assert.Equal(ControlChange.Yielded, ControlOwnership.Observe(record, ControlOwnership.Yield(dominated), 10d), "yielded once");
            Assert.Equal(ControlChange.None, ControlOwnership.Observe(record, ControlOwnership.Yield(dominated), 11d), "not again while it lasts");
            Assert.Equal(ControlChange.Resumed, ControlOwnership.Observe(record, null, 40d), "resumed when the effect ends");
            Assert.Equal(1, record.Yields, "counted");
            Assert.Equal(ControlChange.None, ControlOwnership.Observe(record, "dead", 50d), "death is not a change of control");
        }

        public static void TestDarknessKeepsEveryNativeExemptionAndYieldsToTrueSeeing()
        {
            var plain = new DarknessFacts { TargetInDarkness = true };
            Assert.Equal(true, DarknessRule.AddsConcealment(true, plain), "an ordinary attacker: 20%");
            Assert.Equal(false, DarknessRule.AddsConcealment(false, plain), "a native concealment already applies: nothing added");
            Assert.Equal(false, DarknessRule.AddsConcealment(true, new DarknessFacts()), "outside the darkness");
            foreach (var exempt in new[]
            {
                new DarknessFacts { TargetInDarkness = true, AttackerTrueSeeing = true },
                new DarknessFacts { TargetInDarkness = true, AttackerDarkvisionWithin60 = true },
                new DarknessFacts { TargetInDarkness = true, AttackerBlindsightReaches = true },
                new DarknessFacts { TargetInDarkness = true, AttackerIgnoresAllConcealment = true },
                new DarknessFacts { TargetInDarkness = true, AttackerIgnoresPartial = true },
                new DarknessFacts { TargetInDarkness = true, TargetCannotBenefit = true },
            })
            {
                Assert.Equal(false, DarknessRule.AddsConcealment(true, exempt), DarknessRule.Why(true, exempt));
            }
            Assert.Equal("attacker has true seeing", DarknessRule.Why(true, new DarknessFacts { TargetInDarkness = true, AttackerTrueSeeing = true }), "true seeing pierces magical darkness");
        }

        public static void TestTheBurstsHitOutsidersByTheirOwnRule()
        {
            AlignedBurst.Roll(8, BlightShare.Full, outsider: false, out int count, out int sides);
            Assert.Equal("4d8", $"{count}d{sides}", "an ordinary chaotic or good creature at CL 8");
            AlignedBurst.Roll(8, BlightShare.Full, outsider: true, out count, out sides);
            Assert.Equal("8d6", $"{count}d{sides}", "a qualifying outsider at CL 8: 1d6 per level");
            AlignedBurst.Roll(14, BlightShare.Full, true, out count, out sides);
            Assert.Equal("10d6", $"{count}d{sides}", "at most 10d6");
            AlignedBurst.Roll(14, BlightShare.Full, false, out count, out sides);
            Assert.Equal("5d8", $"{count}d{sides}", "at most 5d8");
            AlignedBurst.Roll(8, BlightShare.Half, true, out count, out sides);
            Assert.Equal("4d8", $"{count}d{sides}", "a neutral outsider takes half of the ordinary roll");
        }

        public static void TestTheWhipIsNonlethalAndStoppedByArmor()
        {
            Assert.Equal(true, Nonlethal.WhipBlocked(armorBonus: 1, naturalArmorBonus: 0), "armor bonus +1");
            Assert.Equal(true, Nonlethal.WhipBlocked(0, 3), "natural armor +3");
            Assert.Equal(false, Nonlethal.WhipBlocked(0, 2), "natural armor +2 is not enough");
            Nonlethal.Apply(0, 3, maxHitPoints: 50, lethalDamage: 10, canFallUnconscious: true, out int after, out int overflow);
            Assert.Equal(3, after, "nonlethal accumulates");
            Assert.Equal(0, overflow, "no lethal damage");
            Nonlethal.Apply(49, 4, 50, 0, true, out after, out overflow);
            Assert.Equal(50, after, "capped at maximum hit points");
            Assert.Equal(3, overflow, "the rest is lethal");
            Nonlethal.Apply(30, 30, 50, 10, false, out after, out overflow);
            Assert.Equal(39, after, "a creature this game would kill stops one short");
            Assert.Equal(0, overflow, "and takes no lethal overflow");
            Assert.Equal(2, Nonlethal.Heal(10, 8), "healing heals nonlethal too");
            Assert.Equal(0, Nonlethal.Recover(10, hitDice: 12, hours: 1), "a Hit Die an hour");
            Assert.Equal(49, Nonlethal.AfterCombat(70, 60, 10), "revived after the fight: no longer held down");
            Assert.Equal(5, Nonlethal.AfterCombat(5, 60, 10), "a small total is kept");
        }

        public static void TestSanctuaryBarsEachAttackerByItsOneSave()
        {
            var verdicts = new System.Collections.Generic.Dictionary<string, bool>();
            int rolls = 0;
            Assert.Equal(true, SanctuaryRule.Barred(verdicts, "a", () => { rolls++; return false; }, out bool rolled), "failed: barred");
            Assert.Equal(true, rolled, "rolled on the first attempt");
            Assert.Equal(true, SanctuaryRule.Barred(verdicts, "a", () => { rolls++; return true; }, out rolled), "still barred: one save per casting");
            Assert.Equal(1, rolls, "never rolled again");
            Assert.Equal(false, SanctuaryRule.Barred(verdicts, "b", () => true, out rolled), "passed: unaffected");
            Assert.Equal(false, SanctuaryRule.KnownBarred(verdicts, "c"), "one that never tried is not barred");
            Assert.Equal(true, SanctuaryRule.KnownBarred(verdicts, "a"), "the failed one is");
        }

        public static void TestHerConjurerFeaturesAndBond()
        {
            Assert.Equal(2, ConjurerRules.SummonersCharmRounds(4), "wizard 4: +2 rounds");
            Assert.Equal(1, ConjurerRules.SummonersCharmRounds(1), "at least 1");
            Assert.Equal(9, ConjurerRules.ChillTouchCharges(9), "a touch per caster level");
            PreparedEntry Entry(string source) => VandianaSpells.Wizard.First(e => e.Source == source);
            var slots = new[]
            {
                new System.Collections.Generic.KeyValuePair<PreparedEntry, bool>(Entry("MagesFaithfulHound"), false),
                new System.Collections.Generic.KeyValuePair<PreparedEntry, bool>(Entry("Haste"), false),
                new System.Collections.Generic.KeyValuePair<PreparedEntry, bool>(Entry("ConfusionSpell"), false),
                new System.Collections.Generic.KeyValuePair<PreparedEntry, bool>(Entry("BalefulPolymorph"), true),
            };
            Assert.Equal("ConfusionSpell", ConjurerRules.ArcaneBondChoice(slots), "the highest spent slot she can cast here; the hound was cast before arrival");
            Assert.Equal(null, ConjurerRules.ArcaneBondChoice(slots.Where(s => s.Value || s.Key.Carry == SpellCarry.PreCast)), "nothing spent that she could cast");
        }

        public static void TestHerSanctuaryHoldsBackHerAttacksWhileSheRecovers()
        {
            var warded = new A4Self { VisibleEnemies = 2, HitPointFraction = 0.3d, SanctuaryActive = true, EnemiesAdjacent = 1 };
            Assert.Equal(0f, LashmistressTactics.Actor(A4Aim.Confusion, warded), "no attack while warded and hurt");
            Assert.Equal(1f, LashmistressTactics.Actor(A4Aim.CureSelf, warded), "she heals");
            Assert.Equal(1f, LashmistressTactics.Actor(A4Aim.Sanctuary, new A4Self { VisibleEnemies = 2, HitPointFraction = 0.3d, EnemiesAdjacent = 1 }), "a last resort when pressed");
            Assert.Equal(0f, LashmistressTactics.Actor(A4Aim.Sanctuary, new A4Self { VisibleEnemies = 2, HitPointFraction = 0.8d, EnemiesAdjacent = 1 }), "not when healthy");
            Assert.Equal(1f, LashmistressTactics.Actor(A4Aim.MagicCircle, new A4Self { VisibleEnemies = 2, GoodEnemiesVisible = 1 }), "a circle against good foes");
            Assert.Equal(0f, LashmistressTactics.Actor(A4Aim.MagicCircle, new A4Self { VisibleEnemies = 2 }), "not without a good foe");
            Assert.Equal(1f, LashmistressTactics.Actor(A4Aim.Shield, new A4Self { VisibleEnemies = 1 }), "the naga's Shield is in its repertoire");
            Assert.Equal(true, NagaSpells.Known.Any(e => e.Source == "MageShield" && e.Carry == SpellCarry.Native), "Shield is the game's own spell");
            Assert.Equal(0f, LashmistressTactics.Actor(A4Aim.ChillTouch, new A4Self { VisibleEnemies = 2 }), "no touch without a foe at her side");
            var holding = new A4Self { VisibleEnemies = 2, EnemiesAdjacent = 1, ChillTouchCharged = true, HitPointFraction = 0.9d };
            Assert.Equal(0f, LashmistressTactics.Actor(A4Aim.Command, holding), "holding a touch with a foe at her side, she casts no other spell");
            Assert.Equal(1f, LashmistressTactics.Actor(A4Aim.ChillTouchStrike, holding), "she touches");
            Assert.Equal(1f, LashmistressTactics.Actor(A4Aim.AcidDart, holding), "her school power is not a spell and wastes nothing");
            holding.EnemiesAdjacent = 0;
            Assert.Equal(1f, LashmistressTactics.Actor(A4Aim.Command, holding), "with no foe at her side she may spend her turn otherwise");
        }

        public static void TestAVersionFourSanctumStateWaitsForTheCheck()
        {
            var state = new SanctumState { Version = 4, Initialized = true };
            state.Lashmistress = null;
            Assert.Equal(true, state.Migrate(), "migrated");
            Assert.Equal(LashmistressPolicy.AwaitingUpgradeCheck, state.Lashmistress.Policy, "decided at the load, from the party's place");
            var fresh = new SanctumState();
            fresh.Initialize(0d, legacyEntered: false);
            Assert.Equal(LashmistressPolicy.Active, fresh.Lashmistress.Policy, "a new sanctum gets the encounter");
        }
    }
}
