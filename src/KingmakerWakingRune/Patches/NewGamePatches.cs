using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.LevelUp.NewGame;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Selection;
using UnityEngine.UI;

namespace KingmakerWakingRune.Patches
{
    [HarmonyPatch(typeof(NewGameWinPhaseStory), nameof(NewGameWinPhaseStory.SetStory))]
    internal static class StorySetStoryPatch
    {
        private static void Postfix(NewGameWinPhaseStory __instance, NewGameRoot.StoryEntity story)
        {
            if (story == null)
            {
                return;
            }
            if (story == ScenarioBlueprints.StoryEntry)
            {
                // Native key art is keyed by DlcType; reuse the base adventure's art instead of an empty image.
                var keyArts = Traverse.Create(__instance).Field("m_KeyArtImages").GetValue<NewGameKeyArt[]>();
                var image = Traverse.Create(__instance).Field("m_KeyArt").GetValue<Image>();
                if (image != null && keyArts != null)
                {
                    image.sprite = keyArts.FirstOrDefault(k => k.Type == DlcType.None)?.KeyArt;
                }
            }
            StartFlow.OnStorySelected(story);
        }
    }

    /// <summary>The New Game window hides only when the player backs out or cancels (never on game entry).</summary>
    [HarmonyPatch(typeof(NewGameWindowController), "OnHide")]
    internal static class NewGameWindowHidePatch
    {
        private static void Postfix()
        {
            StartFlow.Clear("new game flow abandoned");
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
    internal static class GameLoadGamePatch
    {
        private static void Prefix(SaveInfo saveInfo)
        {
            StartFlow.Clear("save load");
            Log.Info($"Loading save '{saveInfo?.Name}' area={saveInfo?.Area?.name}/{saveInfo?.Area?.AssetGuid}");
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.LoadNewGame), typeof(BlueprintAreaPreset), typeof(SaveInfo))]
    internal static class GameLoadNewGamePatch
    {
        private static void Prefix(BlueprintAreaPreset preset)
        {
            StartFlow.OnNewGameStarting(preset);
        }
    }
}
