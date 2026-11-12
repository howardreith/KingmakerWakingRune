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
    /// Vandiana's darkness (WR-06), bounded: a creature standing in it has concealment (20%) against an attacker without
    /// darkvision within 60 feet. Only a target carrying the darkness buff is touched; every other check is native and unchanged.
    /// </summary>
    [HarmonyPatch(typeof(Kingmaker.UnitLogic.Parts.UnitPartConcealment), nameof(Kingmaker.UnitLogic.Parts.UnitPartConcealment.Calculate))]
    internal static class DarknessConcealmentPatch
    {
        private static void Postfix(UnitEntityData initiator, UnitEntityData target, ref Kingmaker.Enums.Concealment __result)
        {
            if (__result != Kingmaker.Enums.Concealment.None || target == null || initiator == null || Sanctum.Lashmistress.LashmistressBlueprints.InDarkness == null)
            {
                return;
            }
            if (target.Buffs.GetBuff(Sanctum.Lashmistress.LashmistressBlueprints.InDarkness) == null)
            {
                return;
            }
            bool seesInDark = (initiator.Descriptor.HasFact(Sanctum.Lashmistress.LashmistressBlueprints.DarkvisionSense) || initiator.Buffs.GetBuff(Sanctum.Lashmistress.LashmistressBlueprints.DarkvisionBuff) != null)
                && initiator.DistanceTo(target) <= 60f * Sanctum.SanctumPlan.MetersPerFoot;
            if (!seesInDark)
            {
                __result = Kingmaker.Enums.Concealment.Partial;
            }
        }
    }

    /// <summary>The ward clock's only per-tick hook: native code runs this in simulation modes alone, never in pause or menus.</summary>
    [HarmonyPatch(typeof(TimeController), "Tick")]
    internal static class TimeControllerTickPatch
    {
        private static void Postfix()
        {
            WardRuntime.Tick();
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
