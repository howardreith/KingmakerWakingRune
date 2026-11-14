using Harmony12;
using Kingmaker.Items;
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
}
