using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.View;
using Kingmaker.Visual.LocalMap;
using KingmakerWakingRune.Blueprints;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KingmakerWakingRune.Diagnostics
{
    /// <summary>
    /// Developer reconnaissance, inert unless a 'recon.flag' file exists in the mod folder.
    /// Writes facts about installed donors and the loaded lodge to the mod's recon folder.
    /// </summary>
    internal static class Recon
    {
        private static bool s_BlueprintsDumped;

        private static bool Enabled => Main.Mod != null && File.Exists(Path.Combine(Main.Mod.Path, "recon.flag"));

        public static void DumpBlueprints(LibraryScriptableObject library)
        {
            if (s_BlueprintsDumped || !Enabled)
            {
                return;
            }
            s_BlueprintsDumped = true;
            Write("blueprints.txt", sb =>
            {
                var all = library.GetAllBlueprints();
                var root = library.Root;
                sb.AppendLine("# Stories");
                foreach (var s in root.NewGameSettings.StoryList)
                {
                    sb.AppendLine($"{(int)s.Type} {s.Type} '{s.Title}' soon={s.ComingSoon}");
                }
                sb.AppendLine("# BlueprintRoot.StartGameActions");
                foreach (var a in root.StartGameActions?.Actions ?? new Kingmaker.ElementsSystem.GameAction[0])
                {
                    sb.AppendLine($"{a?.GetType().FullName} :: {SafeCaption(a)}");
                }
                sb.AppendLine($"# Main preset: {root.NewGamePreset?.name}/{root.NewGamePreset?.AssetGuid}");
                sb.AppendLine("# Area parts with Restov/Jamandi/Capital scenes");
                foreach (var p in all.OfType<BlueprintAreaPart>().Where(p => Regex.IsMatch(p.name, "CapitalLibrary", RegexOptions.IgnoreCase)))
                {
                    sb.AppendLine($"{p.GetType().Name} {p.name}/{p.AssetGuid} static={p.StaticScene?.SceneName} mechanics={p.DynamicScene?.SceneName} " +
                                  $"light=[{string.Join(",", p.TimeOfDayScenes.Select(x => x.SceneName).ToArray())}] audio=[{string.Join(",", p.AudioScenes.Select(x => x.SceneName).ToArray())}] " +
                                  $"indoor={p.IsIndoor} rot={p.CameraRotation} components=[{string.Join(",", p.ComponentsArray.Select(c => c.GetType().Name).ToArray())}]" +
                                  (p is BlueprintArea a ? $" areaName='{a.AreaName}' parts=[{string.Join(",", a.GetParts().Select(x => x.name).ToArray())}] music={a.MusicTheme}" : ""));
                }
                sb.AppendLine("# Enter points into those areas");
                foreach (var e in all.OfType<BlueprintAreaEnterPoint>().Where(e => e.Area != null && Regex.IsMatch(e.Area.name, "CapitalLibrary", RegexOptions.IgnoreCase)))
                {
                    sb.AppendLine($"{e.name}/{e.AssetGuid} area={e.Area.name}");
                }
                sb.AppendLine("# Human female fixed-model candidates");
                foreach (var u in DonorSelection.HumanFemaleCandidates(all))
                {
                    sb.AppendLine($"{u.name}/{u.AssetGuid} name='{SafeName(u)}' prefab={u.Prefab.AssetId} faction={u.Faction?.name} " +
                                  $"components=[{string.Join(",", u.ComponentsArray.Select(c => c.GetType().Name).Distinct().ToArray())}]");
                }
            });
            Write("units.txt", sb =>
            {
                foreach (BlueprintUnit u in library.GetAllBlueprints().OfType<BlueprintUnit>().OrderBy(u => u.name))
                {
                    sb.AppendLine($"{u.AssetGuid}|{u.name}|{SafeName(u)}|{u.Gender}|{u.Race?.name}|preset={(u.CustomizationPreset != null)}|prefab={u.Prefab?.AssetId}|faction={u.Faction?.name}|portrait={u.PortraitSafe?.name}");
                }
            });
            Write("items.txt", sb =>
            {
                foreach (BlueprintItem i in library.GetAllBlueprints().OfType<BlueprintItem>().OrderBy(i => i.GetType().Name).ThenBy(i => i.name))
                {
                    sb.AppendLine($"{i.GetType().Name}|{i.AssetGuid}|{i.name}|{SafeItemName(i)}|cost={i.Cost}|wt={i.Weight}|notable={i.IsNotable}|{ItemDetail(i)}");
                }
            });
        }

        private static string SafeItemName(BlueprintItem i)
        {
            try { return i.Name; } catch { return "?"; }
        }

        private static string ItemDetail(BlueprintItem i)
        {
            try
            {
                string ench = string.Join(",", i.Enchantments.Where(e => e != null).Select(e => e.name).ToArray());
                switch (i)
                {
                    case BlueprintItemWeapon w:
                        return $"cat={w.Category} 2h={w.IsTwoHanded} natural={w.IsNatural} ench=[{ench}]";
                    case BlueprintItemArmor a:
                        return $"group={a.ProficiencyGroup} ench=[{ench}]";
                    case BlueprintItemShield s:
                        return $"ench=[{ench}]";
                    case BlueprintItemEquipmentUsable u:
                        return $"use={u.Type} spell={u.Ability?.name} cl={u.CasterLevel} sl={u.SpellLevel} charges={u.Charges}";
                    default:
                        return $"ench=[{ench}]";
                }
            }
            catch (Exception ex)
            {
                return "err=" + ex.GetType().Name;
            }
        }

        public static void DumpLodge()
        {
            if (!Enabled)
            {
                return;
            }
            Write("navmap.txt", sb =>
            {
                sb.AppendLine("# walkable map, 1 m cells: x -32..-2 left to right, z +18 (top) .. -18 (bottom); '.' walkable, '#' not");
                for (int z = 18; z >= -18; z--)
                {
                    sb.Append($"{z,4} ");
                    for (int x = -32; x <= -2; x++)
                    {
                        var req = new Vector3(x, 0f, z);
                        Vector3 got = ObstacleAnalyzer.GetNearestNode(req).clampedPosition;
                        sb.Append(Vector3.Distance(new Vector3(got.x, 0f, got.z), req) < 0.3f ? '.' : '#');
                    }
                    sb.AppendLine();
                }
            });
            Write($"lodge-{DateTime.Now:HHmmss}.txt", sb =>
            {
                sb.AppendLine($"# Area {Game.Instance.CurrentlyLoadedArea?.name} main={Game.Instance.Player.MainCharacter.Value?.Position}");
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    GameObject[] roots = scene.isLoaded ? scene.GetRootGameObjects() : new GameObject[0];
                    sb.AppendLine($"## Scene {scene.name} loaded={scene.isLoaded} roots={roots.Length}");
                    var counts = new Dictionary<string, int>();
                    foreach (var root in roots)
                    {
                        foreach (var c in root.GetComponentsInChildren<Component>(true))
                        {
                            if (c == null)
                            {
                                continue;
                            }
                            string n = c.GetType().Name;
                            if (Regex.IsMatch(n, "Astar|LocalMap|EnterPoint|EntityView|Light$|Collider|NavmeshCut|Spawn|Cutscene|Trigger|ScriptZone|Interaction|Door"))
                            {
                                counts[n] = counts.TryGetValue(n, out int k) ? k + 1 : 1;
                            }
                        }
                    }
                    sb.AppendLine("   components: " + string.Join(", ", counts.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}").ToArray()));
                    foreach (var root in roots.Take(60))
                    {
                        sb.AppendLine($"   root {root.name} active={root.activeSelf} pos={root.transform.position}");
                    }
                }
                foreach (var map in LocalMapArea.All)
                {
                    sb.AppendLine($"# LocalMapArea {map.name} scene={map.gameObject.scene.name} bounds={map.Bounds} camera={map.CameraBounds}");
                }
                var area = LocalMapArea.All.FirstOrDefault();
                if (area != null)
                {
                    Bounds b = area.Bounds;
                    sb.AppendLine("# Navmesh samples (requested -> nearest walkable)");
                    for (float x = b.min.x; x <= b.max.x; x += Mathf.Max(1f, b.size.x / 8f))
                    {
                        for (float z = b.min.z; z <= b.max.z; z += Mathf.Max(1f, b.size.z / 8f))
                        {
                            var req = new Vector3(x, b.center.y, z);
                            sb.AppendLine($"   {req} -> {ObstacleAnalyzer.GetNearestNode(req).clampedPosition}");
                        }
                    }
                }
                sb.AppendLine("# Furniture-like renderers");
                foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>().Where(r => Regex.IsMatch(r.name, "desk|table|book|shelf|chair|globe|map|fire|bed|carpet|rug|door", RegexOptions.IgnoreCase)).Take(150))
                {
                    sb.AppendLine($"   {r.name} scene={r.gameObject.scene.name} center={r.bounds.center} size={r.bounds.size}");
                }
            });
        }

        private static string SafeName(BlueprintUnit u)
        {
            try { return u.CharacterName; } catch { return "?"; }
        }

        private static string SafeCaption(Kingmaker.ElementsSystem.GameAction a)
        {
            try { return a?.GetCaption(); } catch { return "?"; }
        }

        private static void Write(string file, Action<StringBuilder> fill)
        {
            try
            {
                var sb = new StringBuilder();
                fill(sb);
                string dir = Path.Combine(Main.Mod.Path, "recon");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, file), sb.ToString());
                Log.Info($"Recon written: {file}");
            }
            catch (Exception ex)
            {
                Log.Error($"Recon {file} failed.", ex);
            }
        }
    }
}
