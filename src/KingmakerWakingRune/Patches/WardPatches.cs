using System.Linq;
using Harmony12;
using Kingmaker.Controllers;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using KingmakerWakingRune.Sanctum.Wards;

namespace KingmakerWakingRune.Patches
{
    /// <summary>
    /// Vandiana's darkness (WR-06): a creature standing in it has partial concealment (20%), as if the native concealment
    /// list held one more entry. Every native reason a partial concealment would not count is honoured (the attacker ignores
    /// concealment or has blindsight in reach, the target cannot benefit, the attacker ignores partial concealment), and it
    /// is seen through by darkvision within 60 feet and by true seeing. Only a target carrying the darkness buff is touched.
    /// </summary>
    [HarmonyPatch(typeof(Kingmaker.UnitLogic.Parts.UnitPartConcealment), nameof(Kingmaker.UnitLogic.Parts.UnitPartConcealment.Calculate))]
    internal static class DarknessConcealmentPatch
    {
        private static readonly System.Reflection.FieldInfo s_Blindsight = AccessTools.Field(typeof(Kingmaker.UnitLogic.Parts.UnitPartConcealment), "m_BlindsightRanges");
        private static readonly System.Collections.Generic.Dictionary<string, bool> s_LastLogged = new System.Collections.Generic.Dictionary<string, bool>();

        private static void Postfix(UnitEntityData initiator, UnitEntityData target, bool attack, ref Kingmaker.Enums.Concealment __result)
        {
            var darkness = Sanctum.Lashmistress.LashmistressBlueprints.InDarkness;
            if (target == null || initiator == null || darkness == null || target.Buffs.GetBuff(darkness) == null)
            {
                return;
            }
            var mine = initiator.Get<Kingmaker.UnitLogic.Parts.UnitPartConcealment>();
            var theirs = target.Get<Kingmaker.UnitLogic.Parts.UnitPartConcealment>();
            var facts = new Sanctum.Lashmistress.DarknessFacts
            {
                TargetInDarkness = true,
                AttackerIgnoresAllConcealment = mine != null && mine.IgnoreAll,
                AttackerBlindsightReaches = BlindsightReaches(mine, initiator, target),
                TargetCannotBenefit = theirs != null && theirs.Disable,
                AttackerIgnoresPartial = mine != null && mine.IgnorePartial,
                AttackerDarkvisionWithin60 = Sanctum.Lashmistress.KwrDarkvisionSense.Present(initiator)
                    && initiator.DistanceTo(target) <= 60f * Sanctum.SanctumPlan.MetersPerFoot,
                AttackerTrueSeeing = initiator.Descriptor.State.HasCondition(UnitCondition.TrueSeeing),
            };
            bool none = __result == Kingmaker.Enums.Concealment.None;
            bool adds = Sanctum.Lashmistress.DarknessRule.AddsConcealment(none, facts);
            if (adds)
            {
                __result = Kingmaker.Enums.Concealment.Partial;
            }
            if (attack)
            {
                // Evidence: each attacker and target pair is logged when its answer changes.
                string pair = initiator.UniqueId + ">" + target.UniqueId;
                if (!s_LastLogged.TryGetValue(pair, out bool last) || last != adds)
                {
                    s_LastLogged[pair] = adds;
                    Sanctum.Lashmistress.LashmistressEncounter.Note($"darkness: {initiator.CharacterName} attacking {target.CharacterName}: {(adds ? "partial concealment (20%)" : "no darkness concealment")} ({Sanctum.Lashmistress.DarknessRule.Why(none, facts)}); result {__result}.");
                }
            }
        }

        private static bool BlindsightReaches(Kingmaker.UnitLogic.Parts.UnitPartConcealment part, UnitEntityData initiator, UnitEntityData target)
        {
            var ranges = part == null ? null : s_Blindsight.GetValue(part) as System.Collections.Generic.List<Feet>;
            if (ranges == null || ranges.Count == 0)
            {
                return false;
            }
            float meters = ranges.Max(r => r.Meters);
            return initiator.DistanceTo(target) - (initiator.View.Corpulence + target.View.Corpulence) <= meters;
        }
    }

    /// <summary>
    /// A potion is drunk by its holder on itself. The game's own target check refuses a spell whose blueprint cannot target
    /// allies (Blur) on any creature outside the player's faction, the drinker included, so a Blackstrike could never drink
    /// the Blur potion it carries. Only an A4 defender's own potion, on itself, is let through; everything else is native.
    /// </summary>
    [HarmonyPatch(typeof(Kingmaker.UnitLogic.Abilities.AbilityData), nameof(Kingmaker.UnitLogic.Abilities.AbilityData.CanTarget))]
    internal static class DefenderOwnPotionPatch
    {
        private static void Postfix(Kingmaker.UnitLogic.Abilities.AbilityData __instance, TargetWrapper target, ref bool __result)
        {
            if (__result || target?.Unit == null || __instance.Caster?.Unit != target.Unit)
            {
                return;
            }
            var usable = __instance.SourceItemUsableBlueprint;
            if (usable != null && usable.Type == Kingmaker.Blueprints.Items.Equipment.UsableItemType.Potion && !__instance.PotionForOther && Sanctum.Lashmistress.LashmistressEncounter.IsDefender(target.Unit)
                && !target.Unit.Descriptor.State.IsDead)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// Disruptive (the Blackstrikes): a foe casting defensively while one of them threatens it faces a DC 4 higher, on the
    /// game's own defensive-casting check.
    /// </summary>
    [HarmonyPatch(typeof(Kingmaker.RuleSystem.Rules.Abilities.RuleCheckCastingDefensively), nameof(Kingmaker.RuleSystem.Rules.Abilities.RuleCheckCastingDefensively.OnTrigger))]
    internal static class DisruptivePatch
    {
        private static readonly System.Reflection.PropertyInfo s_DC = AccessTools.Property(typeof(Kingmaker.RuleSystem.Rules.Abilities.RuleCheckCastingDefensively), "DC");

        private static void Postfix(Kingmaker.RuleSystem.Rules.Abilities.RuleCheckCastingDefensively __instance)
        {
            var disruptive = Sanctum.Lashmistress.LashmistressBlueprints.Disruptive;
            UnitEntityData caster = __instance.Initiator;
            if (disruptive == null || caster == null)
            {
                return;
            }
            UnitEntityData by = caster.CombatState.EngagedBy.FirstOrDefault(u => u != null && u.Descriptor.State.IsConscious && u.Descriptor.HasFact(disruptive) && u.IsEnemy(caster));
            if (by == null)
            {
                return;
            }
            int before = __instance.DC;
            s_DC.SetValue(__instance, before + Sanctum.Lashmistress.ConjurerRules.DisruptiveBonus, null);
            Sanctum.Lashmistress.LashmistressEncounter.Note($"Disruptive: {caster.CharacterName} casts {__instance.Spell?.Blueprint.name} defensively while {by.CharacterName} threatens: DC {before} -> {__instance.DC}; roll {__instance.ResultRoll} {(__instance.Success ? "succeeds" : "fails")}.");
        }
    }

    /// <summary>
    /// Her Sanctuary, for commands that target her: when a foe's attack or harmful spell command comes to her, that foe makes
    /// its Will save (once for the casting); a foe that failed cannot target her, so its command is lost and its brain chooses
    /// another target. Area effects and rules (damage from a burst) are not commands at her and are untouched.
    /// </summary>
    [HarmonyPatch(typeof(Kingmaker.UnitLogic.Commands.Base.UnitCommand), nameof(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandTargetUntargetable))]
    internal static class SanctuaryTargetPatch
    {
        private static void Postfix(Kingmaker.EntitySystem.EntityDataBase sourceEntity, UnitEntityData targetUnit, Kingmaker.RuleSystem.RulebookEvent evt, ref bool __result)
        {
            var sanctuary = Sanctum.Lashmistress.LashmistressBlueprints.SanctuaryBuff;
            if (__result || evt != null || sanctuary == null || targetUnit == null || !(sourceEntity is UnitEntityData attacker))
            {
                return;
            }
            Kingmaker.UnitLogic.Buffs.Buff buff = targetUnit.Buffs.GetBuff(sanctuary);
            var ward = buff?.SelectComponents<Sanctum.Lashmistress.KwrSanctuaryWard>().FirstOrDefault();
            if (ward == null)
            {
                return;
            }
            Kingmaker.UnitLogic.Commands.Base.UnitCommand ticking = SanctuaryCommandTick.Current;
            bool thisCommand = ticking != null && ticking.Executor == attacker && ticking.TargetUnit == targetUnit;
            bool? hostile = thisCommand ? SanctuaryCommandTick.IsHostile(ticking) : (bool?)null;
            bool enemy = attacker.IsEnemy(targetUnit);
            if (hostile == true && enemy && !ward.KnownBarred(attacker))
            {
                // A real attempt: its save, once for this casting.
                __result = ward.Attempt(attacker, "act against her (" + ticking.GetType().Name + ")");
                return;
            }
            // A kept failure bars only a current enemy's hostile targeting: never a heal or other help, never once she is no
            // longer its enemy.
            __result = Sanctum.Lashmistress.SanctuaryRule.BarsTargeting(ward.KnownBarred(attacker), enemy, hostile);
            if (__result && thisCommand)
            {
                ward.NoteBarred(attacker, ticking.GetType().Name);
            }
        }
    }

    /// <summary>Which command the native action controller is checking, so Sanctuary knows a real attempt from a brain's survey.</summary>
    [HarmonyPatch(typeof(Kingmaker.Controllers.Units.UnitActionController), "TickCommand")]
    internal static class SanctuaryCommandTick
    {
        public static Kingmaker.UnitLogic.Commands.Base.UnitCommand Current;

        public static bool IsHostile(Kingmaker.UnitLogic.Commands.Base.UnitCommand command)
        {
            if (command is Kingmaker.UnitLogic.Commands.UnitAttack)
            {
                return true;
            }
            return command is Kingmaker.UnitLogic.Commands.UnitUseAbility use && use.Spell?.Blueprint.EffectOnEnemy == Kingmaker.UnitLogic.Abilities.Blueprints.AbilityEffectOnUnit.Harmful;
        }

        private static void Prefix(Kingmaker.UnitLogic.Commands.Base.UnitCommand command)
        {
            Current = command;
        }

        private static void Postfix()
        {
            Current = null;
        }
    }

    /// <summary>
    /// A full rest heals her whip's nonlethal damage. Only a creature with the whip's ledger is touched, and only by the
    /// whip's share; any other nonlethal damage is left exactly as the game leaves it.
    /// </summary>
    [HarmonyPatch(typeof(Kingmaker.Controllers.Rest.RestController), nameof(Kingmaker.Controllers.Rest.RestController.ApplyRest))]
    internal static class NonlethalRestPatch
    {
        private static void Postfix(UnitDescriptor unit)
        {
            if (unit != null)
            {
                Sanctum.Lashmistress.WhipNonlethal.Rest(unit);
            }
        }
    }

    /// <summary>
    /// When the game revives the fallen after a fight, her whip's nonlethal damage no longer holds a creature down. Only the
    /// whip's share is reduced, and only on a creature with its ledger.
    /// </summary>
    [HarmonyPatch(typeof(Kingmaker.Controllers.Units.UnitReturnToConsciousController), nameof(Kingmaker.Controllers.Units.UnitReturnToConsciousController.MakeUnitConscious))]
    internal static class NonlethalRevivePatch
    {
        private static void Postfix(UnitEntityData unit)
        {
            if (unit != null)
            {
                Sanctum.Lashmistress.WhipNonlethal.Revive(unit);
            }
        }
    }

    /// <summary>The ward clock's only per-tick hook: native code runs this in simulation modes alone, never in pause or menus.</summary>
    [HarmonyPatch(typeof(TimeController), "Tick")]
    internal static class TimeControllerTickPatch
    {
        /// <summary>A frame that runs while Krune's prototype checkpoint holds (after a load) pauses the game again at once.</summary>
        private static void Prefix()
        {
            if (!Kingmaker.Game.Instance.IsPaused && Sanctum.Krune.KruneAwakening.CheckpointHolds)
            {
                Kingmaker.Game.Instance.IsPaused = true;
            }
        }

        private static void Postfix()
        {
            WardRuntime.Tick();
        }
    }

    /// <summary>
    /// The WR-07A prototype checkpoint pauses the whole simulation, both sides alike: while it holds, nothing unpauses the
    /// game. Inspection, menus and saving (where the game allows) still work. Outside a checkpoint save this does nothing.
    /// </summary>
    [HarmonyPatch(typeof(Kingmaker.Game), "set_IsPaused")]
    internal static class KruneCheckpointPausePatch
    {
        private static bool Prefix(bool value)
        {
            return value || !Sanctum.Krune.KruneAwakening.CheckpointHolds;
        }
    }

    /// <summary>
    /// Out of combat, a click on a fixture proxy opens its inspection instead of the native attack on a neutral. In combat,
    /// or with a spell selected, it is untouched: the proxy is then an ordinary native target.
    /// </summary>
    [HarmonyPatch(typeof(Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler), nameof(Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler.OnClick))]
    internal static class FixtureClickPatch
    {
        private static bool Prefix(UnityEngine.GameObject gameObject, int button, bool simulate, ref bool __result)
        {
            if (simulate || button != 0)
            {
                return true;
            }
            var view = gameObject.GetComponent<Kingmaker.View.UnitEntityView>();
            if (view == null || !FixtureCursorPatch.Examines(view.EntityData))
            {
                return true;
            }
            UnitEntityData actor = Kingmaker.Game.Instance.UI.SelectionManager.GetNearestSelectedUnit(view.transform.position);
            if (actor == null)
            {
                return true;
            }
            actor.Commands.Run(new Kingmaker.UnitLogic.Commands.UnitInteractWithUnit(view.EntityData) { CreatedByPlayer = true });
            __result = true;
            return false;
        }
    }

    /// <summary>
    /// The cursor agrees with <see cref="FixtureClickPatch"/>: hovering authored scenery out of combat shows the native
    /// magnifying glass, because a click there examines it. An aimed spell, combat (where a click attacks natively) and
    /// turn-based combat keep the native cursor, and so does every other unit, a restored cultist included.
    /// </summary>
    [HarmonyPatch(typeof(Kingmaker.UI.CursorController), nameof(Kingmaker.UI.CursorController.SetUnitCursor))]
    internal static class FixtureCursorPatch
    {
        private static readonly System.Reflection.FieldInfo MoveOn = AccessTools.Field(typeof(Kingmaker.UI.CursorController), "m_MoveOn");
        private static readonly System.Reflection.FieldInfo CastMode = AccessTools.Field(typeof(Kingmaker.UI.CursorController), "m_CastMode");
        private static readonly System.Reflection.MethodInfo SetCursor = AccessTools.Method(typeof(Kingmaker.UI.CursorController), "SetCursor");

        private static void Postfix(Kingmaker.UI.CursorController __instance, bool isUnitHighlighted, UnitEntityData unit)
        {
            if (!isUnitHighlighted || !Examines(unit) || (bool)MoveOn.GetValue(__instance) || (bool)CastMode.GetValue(__instance))
            {
                return;
            }
            SetCursor.Invoke(__instance, new object[] { Kingmaker.Game.Instance.BlueprintRoot.Cursors.InfoCursor, null, false, false, null });
        }

        /// <summary>A plain click on this unit examines it rather than attacking or selecting a target.</summary>
        public static bool Examines(UnitEntityData unit)
        {
            string id = WardFixtures.IdOf(unit);
            return id != null
                && WardFixtures.Clickable(id)
                && !Kingmaker.Game.Instance.Player.IsInCombat
                && !TurnBased.Controllers.CombatController.IsInTurnBasedCombat()
                && Kingmaker.Game.Instance.ClickEventsController?.Mode == Kingmaker.Controllers.Clicks.PointerMode.Default;
        }
    }

    /// <summary>
    /// The printed rune plates "receive no saving throw to resist effects to which they are vulnerable": only a rune plate,
    /// and only against an effect of the kind its own routes name, by descriptor or by what the effect does (Harm's
    /// negative energy has no descriptor). Statues, props, Krune and Vandiana save normally.
    /// </summary>
    [HarmonyPatch(typeof(RuleSavingThrow), nameof(RuleSavingThrow.IsPassed), MethodType.Getter)]
    internal static class FixtureSaveFailPatch
    {
        private static readonly System.Collections.Generic.HashSet<RuleSavingThrow> s_Logged = new System.Collections.Generic.HashSet<RuleSavingThrow>();

        private static void Postfix(RuleSavingThrow __instance, ref bool __result)
        {
            string id = WardFixtures.IdOf(__instance.Initiator);
            if (!__result || id == null)
            {
                return;
            }
            var context = __instance.Reason?.Context;
            long descriptor = (long)(context?.SpellDescriptor ?? Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.None);
            RuneEffect effects = RuneEffectScan.Of(context);
            if (!RuneVulnerability.Denies(id, descriptor, effects))
            {
                return;
            }
            __result = false;
            if (s_Logged.Count > 64)
            {
                s_Logged.Clear();
            }
            if (s_Logged.Add(__instance))
            {
                Log.Info($"Rune save denied: {id} {__instance.Type} {__instance.RollResult} vs DC {__instance.DifficultyClass} from {context?.AssociatedBlueprint?.name}; descriptor {context?.SpellDescriptor}; effects {effects}.");
            }
        }
    }

    /// <summary>
    /// A rune plate makes no attempt to break free of an effect its own routes name, as it rolls no save against one: the
    /// printed plates do not resist what they are vulnerable to, and an object has no will to struggle. Native Entangle
    /// otherwise has the plate roll an escape check every round, so ten unbroken rounds were left to the plate's dice.
    /// </summary>
    [HarmonyPatch(typeof(Kingmaker.UnitLogic.UnitHelper), nameof(Kingmaker.UnitLogic.UnitHelper.TryBreakFree))]
    internal static class FixtureBreakFreePatch
    {
        private static object s_LastLogged;

        private static bool Prefix(UnitEntityData _this, Kingmaker.UnitLogic.Mechanics.MechanicsContext context, ref bool __result)
        {
            string id = WardFixtures.IdOf(_this);
            if (id == null)
            {
                return true;
            }
            long descriptor = (long)(context?.SpellDescriptor ?? Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.None);
            if (!RuneVulnerability.Denies(id, descriptor, RuneEffectScan.Of(context)))
            {
                return true;
            }
            __result = false;
            if (s_LastLogged != context)
            {
                s_LastLogged = context;
                Log.Info($"Rune break-free refused: {id} against {context?.AssociatedBlueprint?.name}; descriptor {context?.SpellDescriptor}.");
            }
            return false;
        }
    }

    /// <summary>
    /// Ability damage on a fixture is a route signal, not harm to a creature: the rune rules read the rolled damage, and the
    /// object's own scores are put back at once. The native rule kills any creature whose score falls below 1, and a plate
    /// has the Dexterity of an object, so one point of a summon's poison would otherwise destroy the plate and its rune.
    /// </summary>
    [HarmonyPatch(typeof(RuleDealStatDamage), nameof(RuleDealStatDamage.OnTrigger))]
    internal static class FixtureStatDamagePatch
    {
        private static void Postfix(RuleDealStatDamage __instance)
        {
            string id = WardFixtures.IdOf(__instance.Target);
            if (id == null || __instance.Stat == null || __instance.Damage <= 0)
            {
                return;
            }
            if (__instance.IsDrain)
            {
                __instance.Stat.Drain = System.Math.Max(0, __instance.Stat.Drain - __instance.Damage);
            }
            else
            {
                __instance.Stat.Damage = System.Math.Max(0, __instance.Stat.Damage - __instance.Damage);
            }
            __instance.Target.Descriptor.State.MarkedForDeath = false;
            Log.Info($"Fixture {id}: {__instance.Damage} {__instance.Stat.Type} {(__instance.IsDrain ? "drain" : "damage")} recorded for its rune; the object's own score is unchanged.");
        }
    }

    /// <summary>A death effect is observed on a rune plate as a route, but a fixture never dies of it: its target must remain.</summary>
    [HarmonyPatch(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionKillTarget), nameof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionKillTarget.RunAction))]
    internal static class FixtureNoKillPatch
    {
        private static bool Prefix(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionKillTarget __instance)
        {
            return WardFixtures.IdOf(Kingmaker.ElementsSystem.ElementsContext.GetData<Kingmaker.UnitLogic.Mechanics.MechanicsContext.Data>()?.CurrentTarget?.Unit) == null;
        }
    }

    /// <summary>
    /// Every turn-based turn ends by disposing its controller: a completed turn, the last turn as combat ends, a unit
    /// leaving combat or a switch to real time. A delay disposes it too, which the hazards tell apart.
    /// </summary>
    [HarmonyPatch(typeof(TurnBased.Controllers.TurnController), nameof(TurnBased.Controllers.TurnController.Dispose))]
    internal static class TurnEndPatch
    {
        private static object s_Last;

        private static void Prefix(TurnBased.Controllers.TurnController __instance)
        {
            // Ordinary campaign turns pass straight through, touching no scenario state.
            if (!KingmakerWakingRune.Sanctum.SanctumScope.IsSanctumLoaded || ReferenceEquals(s_Last, __instance))
            {
                return;
            }
            s_Last = __instance;
            try
            {
                WardHazards.TurnEnded(__instance.Unit, __instance.Status == TurnBased.Controllers.TurnController.TurnStatus.Ended);
            }
            catch (System.Exception ex)
            {
                Log.Error("Ward turn end failed.", ex);
            }
        }
    }

    /// <summary>
    /// The mist sleep's whole lifecycle in the log: how the native apply rule treated it (immunity, the difficulty preset's
    /// limits on negative effects) and, on removal, the call path that removed it. Read only.
    /// </summary>
    [HarmonyPatch(typeof(RuleApplyBuff), nameof(RuleApplyBuff.OnTrigger))]
    internal static class MistSleepApplyTrace
    {
        private static void Postfix(RuleApplyBuff __instance)
        {
            if (__instance.Blueprint == WardBlueprints.MistSleep)
            {
                Log.Info($"Mist sleep apply on {__instance.Initiator.CharacterName}: applied={__instance.AppliedBuff != null} canApply={__instance.CanApply} immunity={__instance.Immunity} storyModeImmunity={__instance.StoryModeImmunity} duration={(__instance.Duration.HasValue ? __instance.Duration.Value.TotalSeconds.ToString("0") + "s" : "permanent")} difficultyStats={Kingmaker.Game.Instance.Player.Difficulty.StatsAdjustmentsType} inCombat={__instance.Initiator.IsInCombat}.");
            }
        }
    }

    /// <summary>
    /// The native disarm approach to a trap with a trigger zone stops where the next step would enter the zone, and the
    /// disarm is worked from there. The sanctum's alarm and glyph are polled areas, not native zones, so the same rule is
    /// applied against their own areas; without it the disarming body walks in and sets off the ward it came to disarm.
    /// </summary>
    [HarmonyPatch(typeof(Kingmaker.UnitLogic.Commands.UnitInteractWithObject), "UpdateTarget")]
    internal static class WardDisarmApproachPatch
    {
        private static Kingmaker.UnitLogic.Commands.UnitInteractWithObject s_LastSeen;

        private static void Postfix(Kingmaker.UnitLogic.Commands.UnitInteractWithObject __instance)
        {
            if (__instance.OverrideTarget.HasValue || !(__instance.Interaction is Kingmaker.View.MapObjects.DisableTrapInteractionComponent))
            {
                return;
            }
            var view = __instance.Interaction.MapObject as Kingmaker.View.MapObjects.TrapObjectView;
            UnitEntityData executor = __instance.Executor;
            if (view == null || executor?.View == null)
            {
                return;
            }
            UnityEngine.Vector3 direction = (view.transform.position - executor.Position).normalized;
            UnityEngine.Vector3 ahead = executor.Position + direction * (executor.View.Corpulence + 0.25f);
            if (s_LastSeen != __instance)
            {
                s_LastSeen = __instance;
                Log.Info($"Ward disarm approach: {executor.CharacterName} toward {view.UniqueId}.");
            }
            if (WardTraps.InTriggerArea(view, ahead))
            {
                __instance.OverrideTarget = executor.Position + direction * 0.01f;
                Log.Info($"Ward disarm: {executor.CharacterName} works {view.name} from the edge of its area.");
            }
        }
    }

    [HarmonyPatch(typeof(Kingmaker.UnitLogic.Buffs.Buff), nameof(Kingmaker.UnitLogic.Buffs.Buff.OnRemove))]
    internal static class MistSleepRemoveTrace
    {
        private static void Prefix(Kingmaker.UnitLogic.Buffs.Buff __instance)
        {
            if (__instance.Blueprint != WardBlueprints.MistSleep)
            {
                return;
            }
            string path = string.Join(" < ", new System.Diagnostics.StackTrace(1, false).GetFrames().Take(14).Select(f => f.GetMethod()).Where(m => m != null).Select(m => m.DeclaringType?.Name + "." + m.Name).ToArray());
            string unitId = __instance.Owner?.Unit?.UniqueId;
            if (unitId != null)
            {
                WardHazards.NoteSleepRemoval(unitId, path);
            }
            Log.Info($"Mist sleep removed from {__instance.Owner?.CharacterName} at {Kingmaker.Game.Instance.TimeController.GameTime.TotalSeconds:0}: endTime={(__instance.IsPermanent ? "permanent" : __instance.EndTime.TotalSeconds.ToString("0"))} inCombat={__instance.Owner?.Unit?.IsInCombat}; path {path}.");
        }
    }

    /// <summary>Lets the stock Stone to Flesh reach the sanctum's statue proxies, and nothing else it would not already reach.</summary>
    [HarmonyPatch(typeof(AbilityTargetStoneToFlesh), nameof(AbilityTargetStoneToFlesh.CanTarget))]
    internal static class StoneToFleshTargetPatch
    {
        private static void Postfix(UnitEntityData caster, TargetWrapper target, ref bool __result)
        {
            if (!__result && target.Unit != null && IsStatueSlot(WardFixtures.IdOf(target.Unit)))
            {
                __result = true;
            }
        }

        private static bool IsStatueSlot(string id)
        {
            return id != null && System.Linq.Enumerable.Any(KingmakerWakingRune.Sanctum.StatueManifest.Slots, s => s.Id == id);
        }
    }
}

namespace KingmakerWakingRune.Patches
{
    /// <summary>Scenery is not a creature to be studied in the bestiary, so a proxy never feeds a knowledge roll or the log.</summary>
    [HarmonyPatch(typeof(Kingmaker.Inspect.InspectUnitsHelper), nameof(Kingmaker.Inspect.InspectUnitsHelper.IsInspectAllow))]
    internal static class FixtureNoInspectPatch
    {
        private static void Postfix(UnitEntityData unit, ref bool __result)
        {
            if (__result && WardFixtures.IdOf(unit) != null)
            {
                __result = false;
            }
        }
    }
}
