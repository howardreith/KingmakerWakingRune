using Harmony12;
using Kingmaker.Blueprints.Items;
using Kingmaker.Items;
using Kingmaker.UnitLogic.Parts;
using KingmakerWakingRune.Blueprints;

namespace KingmakerWakingRune.Patches
{
    /// <summary>
    /// The Quartermaster's shelf is a native shared vendor table, which stacks everything: a part-spent wand the party sells
    /// there would join the merchant's full stack of the same wand and take on its charges. On that shelf only, items with
    /// charges merge only when their charges match, so a sold wand stays the item it was and new stock stays new.
    /// </summary>
    [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.CanBeMerged))]
    internal static class QuartermasterMergePatch
    {
        private static void Postfix(ItemEntity __instance, ItemEntity other, ref bool __result)
        {
            if (__result && other != null && QuartermasterStock.Shelf != null && __instance.Collection == QuartermasterStock.Shelf
                && __instance.IsSpendCharges && __instance.Charges != other.Charges)
            {
                __result = false;
            }
        }
    }

    /// <summary>The charges of part-spent items on the Quartermaster's shelf are recorded as the game saves (see <see cref="QuartermasterCharges"/>).</summary>
    [HarmonyPatch(typeof(SharedVendorTables), nameof(SharedVendorTables.PreSave))]
    internal static class QuartermasterChargesSavePatch
    {
        private static void Postfix(SharedVendorTables __instance)
        {
            QuartermasterCharges.Record(__instance);
        }
    }

    /// <summary>...and given back when the shelf is rebuilt from the save.</summary>
    [HarmonyPatch(typeof(SharedVendorTables), nameof(SharedVendorTables.GetTable))]
    internal static class QuartermasterChargesLoadPatch
    {
        /// <summary>A saved shelf keeps whatever it holds of a retired provider line (see <see cref="QuartermasterStock.PreserveRetiredLines"/>).</summary>
        private static void Prefix(SharedVendorTables __instance, BlueprintSharedVendorTable blueprint)
        {
            QuartermasterStock.PreserveRetiredLines(__instance, blueprint);
        }

        private static void Postfix(BlueprintSharedVendorTable blueprint, ItemsCollection __result)
        {
            if (blueprint != null && blueprint == QuartermasterStock.Table)
            {
                QuartermasterCharges.Restore(__result);
            }
        }
    }
}
