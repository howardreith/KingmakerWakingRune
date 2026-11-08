using Harmony12;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Persistence.Scenes;
using KingmakerWakingRune.Lodge;

namespace KingmakerWakingRune.Patches
{
    /// <summary>Provides the runtime lodge mechanics scene right before the native loader requests it.</summary>
    [HarmonyPatch(typeof(SceneLoader), "LoadSceneCoroutine")]
    internal static class SceneLoaderLoadScenePatch
    {
        private static void Prefix(SceneReference scene)
        {
            if (scene != null && scene.SceneName == ScenarioIds.MechanicsScene)
            {
                LodgeScene.EnsureCreated();
            }
        }
    }
}
