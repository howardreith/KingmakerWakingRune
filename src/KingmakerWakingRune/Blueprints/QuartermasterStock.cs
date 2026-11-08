using System;
using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
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
    /// The Quartermaster's native shared vendor table. The native vendor system owns stock, prices and
    /// persistence (stock is kept in the save and not regenerated).
    /// </summary>
    internal static class QuartermasterStock
    {
        public static BlueprintSharedVendorTable Table { get; private set; }

        public static void Attach(BlueprintUnit quartermaster, List<BlueprintScriptableObject> all, Action<BlueprintScriptableObject> register)
        {
            Dictionary<string, BlueprintItem> items = all.OfType<BlueprintItem>().GroupBy(i => i.AssetGuid).ToDictionary(g => g.Key, g => g.First());
            var components = new List<BlueprintComponent>();
            foreach (CatalogueEntry entry in QuartermasterCatalogue.Items)
            {
                if (!items.TryGetValue(entry.Guid, out BlueprintItem item))
                {
                    Log.Error($"Quartermaster stock item {entry.Guid} is not in the installed game; skipped.");
                    continue;
                }
                var pack = ScriptableObject.CreateInstance<LootItemsPackFixed>();
                pack.name = "KWR_Stock_" + entry.Guid;
                var lootItem = new LootItem();
                AccessTools.Field(typeof(LootItem), "m_Type").SetValue(lootItem, LootItemType.Item);
                AccessTools.Field(typeof(LootItem), "m_Item").SetValue(lootItem, item);
                AccessTools.Field(typeof(LootItemsPackFixed), "m_Item").SetValue(pack, lootItem);
                AccessTools.Field(typeof(LootItemsPackFixed), "m_Count").SetValue(pack, entry.Count);
                components.Add(pack);
            }
            Table = ScriptableObject.CreateInstance<BlueprintSharedVendorTable>();
            Table.name = "KWR_QuartermasterStock";
            Table.AssetGuid = ScenarioIds.Derive("vendor.quartermaster");
            Table.ComponentsArray = components.ToArray();
            register(Table);

            var vendor = ScriptableObject.CreateInstance<AddSharedVendor>();
            vendor.name = "KWR_QuartermasterVendor";
            vendor.m_Table = Table;
            quartermaster.ComponentsArray = quartermaster.ComponentsArray.Concat(new BlueprintComponent[] { vendor }).ToArray();
            Log.Info($"Quartermaster stock prepared: {components.Count} lines.");
        }
    }
}
