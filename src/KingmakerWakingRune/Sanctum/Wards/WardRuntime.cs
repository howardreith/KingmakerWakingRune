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
using UnityEngine;

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

        /// <summary>Writes this source's real consumption into the saved grants, so spent points can never be restored.</summary>
        public static void Sync(SanctumState state, double now)
        {
            Buff buff = WardFixtures.Krune?.Buffs.GetBuff(WardBlueprints.FeederTempHp);
            KwrFeederTempHp logic = buff?.SelectComponents<KwrFeederTempHp>().FirstOrDefault();
            if (logic == null)
            {
                return;
            }
            // An older build's buff: whatever the saved grants show beyond what is left was spent.
            int spent = logic.Unreconciled ? System.Math.Max(0, FeederRules.ExposedTemporaryHP(state.Door.Feeder, now) - logic.Remaining) : 0;
            spent += logic.TakeConsumed();
            if (spent > 0)
            {
                int before = FeederRules.ExposedTemporaryHP(state.Door.Feeder, now);
                FeederRules.Consume(state.Door.Feeder, spent, now);
                Log.Info($"Krune receiver: {spent} feeder temporary HP spent; {before} -> {FeederRules.ExposedTemporaryHP(state.Door.Feeder, now)}.");
            }
        }

        public static void Refresh(SanctumState state, double now)
        {
            UnitEntityData krune = WardFixtures.Krune;
            if (krune == null)
            {
                return;
            }
            Sync(state, now);
            FeederRules.Expire(state.Door.Feeder, now);
            int amount = FeederRules.ExposedTemporaryHP(state.Door.Feeder, now);
            Buff buff = krune.Buffs.GetBuff(WardBlueprints.FeederTempHp);
            int shown = buff?.SelectComponents<KwrFeederTempHp>().FirstOrDefault()?.Remaining ?? 0;
            // A spent buff is removed rather than left on the receiver at zero.
            if (buff != null && amount == shown && amount > 0)
            {
                return;
            }
            if (buff == null && amount == 0)
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
        private static readonly WardTurnHandler s_TurnHandler = new WardTurnHandler();
        private static bool s_Subscribed;
        private static double s_NextTick;

        /// <summary>The existing state only: nothing outside <see cref="OnSanctumLoaded"/> may create scenario state.</summary>
        public static SanctumState State => WardStore.Existing;

        public static double Now => Game.Instance.Player.GameTime.TotalSeconds;

        public static void OnSanctumLoaded(bool legacyEntered)
        {
            if (!s_Subscribed)
            {
                EventBus.Subscribe(s_Observer);
                EventBus.Subscribe(s_TurnHandler);
                s_Subscribed = true;
            }
            SanctumState state = SanctumScope.IsSanctumLoaded ? WardStore.Create() : null;
            if (state == null)
            {
                return;
            }
            if (state.Migrate())
            {
                Log.Info($"Sanctum state migrated to version {state.Version}; nothing it recorded was reset.");
            }
            // The entered flag is set on every reload inside the sanctum; only the first initialization decides legacy.
            bool initializedNow = state.Initialize(Now, legacyEntered);
            if (initializedNow)
            {
                Log.Info($"Sanctum state initialized at {Now:0} ({(legacyEntered ? "legacy save" : "fresh")}); profile {state.ProfileSubtier}, {state.ProfilePartyBaseline} PCs, extra challenge {state.ProfileExtraChallenge}.");
            }
            WardHazards.ForgetTurns();
            WardStatues.ForgetStatsProbe();
            WardFixtures.Ensure();
            WardStatues.MigrateMarked(state);
            WardCredentials.Reconcile(state);
            EnsureDefenseWard(state);
            WardTraps.AfterLoad(state);
            WardDoor.ApplyVisual();
            ApplyFlailVisual(state);
            WardStatues.ApplyAll(state, Now);
            EnsureWardMarkers(state);
            WardRitual.EnsureBaseline(state);
            ApplyRuneVisuals(state);
            GrantWake();
            if (initializedNow && state.MigratedLegacy)
            {
                WardHazards.SeedLegacyOccupants(state);
            }
            if (state.MigratedLegacy && !state.LegacyNoticeShown)
            {
                state.LegacyNoticeShown = true;
                Say("The sanctum's wards have awakened since you last stood here. For the full challenge, return to a save made before departure.");
            }
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
            WardFixtures.Protect();
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
            WardHazards.Tick(state, now);
            WardStatues.Tick(state, now);
            WardRitual.Tick(state, now);
            WardTraps.Tick();
            UpdateWardVisibility(state, now);
            RestoreSuppressedFeeder(state, now);
            WardKrune.Refresh(state, now);
        }

        /// <summary>Every load and every disabling rule show the same thing: a dead plate, a dimmed light.</summary>
        private static void ApplyRuneVisuals(SanctumState state)
        {
            foreach (RuneFocus focus in KruneProjection.Foci)
            {
                bool disabled = state.Focus(focus.Id).Disabled;
                if (SanctumDressing.BrokenRunes.TryGetValue(focus.Id, out GameObject[] parts))
                {
                    foreach (GameObject part in parts)
                    {
                        part.SetActive(disabled);
                    }
                }
                if (SanctumDressing.RuneLights.TryGetValue(focus.Id, out Light light) && light != null)
                {
                    light.enabled = !disabled;
                }
            }
        }

        /// <summary>A real door transition: one sound, and the east door is what Vandiana hears.</summary>
        public static void OnDoorTransition(string id, bool opened)
        {
            if (id == "A4Approach" && opened)
            {
                WardRitual.HearDoor();
            }
        }

        public static bool HasBeenExposedToMist()
        {
            SanctumState state = State;
            return state != null && state.Mist.Values.Any(r => r.Exposed);
        }

        public static bool CanSpendWork()
        {
            return CanSpendWork(out _);
        }

        /// <summary>
        /// Long work is a single time jump, so it is refused wherever that jump would skip something that must happen in
        /// order: a fight, anyone standing in the mist, a rune attempt still being timed, a sentry close enough to bite, or a
        /// confrontation already running.
        /// </summary>
        public static bool CanSpendWork(out string refusal)
        {
            SanctumState state = State;
            refusal = null;
            if (state == null)
            {
                refusal = "There is no time for that here.";
            }
            else if (Game.Instance.Player.IsInCombat || TurnBased.Controllers.CombatController.IsInTurnBasedCombat())
            {
                refusal = "Not while there is fighting to be done.";
            }
            else if (state.Ritual.ConfrontationStarted || state.Ritual.AwakeningDue)
            {
                refusal = "The ritual will not wait for slow work now.";
            }
            else if (WardHazards.AnyoneInMist())
            {
                refusal = "Not while anyone stands in the mist.";
            }
            else if (KruneProjection.Foci.Any(f => RuneRules.HasTimedAttempt(state.Focus(f.Id))))
            {
                refusal = "Not while a rune is still answering what was done to it.";
            }
            else if (WardHazards.HoundWouldNotice())
            {
                refusal = "Not with the unseen hound baying so close.";
            }
            if (refusal != null)
            {
                Log.Info($"Work refused: {refusal}");
            }
            return refusal == null;
        }

        /// <summary>
        /// Advances the simulation for completed work, then lets every clock catch up at once: expiries, rune windows, the
        /// hound, temporary hit points and deadlines. A deadline inside the interval stops the work there; false means no reward.
        /// </summary>
        public static bool SpendWork(double seconds, string what)
        {
            SanctumState state = State;
            double now = Now;
            double? remaining = RitualRules.RemainingSeconds(state.Ritual, now);
            WorkPlan plan = WorkRules.Plan(now, seconds, remaining.HasValue ? now + remaining.Value : (double?)null);
            Game.Instance.AdvanceGameTime(TimeSpan.FromSeconds(plan.Advance));
            s_NextTick = 0d;
            Tick();
            Log.Info($"Work: {what} advanced the simulation {plan.Advance:0} of {seconds:0} seconds; {(plan.Completed ? "completed" : "cut short by a deadline")}.");
            return plan.Completed;
        }

        public static void ApplyFlailVisual(SanctumState state)
        {
            if (!state.FlailAcquired)
            {
                return;
            }
            foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>().Where(t => t.name.StartsWith(SanctumDressing.FlailObjectName)))
            {
                t.gameObject.SetActive(false);
            }
        }

        /// <summary>A real dispel result against one of the sanctum's standing wards. The check itself is native.</summary>
        public static void OnWardDispel(string fixtureId, bool success, int check, int dc)
        {
            SanctumState state = State;
            Log.Info($"Ward dispel on {fixtureId}: {check} vs DC {dc}: {(success ? "success" : "failure")}.");
            if (!success)
            {
                return;
            }
            switch (fixtureId)
            {
                case "AlarmWard":
                    state.Alarm.Dispelled = true;
                    WardTraps.Alarm?.Deactivate();
                    break;
                case "GlyphWard":
                    state.Glyph.Dispelled = true;
                    WardTraps.Glyph?.Deactivate();
                    break;
                case "Hound":
                    state.Hound.Dispelled = true;
                    break;
                case WardText.DoorId:
                    // A resetting magical device suppresses for 1d4 rounds rather than being unmade.
                    FeederRules.Suppress(state.Door.Feeder, RulebookEvent.Dice.D4, Now);
                    Log.Info($"Feeder suppressed until {state.Door.Feeder.SuppressedUntil:0}.");
                    break;
            }
        }

        private static void EnsureWardMarkers(SanctumState state)
        {
            Mark("AlarmWard", state.Alarm.IsActive(Now), WardProfile.ArcaneCasterLevel);
            Mark("GlyphWard", !state.Glyph.Triggered && !state.Glyph.Disarmed && !state.Glyph.Dispelled, WardProfile.DivineCasterLevel);
            Mark("Hound", state.Hound.IsActive(Now), WardProfile.ArcaneCasterLevel);
            Mark("DoorA2", !state.Door.Feeder.Disarmed && !state.Door.Destroyed && FeederRules.IsArmed(state.Door.Feeder, Now), WardProfile.FeederStoredCasterLevel);
        }

        private static void Mark(string id, bool wanted, int casterLevel)
        {
            UnitEntityData proxy = WardFixtures.Find(id);
            if (proxy == null)
            {
                return;
            }
            Buff buff = proxy.Buffs.GetBuff(WardBlueprints.WardMarker);
            if (wanted && buff == null)
            {
                SetParams(proxy.Descriptor.AddBuff(WardBlueprints.WardMarker, WardFixtures.Krune ?? proxy, null), casterLevel, 11 + casterLevel, 1);
            }
            else if (!wanted && buff != null)
            {
                buff.Remove();
            }
        }

        /// <summary>Discovered wards become visible targets for Dispel Magic; undiscovered ones stay out of the game entirely.</summary>
        private static void UpdateWardVisibility(SanctumState state, double now)
        {
            SetInGame("AlarmWard", WardTraps.Perceived(WardTraps.Alarm) && state.Alarm.IsActive(now));
            SetInGame("GlyphWard", WardTraps.Perceived(WardTraps.Glyph) && !state.Glyph.Triggered && !state.Glyph.Disarmed && !state.Glyph.Dispelled);
            SetInGame("Hound", state.Hound.FirstBarkAt >= 0d && state.Hound.IsActive(now));
        }

        private static void SetInGame(string id, bool wanted)
        {
            UnitEntityData proxy = WardFixtures.Find(id);
            if (proxy != null && proxy.IsInGame != wanted)
            {
                proxy.IsInGame = wanted;
            }
        }

        private static void RestoreSuppressedFeeder(SanctumState state, double now)
        {
            if (state.Door.Feeder.SuppressedUntil >= 0d && now >= state.Door.Feeder.SuppressedUntil)
            {
                state.Door.Feeder.SuppressedUntil = -1d;
                Mark("DoorA2", !state.Door.Feeder.Disarmed && !state.Door.Destroyed, WardProfile.FeederStoredCasterLevel);
                Log.Info("Feeder suppression ended.");
            }
        }

        public static void ReportRune(RuneFocus focus, RuneOutcome outcome, string what)
        {
            if (outcome.Disabled)
            {
                SanctumState state = State;
                state.RecomputeMask();
                ApplyRuneVisuals(state);
                Say($"The {focus.Kind} rune ({focus.Id}) gutters and goes dark.");
                Log.Info($"RUNE {focus.Id} DISABLED by {outcome.Route}: {what}; {outcome.Detail}; mask now {state.RuneMask}.");
                WardKrune.Refresh(state, Now);
                return;
            }
            if (outcome.FailedActors.Count > 0 || outcome.Detail.Length > 0)
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
                buff = actor.Descriptor.AddBuff(WardBlueprints.CurseBuff, caster, null);
                SetParams(buff, 17, dc, 7);
            }
            else
            {
                SetParams(buff, buff.Context.Params.CasterLevel, dc, buff.Context.Params.SpellLevel);
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
            SetParams(proxy.Descriptor.AddBuff(WardBlueprints.DefenseWard, caster, null), RuneRules.DispelCasterLevel, 11 + RuneRules.DispelCasterLevel, 9);
        }


        /// <summary>The standard action to wake an adjacent sleeper, on every party member while they are here.</summary>
        private static void GrantWake()
        {
            foreach (UnitEntityData unit in Game.Instance.Player.Party)
            {
                if (!unit.Descriptor.HasFact(WardBlueprints.WakeAlly))
                {
                    unit.Descriptor.AddFact<Ability>(WardBlueprints.WakeAlly);
                }
            }
        }

        public static void OnLeavingSanctum()
        {
            foreach (UnitEntityData unit in Game.Instance.Player.Party)
            {
                unit.Descriptor.RemoveFact(WardBlueprints.WakeAlly);
            }
        }
        /// <summary>The native add ignores its parameters, so a buff's caster level and DC are set on its context here.</summary>
        private static void SetParams(Buff buff, int casterLevel, int dc, int spellLevel)
        {
            if (buff != null)
            {
                Harmony12.AccessTools.Field(typeof(Kingmaker.UnitLogic.Mechanics.MechanicsContext), "m_Params").SetValue(buff.Context, new AbilityParams { CasterLevel = casterLevel, DC = dc, SpellLevel = spellLevel });
            }
        }

        public static void Say(string message)
        {
            Game.Instance.UI.BattleLogManager?.HandleOnDialogHistory(message);
        }
    }
}
