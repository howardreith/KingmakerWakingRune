using System;
using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.View;
using Kingmaker.Visual;
using Kingmaker.Visual.FogOfWar;
using Kingmaker.Visual.LocalMap;
using KingmakerWakingRune.Blueprints;
using Pathfinding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// Turns the loaded donor scenes into a shell for the Sanctum of Sloth: every donor object is hidden
    /// before it can act, and native navigation, camera bounds, fog of war and local map are pointed at the
    /// plan instead. Applied once for each load of the area and scoped to it.
    /// </summary>
    /// <summary>
    /// Marks the mod-built hierarchy. The native time-of-day step loads the donor's light scene after the area
    /// reports loaded, so its lamps are hidden, and the ambient re-applied, whenever that scene turns up.
    /// </summary>
    internal sealed class SanctumRootMarker : MonoBehaviour
    {
        private const float PollSeconds = 0.25f;
        private float m_NextPoll;

        private void Update()
        {
            if (Time.unscaledTime < m_NextPoll)
            {
                return;
            }
            m_NextPoll = Time.unscaledTime + PollSeconds;
            int hidden = SanctumScope.HideDonorLighting();
            if (hidden > 0)
            {
                SanctumLighting.ApplyEnvironment();
                Log.Info($"Sanctum lighting applied: {hidden} donor light roots hidden.");
            }
        }
    }

    internal static class SanctumScope
    {
        private const float MapHalfWidth = 22f;
        private const float MapHalfDepth = 26f;

        private static GameObject s_Root;

        public static bool IsBuilt => s_Root != null;

        public static bool IsSanctumLoaded => ScenarioBlueprints.Sanctum != null && Game.Instance.CurrentlyLoadedArea == ScenarioBlueprints.Sanctum;

        /// <summary>Called both before native party placement (travel) and at scene-load completion (saves).</summary>
        public static void EnsureBuilt()
        {
            if (IsBuilt)
            {
                return;
            }
            try
            {
                Build();
            }
            catch (Exception ex)
            {
                Log.Error("Sanctum construction failed.", ex);
            }
        }

        private static void Build()
        {
            BlueprintArea area = ScenarioBlueprints.Sanctum;
            Scene statics = SceneManager.GetSceneByName(area.StaticScene.SceneName);
            Scene mechanics = SceneManager.GetSceneByName(area.DynamicScene.SceneName);
            if (!statics.isLoaded || !mechanics.isLoaded || AstarPath.active == null)
            {
                Log.Error($"Sanctum scenes are not ready: static={statics.isLoaded} mechanics={mechanics.isLoaded} astar={AstarPath.active != null}.");
                return;
            }
            SanctumMaterials materials = HarvestMaterials(statics);
            materials.Palette = new SanctumPalette(materials.Rock);
            int hidden = HideDonorObjects(statics) + HideDonorObjects(mechanics);

            s_Root = new GameObject("KWR_SanctumEnvironment");
            s_Root.AddComponent<SanctumRootMarker>();
            SceneManager.MoveGameObjectToScene(s_Root, mechanics);
            SanctumBuilder.Build(s_Root.transform, materials);
            SanctumDressing.Build(s_Root.transform, materials);
            SanctumLighting.Apply(s_Root.transform);
            ConfigureBounds();
            ScanNavigation();
            SanctumDoors.ReleaseOccupied();
            Log.Info($"Sanctum built: {hidden} donor roots hidden, {s_Root.GetComponentsInChildren<Renderer>().Length} renderers, {NavNodeCount()} navmesh nodes.");
        }

        public static SanctumMaterials HarvestMaterials(Scene statics)
        {
            var found = new Dictionary<string, Material>();
            foreach (GameObject root in statics.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null && !found.ContainsKey(material.name))
                        {
                            found[material.name] = material;
                        }
                    }
                }
            }
            return new SanctumMaterials
            {
                Floor = Copy(found, "cyclops_dungeon_floor_tile_02_opaque_d"),
                Rock = Copy(found, "cyclops_dungeon_rock_opaque_d"),
                Black = Copy(found, "black_cut_opaque_d"),

            };
        }

        private static Material Copy(Dictionary<string, Material> found, string name)
        {
            if (!found.TryGetValue(name, out Material material))
            {
                throw new InvalidOperationException($"Donor material {name} is missing from the installed game.");
            }
            return new Material(material);
        }

        private static int HideDonorObjects(Scene scene)
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<AstarPath>(true) != null
                    || root.GetComponentInChildren<LocalMapArea>(true) != null
                    || root.GetComponentInChildren<LocationMaskRenderer>(true) != null
                    || root.GetComponentInChildren<SanctumDoor>(true) != null)
                {
                    continue;
                }
                root.SetActive(false);
                count++;
            }
            foreach (FogOfWarStaticMaskHolder holder in FogOfWarStaticMaskHolder.Instances.ToArray())
            {
                holder.gameObject.SetActive(false);
            }
            return count;
        }

        public static int HideDonorLighting()
        {
            int count = 0;
            foreach (SceneReference reference in ScenarioBlueprints.Sanctum.LightScenes)
            {
                Scene scene = SceneManager.GetSceneByName(reference.SceneName);
                if (!scene.isLoaded)
                {
                    continue;
                }
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.activeSelf && (root.name.Contains("LIGHT_SETUP") || root.name.Contains("Particles")))
                    {
                        root.SetActive(false);
                        count++;
                    }
                }
            }
            return count;
        }

        private static void ConfigureBounds()
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(2f * MapHalfWidth, 50f, 2f * MapHalfDepth));
            foreach (LocalMapArea map in LocalMapArea.All.ToArray())
            {
                AccessTools.Field(typeof(LocalMapArea), "m_Bounds").SetValue(map, new Bounds(bounds.center - map.transform.position, bounds.size));
                AccessTools.Field(typeof(LocalMapArea), "m_CameraBounds").SetValue(map, new Bounds(bounds.center - map.transform.position, new Vector3(2f * MapHalfWidth - 4f, 0f, 2f * MapHalfDepth - 4f)));
            }
            LocationMaskRenderer mask = LocationMaskRenderer.Instance;
            if (mask != null)
            {
                mask.FogOfWar.StaticMask = null;
                object renderer = AccessTools.Field(typeof(LocationMaskRenderer), "m_FogOfWarRenderer").GetValue(mask);
                if (renderer != null)
                {
                    AccessTools.Method(renderer.GetType(), "InitStaticMask").Invoke(renderer, null);
                }
            }
        }

        private static void ScanNavigation()
        {
            AstarPath astar = AstarPath.active;
            // Baked add/remove masks describe the donor's floor plan.
            NavmeshMasks donorMasks = astar.GetComponent<NavmeshMasks>();
            if (donorMasks != null)
            {
                UnityEngine.Object.DestroyImmediate(donorMasks);
            }
            if (astar.graphs.FirstOrDefault() is RecastGraph recast)
            {

                recast.scanEmptyGraph = false;
                recast.relevantGraphSurfaceMode = RecastGraph.RelevantGraphSurfaceMode.DoNotRequire;
                recast.minRegionSize = 1f;
                // The donor's one-voxel climb marks the source's short, steep flights as ledges. Below the 1.83 m terrace face.
                recast.walkableClimb = 1.5f;
                recast.forcedBoundsCenter = Vector3.zero;
                recast.forcedBoundsSize = new Vector3(2f * MapHalfWidth, 40f, 2f * MapHalfDepth);
                recast.mask = 1 << SanctumBuilder.GroundLayer;
                recast.obstacleMask = 0;
            }
            // The scan keeps only areas connected to these markers, which also discards any stray walkable island.
            foreach (ValidNavmeshArea donorMarker in UnityEngine.Object.FindObjectsOfType<ValidNavmeshArea>())
            {
                UnityEngine.Object.DestroyImmediate(donorMarker);
            }
            var validArea = new GameObject("ValidNavmeshArea_Arrival");
            validArea.transform.SetParent(s_Root.transform, false);
            validArea.transform.position = new Vector3(SanctumPlan.ToWorldX(SanctumPlan.Arrival.X), 0f, SanctumPlan.ToWorldZ(SanctumPlan.Arrival.Y));
            validArea.AddComponent<ValidNavmeshArea>();
            astar.Scan();
            if (astar.graphs.FirstOrDefault() is RecastGraph scanned)
            {
                EnableDoorCuts(astar, scanned);
            }
        }

        /// <summary>Door cuts act through a handler built from the tiles, so it has to be made after this area's own scan.</summary>
        private static void EnableDoorCuts(AstarPath astar, RecastGraph graph)
        {
            TileHandlerHelper helper = UnityEngine.Object.FindObjectOfType<TileHandlerHelper>();
            bool existed = helper != null;
            if (!existed)
            {
                helper = astar.gameObject.AddComponent<TileHandlerHelper>();
            }
            var handler = new Pathfinding.Util.TileHandler(graph);
            handler.CreateTileTypesFromGraph();
            helper.UseSpecifiedHandler(handler);
            helper.ForceUpdate();
            Log.Info($"Sanctum door cuts ready: helper {(existed ? "native" : "added")}, {NavmeshCut.GetAll().Count} cuts, tiles={graph.useTiles}, {graph.tileXCount}x{graph.tileZCount}.");
        }

        private static int NavNodeCount()
        {
            return AstarPath.active.graphs[0]?.CountNodes() ?? 0;
        }

        /// <summary>Places the arrival marker for native party placement, which happens right after this.</summary>
        public static void EnsureEnterMarker(BlueprintAreaEnterPoint enterPoint)
        {
            if (enterPoint != ScenarioBlueprints.SanctumEnter || AreaEnterPoint.FindAreaEnterPointOnScene(enterPoint) != null || !IsBuilt)
            {
                return;
            }
            var marker = new GameObject("KWR_SanctumEnterPoint");
            marker.SetActive(false);
            GridPoint arrival = SanctumPlan.Arrival;
            marker.transform.SetPositionAndRotation(new Vector3(SanctumPlan.ToWorldX(arrival.X), 0f, SanctumPlan.ToWorldZ(arrival.Y)), Quaternion.Euler(0f, 180f, 0f));
            marker.transform.SetParent(s_Root.transform, true);
            // Children switch the native placement to preset slots: a centred formation would stand the party on the front stairs.
            for (int i = 0; i < SanctumPlan.ArrivalSlots.Length; i++)
            {
                GridPoint slot = SanctumPlan.ArrivalSlots[i];
                var child = new GameObject($"Slot{i + 1}");
                child.transform.SetPositionAndRotation(new Vector3(SanctumPlan.ToWorldX(slot.X), 0f, SanctumPlan.ToWorldZ(slot.Y)), marker.transform.rotation);
                child.transform.SetParent(marker.transform, true);
            }
            marker.AddComponent<AreaEnterPoint>().Blueprint = enterPoint;
            marker.SetActive(true);
        }
    }
}
