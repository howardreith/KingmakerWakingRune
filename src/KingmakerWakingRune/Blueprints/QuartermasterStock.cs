using System;
using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Items;
using Kingmaker.Utility;
using Kingmaker.Blueprints.Loot;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;

namespace KingmakerWakingRune.Blueprints
{
    internal struct CatalogueEntry
    {
        public readonly string Guid;
        public readonly int Count;

        public CatalogueEntry(string guid, int count)
        {
            Guid = guid;
            Count = count;
        }
    }

    /// <summary>
    /// The Quartermaster's native shared vendor table. The native vendor system owns stock, prices and persistence; its own
    /// reconciliation (SharedVendorTables.GetTable) applies catalogue changes to a visited shop once, by difference, so new
    /// lines arrive, a removed retail line leaves, and what the player sold or bought is kept.
    /// The table is assembled in two steps: the CSV catalogue and the Wizard scroll wrappers at library load (so saves always
    /// resolve them), then, on the first lodge entry of the process, the Wizard scroll selection and the optional provider's
    /// lines, after every mod has published its content.
    /// </summary>
    internal static class QuartermasterStock
    {
        /// <summary>Copies of each stocked scroll (and the floor for any scroll line in the CSV).</summary>
        public const int ScrollCount = 99;

        public static BlueprintSharedVendorTable Table { get; private set; }

        /// <summary>Every line of the assembled catalogue and its target count, for the resupply and the audit.</summary>
        public static readonly Dictionary<BlueprintItem, int> Catalogue = new Dictionary<BlueprintItem, int>();

        public static readonly Dictionary<BlueprintItem, string> Sources = new Dictionary<BlueprintItem, string>();

        /// <summary>The Quartermaster's shelf as the shop last opened it (see <see cref="Patches.QuartermasterMergePatch"/>).</summary>
        public static ItemsCollection Shelf;

        private static readonly List<KeyValuePair<BlueprintItem, int>> s_Base = new List<KeyValuePair<BlueprintItem, int>>();
        private static bool s_Assembled;

        public static void Attach(BlueprintUnit quartermaster, List<BlueprintScriptableObject> all, Action<BlueprintScriptableObject> register)
        {
            Dictionary<string, BlueprintItem> items = all.OfType<BlueprintItem>().GroupBy(i => i.AssetGuid).ToDictionary(g => g.Key, g => g.First());
            foreach (CatalogueEntry entry in QuartermasterCatalogue.Items)
            {
                if (!items.TryGetValue(entry.Guid, out BlueprintItem item))
                {
                    Log.Error($"Quartermaster stock item {entry.Guid} is not in the installed game; skipped.");
                    continue;
                }
                s_Base.Add(new KeyValuePair<BlueprintItem, int>(item, entry.Count));
            }
            WizardScrolls.Build(all, register);
            Table = ScriptableObject.CreateInstance<BlueprintSharedVendorTable>();
            Table.name = "KWR_QuartermasterStock";
            Table.AssetGuid = ScenarioIds.Derive("vendor.quartermaster");
            Table.ComponentsArray = new BlueprintComponent[0];
            register(Table);

            var vendor = ScriptableObject.CreateInstance<AddSharedVendor>();
            vendor.name = "KWR_QuartermasterVendor";
            vendor.m_Table = Table;
            quartermaster.ComponentsArray = quartermaster.ComponentsArray.Concat(new BlueprintComponent[] { vendor }).ToArray();
            Log.Info($"Quartermaster catalogue prepared: {s_Base.Count} catalogue lines; scrolls and optional stock join on the first lodge entry.");
        }

        /// <summary>Once per process, before the shop can first open: the full catalogue onto the table blueprint.</summary>
        public static void EnsureAssembled()
        {
            if (s_Assembled || Table == null)
            {
                return;
            }
            s_Assembled = true;
            Catalogue.Clear();
            Sources.Clear();
            foreach (KeyValuePair<BlueprintItem, int> line in s_Base)
            {
                Add(line.Key, line.Value, "catalogue");
            }
            foreach (BlueprintItemEquipmentUsable scroll in WizardScrolls.Build(ResourcesLibrary.LibraryObject.BlueprintsByAssetId.Values, null))
            {
                Add(scroll, ScrollCount, "Wizard scrolls");
            }
            foreach (BlueprintItem scroll in Catalogue.Keys.OfType<BlueprintItemEquipmentUsable>().Where(u => u.Type == UsableItemType.Scroll).ToList())
            {
                Catalogue[scroll] = Math.Max(Catalogue[scroll], ScrollCount);
            }
            foreach (KeyValuePair<BlueprintItem, int> line in ProviderStock.Resolve())
            {
                Add(line.Key, line.Value, ProviderStock.DisplayName);
            }
            Table.ComponentsArray = Catalogue.Select(pair => (BlueprintComponent)Pack(pair.Key, pair.Value)).ToArray();
            Log.Info($"Quartermaster stock assembled: {Catalogue.Count} lines, {Catalogue.Values.Sum()} items; provider: {ProviderStock.Status}.");
        }

        /// <summary>
        /// The bounded resupply, run as the shop opens: each catalogue line whose shelf count (merchant stock and anything
        /// the party sold, together) is below its catalogue count is topped up with new merchant stock to that count. Nothing
        /// above it is touched, so goods the party sold are never duplicated or refreshed; prices are native, and selling pays
        /// less than buying, so it is no profit loop. One pass over the catalogue per opening; no frame work.
        /// </summary>
        public static void Resupply()
        {
            EnsureAssembled();
            ItemsCollection shelf = Game.Instance.Player.SharedVendorTables.GetTable(Table);
            Shelf = shelf;
            if (shelf == null)
            {
                return;
            }
            Dictionary<BlueprintItem, int> onShelf = shelf.Items.GroupBy(i => i.Blueprint).ToDictionary(g => g.Key, g => g.Sum(i => i.Count));
            int lines = 0, added = 0;
            foreach (KeyValuePair<BlueprintItem, int> line in Catalogue)
            {
                int missing = line.Value - onShelf.Get(line.Key, 0);
                if (missing > 0)
                {
                    // New stock never joins a part-spent item the party sold here (Patches.QuartermasterMergePatch).
                    shelf.Add(line.Key, missing);
                    lines++;
                    added += missing;
                }
            }
            if (added > 0)
            {
                Log.Info($"Quartermaster resupply: {lines} lines topped up with {added} new items.");
            }
        }

        private static void Add(BlueprintItem item, int count, string source)
        {
            if (Catalogue.TryGetValue(item, out int existing))
            {
                Catalogue[item] = Math.Max(existing, count);
                return;
            }
            Catalogue[item] = count;
            Sources[item] = source;
        }

        private static LootItemsPackFixed Pack(BlueprintItem item, int count)
        {
            var pack = ScriptableObject.CreateInstance<LootItemsPackFixed>();
            pack.name = "KWR_Stock_" + item.AssetGuid;
            var lootItem = new LootItem();
            AccessTools.Field(typeof(LootItem), "m_Type").SetValue(lootItem, LootItemType.Item);
            AccessTools.Field(typeof(LootItem), "m_Item").SetValue(lootItem, item);
            AccessTools.Field(typeof(LootItemsPackFixed), "m_Item").SetValue(pack, lootItem);
            AccessTools.Field(typeof(LootItemsPackFixed), "m_Count").SetValue(pack, count);
            return pack;
        }
    }
}
