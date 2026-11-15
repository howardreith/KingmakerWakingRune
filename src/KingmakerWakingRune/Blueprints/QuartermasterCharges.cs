using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
using Newtonsoft.Json;

namespace KingmakerWakingRune.Blueprints
{
    /// <summary>Part-spent items the party sold to the Quartermaster, saved on the main character: "guid:charges:count".</summary>
    public sealed class QuartermasterChargesPart : UnitPart
    {
        [JsonProperty]
        public List<string> Items = new List<string>();
    }

    /// <summary>
    /// The native shared vendor table saves each item as blueprint and count only, so a part-spent wand sold to the
    /// Quartermaster would come back from a save as a full one. Its charges are recorded as the game saves and given back
    /// when the shelf is rebuilt from the save: the same number of items is split off the merchant's stack with the
    /// recorded charges. Counts and prices stay native; nothing is created.
    /// </summary>
    internal static class QuartermasterCharges
    {
        private static ItemsCollection s_Restored;

        /// <summary>As the game saves: the shelf's part-spent items, when the shelf exists in this session (else the record stands).</summary>
        public static void Record(SharedVendorTables tables)
        {
            UnitEntityData main = Game.Instance.Player.MainCharacter.Value;
            var shelves = Traverse.Create(tables).Field("m_Dictionary").GetValue<Dictionary<BlueprintSharedVendorTable, ItemsCollection>>();
            if (main == null || QuartermasterStock.Table == null || shelves == null || !shelves.TryGetValue(QuartermasterStock.Table, out ItemsCollection shelf))
            {
                return;
            }
            List<string> spent = shelf.Items.Where(i => i.IsSpendCharges && i.Charges != Full(i))
                .Select(i => string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}", i.Blueprint.AssetGuid, i.Charges, i.Count)).ToList();
            if (spent.Count == 0)
            {
                main.Descriptor.Remove<QuartermasterChargesPart>();
                return;
            }
            main.Descriptor.Ensure<QuartermasterChargesPart>().Items = spent;
        }

        /// <summary>Once per shelf instance (each load builds a new one): the recorded items get their charges back.</summary>
        public static void Restore(ItemsCollection shelf)
        {
            if (shelf == null || shelf == s_Restored)
            {
                return;
            }
            s_Restored = shelf;
            QuartermasterStock.Shelf = shelf;
            List<string> items = Game.Instance.Player.MainCharacter.Value?.Descriptor.Get<QuartermasterChargesPart>()?.Items;
            if (items == null)
            {
                return;
            }
            var restored = new List<string>();
            foreach (string entry in items)
            {
                string[] parts = entry.Split(':');
                BlueprintItem blueprint = parts.Length == 3 ? ResourcesLibrary.TryGetBlueprint<BlueprintItem>(parts[0]) : null;
                if (blueprint == null || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int charges) || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count))
                {
                    continue;
                }
                ItemEntity stack = shelf.Items.FirstOrDefault(i => i.Blueprint == blueprint && i.Charges == Full(i) && i.Count >= count);
                if (stack == null)
                {
                    continue;
                }
                ItemEntity item = shelf.Remove(stack, count);
                item.Charges = charges;
                shelf.Add(item);
                restored.Add($"{blueprint.name} x{count} at {charges} charges");
            }
            if (restored.Count > 0)
            {
                Log.Info($"Quartermaster: part-spent items sold here keep their charges: {string.Join(", ", restored.ToArray())}.");
            }
        }

        private static int Full(ItemEntity item)
        {
            return (item.Blueprint as BlueprintItemEquipment)?.Charges ?? item.Charges;
        }
    }
}
