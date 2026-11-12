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
            Assert.Equal(3, LashmistressRules.OnAwareness(state, darkvisionPrepared: true, now: 10d).Count(), "darkvision and two potions");
            Assert.Equal(0, LashmistressRules.OnAwareness(state, true, 20d).Count(), "a second cause prepares nothing again");
            string[] door = LashmistressRules.OnDoorHeard(state, 30d).ToArray();
            Assert.Equal(3, door.Length, "cat's grace and the two invisibilities; the potions were already requested");
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
