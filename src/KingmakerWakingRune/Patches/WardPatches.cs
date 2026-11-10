using System.Linq;
using Harmony12;
using Kingmaker.Controllers;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Utility;
using KingmakerWakingRune.Sanctum.Wards;

namespace KingmakerWakingRune.Patches
{
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
            string id = view != null ? WardFixtures.IdOf(view.EntityData) : null;
            if (id == null || Kingmaker.Game.Instance.Player.IsInCombat || !WardFixtures.Clickable(id))
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
