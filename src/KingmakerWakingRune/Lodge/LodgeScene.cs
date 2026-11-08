using Kingmaker.View;
using KingmakerWakingRune.Blueprints;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KingmakerWakingRune.Lodge
{
    /// <summary>
    /// Supplies the lodge's mechanics scene. The donor's own mechanics scene (its NPCs, cutscenes,
    /// triggers, loot and exits) is never loaded; instead an empty runtime scene with the same role
    /// is created just before the native loader asks for it, so the loader treats it as loaded and
    /// matches the area's persistent entity state (the player, Sheila) against it.
    /// </summary>
    internal static class LodgeScene
    {
        public static void EnsureCreated()
        {
            Scene scene = SceneManager.GetSceneByName(ScenarioIds.MechanicsScene);
            if (scene.isLoaded)
            {
                return;
            }
            scene = SceneManager.CreateScene(ScenarioIds.MechanicsScene);

            // Created inactive so AreaEnterPoint.OnEnable never registers a point without a blueprint.
            var point = new GameObject("KWR_LodgeEnterPoint");
            point.SetActive(false);
            point.transform.SetPositionAndRotation(LodgeLayout.SpawnPosition, Quaternion.Euler(0f, LodgeLayout.SpawnYaw, 0f));
            // A child selects the native preset placement path, which clamps the leader to the nearest navmesh node.
            var leaderSlot = new GameObject("KWR_LeaderSlot");
            leaderSlot.transform.SetParent(point.transform, false);
            point.AddComponent<AreaEnterPoint>().Blueprint = ScenarioBlueprints.EnterPoint;
            SceneManager.MoveGameObjectToScene(point, scene);
            point.SetActive(true);
            Log.Info($"Lodge mechanics scene created: scene={ScenarioIds.MechanicsScene} enter={ScenarioBlueprints.EnterPoint.AssetGuid} at {LodgeLayout.SpawnPosition} yaw={LodgeLayout.SpawnYaw}");
        }
    }
}
