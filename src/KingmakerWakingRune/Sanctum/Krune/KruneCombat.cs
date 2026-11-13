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
        }
    }
}
