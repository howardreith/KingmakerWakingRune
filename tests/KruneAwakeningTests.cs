using System.Collections.Generic;
using System.Linq;
using KingmakerWakingRune.Sanctum;
using KingmakerWakingRune.Sanctum.Krune;
using KingmakerWakingRune.Sanctum.Wards;

namespace KingmakerWakingRune.Tests
{
    /// <summary>
    /// Exercises the production WR-07A awakening rules: the ritual boundary, the save-bound lifecycle, the revival snapshot
    /// against the live rune links, the feeder handoff, placement and the migration of older saves. Native delivery (the
    /// actor, its modifiers, the dialogue and the pause) is proven in the running game.
    /// </summary>
    public static class KruneAwakeningTests
    {
        private const double Day = 86400d;

        private static RitualState DueBy(AwakeningReason reason, double at)
        {
            var ritual = new RitualState();
            if (reason == AwakeningReason.EmeraldDestroyed)
            {
                RitualRules.RequestImmediateAwakening(ritual, at);
            }
            else
            {
                ritual.GlobalDeadline = at;
                RitualRules.Tick(ritual, at);
            }
            return ritual;
        }

        public static void TestFirstTriggerWinsAndLateEmeraldIsNotASwitch()
        {
            var ritual = new RitualState { KurshuDefeated = true, GlobalDeadline = 100 };
            Assert.Equal(true, RitualRules.Tick(ritual, 100), "deadline first");
            Assert.Equal(false, RitualRules.RequestImmediateAwakening(ritual, 150), "the emerald after the soul returned is no switch");
            Assert.Equal(AwakeningReason.GlobalDeadline, ritual.Reason, "cause kept");
            Assert.Equal(100d, ritual.AwakeningAt, "time kept");
            Assert.Equal(false, ritual.AwakeningOpeningStagger, "no stagger for a late emerald");
        }

        public static void TestSameBoundaryDeadlineComesBeforeTheEmerald()
        {
            var ritual = new RitualState { KurshuDefeated = true, GlobalDeadline = 200 };
            Assert.Equal(false, RitualRules.RequestImmediateAwakening(ritual, 200), "a deadline reached at this boundary came first");
            Assert.Equal(true, ritual.AwakeningDue, "due");
            Assert.Equal(AwakeningReason.GlobalDeadline, ritual.Reason, "deadline is the cause");
            Assert.Equal(false, ritual.AwakeningOpeningStagger, "no stagger");
            var early = new RitualState { KurshuDefeated = true, GlobalDeadline = 200 };
            Assert.Equal(true, RitualRules.RequestImmediateAwakening(early, 199.9), "before the deadline the emerald releases him");
            Assert.Equal(true, early.AwakeningOpeningStagger, "early release staggers");
            Assert.Equal(false, RitualRules.Tick(early, 200), "the later deadline adds nothing");
            Assert.Equal(AwakeningReason.EmeraldDestroyed, early.Reason, "emerald stays the cause");
        }

        public static void TestCommitIsOnceAndTrustsOnlyAnEmeraldStagger()
        {
            var krune = new KruneState();
            RitualState ritual = DueBy(AwakeningReason.GlobalDeadline, 500);
            Assert.Equal(true, KruneRules.Commit(krune, ritual, 5, 520), "commits");
            Assert.Equal(false, KruneRules.Commit(krune, ritual, 127, 900), "never twice");
            Assert.Equal(AwakeningStage.Committed, krune.Stage, "stage");
            Assert.Equal(500d, krune.EffectiveAt, "the ritual's time, not the commit's");
            Assert.Equal(5, krune.RevivalMask, "snapshot");
            // An older build set the flag for an emerald broken after a deadline: the cause decides, not the flag.
            var stale = new RitualState { AwakeningDue = true, Reason = AwakeningReason.ConfrontationDeadline, AwakeningAt = 10, AwakeningOpeningStagger = true };
            var legacy = new KruneState();
            KruneRules.Commit(legacy, stale, 0, 10);
            Assert.Equal(false, legacy.OpeningStaggerOwed, "no stagger for a deadline");
            var emerald = new KruneState();
            KruneRules.Commit(emerald, DueBy(AwakeningReason.EmeraldDestroyed, 30), 0, 30);
            Assert.Equal(true, emerald.OpeningStaggerOwed, "emerald stagger owed");
            Assert.Equal(false, KruneRules.Commit(new KruneState(), new RitualState(), 0, 1), "nothing due, nothing committed");
        }

        public static void TestStagesAdvanceOnlyInOrder()
        {
            var krune = new KruneState();
            Assert.Equal(false, KruneRules.Advance(krune, AwakeningStage.Dormant, AwakeningStage.ActorReady), "no skipping");
            krune.Stage = AwakeningStage.Committed;
            Assert.Equal(true, KruneRules.Advance(krune, AwakeningStage.Committed, AwakeningStage.ActorReady), "next");
            Assert.Equal(false, KruneRules.Advance(krune, AwakeningStage.Committed, AwakeningStage.ActorReady), "a repeated callback does nothing");
            Assert.Equal(AwakeningStage.ActorReady, krune.Stage, "stage");
        }

        public static void TestRevivalSnapshotAgainstLiveRuneLinks()
        {
            var krune = new KruneState();
            KruneRules.Commit(krune, DueBy(AwakeningReason.GlobalDeadline, 1000), 0, 1000);
            KruneSheet intact = KruneRules.Compose(krune, 0, 1000);
            Assert.Equal(293, intact.MaximumHitPoints, "full hp, not 293 plus 120");
            Assert.Equal(17, intact.CasterLevel, "no runes dark at the revival");
            // Every rune darkened after he woke: the links follow, the revival does not restart.
            KruneSheet later = KruneRules.Compose(krune, 127, 1500);
            Assert.Equal(233, later.MaximumHitPoints, "three dark Life runes");
            Assert.Equal(0, later.Linked.LifeFastHealing, "no fast healing");
            Assert.Equal(3, later.Linked.NaturalArmor, "armor halved");
            Assert.Equal(2, later.Linked.DodgeAC, "dodge halved");
            Assert.Equal(2, later.Linked.InitiativeInsight, "initiative halved");
            Assert.Equal(2, later.Linked.DeflectionAC, "deflection halved");
            Assert.Equal(2, later.Linked.ResistanceSaves, "resistance halved");
            Assert.Equal(1, later.Linked.SpearCompetence, "spear halved");
            Assert.Equal(17, later.CasterLevel, "the revival snapshot governs caster level");
        }

        public static void TestRevivalPenaltyLastsTwentyFourHours()
        {
            int[] expected = { 17, 16, 16, 15, 15, 15, 15, 14 };
            for (int dark = 0; dark <= 7; dark++)
            {
                int mask = (1 << dark) - 1;
                var krune = new KruneState();
                KruneRules.Commit(krune, DueBy(AwakeningReason.GlobalDeadline, 100), mask, 100);
                Assert.Equal(expected[dark], KruneRules.Compose(krune, mask, 100).CasterLevel, $"{dark} dark at the revival");
                Assert.Equal(expected[dark], KruneRules.Compose(krune, 127, 100 + Day - 1).CasterLevel, $"{dark}: still within 24 hours, whatever is dark now");
                Assert.Equal(17, KruneRules.Compose(krune, mask, 100 + Day).CasterLevel, $"{dark}: restored after 24 hours");
            }
            Assert.Equal(0, KruneRules.RevivalCasterLevelLoss(new KruneState(), 0), "nothing before a commit");
        }

        public static void TestHitPointChangesKeepDamage()
        {
            var krune = new KruneState();
            KruneRules.Commit(krune, DueBy(AwakeningReason.GlobalDeadline, 0), 0, 0);
            int damage = 100;
            Assert.Equal(193, KruneRules.HitPointsLeft(KruneRules.Compose(krune, 0, 0).MaximumHitPoints, damage), "full");
            int oneLifeDark = 1 << KruneProjection.Focus("A3a").Bit;
            Assert.Equal(273, KruneRules.Compose(krune, oneLifeDark, 10).MaximumHitPoints, "one Life rune dark");
            Assert.Equal(173, KruneRules.HitPointsLeft(KruneRules.Compose(krune, oneLifeDark, 10).MaximumHitPoints, damage), "the same damage, nothing healed");
            Assert.Equal(KruneRules.Compose(krune, oneLifeDark, 10).MaximumHitPoints, KruneRules.Compose(krune, oneLifeDark, 20).MaximumHitPoints, "a repeated projection changes nothing");
        }

        public static void TestFeederHandoffTransfersTheLedgerNotTheRoll()
        {
            var feeder = new FeederState();
            FeederRules.Grant(feeder, 57, 0);
            FeederRules.Consume(feeder, 20, 100);
            Assert.Equal(37, KruneRules.FeederHandoff(feeder, 600, out double expires), "57 granted, 20 spent: 37 handed over");
            Assert.Equal(3600d, expires, "the original hour, not a new one");
            FeederRules.Consume(feeder, 10, 700);
            Assert.Equal(27, KruneRules.FeederHandoff(feeder, 800, out _), "a restart reads the ledger again: 27, never 57");
            Assert.Equal(0, KruneRules.FeederHandoff(feeder, 3600, out double gone), "expired at its own time");
            Assert.Equal(-1d, gone, "nothing left to time");
            Assert.Equal(true, feeder.AccelerationEligible, "natural expiry with points left");
        }

        public static void TestFeederHandoffEdges()
        {
            var expired = new FeederState();
            FeederRules.Grant(expired, 30, 0);
            Assert.Equal(0, KruneRules.FeederHandoff(expired, 4000, out _), "expired before the emergence");
            var exhausted = new FeederState();
            FeederRules.Grant(exhausted, 30, 0);
            FeederRules.Consume(exhausted, 30, 10);
            Assert.Equal(0, KruneRules.FeederHandoff(exhausted, 20, out _), "an exhausted grant hands over nothing");
            FeederRules.Expire(exhausted, 4000);
            Assert.Equal(false, exhausted.AccelerationEligible, "a consumed grant earns no acceleration");
            var repeated = new FeederState();
            FeederRules.Grant(repeated, 57, 0);
            FeederRules.Grant(repeated, 30, 100);
            Assert.Equal(57, KruneRules.FeederHandoff(repeated, 200, out double strongest), "strongest only, no stacking");
            Assert.Equal(3600d, strongest, "the strongest grant's own expiry");
            // A legitimate feeder trigger after the awakening feeds the woken Krune through the same ledger.
            FeederRules.Grant(repeated, 70, 300);
            Assert.Equal(70, KruneRules.FeederHandoff(repeated, 301, out double fresh), "a new, stronger grant");
            Assert.Equal(3900d, fresh, "its own hour");
        }

        public static void TestPlacementNeverLandsOnAnyone()
        {
            var candidates = new[] { new GridPoint(5, 5), new GridPoint(7, 5), new GridPoint(9, 5) };
            bool Floor(float x, float y) => x > 0 && x < 20 && y > 0 && y < 20;
            Assert.Equal(5f, KruneRules.ChoosePlacement(candidates, new List<Occupant>(), Floor).Value.X, "the preferred point when clear");
            var party = new List<Occupant> { new Occupant { X = 5.2f, Y = 5, Radius = 0.4f } };
            Assert.Equal(7f, KruneRules.ChoosePlacement(candidates, party, Floor).Value.X, "an occupied preferred point moves him");
            party.Add(new Occupant { X = 7, Y = 5, Radius = 0.4f });
            party.Add(new Occupant { X = 9, Y = 5.3f, Radius = 0.4f });
            Assert.Equal(false, KruneRules.ChoosePlacement(candidates, party, Floor).HasValue, "nowhere clear: no placement, never an overlap");
            Assert.Equal(false, KruneRules.ChoosePlacement(new[] { new GridPoint(0.2f, 5) }, new List<Occupant>(), Floor).HasValue, "the whole footprint must be floor");
        }

        public static void TestEmergenceCandidatesAreA5FloorOutsideTheCoffin()
        {
            GridPoint? first = KruneRules.ChoosePlacement(KruneRules.EmergenceCandidates(), new List<Occupant>(), KruneRules.A5Floor);
            Assert.Equal(true, first.HasValue, "a clear point exists");
            Assert.Equal(false, KruneRules.InCoffin(first.Value.X, first.Value.Y), "never inside the coffin");
            Assert.Equal(SanctumSpace.Atrium, SanctumPlan.SpaceAt(first.Value.X, first.Value.Y), "in A4/A5");
            Assert.Equal(true, first.Value.X < 12.6f, "in the west chamber");
            var crowd = new List<Occupant> { new Occupant { X = first.Value.X, Y = first.Value.Y, Radius = 0.4f } };
            GridPoint? second = KruneRules.ChoosePlacement(KruneRules.EmergenceCandidates(), crowd, KruneRules.A5Floor);
            Assert.Equal(true, second.HasValue && (second.Value.X != first.Value.X || second.Value.Y != first.Value.Y), "someone on the preferred point: elsewhere");
            Assert.Equal(10, KruneRules.StepFeet(SanctumPlan.SarcophagusCenter.X, SanctumPlan.SarcophagusCenter.Y, SanctumPlan.SarcophagusCenter.X + 1.55f, SanctumPlan.SarcophagusCenter.Y), "two squares out of the coffin");
            Assert.Equal(1, KruneRules.StepUses(10), "a 10-foot step is one of the game's 30-foot uses");
            Assert.Equal(2, KruneRules.StepUses(35), "35 feet takes two");
            Assert.Equal(510, KruneRules.StepsFeetPerUse * 17, "17 uses are the printed 510 feet");
        }

        public static void TestOlderDueSaveIsOfferedAChoiceAndKeepsItsRecord()
        {
            var state = new SanctumState { Version = 5, Initialized = true, Krune = null };
            state.Ritual.KurshuDefeated = true;
            state.Ritual.GlobalDeadline = 5400;
            RitualRules.Tick(state.Ritual, 5400);
            state.RuneMask = 9;
            Assert.Equal(true, state.Migrate(), "migrates");
            Assert.Equal(6, state.Version, "version 6");
            Assert.Equal(LegacyAwakening.AwaitingChoice, state.Krune.Legacy, "asked once");
            Assert.Equal(AwakeningStage.Dormant, state.Krune.Stage, "no pretence that the old build made him");
            Assert.Equal(false, KruneRules.MayAdvance(state.Krune), "nothing happens before the choice");
            Assert.Equal(5400d, state.Ritual.AwakeningAt, "the recorded time is kept");
            Assert.Equal(true, KruneRules.AdoptLegacy(state.Krune, state.Ritual, state.RuneMask, 6000), "adopted");
            Assert.Equal(AwakeningStage.Committed, state.Krune.Stage, "committed from the record");
            Assert.Equal(5400d, state.Krune.EffectiveAt, "recorded time, not the load time");
            Assert.Equal(9, state.Krune.RevivalMask, "the saved mask is the migration snapshot");
            Assert.Equal(true, state.Krune.RevivalMaskFromMigration, "labelled as such");
            Assert.Equal(false, KruneRules.AdoptLegacy(state.Krune, state.Ritual, 0, 7000), "asked once only");
            Assert.Equal(false, state.Migrate(), "never migrated twice");

            var kept = new SanctumState { Version = 5, Initialized = true };
            kept.Ritual.AwakeningDue = true;
            kept.Migrate();
            Assert.Equal(true, KruneRules.KeepLegacy(kept.Krune), "kept as a legacy exploration save");
            Assert.Equal(false, KruneRules.MayAdvance(kept.Krune), "and stays so");

            var notYet = new SanctumState { Version = 5, Initialized = true };
            notYet.Ritual.KurshuDefeated = true;
            notYet.Ritual.GlobalDeadline = 9000;
            notYet.Migrate();
            Assert.Equal(LegacyAwakening.None, notYet.Krune.Legacy, "not due: simply adopts the new lifecycle");
            Assert.Equal(9000d, notYet.Ritual.GlobalDeadline, "no clock restarted");
            Assert.Equal(true, KruneRules.MayAdvance(notYet.Krune), "continues to the real awakening");
        }

        public static void TestHostilityAndCheckpoint()
        {
            var krune = new KruneState();
            Assert.Equal(true, KruneRules.TurnHostile(krune, HostilityCause.PartyCastSpell, "Haste", 10), "first observation");
            Assert.Equal(false, KruneRules.TurnHostile(krune, HostilityCause.Challenged, "later", 20), "first wins");
            Assert.Equal(HostilityCause.PartyCastSpell, krune.HostileCause, "cause kept");
            Assert.Equal(false, KruneRules.CheckpointHolds(krune), "not before the handoff");
            krune.CheckpointReached = true;
            Assert.Equal(true, KruneRules.CheckpointHolds(krune), "holds");
            krune.DiagnosticRelease = true;
            Assert.Equal(false, KruneRules.CheckpointHolds(krune), "released only by the labelled harness");
        }

        public static void TestManifestCarriesTheExtraChallengeAdjustments()
        {
            Assert.Equal(4, KruneManifest.PreparedCount(9), "9th-level slots");
            Assert.Equal(1, KruneManifest.CountOf("Summon Monster V", "Quickened"), "one quickened summon monster V left");
            Assert.Equal(1, KruneManifest.CountOf("Summon Monster IX", string.Empty), "an ordinary summon monster IX, not quickened");
            Assert.Equal(0, KruneManifest.CountOf("Summon Monster IX", "Quickened"), "no free quickened ninth-level summon");
            int[] printed = { 4, 9, 9, 8, 8, 8, 8, 6, 5, 4 };
            for (int level = 0; level <= 9; level++)
            {
                Assert.Equal(printed[level], KruneManifest.PreparedCount(level), $"level {level} count as printed");
            }
            Assert.Equal(ManifestStatus.Spent, KruneManifest.Prepared.Single(p => p.Spell == "Mage Armor").Status, "cast before hibernation");
            Assert.Equal(true, KruneManifest.Prepared.Where(p => p.Spell == "Foresight" || p.Spell == "Stoneskin" || p.Spell == "Spell Turning" || p.Spell == "True Seeing").All(p => p.Status == ManifestStatus.Deferred), "no free pre-buffs");
            Assert.Equal(true, KruneManifest.Items.Any(i => i.Key == "Diamonds" && i.Detail.Contains("not party gold")), "diamonds are a finite reserve");
            Assert.Equal(true, KruneManifest.KnownExtra.Any(k => k.StartsWith("Wish")), "wish known");
            Assert.Equal(true, KruneManifest.Resources.Any(r => r.Key == "RodCharges" && r.Detail.StartsWith("10")), "the rod's own pool");
        }
    }
}
