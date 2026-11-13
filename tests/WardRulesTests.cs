using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using KingmakerWakingRune.Sanctum.Wards;

namespace KingmakerWakingRune.Tests
{
    /// <summary>Exercises the production WR-04 rule code. Native delivery, targeting and combat results are proven in the running game.</summary>
    public static class WardRulesTests
    {
        private static RuneFocus Life => KruneProjection.Focus("A3a");
        private static RuneFocus Alacrity => KruneProjection.Focus("A3b");
        private static RuneFocus Defense => KruneProjection.Focus("A3c");
        private static RuneFocus Armor => KruneProjection.Focus("A3e");
        private static RuneFocus Control => KruneProjection.Focus("A3f");

        private static RuneDamageComponent[] Hit(RuneDamageType type, int amount)
        {
            return new[] { new RuneDamageComponent(type, amount) };
        }

        public static void TestRuneReadingsTellDiscoveriesWithoutRulesNumbers()
        {
            foreach (RuneKind kind in Enum.GetValues(typeof(RuneKind)))
            {
                foreach (bool disabled in new[] { false, true })
                {
                    string deep = WardText.RuneUnderstanding(kind, disabled);
                    string basic = WardText.RuneIdentity(KruneProjection.Foci.First(f => f.Kind == kind), new RuneFocusState { Disabled = disabled });
                    foreach (string text in new[] { deep, basic })
                    {
                        Assert.Equal(true, !text.Any(char.IsDigit), $"{kind} reading has no numbers: {text}");
                        Assert.Equal(true, text.IndexOf("hardness", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("DC ", StringComparison.Ordinal) < 0 && text.IndexOf("threshold", StringComparison.OrdinalIgnoreCase) < 0, $"{kind} reading has no rules terms");
                    }
                    Assert.Equal(true, deep.StartsWith(disabled ? "This is a spent" : "This is an active", StringComparison.Ordinal), $"{kind} deeper reading opens with its state");
                    Assert.Equal(true, deep.Length > basic.Length, $"{kind} deeper reading says more than the identification");
                }
            }
        }

        public static void TestAllKruneMasksMatchTheReferenceTable()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "krune_mask_expected.json");
            var root = (Dictionary<string, object>)new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.DeserializeObject(File.ReadAllText(path));
            var cases = (object[])root["cases"];
            Assert.Equal(128, cases.Length, "reference cases");
            foreach (Dictionary<string, object> c in cases)
            {
                int mask = (int)c["mask"];
                KruneProjectionResult r = KruneProjection.Project(mask);
                Assert.Equal((int)c["maximumHP"], r.MaximumHP, $"mask {mask} HP");
                Assert.Equal((int)c["lifeFastHealing"], r.LifeFastHealing, $"mask {mask} fast healing");
                Assert.Equal((int)c["runeNaturalArmor"], r.NaturalArmor, $"mask {mask} natural armor");
                Assert.Equal((int)c["runeDodgeAC"], r.DodgeAC, $"mask {mask} dodge");
                Assert.Equal((int)c["runeInitiativeInsight"], r.InitiativeInsight, $"mask {mask} initiative");
                Assert.Equal((int)c["runeDeflectionAC"], r.DeflectionAC, $"mask {mask} deflection");
                Assert.Equal((int)c["runeResistanceSaves"], r.ResistanceSaves, $"mask {mask} resistance");
                Assert.Equal((int)c["spearCompetence"], r.SpearCompetence, $"mask {mask} spear");
                Assert.Equal((bool)c["conjuredCreatureProtection"], r.ConjuredCreatureProtection, $"mask {mask} control protection");
                Assert.Equal((int)c["casterLevel"], r.CasterLevel, $"mask {mask} caster level");
                Assert.Equal(0, ((object[])c["spellsExpended"]).Length, $"mask {mask} spells expended");
            }
        }

        public static void TestProjectionIsRepeatable()
        {
            Assert.Equal(KruneProjection.Project(127).MaximumHP, KruneProjection.Project(127).MaximumHP, "pure function");
            Assert.Equal(233, KruneProjection.Project(127).MaximumHP, "three disabled Life foci");
        }

        public static void TestNegativeEnergyAccumulatesInOneRoundAfterHardnessPerHit()
        {
            var state = new RuneFocusState();
            RuneOutcome first = RuneRules.ApplyDamage(Life, state, 10, "cleric", false, false, Hit(RuneDamageType.NegativeEnergy, 20));
            Assert.Equal(true, first.Pending, "first hit stays open");
            Assert.Equal(0, first.FailedActors.Count, "no curse while the window is open");
            RuneOutcome second = RuneRules.ApplyDamage(Life, state, 12, "wizard", false, false, Hit(RuneDamageType.NegativeEnergy, 23));
            Assert.Equal(true, second.Disabled, "12 + 15 = 27 after hardness on each hit");
            Assert.Equal("NegativeEnergyDamage", state.DisabledRoute, "route");
        }

        public static void TestShortWindowFailsContributorsOnceWhenItCloses()
        {
            var state = new RuneFocusState();
            RuneRules.ApplyDamage(Life, state, 0, "cleric", false, false, Hit(RuneDamageType.NegativeEnergy, 20));
            Assert.Equal(0, RuneRules.Tick(Life, state, 5.9).FailedActors.Count, "still open");
            RuneOutcome closed = RuneRules.Tick(Life, state, 6);
            Assert.Equal("cleric", closed.FailedActors.Single(), "participant");
            Assert.Equal(0, RuneRules.Tick(Life, state, 7).FailedActors.Count, "not again");
        }

        public static void TestTwoNegativeLevelsDisableLifeAndExpiryFails()
        {
            var state = new RuneFocusState();
            Assert.Equal(true, RuneRules.ApplyNegativeLevels(Life, state, 0, "a", 1, 100).Pending, "one level waits");
            Assert.Equal(true, RuneRules.ApplyNegativeLevels(Life, state, 1, "a", 1, 100).Disabled, "two levels");
            var other = new RuneFocusState();
            RuneRules.ApplyNegativeLevels(Life, other, 0, "a", 1, 50);
            Assert.Equal("a", RuneRules.Tick(Life, other, 50).FailedActors.Single(), "expired short");
        }

        public static void TestPoisonNeedsThreeToOneAbility()
        {
            var state = new RuneFocusState();
            RuneRules.ApplyAbilityDamage(Life, state, 0, "spider", "Strength", 2);
            RuneRules.ApplyAbilityDamage(Life, state, 1, "spider", "Dexterity", 2);
            Assert.Equal(false, state.Disabled, "2 + 2 on different scores");
            Assert.Equal(true, RuneRules.ApplyAbilityDamage(Life, state, 2, "spider", "Strength", 1).Disabled, "3 on one score");
        }

        public static void TestDeathSpellLevelThresholdIsHighTier()
        {
            var low = new RuneFocusState();
            RuneOutcome rejected = RuneRules.ApplySpell(Life, low, 0, "w", true, false, 3);
            Assert.Equal(false, low.Disabled, "level 3 death is the low-tier number");
            Assert.Equal("w", rejected.FailedActors.Single(), "wrong cast fails now");
            Assert.Equal(true, RuneRules.ApplySpell(Life, new RuneFocusState(), 0, "w", true, false, 4).Disabled, "level 4");
        }

        public static void TestHarmlessCastFailsOnlyAfterItsRound()
        {
            var state = new RuneFocusState();
            Assert.Equal(true, RuneRules.ApplySpell(Armor, state, 0, "w", false, false, 5).Pending, "awaiting effects");
            Assert.Equal(0, RuneRules.Tick(Armor, state, 3).FailedActors.Count, "inside its round");
            Assert.Equal("w", RuneRules.Tick(Armor, state, 6).FailedActors.Single(), "it did nothing");
        }

        public static void TestStatusRoutesAreContinuous()
        {
            var state = new RuneFocusState();
            RuneRules.ObserveCondition(Alacrity, state, 0, RuneCondition.Paralyzed, true, "w", true);
            Assert.Equal(true, RuneRules.ObserveCondition(Alacrity, state, 12, RuneCondition.Paralyzed, true, "w", true).Pending, "two rounds in");
            RuneOutcome broken = RuneRules.ObserveCondition(Alacrity, state, 18, RuneCondition.Paralyzed, false, "w", true);
            Assert.Equal("w", broken.FailedActors.Single(), "broken early fails once");
            Assert.Equal(0, RuneRules.ObserveCondition(Alacrity, state, 19, RuneCondition.Paralyzed, false, "w", true).FailedActors.Count, "no repeat curse");
            var held = new RuneFocusState();
            RuneRules.ObserveCondition(Alacrity, held, 0, RuneCondition.Staggered, true, "w", true);
            Assert.Equal(true, RuneRules.ObserveCondition(Alacrity, held, 30, RuneCondition.Staggered, true, "w", true).Disabled, "five rounds");
            var entangled = new RuneFocusState();
            RuneRules.ObserveCondition(Alacrity, entangled, 0, RuneCondition.Entangled, true, "w", true);
            Assert.Equal(false, RuneRules.ObserveCondition(Alacrity, entangled, 54, RuneCondition.Entangled, true, "w", true).Disabled, "nine rounds");
            Assert.Equal(true, RuneRules.ObserveCondition(Alacrity, entangled, 60, RuneCondition.Entangled, true, "w", true).Disabled, "ten rounds");
        }

        public static void TestStatusLapsedUnobservedIsNotASuccess()
        {
            // Paralysis seen on at 0 and 12, then the next observation comes after a time jump and finds it gone.
            var state = new RuneFocusState();
            RuneRules.ObserveCondition(Alacrity, state, 0, RuneCondition.Paralyzed, true, "w", true);
            Assert.Equal(true, RuneRules.HasTimedAttempt(state), "a sustained attempt is timed");
            RuneRules.ObserveCondition(Alacrity, state, 12, RuneCondition.Paralyzed, true, "w", true);
            RuneOutcome jumped = RuneRules.ObserveCondition(Alacrity, state, 300, RuneCondition.Paralyzed, false, "w", true);
            Assert.Equal(false, jumped.Disabled, "elapsed time alone is not five observed rounds");
            Assert.Equal("w", jumped.FailedActors.Single(), "the lapse fails once");
            Assert.Equal(false, RuneRules.HasTimedAttempt(state), "resolved");
            // A status seen on through its full duration still succeeds when the next observation finds it just ended.
            var held = new RuneFocusState();
            RuneRules.ObserveCondition(Alacrity, held, 0, RuneCondition.Staggered, true, "w", true);
            RuneRules.ObserveCondition(Alacrity, held, 29.75, RuneCondition.Staggered, true, "w", true);
            Assert.Equal(true, RuneRules.ObserveCondition(Alacrity, held, 30, RuneCondition.Staggered, true, "w", true).Disabled, "observed on for five rounds");
            // A sustain saved before observations were recorded starts counting from its next sighting.
            var legacy = new RuneFocusState();
            legacy.Sustains.Add(new RuneSustain { Condition = RuneCondition.Paralyzed, StartedAt = 0, Actor = "w" });
            Assert.Equal(false, RuneRules.ObserveCondition(Alacrity, legacy, 40, RuneCondition.Paralyzed, false, "w", true).Disabled, "legacy lapse unobserved");
        }

        public static void TestSustainedStatusIsTimedByItsOwnEffect()
        {
            // A five-round paralysis applied at 100.10, first polled at 100.25, last seen on at 129.25 and removed by its own
            // expiry at 130.10: the polls alone saw 29.0 rounds' worth, the effect held exactly five rounds.
            var exact = new RuneFocusState();
            RuneRules.ObserveCondition(Alacrity, exact, 100.25, RuneCondition.Paralyzed, true, "w", true, 100.10);
            Assert.Equal(100.10, exact.Sustains.Single().StartedAt, "the sustain starts when the effect did");
            RuneRules.ObserveCondition(Alacrity, exact, 129.25, RuneCondition.Paralyzed, true, "w", true);
            Assert.Equal(true, RuneRules.ObserveCondition(Alacrity, exact, 130.25, RuneCondition.Paralyzed, false, "w", true, -1, 130.10).Disabled, "natural expiry at exactly five rounds");
            // The same effect broken free a quarter-second short of five rounds fails.
            var broken = new RuneFocusState();
            RuneRules.ObserveCondition(Alacrity, broken, 100.25, RuneCondition.Paralyzed, true, "w", true, 100.10);
            RuneRules.ObserveCondition(Alacrity, broken, 129.75, RuneCondition.Paralyzed, true, "w", true);
            RuneOutcome shortOutcome = RuneRules.ObserveCondition(Alacrity, broken, 130.00, RuneCondition.Paralyzed, false, "w", true, -1, 129.85);
            Assert.Equal(false, shortOutcome.Disabled, "interrupted 0.25 s short");
            Assert.Equal("w", shortOutcome.FailedActors.Single(), "the interruption is a failed attempt");
            // An end time later than the observation that reports it, or earlier than the last sighting, is not used.
            var odd = new RuneFocusState();
            RuneRules.ObserveCondition(Alacrity, odd, 0, RuneCondition.Staggered, true, "w", true);
            RuneRules.ObserveCondition(Alacrity, odd, 20, RuneCondition.Staggered, true, "w", true);
            Assert.Equal(false, RuneRules.ObserveCondition(Alacrity, odd, 21, RuneCondition.Staggered, false, "w", true, -1, 40).Disabled, "a future end is ignored");
            var stale = new RuneFocusState();
            RuneRules.ObserveCondition(Alacrity, stale, 0, RuneCondition.Staggered, true, "w", true);
            RuneRules.ObserveCondition(Alacrity, stale, 29.9, RuneCondition.Staggered, true, "w", true);
            Assert.Equal(false, RuneRules.ObserveCondition(Alacrity, stale, 31, RuneCondition.Staggered, false, "w", true, -1, 10).Disabled, "an end before the last sighting credits the sighting");
            // An application time after the first sighting is not trusted either.
            var late = new RuneFocusState();
            RuneRules.ObserveCondition(Alacrity, late, 5, RuneCondition.Paralyzed, true, "w", true, 9);
            Assert.Equal(5.0, late.Sustains.Single().StartedAt, "a future application time is ignored");
        }

        public static void TestTimedAttemptsBlockLongWork()
        {
            var open = new RuneFocusState();
            RuneRules.ApplyDamage(Life, open, 0, "c", false, false, Hit(RuneDamageType.NegativeEnergy, 20));
            Assert.Equal(true, RuneRules.HasTimedAttempt(open), "open window");
            var cast = new RuneFocusState();
            RuneRules.ApplySpell(Armor, cast, 0, "w", false, false, 5);
            Assert.Equal(true, RuneRules.HasTimedAttempt(cast), "spell awaiting effects");
            var levels = new RuneFocusState();
            RuneRules.ApplyNegativeLevels(Life, levels, 0, "a", 1, 100);
            Assert.Equal(true, RuneRules.HasTimedAttempt(levels), "expiring negative level");
            var permanent = new RuneFocusState();
            RuneRules.ApplyNegativeLevels(Life, permanent, 0, "a", 1, -1);
            Assert.Equal(false, RuneRules.HasTimedAttempt(permanent), "a level that never expires is not timed");
            Assert.Equal(false, RuneRules.HasTimedAttempt(new RuneFocusState()), "idle focus");
        }

        public static void TestNonMagicalEntanglementDoesNotStartTheRoute()
        {
            var state = new RuneFocusState();
            RuneRules.ObserveCondition(Alacrity, state, 0, RuneCondition.Entangled, true, "w", false);
            Assert.Equal(0, state.Sustains.Count, "origin must be magic or alchemy");
        }

        public static void TestPhysicalRoutesCompareOneHitOnly()
        {
            var state = new RuneFocusState();
            RuneOutcome small = RuneRules.ApplyDamage(Defense, state, 0, "f", false, false, Hit(RuneDamageType.Bludgeoning, 30));
            Assert.Equal("f", small.FailedActors.Single(), "single hit short fails at once");
            Assert.Equal(false, RuneRules.ApplyDamage(Defense, state, 1, "f", false, false, Hit(RuneDamageType.Bludgeoning, 42)).Disabled, "42 - 8 = 34");
            Assert.Equal(true, RuneRules.ApplyDamage(Defense, state, 2, "f", false, false, Hit(RuneDamageType.Bludgeoning, 43)).Disabled, "43 - 8 = 35");
        }

        public static void TestAdamantineSkipsHardness()
        {
            var state = new RuneFocusState();
            Assert.Equal(true, RuneRules.ApplyDamage(Armor, state, 0, "f", false, true, Hit(RuneDamageType.Slashing, 35)).Disabled, "bypass");
        }

        public static void TestAttachedEnergyDoesNotCountTowardAPhysicalRoute()
        {
            var state = new RuneFocusState();
            var mixed = new[] { new RuneDamageComponent(RuneDamageType.Bludgeoning, 30), new RuneDamageComponent(RuneDamageType.Fire, 20) };
            Assert.Equal(false, RuneRules.ApplyDamage(Defense, state, 0, "f", false, false, mixed).Disabled, "fire never counts as bludgeoning");
        }

        public static void TestEnergyRouteCombinesItsTypesWithOneHardnessPerHit()
        {
            var state = new RuneFocusState();
            var mixed = new[] { new RuneDamageComponent(RuneDamageType.Electricity, 25), new RuneDamageComponent(RuneDamageType.Sonic, 18) };
            Assert.Equal(true, RuneRules.ApplyDamage(Defense, state, 0, "w", false, false, mixed).Disabled, "43 - 8 = 35 in one event");
        }

        public static void TestControlCountsSummonedDamageOnly()
        {
            var state = new RuneFocusState();
            RuneOutcome pet = RuneRules.ApplyDamage(Control, state, 0, "wolf", false, false, Hit(RuneDamageType.Piercing, 60));
            Assert.Equal("wolf", pet.FailedActors.Single(), "a pet is not summoned");
            RuneRules.ApplyDamage(Control, state, 1, "demon", true, false, Hit(RuneDamageType.Slashing, 40));
            Assert.Equal(true, RuneRules.ApplyDamage(Control, state, 30, "demon", true, false, Hit(RuneDamageType.Slashing, 26)).Disabled, "32 + 18 = 50 inside one minute");
        }

        public static void TestControlWindowTimesOut()
        {
            var state = new RuneFocusState();
            RuneRules.ApplyDamage(Control, state, 0, "demon", true, false, Hit(RuneDamageType.Slashing, 20));
            Assert.Equal("demon", RuneRules.Tick(Control, state, 60).FailedActors.Single(), "ten rounds without enough");
        }

        public static void TestSkillRoutesAndWrongSkill()
        {
            Assert.Equal(true, RuneRules.ApplySkill(Armor, new RuneFocusState(), 0, "r", RuneSkill.Trickery, 35).Disabled, "Trickery 35");
            Assert.Equal("r", RuneRules.ApplySkill(Armor, new RuneFocusState(), 0, "r", RuneSkill.Trickery, 34).FailedActors.Single(), "Trickery 34");
            Assert.Equal("r", RuneRules.ApplySkill(Armor, new RuneFocusState(), 0, "r", RuneSkill.UseMagicDevice, 40).FailedActors.Single(), "wrong skill");
            Assert.Equal(true, RuneRules.ApplySkill(Control, new RuneFocusState(), 0, "r", RuneSkill.UseMagicDevice, 35).Disabled, "UMD 35");
        }

        public static void TestDispelRouteIsDefenseOnly()
        {
            Assert.Equal(true, RuneRules.ApplyDispel(Defense, new RuneFocusState(), 0, "w", true, 26, 25).Disabled, "success");
            Assert.Equal("w", RuneRules.ApplyDispel(Defense, new RuneFocusState(), 0, "w", false, 24, 25).FailedActors.Single(), "failed check");
            Assert.Equal("w", RuneRules.ApplyDispel(Armor, new RuneFocusState(), 0, "w", true, 26, 25).FailedActors.Single(), "wrong focus");
        }

        public static void TestDisabledFocusStopsResponding()
        {
            var state = new RuneFocusState();
            RuneRules.ApplySkill(Armor, state, 0, "r", RuneSkill.Trickery, 40);
            Assert.Equal(0, RuneRules.ApplySkill(Armor, state, 1, "x", RuneSkill.Trickery, 1).FailedActors.Count, "inert");
        }

        public static void TestCurseBrandsDoNotStackDamageOrRepeatAPlate()
        {
            var record = new CurseRecord();
            Assert.Equal(CurseBrandResult.FirstBrand, CurseRules.Brand(record, 0, false, 100), "first");
            Assert.Equal(100 + 86400d, record.NextProgressionAt, "daily schedule");
            Assert.Equal(CurseBrandResult.AlreadyBrandedThisPlate, CurseRules.Brand(record, 0, false, 200), "same plate");
            Assert.Equal(CurseBrandResult.AdditionalBrand, CurseRules.Brand(record, 3, false, 300), "another plate");
            Assert.Equal(100 + 86400d, record.NextProgressionAt, "no second daily track");
            Assert.Equal(21, CurseRules.RemoveDC(record.PlateMask), "20 + 2 - 1");
            Assert.Equal(CurseBrandResult.SavedNoBrand, CurseRules.Brand(record, 5, true, 400), "a save prevents it");
            Assert.Equal(26, CurseRules.RemoveDC(127), "seven brands");
        }

        public static void TestCurseProgressionAndCureAreSeparateFromDamage()
        {
            var record = new CurseRecord();
            CurseRules.Brand(record, 0, false, 0);
            CurseRules.Brand(record, 1, false, 0);
            Assert.Equal(0, CurseRules.DueProgressions(record, 86399), "not yet");
            Assert.Equal(1, CurseRules.DueProgressions(record, 86400), "one for the whole affliction");
            Assert.Equal(0, CurseRules.DueProgressions(record, 86401), "no repeat tick");
            Assert.Equal(true, CurseRules.UpdateFatigue(record, 4), "fatigue from damage");
            CurseRules.Remove(record);
            Assert.Equal(0, record.PlateMask, "removed");
            Assert.Equal(false, CurseRules.UpdateFatigue(record, 4), "damage keeps the fatigue");
            Assert.Equal(true, CurseRules.UpdateFatigue(record, 0), "healing clears it");
            Assert.Equal(false, record.FatigueActive, "only this source's fatigue");
            Assert.Equal(CurseBrandResult.FirstBrand, CurseRules.Brand(record, 0, false, 500), "can be cursed anew");
        }

        public static void TestFeederUsesEmpoweredRollAndDonorCap()
        {
            Assert.Equal(52, FeederRules.EmpoweredDamage(35), "35 + 17");
            Assert.Equal(30, FeederRules.StolenAmount(52, 20, 10), "hit points left plus Constitution");
            Assert.Equal(52, FeederRules.StolenAmount(52, 100, 10), "uncapped");
        }

        public static void TestFeederGrantsDoNotStackOrResurrect()
        {
            var feeder = new FeederState();
            FeederRules.Grant(feeder, 40, 0);
            FeederRules.Grant(feeder, 30, 10);
            Assert.Equal(40, FeederRules.ExposedTemporaryHP(feeder, 20), "strongest only");
            FeederRules.Consume(feeder, 25, 20);
            Assert.Equal(15, FeederRules.ExposedTemporaryHP(feeder, 21), "the weaker grant does not come back");
            Assert.Equal(false, feeder.AccelerationEligible, "not expired yet");
            FeederRules.Expire(feeder, 3700);
            Assert.Equal(true, feeder.AccelerationEligible, "expired with points left");
        }

        public static void TestFeederFullySpentGrantEarnsNoAcceleration()
        {
            var feeder = new FeederState();
            FeederRules.Grant(feeder, 10, 0);
            FeederRules.Consume(feeder, 10, 1);
            FeederRules.Expire(feeder, 4000);
            Assert.Equal(false, feeder.AccelerationEligible, "spent, not expired");
        }

        public static void TestMistSleepAfterThreeConsecutiveFailures()
        {
            var record = new MistRecord();
            Assert.Equal(MistExposure.Staggered, MistRules.EndRoundInside(record, false, false, false), "1");
            Assert.Equal(MistExposure.Resisted, MistRules.EndRoundInside(record, false, true, false), "success breaks the run");
            Assert.Equal(0, record.ConsecutiveFailures, "streak");
            MistRules.EndRoundInside(record, false, false, false);
            MistRules.EndRoundInside(record, false, false, false);
            Assert.Equal(MistExposure.FellAsleep, MistRules.EndRoundInside(record, false, false, false), "three in a row");
            Assert.Equal(MistExposure.NoEffect, MistRules.EndRoundInside(record, false, false, false), "already asleep");
            MistRules.Wake(record);
            Assert.Equal(0, record.ConsecutiveFailures, "waking resets");
        }

        public static void TestMistSleepImmunityIsSeparateFromStagger()
        {
            var record = new MistRecord { ConsecutiveFailures = 2 };
            Assert.Equal(MistExposure.Staggered, MistRules.EndRoundInside(record, false, false, true), "still staggered");
            Assert.Equal(false, record.Asleep, "not asleep");
        }

        public static void TestMistResistanceBonusDoesNotStack()
        {
            Assert.Equal(4, MistRules.KruneSummonResistanceBonus(0), "no bonus yet");
            Assert.Equal(1, MistRules.KruneSummonResistanceBonus(3), "top up");
            Assert.Equal(0, MistRules.KruneSummonResistanceBonus(5), "a larger bonus wins");
        }

        public static void TestMistRealTimeTickIsOneInterval()
        {
            var record = new MistRecord();
            Assert.Equal(true, MistRules.IsDue(record, 0), "first");
            MistRules.Schedule(record, 0);
            Assert.Equal(false, MistRules.IsDue(record, 5.9), "not per frame");
            Assert.Equal(true, MistRules.IsDue(record, 6), "next round");
        }

        public static void TestAwarenessIsOneStateWithManyCauses()
        {
            var ritual = new RitualState();
            Assert.Equal(true, RitualRules.RaiseAwareness(ritual, AwarenessCause.Alarm, 1), "first");
            Assert.Equal(false, RitualRules.RaiseAwareness(ritual, AwarenessCause.Messenger, 2), "second cause does not re-prepare");
            Assert.Equal(false, RitualRules.RaiseAwareness(ritual, AwarenessCause.Alarm, 3), "repeat");
            Assert.Equal(2, ritual.Causes.Count, "both causes kept");
            Assert.Equal(1d, ritual.AwareAt, "first time");
            Assert.Equal(false, ritual.A4DoorHeard, "awareness is not hearing the door");
            Assert.Equal(false, ritual.ConfrontationStarted, "alarm never starts the countdown");
        }

        public static void TestDoorHearingPreparesOnce()
        {
            var ritual = new RitualState();
            Assert.Equal(true, RitualRules.HearDoor(ritual, 1), "first");
            Assert.Equal(false, RitualRules.HearDoor(ritual, 2), "once");
            Assert.Equal(true, ritual.CatsGraceConsumed, "potion");
            Assert.Equal(true, ritual.NagaInvisibilityIntent, "naga");
        }

        public static void TestConfrontationRollPersistsAndAccelerationAppliesOnce()
        {
            var ritual = new RitualState();
            var feeder = new FeederState();
            FeederRules.Grant(feeder, 40, 0);
            Assert.Equal(true, RitualRules.CommitConfrontation(ritual, feeder, 10, 4000), "commits");
            Assert.Equal(4000d + 9 * 6d, ritual.ConfrontationDeadline, "10 rounds less one");
            Assert.Equal(false, RitualRules.CommitConfrontation(ritual, feeder, 12, 4100), "no reroll");
            Assert.Equal(10, ritual.ConfrontationRoll, "roll kept");
        }

        public static void TestClockEarliestDeadlineWinsAndFiresOnce()
        {
            var ritual = new RitualState();
            RitualRules.DefeatKurshu(ritual, 0);
            Assert.Equal(false, RitualRules.DefeatKurshu(ritual, 10), "idempotent");
            RitualRules.CommitConfrontation(ritual, new FeederState(), 7, 100);
            Assert.Equal(false, RitualRules.Tick(ritual, 141), "142 not reached");
            Assert.Equal(true, RitualRules.Tick(ritual, 142), "short clock");
            Assert.Equal(AwakeningReason.ConfrontationDeadline, ritual.Reason, "reason");
            Assert.Equal(false, RitualRules.Tick(ritual, 6000), "one signal");
        }

        public static void TestEmeraldForcesAwakening()
        {
            var ritual = new RitualState();
            Assert.Equal(true, RitualRules.RequestImmediateAwakening(ritual, 5), "due now");
            Assert.Equal(AwakeningReason.EmeraldDestroyed, ritual.Reason, "reason");
            Assert.Equal(true, ritual.AwakeningOpeningStagger, "stored for the later encounter");
        }

        public static void TestRemainingTimeReportsNoClockBeforeEvents()
        {
            Assert.Equal(false, RitualRules.RemainingSeconds(new RitualState(), 0).HasValue, "not running");
        }

        public static void TestStateInitializesWardsOnce()
        {
            var state = new SanctumState();
            Assert.Equal(true, state.Initialize(100, false), "first");
            double expires = state.Alarm.ExpiresAt;
            Assert.Equal(false, state.Initialize(5000, false), "reload");
            Assert.Equal(expires, state.Alarm.ExpiresAt, "no refresh");
            Assert.Equal(100d + 18 * 3600d, expires, "eighteen hours");
            Assert.Equal(true, state.Door.Latched, "fresh door is locked");
            var legacy = new SanctumState();
            legacy.Initialize(0, true);
            Assert.Equal(false, legacy.Door.Latched, "a pre-WR-04 door stays passable");
        }

        public static void TestMaskRecomputesFromFoci()
        {
            var state = new SanctumState();
            state.Focus("A3d").Disabled = true;
            state.Focus("A3g").Disabled = true;
            Assert.Equal((1 << 3) | (1 << 6), state.RecomputeMask(), "bits");
        }

        public static void TestInspectionPreventsRerollsUntilRankRises()
        {
            var ledger = new List<KnowledgeEntry>();
            Assert.Equal(true, InspectionRules.CanAttempt(ledger, "A3a", "wiz", "Arcana", 10), "first");
            InspectionRules.Record(ledger, "A3a", "wiz", "Arcana", 10, 25);
            Assert.Equal(false, InspectionRules.CanAttempt(ledger, "A3a", "wiz", "Arcana", 10), "no repeat click");
            Assert.Equal(true, InspectionRules.CanAttempt(ledger, "A3a", "wiz", "Arcana", 11), "new rank");
            Assert.Equal(true, InspectionRules.CanAttempt(ledger, "A3a", "cleric", "Religion", 10), "new character");
            Assert.Equal(1, InspectionRules.RuneTier(InspectionRules.BestResult(ledger, "A3a")), "result 25 is a hint");
            Assert.Equal(0, InspectionRules.RuneTier(22), "below the hint");
            Assert.Equal(2, InspectionRules.RuneTier(33), "exact");
        }

        public static void TestAlarmCoversTheStairsAndApexButNotArrival()
        {
            Assert.Equal(true, WardGeometry.InAlarmZone(13.15f, 26f, 0f), "foot of the A2 stair at the door");
            Assert.Equal(true, WardGeometry.InAlarmZone(13f, 19.6f, 6f), "apex");
            Assert.Equal(false, WardGeometry.InAlarmZone(13f, 15.1f, 0f), "printed arrival");
            Assert.Equal(false, WardGeometry.InAlarmZone(11.1f, 13.9f, 0f), "arrival corner");
            Assert.Equal(4f, WardGeometry.AlarmRadiusSquares, "twenty feet in five-foot squares");
        }

        public static void TestGlyphBurstReachesTenFeetFromItsTrigger()
        {
            Assert.Equal(true, WardGeometry.InGlyphTrigger(19.6f, 14.65f), "centre");
            Assert.Equal(false, WardGeometry.InGlyphTrigger(19.6f, 12.5f), "not the room beyond");
            Assert.Equal(2f, WardGeometry.GlyphBurstSquares, "ten feet");
        }

        public static void TestMistStaysInItsVisibleRectangleAndChamber()
        {
            Assert.Equal(true, WardGeometry.InMist(8.9f, 9.5f), "under the sarcophagus");
            Assert.Equal(false, WardGeometry.InMist(16f, 9.5f), "A4 floats no hazard");
            Assert.Equal(false, WardGeometry.InMist(8.9f, 5.2f), "behind the wall");
        }

        public static void TestDoorHardnessAppliesOncePerIndependentHit()
        {
            Assert.Equal(0, DoorRules.HitDamage(7, WardProfile.DoorHardness, false), "7 becomes 0");
            Assert.Equal(4, DoorRules.HitDamage(12, WardProfile.DoorHardness, false), "12 becomes 4");
            Assert.Equal(22, DoorRules.HitDamage(30, WardProfile.DoorHardness, false), "30 becomes 22");
            int two = DoorRules.HitDamage(12, WardProfile.DoorHardness, false) + DoorRules.HitDamage(12, WardProfile.DoorHardness, false);
            Assert.Equal(8, two, "two 12-point hits deal 4 + 4, not 24 - 8");
            Assert.Equal(12, DoorRules.HitDamage(12, WardProfile.DoorHardness, true), "a bypassing hit ignores hardness");
            Assert.Equal(75, WardProfile.DoorHitPoints, "printed hit points");
            Assert.Equal(28, WardProfile.DoorBreakDC, "printed break DC");
            Assert.Equal(20, WardProfile.GemHardness, "the emerald keeps its own hardness");
        }

        public static void TestOnlyTheFiveEnergiesAreHalvedAgainstStone()
        {
            Assert.Equal(20, DoorRules.EnergyShare("Fire", 40), "a fireball is halved");
            Assert.Equal(5, DoorRules.EnergyShare("Acid", 11), "acid is halved, rounded down");
            Assert.Equal(0, DoorRules.EnergyShare("NegativeEnergy", 30), "life energy does not harm stone");
            Assert.Equal(22, DoorRules.EnergyShare("Magic", 22), "Disintegrate and force apply in full");
            Assert.Equal(10, DoorRules.EnergyShare("Divine", 10), "divine power is not energy under the object rule");
        }

        public static void TestDoorFeedbackIsRestrained()
        {
            Assert.Equal("The blow leaves no more than a scratch.", DoorRules.Describe(0, 75, 75), "ineffective");
            Assert.Equal(true, DoorRules.Describe(22, 30, 75).Contains("crack"), "meaningful damage shows");
            Assert.Equal("The stone door breaks and falls.", DoorRules.Describe(10, 0, 75), "destroyed");
        }

        public static void TestImpressionNeedsAMarkedPetrifiedFigureAndNoHeldImpression()
        {
            Assert.Equal(true, CredentialRules.CanTakeImpression("Support-N1", StatueOutcome.Stone, false), "reporter's palm");
            Assert.Equal(true, CredentialRules.CanTakeImpression("Support-N4", StatueOutcome.Stone, false), "attacker's palm");
            Assert.Equal(false, CredentialRules.CanTakeImpression("Support-N2", StatueOutcome.Stone, false), "unmarked figure");
            Assert.Equal(false, CredentialRules.CanTakeImpression("Support-N1", StatueOutcome.Stone, true), "one held impression at a time");
            Assert.Equal(false, CredentialRules.CanTakeImpression("Support-N1", StatueOutcome.Living, false), "no copying a living hostile");
            Assert.Equal(BearerRole.Report, BearerManifest.Roles["Support-N1"], "one marked reporter");
            Assert.Equal(BearerRole.Attack, BearerManifest.Roles["Support-N4"], "one marked attacker");
        }

        public static void TestLostImpressionCanBeRemadeButHandsAreOnce()
        {
            var state = new CredentialState();
            CredentialRules.RecordImpression(state, "impression:Support-N1:10", "Support-N1", "pc", 10);
            CredentialRules.Reconcile(state, new HashSet<string> { "impression:Support-N1:10" });
            Assert.Equal(1, state.Items.Count, "held impression stays recorded");
            CredentialRules.Reconcile(state, new HashSet<string>());
            Assert.Equal(0, state.Items.Count, "a lost impression's record goes, so it can be remade");
            Assert.Equal(false, CredentialRules.CanRecoverHand(state, "Support-N1", false), "no hand from a living or absent body");
            Assert.Equal(true, CredentialRules.CanRecoverHand(state, "Support-N1", true), "the real corpse");
            Assert.Equal(true, CredentialRules.RecordHand(state, "hand:Support-N1", "Support-N1", "pc", 20), "first recovery");
            Assert.Equal(false, CredentialRules.RecordHand(state, "hand:Support-N1", "Support-N1", "pc", 30), "never twice");
            Assert.Equal(false, CredentialRules.CanRecoverHand(state, "Support-N1", true), "recorded once");
            CredentialRules.Reconcile(state, new HashSet<string>());
            Assert.Equal(1, state.Items.Count(r => r.Kind == CredentialKind.Hand), "hand provenance survives");
        }

        public static void TestLongWorkStopsAtADeadlineWithoutReward()
        {
            WorkPlan free = WorkRules.Plan(100, 60, null);
            Assert.Equal(60d, free.Advance, "full minute");
            Assert.Equal(true, free.Completed, "reward");
            WorkPlan cut = WorkRules.Plan(100, 60, 130);
            Assert.Equal(30d, cut.Advance, "stops at the deadline");
            Assert.Equal(false, cut.Completed, "no reward after an interrupted interval");
            Assert.Equal(true, WorkRules.Plan(100, 60, 500).Completed, "a later deadline does not interrupt");
        }

        public static void TestRunePlatesDenySavesOnlyToTheirOwnVulnerabilities()
        {
            Assert.Equal(true, RuneVulnerability.Denies("A3a", RuneVulnerability.Death), "Life against death");
            Assert.Equal(true, RuneVulnerability.Denies("A3a", RuneVulnerability.Poison), "Life against poison");
            Assert.Equal(false, RuneVulnerability.Denies("A3a", RuneVulnerability.Compulsion), "Life saves against compulsion");
            Assert.Equal(true, RuneVulnerability.Denies("A3f", RuneVulnerability.Compulsion), "Control against compulsion");
            Assert.Equal(true, RuneVulnerability.Denies("A3b", RuneVulnerability.Paralysis), "Alacrity against paralysis");
            Assert.Equal(true, RuneVulnerability.Denies("A3c", RuneVulnerability.Electricity), "Defense against electricity");
            Assert.Equal(true, RuneVulnerability.Denies("A3e", RuneVulnerability.Fire), "Armor against fire");
            Assert.Equal(false, RuneVulnerability.Denies("A3e", RuneVulnerability.Cold), "Armor saves against cold");
            Assert.Equal(false, RuneVulnerability.Denies("Support-N1", RuneVulnerability.Death), "statues save normally");
            Assert.Equal(false, RuneVulnerability.Denies("Krune", RuneVulnerability.Compulsion), "Krune saves normally");
            Assert.Equal(false, RuneVulnerability.Denies(null, RuneVulnerability.Fire), "non-fixtures untouched");
        }

        public static void TestRunePlatesDenySavesByWhatTheEffectDoes()
        {
            // Harm and the inflict spells carry no descriptor: Life still answers their negative energy, scoped to Life.
            Assert.Equal(true, RuneVulnerability.Denies("A3a", 0L, RuneEffect.NegativeEnergyDamage), "Life against undescribed negative energy");
            Assert.Equal(true, RuneVulnerability.Denies("A3a", 0L, RuneEffect.NegativeLevels), "Life against energy drain");
            Assert.Equal(false, RuneVulnerability.Denies("A3b", 0L, RuneEffect.NegativeEnergyDamage), "Alacrity saves against negative energy");
            Assert.Equal(false, RuneVulnerability.Denies("A3f", 0L, RuneEffect.NegativeEnergyDamage), "Control saves against negative energy");
            Assert.Equal(true, RuneVulnerability.Denies("A3b", 0L, RuneEffect.ColdDamage), "Alacrity against undescribed cold");
            Assert.Equal(false, RuneVulnerability.Denies("A3e", 0L, RuneEffect.ColdDamage), "Armor saves against cold damage");
            Assert.Equal(false, RuneVulnerability.Denies("A3a", 0L, RuneEffect.None), "no qualifying effect");
            Assert.Equal(false, RuneVulnerability.Denies("Support-N1", 0L, RuneEffect.NegativeEnergyDamage), "statues save normally");
            Assert.Equal(false, RuneVulnerability.Denies("Krune", 0L, RuneEffect.NegativeLevels), "Krune saves normally");
        }

        public static void TestSpentFeederPointsNeverReturnOrAccelerate()
        {
            var feeder = new FeederState();
            FeederRules.Grant(feeder, 57, 0);
            FeederRules.Consume(feeder, 20, 10);
            Assert.Equal(37, FeederRules.ExposedTemporaryHP(feeder, 20), "37 remain after absorbing 20");
            FeederRules.Grant(feeder, 30, 30);
            Assert.Equal(37, FeederRules.ExposedTemporaryHP(feeder, 40), "a weaker grant does not stack or replace");
            FeederRules.Consume(feeder, 37, 50);
            Assert.Equal(0, FeederRules.ExposedTemporaryHP(feeder, 60), "spent entirely, including the weaker grant");
            FeederRules.Expire(feeder, WardProfile.FeederTempHpSeconds + 100);
            Assert.Equal(false, feeder.AccelerationEligible, "a consumed grant earns no acceleration at expiry");
            Assert.Equal(0, feeder.Grants.Count, "expired");

            var fresh = new FeederState();
            FeederRules.Grant(fresh, 20, 0);
            FeederRules.Grant(fresh, 45, 5);
            Assert.Equal(45, FeederRules.ExposedTemporaryHP(fresh, 10), "a stronger later grant shows");
            FeederRules.Expire(fresh, WardProfile.FeederTempHpSeconds + 1);
            Assert.Equal(true, fresh.AccelerationEligible, "an unspent grant running out naturally qualifies once");
            Assert.Equal(45, FeederRules.ExposedTemporaryHP(fresh, WardProfile.FeederTempHpSeconds + 1), "the later grant still runs");
        }

        public static void TestMistBonusImprovesButNeverStacksResistance()
        {
            Assert.Equal(4, MistRules.KruneSummonResistanceBonus(0), "no existing bonus");
            Assert.Equal(2, MistRules.KruneSummonResistanceBonus(2), "+2 improved to +4");
            Assert.Equal(0, MistRules.KruneSummonResistanceBonus(5), "+5 already larger");
        }

        public static void TestVersionOneStateMigratesWithoutReset()
        {
            var state = new SanctumState { Version = 1, RuneMask = 5, Credentials = null };
            state.Door.HitPoints = 40;
            state.Door.Latched = false;
            Assert.Equal(true, state.Migrate(), "migrates");
            Assert.Equal(SanctumState.CurrentVersion, state.Version, "current");
            Assert.Equal(5, state.RuneMask, "mask kept");
            Assert.Equal(40, state.Door.HitPoints, "door damage kept");
            Assert.Equal(false, state.Door.Latched, "open latch kept");
            Assert.Equal(true, state.Credentials != null, "credential state added");
            Assert.Equal(false, state.Migrate(), "once");
        }

        public static void TestVersionTwoStateGainsSummonOrdersAndLeavesReportersUnsettled()
        {
            var state = new SanctumState { Version = 2, SummonOrders = null };
            state.Statue("Support-N1").Outcome = StatueOutcome.Living;
            state.Statue("Support-N1").RouteLeg = 3;
            Assert.Equal(true, state.Migrate(), "migrates");
            Assert.Equal(true, state.SummonOrders != null && state.SummonOrders.Count == 0, "empty summon orders added");
            Assert.Equal(false, state.Statue("Support-N1").HostileSettled, "an older reporter is settled by the runtime, not assumed");
            Assert.Equal(3, state.Statue("Support-N1").RouteLeg, "route progress kept");
        }

        public static void TestBearerManifestIsFiveAndFive()
        {
            Assert.Equal(10, BearerManifest.Roles.Count, "ten bearers");
            Assert.Equal(5, BearerManifest.Roles.Values.Count(r => r == BearerRole.Attack), "attack");
            Assert.Equal(5, BearerManifest.Roles.Values.Count(r => r == BearerRole.Report), "report");
            Assert.Equal(true, BearerManifest.Roles.Keys.All(BearerManifest.IsBearer), "ids");
        }
    }
}
