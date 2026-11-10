using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Components;
using KingmakerWakingRune.Blueprints;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>Wakes a mist sleeper when something actually hurts it. Saved buff; the rule is the saved state's.</summary>
    public sealed class KwrMistSleepWake : BuffLogic, ITargetRulebookHandler<Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage>
    {
        public void OnEventAboutToTrigger(Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage evt)
        {
        }

        public void OnEventDidTrigger(Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage evt)
        {
            if (evt.Damage > 0)
            {
                WardRuntime.WakeSleeper(Owner.Unit);
            }
        }
    }

    /// <summary>Krune's support entity: real temporary hit points from the saved feeder grants, and nothing that acts.</summary>
    internal static class WardKrune
    {
        /// <summary>Read by the buff as it applies, so the amount always comes from saved state.</summary>
        public static int PendingTemporaryHP { get; private set; }

        public static void Refresh(SanctumState state, double now)
        {
            UnitEntityData krune = WardFixtures.Krune;
            if (krune == null)
            {
                return;
            }
            FeederRules.Expire(state.Door.Feeder, now);
            int amount = FeederRules.ExposedTemporaryHP(state.Door.Feeder, now);
            Buff buff = krune.Buffs.GetBuff(WardBlueprints.FeederTempHp);
            int shown = buff == null ? 0 : krune.Stats.TemporaryHitPoints.ModifiedValue;
            if (amount == shown)
            {
                return;
            }
            buff?.Remove();
            if (amount > 0)
            {
                FeederGrant strongest = state.Door.Feeder.Grants.Where(g => g.Remaining == amount).OrderByDescending(g => g.ExpiresAt).First();
                PendingTemporaryHP = amount;
                krune.Descriptor.AddBuff(WardBlueprints.FeederTempHp, krune, TimeSpan.FromSeconds(strongest.ExpiresAt - now));
            }
            Log.Info($"Krune receiver: {amount} temporary HP (was {shown}).");
        }
    }

    /// <summary>
    /// The save-bound sanctum at play. Reads the native simulation clock only, so pause, dialogue and loading advance nothing,
    /// and rebuilds its world from saved state on every load so that a reload cannot repeat what already happened.
    /// </summary>
    internal static class WardRuntime
    {
        private const double TickSeconds = 0.25d;

        private static readonly WardObserver s_Observer = new WardObserver();
        private static bool s_Subscribed;
        private static double s_NextTick;

        public static SanctumState State => WardStore.State;

        public static double Now => Game.Instance.Player.GameTime.TotalSeconds;

        public static void OnSanctumLoaded(bool legacyEntered)
        {
            if (!s_Subscribed)
            {
                EventBus.Subscribe(s_Observer);
                s_Subscribed = true;
            }
            SanctumState state = State;
            if (state == null)
            {
                return;
            }
            if (state.Initialize(Now, legacyEntered))
            {
                Log.Info($"Sanctum state initialized at {Now:0} ({(legacyEntered ? "legacy save" : "fresh")}); profile {state.ProfileSubtier}, {state.ProfilePartyBaseline} PCs, extra challenge {state.ProfileExtraChallenge}.");
            }
            WardFixtures.Ensure();
            EnsureDefenseWard(state);
            WardKrune.Refresh(state, Now);
            s_NextTick = 0d;
            Log.Info($"Wards ready: mask={state.RuneMask} clock={Now:0} focusWindows={state.Foci.Values.Sum(f => f.Windows.Count)}.");
        }

        public static void Tick()
        {
            if (!SanctumScope.IsSanctumLoaded || LoadingProcess.Instance.IsLoadingInProcess)
            {
                return;
            }
            double now = Now;
            if (now < s_NextTick)
            {
                return;
            }
            SanctumState state = State;
            if (state == null || !state.Initialized)
            {
                return;
            }
            s_NextTick = now + TickSeconds;
            state.Clock = now;
            foreach (RuneFocus focus in KruneProjection.Foci)
            {
                RuneFocusState focusState = state.Focus(focus.Id);
                ReportRune(focus, RuneRules.Tick(focus, focusState, now), "window closed");
                if (focus.Kind == RuneKind.Alacrity)
                {
                    ObserveAlacrity(focus, focusState, now);
                }
            }
            foreach (CurseRecord record in state.Curses.Values.ToList())
            {
                UpdateCurse(state, record, now);
            }
            WardKrune.Refresh(state, now);
        }

        public static void ReportRune(RuneFocus focus, RuneOutcome outcome, string what)
        {
            if (outcome.Disabled)
            {
                SanctumState state = State;
                state.RecomputeMask();
                Say($"The {focus.Kind} rune ({focus.Id}) gutters and goes dark.");
                Log.Info($"RUNE {focus.Id} DISABLED by {outcome.Route}: {what}; {outcome.Detail}; mask now {state.RuneMask}.");
                WardKrune.Refresh(state, Now);
                return;
            }
            if (outcome.Pending || outcome.FailedActors.Count > 0 || outcome.Detail.Length > 0)
            {
                Log.Info($"Rune {focus.Id}: {what}: pending={outcome.Pending} failed=[{string.Join(",", outcome.FailedActors.ToArray())}] {outcome.Detail}");
            }
            foreach (string actorId in outcome.FailedActors)
            {
                ApplyFailure(actorId, focus);
            }
        }

        private static void ObserveAlacrity(RuneFocus focus, RuneFocusState focusState, double now)
        {
            UnitEntityData proxy = WardFixtures.Find(focus.Id);
            if (proxy == null)
            {
                return;
            }
            foreach (RuneCondition condition in new[] { RuneCondition.Entangled, RuneCondition.Staggered, RuneCondition.Paralyzed })
            {
                Buff source = ConditionSource(proxy, condition);
                bool active = source != null;
                string actor = source?.Context.MaybeCaster?.UniqueId ?? string.Empty;
                bool origin = source?.Context.SourceAbility != null;
                ReportRune(focus, RuneRules.ObserveCondition(focus, focusState, now, condition, active, actor, origin), condition.ToString());
            }
        }

        private static Buff ConditionSource(UnitEntityData proxy, RuneCondition condition)
        {
            UnitCondition native = condition == RuneCondition.Entangled ? UnitCondition.Entangled : condition == RuneCondition.Staggered ? UnitCondition.Staggered : UnitCondition.Paralyzed;
            if (!proxy.Descriptor.State.HasCondition(native))
            {
                return null;
            }
            foreach (Buff buff in proxy.Buffs.RawFacts)
            {
                if (buff.Blueprint.ComponentsArray.OfType<Kingmaker.UnitLogic.FactLogic.AddCondition>().Any(c => c.Condition == native)
                    || buff.Blueprint.ComponentsArray.OfType<Kingmaker.Designers.Mechanics.Buffs.BuffStatusCondition>().Any(c => c.Condition == native))
                {
                    return buff;
                }
            }
            return null;
        }

        /// <summary>A failed attempt: one real Will save, then at most one new brand on this plate.</summary>
        private static void ApplyFailure(string actorId, RuneFocus focus)
        {
            UnitEntityData actor = Game.Instance.State.Units.FirstOrDefault(u => u.UniqueId == actorId);
            if (actor == null || actor.Descriptor.State.IsDead)
            {
                return;
            }
            SanctumState state = State;
            CurseRecord record = state.Curse(actorId);
            RuleSavingThrow save = Rulebook.Trigger(new RuleSavingThrow(actor, Kingmaker.EntitySystem.Stats.SavingThrowType.Will, WardProfile.CurseSaveDC));
            CurseBrandResult result = CurseRules.Brand(record, focus.Bit, save.IsPassed, Now);
            Log.Info($"Curse: {actor.CharacterName} Will {save.RollResult} vs {WardProfile.CurseSaveDC} on {focus.Id}: {result}; brands {CurseRules.BrandCount(record.PlateMask)}, remove DC {CurseRules.RemoveDC(record.PlateMask)}.");
            if (result == CurseBrandResult.FirstBrand)
            {
                DealCurseDamage(actor);
            }
            if (result == CurseBrandResult.FirstBrand || result == CurseBrandResult.AdditionalBrand)
            {
                Say($"{actor.CharacterName} is branded by the rune's curse.");
                SyncCurseBuff(actor, record);
            }
        }

        private static void DealCurseDamage(UnitEntityData actor)
        {
            UnitEntityData source = WardFixtures.Krune ?? actor;
            var damage = Rulebook.Trigger(new RuleDealStatDamage(source, actor, Kingmaker.EntitySystem.Stats.StatType.Dexterity, new DiceFormula(1, DiceType.D6), 0));
            Log.Info($"Curse damage: {actor.CharacterName} loses {damage.Damage} Dexterity.");
        }

        private static void UpdateCurse(SanctumState state, CurseRecord record, double now)
        {
            UnitEntityData actor = Game.Instance.State.Units.FirstOrDefault(u => u.UniqueId == record.Actor);
            if (actor == null)
            {
                return;
            }
            if (record.PlateMask != 0 && actor.Buffs.GetBuff(WardBlueprints.CurseBuff) == null)
            {
                CurseRules.Remove(record);
                Log.Info($"Curse removed from {actor.CharacterName} by a native cure.");
            }
            int due = CurseRules.DueProgressions(record, now);
            for (int i = 0; i < due; i++)
            {
                DealCurseDamage(actor);
            }
            if (CurseRules.UpdateFatigue(record, actor.Stats.Dexterity.Damage))
            {
                if (record.FatigueActive)
                {
                    actor.Descriptor.AddBuff(WardBlueprints.CurseFatigue, actor);
                }
                else
                {
                    actor.Descriptor.RemoveFact(WardBlueprints.CurseFatigue);
                }
            }
        }

        /// <summary>The curse's removal DC lives on the buff itself, where the native dispel check reads it.</summary>
        private static void SyncCurseBuff(UnitEntityData actor, CurseRecord record)
        {
            Buff buff = actor.Buffs.GetBuff(WardBlueprints.CurseBuff);
            UnitEntityData caster = WardFixtures.Krune ?? actor;
            int dc = CurseRules.RemoveDC(record.PlateMask);
            if (buff == null)
            {
                buff = actor.Descriptor.AddBuff(WardBlueprints.CurseBuff, caster, null, new AbilityParams { CasterLevel = 17, DC = dc, SpellLevel = 7 });
            }
            else
            {
                buff.Context.Params.DC = dc;
            }
        }

        public static void WakeSleeper(UnitEntityData unit)
        {
            SanctumState state = State;
            if (state == null)
            {
                return;
            }
            MistRules.Wake(state.MistOf(unit.UniqueId));
            unit.Descriptor.RemoveFact(WardBlueprints.MistSleep);
            Log.Info($"Mist sleeper woken: {unit.CharacterName}.");
        }

        private static void EnsureDefenseWard(SanctumState state)
        {
            UnitEntityData proxy = WardFixtures.Find("A3c");
            if (proxy == null || state.Focus("A3c").Disabled || proxy.Buffs.GetBuff(WardBlueprints.DefenseWard) != null)
            {
                return;
            }
            UnitEntityData caster = WardFixtures.Krune ?? proxy;
            proxy.Descriptor.AddBuff(WardBlueprints.DefenseWard, caster, null, new AbilityParams { CasterLevel = RuneRules.DispelCasterLevel, SpellLevel = 9, DC = 11 + RuneRules.DispelCasterLevel });
        }

        public static void Say(string message)
        {
            Game.Instance.UI.BattleLogManager?.HandleOnDialogHistory(message);
        }
    }
}
