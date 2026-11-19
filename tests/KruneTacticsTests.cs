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

        public static void TestRejectedQuickenedSummonReleasesOfferAndDispatchesStandard()
        {
            var failed = new FailedKrunePlans();
            object turn = new object(), spell = new object();
            failed.Observe(turn, 0);
            foreach (var stage in new[] { CastStage.Constructed, CastStage.Initialized, CastStage.Started })
            {
                var command = new DirectedCastLifecycle(spell);
                bool extendReserved = true;
                if (stage != CastStage.Constructed) command.Initialize();
                if (stage == CastStage.Started) command.Start();
                int slots = 1, actions = 1, charges = 9;
                object rule = new object();
                command.BeginRule(spell, rule);
                command.ResolveRule(spell, rule, false, () => { slots--; actions--; charges--; });
                command.End(stage == CastStage.Started ? CastResult.Interrupted : CastResult.Rejected, false, reason =>
                {
                    extendReserved = false;
                    failed.Reject(KruneActionBudget.Swift, "summon/quickened", "giant", reason);
                }, () =>
                {
                    Assert.Equal(false, extendReserved, "reservation released before native end subscribers");
                    Assert.Equal(CastStage.Ended, command.Stage, "reentrant native end sees terminal state");
                    for (int i = 0; i < 1000; i++)
                        Assert.Equal(false, failed.Allows(KruneActionBudget.Swift, "summon/quickened", "second point"), "broad rejection blocks re-dispatch");
                    Assert.Equal(true, failed.Allows(KruneActionBudget.Standard, "summon/standard", "giant"), "distinct standard plan is dispatchable");
                });
                Assert.Equal(1, slots, "no commit spend"); Assert.Equal(1, actions, "no action callback");
                Assert.Equal(9, charges, "no Extend charge");
                turn = new object();
                failed.Observe(turn, 0);
                Assert.Equal(true, failed.Allows(KruneActionBudget.Swift, "summon/quickened", "giant"), "next actual turn");
            }
        }

        public static void TestRealTimeFailedQuickenedSummonSurvivesStandardFallbackAndTacticalChanges()
        {
            var view = new KruneView { SummonsAlive = 1 };
            view.Ready["Quickened Summon Monster V"] = 1;
            view.Ready["Foresight"] = 1;
            var exclusions = new FailedKrunePlans();
            exclusions.Observe(null, 0);
            KruneChoice Choose() => KruneTactics.SwiftOptions(view).Concat(KruneTactics.StandardOptions(view))
                .FirstOrDefault(c => exclusions.Allows(c.Metamagic == "Quickened" ? KruneActionBudget.Swift : KruneActionBudget.Standard, c.Key, c.Aim.ToString()));

            var summon = Choose();
            Assert.Equal("Quickened Summon Monster V", summon.Key, "first native order");
            var rejected = new DirectedCastLifecycle(summon);
            rejected.Initialize();
            rejected.End(CastResult.Rejected, false, reason => exclusions.Reject(KruneActionBudget.Swift, summon.Key, summon.Aim.ToString(), reason), () => { });
            var defense = Choose();
            Assert.Equal("Foresight", defense.Key, "same turn chooses a different standard action");
            var fallback = new DirectedCastLifecycle(defense);
            fallback.Initialize();
            fallback.Start();
            var rule = new object();
            fallback.BeginRule(defense, rule);
            fallback.ResolveRule(defense, rule, true, () => view.Ready[defense.Key]--);
            fallback.End(CastResult.NativeFailure, false, result => { }, () => { });
            Assert.Equal(CastResult.Committed, fallback.Result, "fallback resolves");
            Assert.Equal(0, view.Ready[defense.Key], "one committed standard slot");
            Assert.Equal(1, view.Ready[summon.Key], "rejected swift slot remains prepared");
            void StillExcluded(string change)
            {
                exclusions.Observe(null, KruneActionBudget.Standard);
                Assert.Equal(false, exclusions.Allows(KruneActionBudget.Swift, summon.Key, "another point"), change);
                Assert.Equal(null, Choose(), "no duplicate dispatch after " + change);
            }
            StillExcluded("Foresight spent a standard action and a ready slot");
            view.HitPointFraction = 0.4;
            StillExcluded("damage");
            view.HitPointFraction = 0.9;
            StillExcluded("healing");
            view.FoeCasters++;
            StillExcluded("another enemy caster");
            var resources = new KruneState { RodCharges = 9 };
            resources.RodCharges--;
            StillExcluded("rod charge spent");
            exclusions.Observe(null, 0);
            Assert.Equal(null, Choose(), "renewing only the standard cannot renew the summon");
            // A different swift action now spends that budget. Its native cooldown later expires.
            exclusions.Observe(null, KruneActionBudget.Swift);
            exclusions.Observe(null, 0);
            Assert.Equal(summon.Key, Choose().Key, "swift budget itself renewed");
        }

        public static void TestCommitNeedsExactRuleAndRelocationNeedsDelivery()
        {
            foreach (bool relocate in new[] { false, true })
            foreach (bool delivered in new[] { false, true })
            foreach (bool offerAccepted in new[] { false, true })
            {
                var spell = new object();
                var nativeRule = new object();
                var cast = new DirectedCastLifecycle(spell);
                cast.Initialize(); cast.Start(); cast.BeginRule(spell, nativeRule);
                Assert.Equal(false, cast.ResolveRule(new object(), nativeRule, true), "different AbilityData");
                Assert.Equal(false, cast.ResolveRule(spell, new object(), true), "different rule");
                int charges = 9;
                var extend = RodRules.Apply(new[] { "Extend" }, new[] { "Extend" }, new string[0], offerAccepted ? charges : 0);
                Assert.Equal(true, cast.ResolveRule(spell, nativeRule, true, () => charges -= extend.Sum(o => o.Cost)), "unmodified spell also commits");
                Assert.Equal(false, cast.ResolveRule(spell, nativeRule, true), "duplicate rule cannot pay twice");
                if (relocate && delivered) cast.Delivered();
                cast.End(CastResult.NativeFailure, relocate, result => { }, () => { });
                Assert.Equal(relocate ? (delivered ? CastResult.Delivered : CastResult.DeliveryFailed) : CastResult.Committed,
                    cast.Result, "successful rule and actual delivery are separate");
                Assert.Equal(offerAccepted ? 8 : 9, charges, "payment only for accepted offer");
            }
        }

        public static void TestInterruptedCommittedCommandKeepsSpendButNeverReportsDelivery()
        {
            object spell = new object(), rule = new object();
            var cast = new DirectedCastLifecycle(spell);
            cast.Initialize(); cast.Start(); cast.BeginRule(spell, rule);
            cast.ResolveRule(spell, rule, true);
            int nativeEnds = 0;
            try
            {
                cast.End(CastResult.Interrupted, true, result => { throw new InvalidOperationException(); }, () => nativeEnds++);
            }
            catch (InvalidOperationException) { }
            Assert.Equal(CastResult.InterruptedAfterCommit, cast.Result, "no retreat after interrupt");
            Assert.Equal(true, cast.Committed, "native spend remains authoritative");
            cast.End(CastResult.Interrupted, true, result => { }, () => nativeEnds++);
            Assert.Equal(1, nativeEnds, "teardown once even when the observer throws");
        }

        public static void TestFailureEpochTracksBudgetAndTargetInsteadOfGameTime()
        {
            var plans = new FailedKrunePlans();
            plans.Observe(null, 0);
            plans.Reject(KruneActionBudget.Swift, "swift", "giant", CastResult.NativeFailure);
            plans.Observe(null, 0); // 5.99 -> 6.01 game seconds
            Assert.Equal(false, plans.Allows(KruneActionBudget.Swift, "swift", "empty"), "a clock boundary renews nothing");
            plans.Reject(KruneActionBudget.Move, "passage", "stone", CastResult.ResourceUnavailable);
            plans.Observe(null, KruneActionBudget.Swift);
            plans.Observe(null, 0);
            Assert.Equal(true, plans.Allows(KruneActionBudget.Swift, "swift", "giant"), "renewed swift budget");
            Assert.Equal(false, plans.Allows(KruneActionBudget.Move, "passage", "other"), "move failure stays broad");
            plans.Reject(KruneActionBudget.Swift, "swift", "giant", CastResult.RangeInvalid);
            Assert.Equal(false, plans.Allows(KruneActionBudget.Swift, "swift", "giant"), "invalid landing");
            Assert.Equal(true, plans.Allows(KruneActionBudget.Swift, "swift", "empty"), "different legal landing");

            var turn = new object();
            plans.Observe(turn, 0);
            plans.Reject(KruneActionBudget.Standard, "spell", "one", CastResult.ActionUnavailable);
            plans.Observe(turn, KruneActionBudget.Standard);
            plans.Observe(turn, 0);
            Assert.Equal(false, plans.Allows(KruneActionBudget.Standard, "spell", "two"), "same turn regardless of observed cooldown");
            plans.Observe(new object(), 0);
            Assert.Equal(true, plans.Allows(KruneActionBudget.Standard, "spell", "two"), "new native turn");
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
            Assert.Equal(KrunePhase.Fight, KruneTactics.NextPhase(v), "a planned relocation has not yet delivered");
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
