using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using KingmakerWakingRune.Sanctum.Wards;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Krune's gear after his death. Before his body is consumed, every lootable item he holds or wears (the two artifacts,
    /// his remaining wand charges, unspent diamonds and the rest) is moved, as the same item instances, into one native
    /// ground loot bag where he fell, saved with the area. Nothing is copied, refilled or spawned "if missing"; spent
    /// diamonds stay spent. The artifacts' identities are recorded for the return and the handover.
    /// </summary>
    internal static class KruneLoot
    {
        public static bool IsSpear(ItemEntity item)
        {
            return item != null && item.Blueprint == KruneBlueprints.Spear;
        }

        public static bool IsRod(ItemEntity item)
        {
            return item != null && item.Blueprint == KruneBlueprints.Rod;
        }

        public static bool IsArtifact(ItemEntity item)
        {
            return IsSpear(item) || IsRod(item);
        }

        /// <summary>Moves his remaining gear into a stable loot bag before his body goes. Idempotent: once a bag exists, nothing more.</summary>
        public static void Secure(SanctumState state, UnitEntityData actor)
        {
            ConclusionState c = state.Conclusion;
            if (c.LootContainerId.Length > 0 || actor == null)
            {
                return;
            }
            var items = actor.Inventory.Items.Where(i => i.IsLootable).ToList();
            ItemEntity spear = items.FirstOrDefault(IsSpear);
            ItemEntity rod = items.FirstOrDefault(IsRod);
            // Items carry no saved identity in this game; each artifact blueprint exists once, on Krune: it is the identity.
            c.SpearId = spear?.Blueprint.name ?? string.Empty;
            c.RodId = rod?.Blueprint.name ?? string.Empty;
            if (items.Count == 0)
            {
                c.LootContainerId = "none";
                KruneAwakening.Note("his body held nothing to recover.");
                return;
            }
            Vector3 at = ObstacleAnalyzer.GetNearestNode(actor.Position).clampedPosition;
            DroppedLoot bag = Game.Instance.EntityCreator.SpawnEntityView(BlueprintRoot.Instance.Prefabs.DroppedLootBag, at, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
            bag.Loot = new ItemsCollection();
            ItemSlot.IgnoreLock.Retain();
            try
            {
                foreach (ItemEntity item in items)
                {
                    actor.Inventory.Transfer(item, bag.Loot);
                }
            }
            finally
            {
                ItemSlot.IgnoreLock.Release();
            }
            c.LootContainerId = bag.Data?.UniqueId ?? "bag";
            KruneAwakening.Note($"his gear set down where he fell ({bag.Loot.Items.Count} items: {string.Join(", ", bag.Loot.Items.Select(i => i.Name + (i.Count > 1 ? " x" + i.Count : string.Empty)).ToArray())}); spear {c.SpearId}, rod {c.RodId}.");
        }
    }
}

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>The two quest artifacts cannot be dropped (the game's only way to discard an item): they are carried home or left in their bag.</summary>
    [Harmony12.HarmonyPatch(typeof(ItemsCollection), nameof(ItemsCollection.DropItem))]
    internal static class ArtifactDropPatch
    {
        private static bool Prefix(ItemEntity item, ref bool __result)
        {
            if (!KruneLoot.IsArtifact(item))
            {
                return true;
            }
            __result = false;
            Kingmaker.PubSubSystem.EventBus.RaiseEvent(delegate(Kingmaker.PubSubSystem.IWarningNotificationUIHandler h) { h.HandleWarning("The Society asked for this artifact: it is not something to throw away."); });
            return false;
        }
    }
}
