using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Patches
{
    /// <summary>
    /// A slot the player prepares while a readied departure is on record (after a failed transfer, or after loading the
    /// checkpoint) is noted, so the next use of the token readies it even when it holds the same spell as before. The native
    /// preparation itself is unchanged: the slots it filled are found by their newly assigned spell.
    /// </summary>
    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.Memorize))]
    internal static class DepartureMemorizePatch
    {
        private static void Prefix(Spellbook __instance, out Dictionary<SpellSlot, AbilityData> __state)
        {
            __state = DeparturePreparation.Recorded ? DeparturePreparation.SlotsOf(__instance).ToDictionary(s => s, s => s.Spell) : null;
        }

        private static void Postfix(Spellbook __instance, AbilityData data, bool __result, Dictionary<SpellSlot, AbilityData> __state)
        {
            if (__result && __state != null)
            {
                DeparturePreparation.NotePrepared(__instance, __state.Where(s => s.Key.Spell == data && s.Value != data).Select(s => s.Key));
            }
        }
    }
}
