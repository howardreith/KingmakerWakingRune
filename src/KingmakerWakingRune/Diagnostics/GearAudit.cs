using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.UnitLogic.FactLogic;
using KingmakerWakingRune.Blueprints;

namespace KingmakerWakingRune.Diagnostics
{
    /// <summary>
    /// Read only (WR-PLAYTEST-02 equipment audit): from the loaded library, every armor whose enchantments carry a physical
    /// damage resistance or name adamantine, every item named like a haramaki, the dueling gloves, and each provider weapon
    /// line, stocked or retired, with its enchantments' components; and, for those items, every blueprint that references
    /// them (vendor tables, loot, unit bodies), so creature-only, quest-only or unused copies can be told apart.
    /// </summary>
    internal static class GearAudit
    {
        public static string Write(string path)
        {
            List<BlueprintScriptableObject> all = ResourcesLibrary.LibraryObject.BlueprintsByAssetId.Values.ToList();
            List<BlueprintItem> items = all.OfType<BlueprintItem>().ToList();
            var targets = new Dictionary<BlueprintItem, string>();
            foreach (BlueprintItemArmor armor in items.OfType<BlueprintItemArmor>())
            {
                if (armor.Enchantments.Any(e => e.name.IndexOf("Adamant", StringComparison.OrdinalIgnoreCase) >= 0 || e.GetComponents<AddDamageResistancePhysical>().Any()))
                {
                    targets[armor] = "armor with physical DR or an adamantine enchantment";
                }
            }
            foreach (BlueprintItem item in items.Where(i => Named(i, "haramak") || Named(i, "dueling") && !(i is BlueprintItemWeapon)))
            {
                targets[item] = "named search (haramaki, dueling)";
            }
            foreach (string guid in ProviderStock.Lines.Select(l => l.Guid).Concat(ProviderStock.Retired))
            {
                if (ResourcesLibrary.TryGetBlueprint<BlueprintItem>(guid) is BlueprintItemWeapon weapon)
                {
                    targets[weapon] = ProviderStock.Retired.Contains(guid) ? "provider, retired" : "provider, stocked";
                }
            }
            Dictionary<BlueprintItem, List<string>> references = References(all, new HashSet<BlueprintItem>(targets.Keys));
            var text = new StringBuilder($"GEAR AUDIT: {items.Count} items in the library; {targets.Count} audited.\n");
            foreach (KeyValuePair<BlueprintItem, string> pair in targets.OrderBy(p => p.Value).ThenBy(p => p.Key.name))
            {
                BlueprintItem item = pair.Key;
                text.Append($"\n== {item.name} | {item.AssetGuid} | \"{item.Name}\" | cost {item.Cost} | {item.GetType().Name} | {pair.Value}");
                if (item is BlueprintItemArmor a)
                {
                    text.Append($" | group {a.ProficiencyGroup} AC {a.ArmorBonus} ACP {a.ArmorChecksPenalty} ASF {a.ArcaneSpellFailureChance}% type {a.Type?.name}");
                }
                if (item is BlueprintItemWeapon w)
                {
                    text.Append($" | IsMagic {w.IsMagic} masterwork {w.IsMasterwork} type {w.Type?.name}");
                }
                text.Append($" | notable {item.IsNotable}\n");
                foreach (BlueprintItemEnchantment enchantment in item.Enchantments)
                {
                    text.Append($"  enchantment {enchantment.name} \"{enchantment.Name}\"\n");
                    foreach (BlueprintComponent component in enchantment.ComponentsArray)
                    {
                        WardHarness.Deep(text, component, 2, new HashSet<object>());
                    }
                }
                List<string> refs = references.TryGetValue(item, out List<string> r) ? r : new List<string>();
                text.Append($"  referenced by {refs.Count}: {string.Join("; ", refs.Take(25).ToArray())}\n");
            }
            File.WriteAllText(path, text.ToString());
            return $"READ gear audit: {targets.Count} items written to {path}.";
        }

        private static bool Named(BlueprintItem item, string fragment)
        {
            return item.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0 || (item.Name ?? "").IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Every blueprint whose fields or components (to a few levels of plain data) point at one of the items.</summary>
        private static Dictionary<BlueprintItem, List<string>> References(List<BlueprintScriptableObject> all, HashSet<BlueprintItem> wanted)
        {
            var found = new Dictionary<BlueprintItem, List<string>>();
            foreach (BlueprintScriptableObject blueprint in all)
            {
                if (blueprint is BlueprintItem self && wanted.Contains(self))
                {
                    continue;
                }
                var hits = new HashSet<BlueprintItem>();
                Walk(blueprint, 0, wanted, hits, new HashSet<object>());
                foreach (BlueprintComponent component in blueprint.ComponentsArray ?? new BlueprintComponent[0])
                {
                    Walk(component, 0, wanted, hits, new HashSet<object>());
                }
                foreach (BlueprintItem hit in hits)
                {
                    if (!found.TryGetValue(hit, out List<string> list))
                    {
                        found[hit] = list = new List<string>();
                    }
                    list.Add($"{blueprint.GetType().Name} {blueprint.name}");
                }
            }
            return found;
        }

        private static void Walk(object value, int depth, HashSet<BlueprintItem> wanted, HashSet<BlueprintItem> hits, HashSet<object> seen)
        {
            if (value == null || depth > 4 || !seen.Add(value))
            {
                return;
            }
            for (Type t = value.GetType(); t != null && t != typeof(object) && t != typeof(UnityEngine.Object) && t != typeof(UnityEngine.ScriptableObject); t = t.BaseType)
            {
                foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (f.FieldType.IsPrimitive || f.FieldType.IsEnum || f.FieldType == typeof(string))
                    {
                        continue;
                    }
                    object v;
                    try
                    {
                        v = f.GetValue(value);
                    }
                    catch
                    {
                        continue;
                    }
                    Visit(v, depth, wanted, hits, seen);
                }
            }
        }

        private static void Visit(object v, int depth, HashSet<BlueprintItem> wanted, HashSet<BlueprintItem> hits, HashSet<object> seen)
        {
            if (v == null)
            {
                return;
            }
            if (v is BlueprintItem item)
            {
                if (wanted.Contains(item))
                {
                    hits.Add(item);
                }
                return;
            }
            if (v is UnityEngine.Object)
            {
                return;
            }
            if (v is IEnumerable sequence && !(v is string))
            {
                foreach (object element in sequence)
                {
                    Visit(element, depth + 1, wanted, hits, seen);
                }
                return;
            }
            if (!v.GetType().IsValueType || v.GetType().Namespace?.StartsWith("Kingmaker") == true)
            {
                Walk(v, depth + 1, wanted, hits, seen);
            }
        }
    }
}
