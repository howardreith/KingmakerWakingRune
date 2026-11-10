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

    /// <summary>The printed rune plates "receive no saving throw to resist effects to which they are vulnerable". Fixture proxies only.</summary>
    [HarmonyPatch(typeof(RuleSavingThrow), nameof(RuleSavingThrow.IsPassed), MethodType.Getter)]
    internal static class FixtureSaveFailPatch
    {
        private static void Postfix(RuleSavingThrow __instance, ref bool __result)
        {
            if (__result && WardFixtures.IdOf(__instance.Initiator) != null)
            {
                __result = false;
            }
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
