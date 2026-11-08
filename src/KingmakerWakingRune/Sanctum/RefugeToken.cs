using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using KingmakerWakingRune.Blueprints;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>Once-only grant and placement of the one Refuge Token, and its removal on arrival.</summary>
    internal static class RefugeToken
    {
        public static void EnsureGranted()
        {
            Player player = Game.Instance.Player;
            var flags = player.UnlockableFlags;
            if (!SanctumPolicy.ShouldGrantToken(flags.IsUnlocked(ScenarioBlueprints.TokenGrantedFlag), flags.IsUnlocked(ScenarioBlueprints.SanctumEnteredFlag), CountOwned() > 0))
            {
                if (CountOwned() > 0 && !flags.IsUnlocked(ScenarioBlueprints.TokenGrantedFlag))
                {
                    flags.Unlock(ScenarioBlueprints.TokenGrantedFlag);
                }
                return;
            }
            UnitEntityData main = player.MainCharacter.Value;
            ItemEntity token = player.Inventory.Add(ScenarioBlueprints.Token);
            token.Charges = 1;
            UsableSlot[] slots = main.Body.QuickSlots.ToArray();
            UsableSlot slot = slots.FirstOrDefault(s => !s.HasItem);
            string displaced = null;
            if (slot == null)
            {
                // A prepared party may have a full belt: move one stack to the shared stash, never lose it.
                slot = slots.Last();
                displaced = slot.Item.Blueprint.name;
                slot.RemoveItem();
            }
            slot.InsertItem(token);
            flags.Unlock(ScenarioBlueprints.TokenGrantedFlag);
            Log.Info($"Refuge Token granted to '{main.CharacterName}' belt slot {System.Array.IndexOf(slots, slot)} of {slots.Length}; displaced={displaced ?? "none"}.");
        }

        public static int CountOwned()
        {
            return Game.Instance.Player.Inventory.Where(i => i.Blueprint == ScenarioBlueprints.Token).Sum(i => i.Count);
        }

        public static void Consume()
        {
            foreach (ItemEntity item in Game.Instance.Player.Inventory.Where(i => i.Blueprint == ScenarioBlueprints.Token).ToList())
            {
                item.HoldingSlot?.RemoveItem();
                Game.Instance.Player.Inventory.Remove(item);
            }
        }
    }
}
