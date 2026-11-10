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
