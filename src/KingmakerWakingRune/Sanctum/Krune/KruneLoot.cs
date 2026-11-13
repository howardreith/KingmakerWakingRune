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

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Wielding the dragon-tooth spear is not carrying it (page 15: Ego 22, it serves Lissala and harangues other wielders).
    /// Carrying it home needs no check. A party member who takes it in hand faces its Ego: a Will save against DC 22. On a
    /// failure the spear will not serve: it leaves the hand for the shared inventory, and that character may not take it up
    /// again for a day. On a success it can be wielded as a +3 longspear, haranguing its bearer; its powers serve Krune alone.
    /// </summary>
    [Harmony12.HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.OnDidEquipped))]
    internal static class SpearEgoPatch
    {
        private static void Postfix(ItemEntity __instance, Kingmaker.UnitLogic.UnitDescriptor wielder)
        {
            if (!KruneLoot.IsSpear(__instance) || wielder?.Unit == null || !wielder.Unit.IsPlayerFaction || wielder.Unit == KruneAwakening.Actor)
            {
                return;
            }
            UnitEntityData unit = wielder.Unit;
            double now = Kingmaker.Game.Instance.TimeController.GameTime.TotalSeconds;
            // The refusal is kept in the saved conclusion state: a reload does not offer a fresh roll.
            System.Collections.Generic.Dictionary<string, double> refusals = WardStore.Existing?.Conclusion.SpearRefusedUntil ?? new System.Collections.Generic.Dictionary<string, double>();
            bool refused = refusals.TryGetValue(unit.UniqueId, out double until) && now < until;
            if (!refused)
            {
                Kingmaker.RuleSystem.Rules.RuleSavingThrow save = Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.RuleSavingThrow(unit, Kingmaker.EntitySystem.Stats.SavingThrowType.Will, 22));
                Log.Info($"Dragon-tooth spear: {unit.CharacterName} takes it in hand: Will {save.RollResult} vs DC 22 ({(save.IsPassed ? "mastered" : "refused")}).");
                if (save.IsPassed)
                {
                    WardRuntime.Say($"The dragon-tooth spear yields to {unit.CharacterName}, muttering about rash, spontaneous hands.");
                    return;
                }
                refusals[unit.UniqueId] = now + Wards.WardProfile.DaySeconds;
            }
            WardRuntime.Say($"The dragon-tooth spear will not serve {unit.CharacterName}: it twists out of the grip.");
            Kingmaker.Items.Slots.ItemSlot slot = __instance.HoldingSlot;
            Kingmaker.Items.Slots.ItemSlot.IgnoreLock.Retain();
            try
            {
                slot?.RemoveItem();
            }
            finally
            {
                Kingmaker.Items.Slots.ItemSlot.IgnoreLock.Release();
            }
        }
    }
}
