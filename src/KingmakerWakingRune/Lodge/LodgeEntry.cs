using System.Linq;
using Kingmaker.Blueprints.Area;
using Kingmaker.View;
using Kingmaker.Visual.LocalMap;
using KingmakerWakingRune.Blueprints;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KingmakerWakingRune.Lodge
{
    /// <summary>
    /// Supplies the scene marker for the mod's enter point, placed at the donor's own office entry
    /// pose, right before the native party placement looks it up.
    /// </summary>
    internal static class LodgeEntry
    {
        /// <summary>Pose used for the most recent first entry; Sheila is placed relative to it.</summary>
        public static Pose? LastEntryPose { get; private set; }

        public static void EnsureMarker(BlueprintAreaEnterPoint enterPoint)
        {
            if (!ScenarioBlueprints.IsRegistered || enterPoint != ScenarioBlueprints.EnterPoint
                || AreaEnterPoint.FindAreaEnterPointOnScene(enterPoint) != null)
            {
                return;
            }
            if (AstarPath.active == null)
            {
                Log.Error("No navmesh is active in the lodge scenes; the donor area cannot host the party.");
                return;
            }
            string donorScene = ScenarioBlueprints.Area.DynamicScene.SceneName;
            var markers = AreaEnterPoint.Instances.Where(p => p.gameObject.scene.name == donorScene && p.Blueprint != null).ToList();
            Log.Info($"Donor entry markers: [{string.Join(", ", markers.Select(p => $"{p.Blueprint.name}/{p.Blueprint.AssetGuid}@{p.transform.position}").ToArray())}]");
            AreaEnterPoint donor = markers.OrderBy(p => p.Blueprint.name).FirstOrDefault();
            Pose pose;
            if (donor != null)
            {
                pose = new Pose(donor.transform.position, donor.transform.rotation);
            }
            else
            {
                LocalMapArea map = LocalMapArea.All.FirstOrDefault();
                Vector3 center = map != null ? map.Bounds.center : Vector3.zero;
                pose = new Pose(ObstacleAnalyzer.GetNearestNode(center).clampedPosition, Quaternion.identity);
                Log.Error($"No donor entry marker in {donorScene}; using local-map centre {pose.position}. " +
                          $"Markers present: [{string.Join(", ", AreaEnterPoint.Instances.Select(p => p.Blueprint?.name).ToArray())}]");
            }

            // Created inactive so AreaEnterPoint.OnEnable never registers a point without a blueprint.
            var marker = new GameObject("KWR_LodgeEnterPoint");
            marker.SetActive(false);
            marker.transform.SetPositionAndRotation(pose.position, pose.rotation);
            // One child selects the native preset placement, which clamps the leader onto the navmesh.
            new GameObject("KWR_LeaderSlot").transform.SetParent(marker.transform, false);
            marker.AddComponent<AreaEnterPoint>().Blueprint = enterPoint;
            Scene scene = SceneManager.GetSceneByName(ScenarioBlueprints.Area.DynamicScene.SceneName);
            if (scene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(marker, scene);
            }
            marker.SetActive(true);
            LastEntryPose = pose;
            Log.Info($"Lodge entry marker placed at {pose.position} yaw={pose.rotation.eulerAngles.y:0} (donor marker found: {donor != null})");
        }
    }
}
