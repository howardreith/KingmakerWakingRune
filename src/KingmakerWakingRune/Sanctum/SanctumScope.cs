using System;
using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using Kingmaker.Visual;
using Kingmaker.Visual.FogOfWar;
using Kingmaker.Visual.LocalMap;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Diagnostics;
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

        /// <summary>Everything below this root was generated for this load, so its meshes and materials go with it.</summary>
        private void OnDestroy()
        {
            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
            {
                Destroy(filter.sharedMesh);
            }
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        // Copies of donor materials still point at the donor's textures, which are not ours to destroy.
                        if (material.mainTexture != null && material.mainTexture.name.StartsWith(SanctumPalette.OwnedTexturePrefix))
                        {
                            Destroy(material.mainTexture);
                        }
                        Destroy(material);
                    }
                }
            }
        }
    }

    internal static class SanctumScope
    {
        private const float MapHalfWidth = 22f * SanctumPlan.HorizontalScale;
        private const float MapHalfDepth = 26f * SanctumPlan.HorizontalScale;
        /// <summary>Metres two arrivals must stand apart; anything closer is the stacked placement this check exists to catch.</summary>
        private const float MinSeparation = 0.2f;
        /// <summary>Metres a restored unit may be from the navmesh and still count as standing on it.</summary>
        private const float NavmeshReach = 0.45f;

        private static readonly SanctumBuildGate s_Gate = new SanctumBuildGate();
        private static GameObject s_Root;
        private static TileHandlerHelper s_CutHelper;
        private static bool s_CutsApplied;

        public static bool IsBuilt => s_Gate.IsReady && s_Root != null;

        public static int StatueCount => s_Root == null ? 0 : s_Root.GetComponentsInChildren<Transform>(true).Select(t => t.name).Where(n => n.StartsWith(StatueBuilder.ObjectPrefix)).Distinct().Count();

        public static bool IsSanctumLoaded => ScenarioBlueprints.Sanctum != null && Game.Instance.CurrentlyLoadedArea == ScenarioBlueprints.Sanctum;

        /// <summary>Called both before native party placement (travel) and at scene-load completion (saves).</summary>
        public static void EnsureBuilt()
        {
            if (s_Root == null && s_Gate.IsReady)
            {
                s_Gate.Reset();
            }
            s_Gate.Ensure(Build, DiscardPartialBuild, ex => Log.Error("Sanctum construction failed.", ex));
        }

        private static void DiscardPartialBuild()
        {
            if (s_Root != null)
            {
                UnityEngine.Object.DestroyImmediate(s_Root);
            }
            s_Root = null;
            s_CutHelper = null;
            s_CutsApplied = false;
            Log.Error("Sanctum build not published; its partial objects were removed and it can be tried again.");
        }

        private static bool Build()
        {
            BlueprintArea area = ScenarioBlueprints.Sanctum;
            Scene statics = SceneManager.GetSceneByName(area.StaticScene.SceneName);
            Scene mechanics = SceneManager.GetSceneByName(area.DynamicScene.SceneName);
            if (!statics.isLoaded || !mechanics.isLoaded || AstarPath.active == null)
            {
                Log.Error($"Sanctum scenes are not ready: static={statics.isLoaded} mechanics={mechanics.isLoaded} astar={AstarPath.active != null}.");
                return false;
            }
            s_CutsApplied = false;
            SanctumMaterials materials = HarvestMaterials(statics);
            materials.Palette = new SanctumPalette(materials.Rock);
            int hidden = HideDonorObjects(statics) + HideDonorObjects(mechanics);

            s_Root = new GameObject("KWR_SanctumEnvironment");
            s_Root.AddComponent<SanctumRootMarker>();
            SceneManager.MoveGameObjectToScene(s_Root, mechanics);
            SanctumBuilder.Build(s_Root.transform, materials);
            if (FaultInjection.Take("build-failed"))
            {
                throw new InvalidOperationException("INJECTED FAULT build-failed: construction stopped after the root was created.");
            }
            SanctumDressing.Build(s_Root.transform, materials);
            SanctumLighting.Apply(s_Root.transform);
            ConfigureBounds();
            ScanNavigation();
            Log.Info($"Sanctum built: {hidden} donor roots hidden, {s_Root.GetComponentsInChildren<Renderer>().Length} renderers, {NavNodeCount()} navmesh nodes.");
            return true;
        }

        /// <summary>Whether a token arrival may commit: everything set up, and the party actually standing on valid, separate ground.</summary>
        public static bool CheckArrival(out string problem)
        {
            problem = null;
            if (!IsBuilt)
            {
                problem = "the sanctum did not finish building";
            }
            else if (!SanctumDoors.AreReady)
            {
                problem = "the doors are not ready";
            }
            else if (!s_CutsApplied)
            {
                problem = "the door cuts are not applied";
            }
            else if (AstarPath.active == null || NavNodeCount() == 0)
            {
                problem = "there is no navigation";
            }
            else if (FaultInjection.Take("arrival-invalid"))
            {
                problem = "INJECTED FAULT arrival-invalid";
            }
            else
            {
                problem = CheckPartyPlacement();
            }
            return problem == null;
        }

        private static string CheckPartyPlacement()
        {
            List<UnitEntityData> party = PartyAndPets().ToList();
            if (party.Count == 0)
            {
                return "nobody arrived";
            }
            foreach (UnitEntityData unit in party)
            {
                if (!unit.IsInGame || unit.View == null)
                {
                    return $"{unit.CharacterName} is not in the area";
                }
                Vector3 p = unit.Position;
                float x = GridX(p), y = GridY(p);
                if (SanctumPlan.SpaceAt(x, y) != SanctumSpace.Interior || !SanctumPlan.IsValidGround(x, y, p.y))
                {
                    return $"{unit.CharacterName} was placed off the arrival floor at ({x:0.0}, {y:0.0})";
                }
                if (party.Any(other => other != unit && (other.Position - p).sqrMagnitude < MinSeparation * MinSeparation))
                {
                    return $"{unit.CharacterName} was placed on top of another party member";
                }
            }
            return null;
        }

        private static IEnumerable<UnitEntityData> PartyAndPets()
        {
            foreach (UnitEntityData unit in Game.Instance.Player.Party)
            {
                yield return unit;
                UnitEntityData pet = unit.Descriptor.Pet;
                if (pet != null)
                {
                    yield return pet;
                }
            }
        }

        private static float GridX(Vector3 p)
        {
            return p.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX;
        }

        private static float GridY(Vector3 p)
        {
            return SanctumPlan.OriginY - p.z / SanctumPlan.MetersPerSquare;
        }

        /// <summary>
        /// Reprojects old-layout actors into the same logical space. On the current layout only invalid placements need repair.
        /// </summary>
        public static bool RepairRestoredPositions(bool previousLayout)
        {
            if (!IsBuilt) return false;
            var state = Wards.WardStore.Existing;
            var members = PartyAndPets();
            if (previousLayout)
            {
                // Include scene entities that have not attached a view yet, including restored corpses.
                members = members.Concat(Game.Instance.CurrentScene.AllEntityData.OfType<UnitEntityData>())
                    .Concat(Game.Instance.Player.CrossSceneState.AllEntityData.OfType<UnitEntityData>()
                        .Where(u => state.PendingLayoutOrigins.ContainsKey(u.UniqueId)))
                    .Concat(Game.Instance.State.Units.Where(u => u.View != null));
            }
            var units = members.Where(u => !u.Destroyed && !u.ShouldBeDestroyed && Wards.WardFixtures.IdOf(u) == null &&
                (u.IsInGame || (previousLayout && state.PendingLayoutOrigins.ContainsKey(u.UniqueId))) && (previousLayout || u.View != null))
                .Distinct().ToDictionary(u => u.UniqueId);
            if (units.Count > 0 && FaultInjection.Take("position-invalid"))
            {
                var subject = units.Values.First();
                subject.Position = new Vector3(SanctumPlan.ToWorldX(13.6f), SanctumPlan.RingHeight, SanctumPlan.ToWorldZ(18.2f));
                Log.Info($"INJECTED FAULT position-invalid: {subject.CharacterName} moved to {subject.Position}.");
            }
            var positions = units.ToDictionary(kv => kv.Key, kv =>
            {
                var from = kv.Value.Position;
                return new LayoutPosition(from.x, from.y, from.z);
            });
            if (previousLayout)
            {
                state.CaptureLayoutOrigins(positions);
                // OnAreaDidLoad has the complete scene and cross-scene lists. A despawned entity needs no landing.
                foreach (var gone in state.PendingLayoutOrigins.Keys.Where(id => !units.ContainsKey(id)).ToList())
                    state.PendingLayoutOrigins.Remove(gone);
            }
            var snapshot = new List<LayoutActor>();
            foreach (var unit in units.Values)
            {
                var from = positions[unit.UniqueId];
                snapshot.Add(previousLayout
                    ? state.SnapshotOldLayout(unit.UniqueId, from, ReservationRadius(unit), !unit.IsInGame || unit.View == null)
                    : new LayoutActor(unit.UniqueId, from, unit.Corpulence, IsStandingValidly(unit.Position), false, false));
            }
            var closed = SanctumDoors.ClosedPlans();
            var stoneFigures = SanctumDressing.StatueFeet.Where(kv => state == null ||
                !state.Statues.TryGetValue(kv.Key, out var statue) || statue.Outcome == Wards.StatueOutcome.Stone)
                .Select(kv => kv.Value).ToList();
            bool Legal(LayoutActor actor, GridPoint point, LayoutPosition target)
            {
                float radius = actor.Radius.Value;
                if (!SanctumPlan.FitsMigration(actor.Logical, point, target.Y, radius / SanctumPlan.MetersPerSquare, closed)) return false;
                var p = new Vector3(target.X, target.Y, target.Z);
                NNInfo nearest = ObstacleAnalyzer.GetNearestNode(p);
                if (nearest.node == null || !nearest.node.Walkable || (nearest.clampedPosition - p).sqrMagnitude > 0.03f * 0.03f) return false;
                return !stoneFigures.Any(foot => Mathf.Abs(foot.y - p.y) < SanctumPlan.GroundTolerance &&
                    Vector2.Distance(new Vector2(foot.x, foot.z), new Vector2(p.x, p.z)) < 0.3f + radius);
            }

            var moves = LayoutMigration.Plan(snapshot, Legal);
            bool complete = true;
            foreach (var actor in snapshot)
            {
                if (actor.Settled) continue;
                if (!moves.TryGetValue(actor.Id, out var to))
                {
                    Log.Info($"Layout repair pending for {actor.Id} ({(actor.AwaitingView ? "awaiting view" : "no reserved destination")}).");
                    complete = false;
                    continue;
                }
                var unit = units[actor.Id];
                unit.Position = new Vector3(to.X, to.Y, to.Z);
                if (previousLayout) state.AppliedLayoutPosition(actor.Id);
                Log.Info($"Restored position corrected: {unit.CharacterName} -> {unit.Position} in {SanctumPlan.SpaceAt(actor.Logical.X, actor.Logical.Y)}.");
            }
            return complete && (!previousLayout || state.PendingLayoutOrigins.Count == 0);
        }

        private static float? ReservationRadius(UnitEntityData unit)
        {
            if (unit.View != null) return unit.Corpulence;
            // Read the same prefab selection as UnitEntityData.CreateView without instantiating or moving the unit.
            var morph = unit.GetActivePolymorph();
            UnitEntityView prefab = morph?.Prefab.Load();
            if (prefab != null) return prefab.Corpulence; // native replacement view disables size scaling
            var doll = unit.Descriptor.Doll;
            if (doll != null)
            {
                var gen = Game.Instance.BlueprintRoot.CharGen;
                prefab = (doll.Gender == Kingmaker.Blueprints.Gender.Male ? gen.MaleDoll : gen.FemaleDoll).GetComponent<UnitEntityView>();
            }
            else prefab = string.IsNullOrEmpty(unit.Descriptor.CustomPrefabGuid) ? unit.Blueprint.Prefab.Load()
                : Kingmaker.Blueprints.ResourcesLibrary.TryGetResource<UnitEntityView>(unit.Descriptor.CustomPrefabGuid);
            if (prefab == null) return null;
            int change = (int)unit.Descriptor.State.Size - (int)unit.Descriptor.OriginalSize;
            float scale = prefab.DisableSizeScaling ? 1f : Mathf.Pow(0.66f, -change);
            return Mathf.Max(0.5f, prefab.Corpulence * scale);
        }

        private static bool IsStandingValidly(Vector3 p)
        {
            return SanctumPlan.IsValidGround(GridX(p), GridY(p), p.y) && IsOnNavmesh(p);
        }

        private static bool IsOnNavmesh(Vector3 p)
        {
            NNInfo nearest = ObstacleAnalyzer.GetNearestNode(p);
            Vector3 clamped = nearest.clampedPosition;
            return nearest.node != null && nearest.node.Walkable && new Vector2(clamped.x - p.x, clamped.z - p.z).sqrMagnitude <= NavmeshReach * NavmeshReach && Mathf.Abs(clamped.y - p.y) <= SanctumPlan.GroundTolerance;
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
                Floor = Copy(found, "cyclops_dungeon_floor_tile_01_opaque_d"),
                Rock = Copy(found, "cyclops_dungeon_rock_opaque_d"),

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
                // The donor's one-voxel climb marks the source's short, steep flights as ledges. This allowance can reach the
                // 1.83 m terrace after voxel rounding, so the plan leaves a walking gap in front of every tier face.
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
                PrepareDoorCuts(astar, scanned);
            }
        }

        /// <summary>Door cuts act through a handler built from the tiles, so it has to be made after this area's own scan.</summary>
        private static void PrepareDoorCuts(AstarPath astar, RecastGraph graph)
        {
            s_CutHelper = UnityEngine.Object.FindObjectOfType<TileHandlerHelper>();
            bool existed = s_CutHelper != null;
            if (!existed)
            {
                s_CutHelper = astar.gameObject.AddComponent<TileHandlerHelper>();
            }
            // On travel the donor's own door cuts are still alive at this point; they describe another floor plan.
            foreach (NavmeshCut cut in NavmeshCut.GetAll().ToArray())
            {
                if (cut.GetComponent<SanctumDoor>() == null)
                {
                    cut.enabled = false;
                }
            }
            var handler = new Pathfinding.Util.TileHandler(graph);
            handler.CreateTileTypesFromGraph();
            s_CutHelper.UseSpecifiedHandler(handler);
            Log.Info($"Sanctum door cuts prepared: helper {(existed ? "native" : "added")}, {NavmeshCut.GetAll().Count} cuts, tiles={graph.useTiles}, {graph.tileXCount}x{graph.tileZCount}.");
        }

        /// <summary>
        /// Applies the door cuts. Runs once the area has finished loading: reloading tiles while the party is being
        /// placed would leave the arrival floor missing and stack everyone on the nearest surviving navmesh.
        /// </summary>
        public static void ApplyDoorCuts()
        {
            if (s_CutHelper == null)
            {
                return;
            }
            foreach (NavmeshCut cut in NavmeshCut.GetAll().ToArray())
            {
                if (cut.GetComponent<SanctumDoor>() != null)
                {
                    // Unmoved cuts report no change, so a door's must be pushed onto the freshly scanned tiles.
                    cut.ForceUpdate();
                }
            }
            s_CutHelper.ForceUpdate();
            s_CutHelper = null;
            s_CutsApplied = true;
            Log.Info("Sanctum door cuts applied.");
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
