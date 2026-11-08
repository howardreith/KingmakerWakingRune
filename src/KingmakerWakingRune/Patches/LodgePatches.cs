using System.Collections.Generic;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Persistence.Scenes;
using Kingmaker.View;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Lodge;

namespace KingmakerWakingRune.Patches
{
    [HarmonyPatch(typeof(SceneLoader), "CreateSceneEntities")]
    internal static class SceneLoaderCreateEntitiesPatch
    {
        private static void Prefix(SceneEntitiesState state, ref IEnumerable<EntityViewBase> allViews)
        {
            allViews = LodgeScope.Filter(state, allViews);
        }
    }

    [HarmonyPatch(typeof(SceneLoader), "LoadSceneEntities")]
    internal static class SceneLoaderLoadEntitiesPatch
    {
        private static void Prefix(SceneEntitiesState state, ref IEnumerable<EntityViewBase> viewList)
        {
            viewList = LodgeScope.Filter(state, viewList);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.MoveCharacters))]
    internal static class PlayerMoveCharactersPatch
    {
        private static void Prefix(BlueprintAreaEnterPoint enterPoint)
        {
            LodgeEntry.EnsureMarker(enterPoint);
        }
    }

    /// <summary>
    /// BlueprintRoot.StartGameActions is main-campaign companion setup; it is not run for a Waking Rune
    /// start. Every other action list, and every other campaign's start, is untouched.
    /// </summary>
    [HarmonyPatch(typeof(ActionList), nameof(ActionList.Run), new System.Type[0])]
    internal static class RootStartGameActionsPatch
    {
        private static bool Prefix(ActionList __instance)
        {
            if (__instance == null || __instance != ScenarioBlueprints.RootStartGameActions
                || Game.Instance.Player.StartPreset != ScenarioBlueprints.Preset)
            {
                return true;
            }
            Log.Info($"Skipped main-campaign BlueprintRoot.StartGameActions ({__instance.Actions.Length} actions) for the Waking Rune start.");
            return false;
        }
    }
}
