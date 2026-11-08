using System.IO;
using System.Linq;
using System.Text;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Visual;
using Kingmaker.Visual.LocalMap;
using Pathfinding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Path = System.IO.Path;

namespace KingmakerWakingRune.Diagnostics
{
    /// <summary>Development-only reconnaissance for WR-03, inert without recon.flag. Removed before the candidate.</summary>
    internal static class ReconAreas
    {
        private static string Dir => Path.Combine(Main.Mod.Path, "recon");

        private static bool Enabled => Main.Mod != null && File.Exists(Path.Combine(Main.Mod.Path, "recon.flag"));

        public static void DumpMetadata(LibraryScriptableObject library)
        {
            if (!Enabled)
            {
                return;
            }
            Directory.CreateDirectory(Dir);
            var sb = new StringBuilder();
            foreach (BlueprintArea a in library.GetAllBlueprints().OfType<BlueprintArea>().OrderBy(a => a.name))
            {
                sb.AppendLine($"{a.AssetGuid}|{a.name}|loot={a.LootSetting}|indoor={a.IsIndoor}|static={a.StaticScene?.SceneName}|mech={a.DynamicScene?.SceneName}|light={string.Join(",", a.TimeOfDayScenes.Select(s => s.SceneName).ToArray())}|audio={string.Join(",", a.AudioScenes.Select(s => s.SceneName).ToArray())}|rot={a.CameraRotation}|parts={a.GetParts().Count()}");
            }
            File.WriteAllText(Path.Combine(Dir, "areas.txt"), sb.ToString());
            sb = new StringBuilder();
            foreach (BlueprintFeature f in library.GetAllBlueprints().OfType<BlueprintFeature>())
            {
                FavoredTerrain[] terrain = f.ComponentsArray?.OfType<FavoredTerrain>().ToArray();
                if (terrain != null && terrain.Length > 0)
                {
                    sb.AppendLine($"{f.AssetGuid}|{f.name}|{f.Name}|settings={string.Join(",", terrain.SelectMany(t => t.Settings).Select(s => s.ToString()).ToArray())}");
                }
            }
            File.WriteAllText(Path.Combine(Dir, "favored.txt"), sb.ToString());
            sb = new StringBuilder();
            foreach (BlueprintItem i in library.GetAllBlueprints().OfType<BlueprintItem>().Where(i => i.name == "PotionOfCureLightWounds" || i.name == "ScrollOfHaste" || i.IsNotable).Take(25))
            {
                Sprite s = i.Icon;
                sb.AppendLine($"{i.name}|{i.GetType().Name}|notable={i.IsNotable}|icon={s?.name} rect={s?.rect} tex={s?.texture?.width}x{s?.texture?.height} ppu={s?.pixelsPerUnit}");
            }
            File.WriteAllText(Path.Combine(Dir, "icons.txt"), sb.ToString());
        }

        public static void Poll()
        {
            if (!Enabled)
            {
                return;
            }
            string load = Path.Combine(Main.Mod.Path, "reconload.request");
            if (File.Exists(load))
            {
                string name = File.ReadAllText(load).Trim();
                File.Delete(load);
                BlueprintAreaEnterPoint enter = ResourcesLibrary.GetBlueprints<BlueprintAreaEnterPoint>().FirstOrDefault(e => e.Area != null && e.Area.name == name);
                Log.Info($"Recon load request {name}: enter={enter?.name}");
                if (enter != null)
                {
                    Game.Instance.LoadArea(enter, AutoSaveMode.None);
                }
            }
            string look = Path.Combine(Main.Mod.Path, "lookat.request");
            if (File.Exists(look))
            {
                string[] parts = File.ReadAllText(look).Trim().Split(' ');
                File.Delete(look);
                if (LocationMaskRenderer.Instance != null)
                {
                    LocationMaskRenderer.Instance.FogOfWar.Enabled = false;
                }
                float gridX = float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                float gridY = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                Game.Instance.UI.GetCameraRig().ScrollToImmediately(new Vector3(Sanctum.SanctumPlan.ToWorldX(gridX), 0f, Sanctum.SanctumPlan.ToWorldZ(gridY)));
            }
            string dump = Path.Combine(Main.Mod.Path, "scenedump.request");
            if (File.Exists(dump))
            {
                File.Delete(dump);
                DumpScenes();
            }
        }

        /// <summary>Compares the built navmesh with the source plan: '.' both, 'x' nav only, 'o' plan only, ' ' neither.</summary>
        private static void DumpSanctumNav()
        {
            var sb = new StringBuilder();
            int navOnly = 0, planOnly = 0, both = 0;
            for (float y = 0.5f; y <= 31f; y += 0.5f)
            {
                sb.Append($"{y,5:0.0} ");
                for (float x = 1f; x <= 25f; x += 0.25f)
                {
                    float? plan = Sanctum.SanctumPlan.HeightAt(x, y);
                    var world = new Vector3(Sanctum.SanctumPlan.ToWorldX(x), plan ?? 0f, Sanctum.SanctumPlan.ToWorldZ(y));
                    Vector3 got = Kingmaker.View.ObstacleAnalyzer.GetNearestNode(world).clampedPosition;
                    bool nav = Vector2.Distance(new Vector2(got.x, got.z), new Vector2(world.x, world.z)) < 0.25f;
                    sb.Append(nav ? (plan.HasValue ? '.' : 'x') : (plan.HasValue ? 'o' : ' '));
                    if (nav && plan.HasValue) { both++; } else if (nav) { navOnly++; } else if (plan.HasValue) { planOnly++; }
                }
                sb.AppendLine();
            }
            sb.Insert(0, $"# both={both} navOnly={navOnly} planOnly={planOnly} (plan-only cells along walls are the agent-radius erosion)\n");
            File.WriteAllText(Path.Combine(Dir, "sanctum-nav.txt"), sb.ToString());
        }

        private static void DumpScenes()
        {
            var sb = new StringBuilder();
            string area = Game.Instance.CurrentlyLoadedArea?.name ?? "none";
            if (Sanctum.SanctumScope.IsSanctumLoaded)
            {
                DumpSanctumNav();
            }
            sb.AppendLine($"# area {area}; ambientMode={RenderSettings.ambientMode} ambient={RenderSettings.ambientLight} sky={RenderSettings.ambientSkyColor} fog={RenderSettings.fog} fogColor={RenderSettings.fogColor} skybox={RenderSettings.skybox?.name} path={Game.GetCamera()?.actualRenderingPath}");
            if (AstarPath.active != null)
            {
                foreach (NavGraph g in AstarPath.active.graphs)
                {
                    if (g is RecastGraph r)
                    {
                        sb.AppendLine($"recast: cell={r.cellSize} cellH={r.cellHeight} radius={r.characterRadius} height={r.walkableHeight} climb={r.walkableClimb} slope={r.maxSlope} edge={r.maxEdgeLength} minRegion={r.minRegionSize} tiles={r.useTiles} {r.tileSizeX}x{r.tileSizeZ} meshes={r.rasterizeMeshes} colliders={r.rasterizeColliders} terrain={r.rasterizeTerrain} bounds={r.forcedBoundsCenter}/{r.forcedBoundsSize} mask={r.mask.value} obstacle={r.obstacleMask.value} tags={string.Join(",", r.tagMask.ToArray())}");
                    }
                    else if (g is GridGraph gg)
                    {
                        sb.AppendLine($"grid: {gg.width}x{gg.depth} node={gg.nodeSize} center={gg.center} erode={gg.erodeIterations} collision={gg.collision?.mask.value} height={gg.collision?.heightMask.value}");
                    }
                    else
                    {
                        sb.AppendLine($"graph {g?.GetType().Name}");
                    }
                }
                sb.AppendLine($"astar cache={AstarPath.active.astarData.cacheStartup} scanOnStartup={AstarPath.active.scanOnStartup}");
            }
            sb.AppendLine($"maskRenderer bounds={LocationMaskRenderer.Instance?.Bounds} fogEnabled={LocationMaskRenderer.Instance?.FogOfWar?.Enabled}");
            foreach (LocalMapArea m in LocalMapArea.All)
            {
                sb.AppendLine($"localmap {m.gameObject.scene.name} bounds={m.Bounds} cam={m.CameraBounds} polygon={m.PolygonBounds != null}");
            }
            sb.AppendLine($"cameraRig rotation={Game.Instance.UI.GetCameraRig()?.transform.rotation.eulerAngles} cam={Game.GetCamera()?.transform.rotation.eulerAngles} fov={Game.GetCamera()?.fieldOfView}");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || scene.name == "Start" || scene.name.StartsWith("UI_") || scene.name == "LibraryScene")
                {
                    continue;
                }
                GameObject[] roots = scene.GetRootGameObjects();
                sb.AppendLine($"## scene {scene.name} roots=[{string.Join(",", roots.Select(r => r.name + (r.activeSelf ? "" : "(off)")).ToArray())}]");
                var renderers = roots.SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).Where(r => !(r is ParticleSystemRenderer)).ToList();
                sb.AppendLine($"renderers={renderers.Count} staticBatch={renderers.Count(r => r.isPartOfStaticBatch)} lightmapped={renderers.Count(r => r.lightmapIndex >= 0 && r.lightmapIndex < 65534)} lights={roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).Count()}");
                foreach (var g in renderers.SelectMany(r => r.sharedMaterials.Where(m => m != null).Select(m => new { r, m })).GroupBy(x => x.m).OrderByDescending(g => g.Count()).Take(45))
                {
                    Material m = g.Key;
                    string tex = m.HasProperty("_MainTex") && m.mainTexture != null ? $"{m.mainTexture.name} {m.mainTexture.width}x{m.mainTexture.height}" : "-";
                    sb.AppendLine($"  mat {m.name} shader={m.shader?.name} n={g.Count()} tex={tex} sample={g.First().r.name}");
                }
                foreach (var g in renderers.GroupBy(r => r.GetComponent<MeshFilter>()?.sharedMesh?.name ?? r.name).OrderByDescending(g => g.Count()).Take(70))
                {
                    MeshFilter f = g.First().GetComponent<MeshFilter>();
                    sb.AppendLine($"  mesh {g.Key} n={g.Count()} readable={f?.sharedMesh?.isReadable} bounds={g.First().bounds.size} go={g.First().name}");
                }
                foreach (Light l in roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).Take(10))
                {
                    sb.AppendLine($"  light {l.name} {l.type} color={l.color} i={l.intensity} range={l.range} active={l.isActiveAndEnabled}");
                }
            }
            File.WriteAllText(Path.Combine(Dir, $"scene-{area}.txt"), sb.ToString());
            Log.Info($"Recon scene dump written for {area}.");
        }
    }
}
