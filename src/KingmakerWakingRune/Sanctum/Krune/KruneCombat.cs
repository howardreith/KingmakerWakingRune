using System.Linq;
using Kingmaker.EntitySystem.Entities;
using KingmakerWakingRune.Sanctum.Wards;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Krune's combat capabilities, set up once per actor (WR-FINAL): his book filled from the printed preparation and his
    /// director brain's actions. A fresh awakening does it when he steps out; a save made before this build (an emerged or
    /// checkpoint Krune) does it at its first tick. It never refills anything he has already spent or lost.
    /// </summary>
    internal static class KruneCombat
    {
        public static void EnsureInitialized(SanctumState state, UnitEntityData actor)
        {
            KruneState krune = state.Krune;
            if (krune.CombatInitialized || actor == null || actor.Descriptor.State.IsDead || !actor.IsInGame)
            {
                return;
            }
            string book = KruneSpellbook.Fill(actor) + " Gear: " + KruneGear.Equip(actor) + ".";
            EnsureBrain(actor);
            krune.CombatInitialized = true;
            krune.CombatInitializedNote = book;
            KruneAwakening.Note("combat capabilities set up once: " + book);
        }

        /// <summary>
        /// A saved unit keeps the action list its brain had when it was made: a Krune from an earlier build has only the
        /// interim attack. Every load brings it up to the blueprint's (the director first), then lists what is castable now,
        /// which restores nothing.
        /// </summary>
        public static void EnsureBrain(UnitEntityData actor)
        {
            if (actor == null || actor.Descriptor.State.IsDead)
            {
                return;
            }
            Kingmaker.Controllers.Brain.Blueprints.BlueprintBrain brain = actor.Blueprint.Brain;
            if (brain != null && (actor.Brain.Blueprint != brain || actor.Brain.Actions.Count != brain.Actions.Length || actor.Brain.Actions.Select(a => a.Blueprint).Except(brain.Actions).Any()))
            {
                actor.Brain.Blueprint = brain;
                actor.Brain.Actions.Clear();
                actor.Brain.Actions.AddRange(brain.Actions.Select(a => new Kingmaker.UnitLogic.AiAction(a)));
                KruneAwakening.Note($"brain actions brought up to date ({brain.Actions.Length}: {string.Join(", ", brain.Actions.Select(a => a.name).ToArray())}).");
            }
            actor.Brain.RestoreAvailableActions();
            EnsureFreedom(actor);
        }

        /// <summary>
        /// A ring of freedom of movement saved before 0.9.11 carries only the native hold immunity: its freedom of movement is
        /// added once (the same ring, worn again so the wearer gains it). Nothing else on the ring changes.
        /// </summary>
        private static void EnsureFreedom(UnitEntityData actor)
        {
            Kingmaker.Items.ItemEntity ring = actor.Inventory.Items.FirstOrDefault(i => i.Blueprint == KruneBlueprints.RingOfFreedom);
            if (ring == null || KruneBlueprints.RingOfFreedomEnchantment == null || ring.HasEnchantment(KruneBlueprints.RingOfFreedomEnchantment))
            {
                return;
            }
            ring.AddEnchantment(KruneBlueprints.RingOfFreedomEnchantment, new Kingmaker.UnitLogic.Mechanics.MechanicsContext(actor, actor.Descriptor, KruneBlueprints.RingOfFreedomEnchantment));
            Kingmaker.Items.Slots.ItemSlot slot = ring.HoldingSlot;
            if (slot != null)
            {
                Kingmaker.Items.Slots.ItemSlot.IgnoreLock.Retain();
                try
                {
                    slot.RemoveItem();
                    slot.InsertItem(ring);
                }
                finally
                {
                    Kingmaker.Items.Slots.ItemSlot.IgnoreLock.Release();
                }
            }
            KruneAwakening.Note($"ring of freedom of movement brought up to date: freedom of movement {(actor.Descriptor.Progression.Features.HasFact(KruneBlueprints.FreedomOfMovement) ? "worn" : "NOT worn")}.");
        }
    }
}
