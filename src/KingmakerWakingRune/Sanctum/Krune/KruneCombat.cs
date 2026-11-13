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
            string book = KruneSpellbook.Fill(actor);
            actor.Brain.RestoreAvailableActions();
            krune.CombatInitialized = true;
            krune.CombatInitializedNote = book;
            KruneAwakening.Note("combat capabilities set up once: " + book);
        }
    }
}
