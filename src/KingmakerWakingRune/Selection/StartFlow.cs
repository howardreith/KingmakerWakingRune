using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Root;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Lodge;

namespace KingmakerWakingRune.Selection
{
    /// <summary>
    /// Temporary menu intent. Selecting the story sets the native Game.NewGamePreset to the mod
    /// preset (via the native selector); this service only clears that intent when the flow is
    /// abandoned or a save is loaded, and logs the handoff. Durable identity lives in the save.
    /// </summary>
    internal static class StartFlow
    {
        public static bool IsPending => ScenarioBlueprints.IsRegistered && Game.NewGamePreset == ScenarioBlueprints.Preset;

        public static void OnStorySelected(NewGameRoot.StoryEntity story)
        {
            Log.Info($"Story selected: type={story.Type} title='{story.Title}' preset={Game.NewGamePreset?.name} waking={IsPending}");
        }

        public static void Clear(string reason)
        {
            if (!IsPending)
            {
                return;
            }
            Game.NewGamePreset = BlueprintRoot.Instance.NewGamePreset;
            Log.Info($"Pending Waking Rune selection cleared ({reason}); preset={Game.NewGamePreset?.name}");
        }

        public static void OnNewGameStarting(BlueprintAreaPreset preset)
        {
            if (!ScenarioBlueprints.IsRegistered || preset != ScenarioBlueprints.Preset)
            {
                return;
            }
            Log.Info($"Character creation complete; starting Waking Rune: player={LodgeSession.Describe(Game.NewGameUnit)} " +
                     $"area={preset.Area.name} enter={preset.EnterPoint.AssetGuid} difficulty={DifficultyReport.Describe()}");
        }
    }
}
