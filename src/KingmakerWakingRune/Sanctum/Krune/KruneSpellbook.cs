using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Krune's real wizard book, filled once from <see cref="KruneBook"/>: the game's own spells, quickened and empowered
    /// entries as the game's own metamagic spells in their raised slots, encounter-local spells where the game has none, and
    /// held slots named with their reason. Mage Armor's slot is spent (cast before hibernation). After the fill the book is
    /// native saved state: a load never refills it.
    /// </summary>
    internal static class KruneSpellbook
    {
        public static Spellbook Book(UnitEntityData krune)
        {
            return krune?.Descriptor.Spellbooks.FirstOrDefault(b => b.Blueprint == KruneBlueprints.WizardClass?.Spellbook);
        }

        public static string Fill(UnitEntityData krune)
        {
            Spellbook book = Book(krune);
            if (book == null)
            {
                return "no wizard book";
            }
            foreach (SpellSlot slot in book.GetAllMemorizedSpells().ToList())
            {
                book.ForgetMemorized(slot);
            }
            var failed = new List<string>();
            int placed = 0;
            foreach (BookEntry entry in KruneBook.Plan())
            {
                BlueprintAbility ability = KruneSpells.Resolve(entry.Blueprint);
                if (ability == null)
                {
                    failed.Add($"{entry.Spell} (no blueprint {entry.Blueprint})");
                    continue;
                }
                if (!book.IsKnown(ability))
                {
                    book.AddKnown(entry.BaseLevel, ability);
                }
                if (entry.Level == 0)
                {
                    // Cantrips are cast at will in this game: known, never memorized.
                    continue;
                }
                AbilityData data = new AbilityData(ability, book);
                if (entry.Metamagic.Length > 0)
                {
                    data.MetamagicData = new MetamagicData();
                    data.MetamagicData.Add(entry.Metamagic == "Quickened" ? Metamagic.Quicken : Metamagic.Empower);
                    data.MetamagicData.SpellLevelCost = KruneBook.MetamagicCost(entry.Metamagic);
                    if (!book.GetCustomSpells(entry.Level).Any(c => c.Equals(data)))
                    {
                        book.AddCustomSpell(data);
                    }
                }
                if (book.Memorize(data))
                {
                    placed++;
                }
                else
                {
                    failed.Add($"{entry.Metamagic} {entry.Spell} ({entry.Level})");
                }
            }
            book.Rest();
            SpellSlot armor = book.GetAllMemorizedSpells().FirstOrDefault(s => s.Spell?.Blueprint == KruneBlueprints.MageArmorSpell && s.Available);
            bool spent = armor != null && book.Spend(armor.Spell);
            return $"book filled: {placed} of {KruneBook.Plan().Count(e => e.Level > 0)} slots (CL {book.CasterLevel}); Mage Armor's slot spent {spent}; per level [{string.Join(" ", Enumerable.Range(1, 9).Select(l => $"{l}:{book.GetMemorizedSpells(l).Count()}/{book.CalcSlotsLimit(l, SpellSlotType.Common)}").ToArray())}]{(failed.Count == 0 ? string.Empty : "; NOT placed: " + string.Join(", ", failed.ToArray()))}.";
        }

        /// <summary>What is still castable: one line per slot, for the director and the diagnostics.</summary>
        public static IEnumerable<SpellSlot> Ready(UnitEntityData krune)
        {
            Spellbook book = Book(krune);
            return book == null ? Enumerable.Empty<SpellSlot>() : book.GetAllMemorizedSpells().Where(s => s.Spell != null && s.Available);
        }

        public static bool IsKrunes(Spellbook book)
        {
            return book != null && book.Owner?.Unit?.Get<KrunePart>() != null && book.Blueprint == KruneBlueprints.WizardClass?.Spellbook;
        }
    }

    /// <summary>
    /// His book holds exactly the printed number of slots at each level. The printed list already counts his school slot, and
    /// this game would put only plain conjuration spells from its own list there (none of his 8th-level entries qualify), so
    /// the book has the printed total as ordinary slots and no separate school slot. Only Krune's own wizard book.
    /// </summary>
    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.GetSpellsPerDay))]
    internal static class KruneSlotsPatch
    {
        private static void Postfix(Spellbook __instance, int spellLevel, ref int __result)
        {
            if (spellLevel > 0 && spellLevel <= 9 && KruneSpellbook.IsKrunes(__instance))
            {
                __result = KruneBook.Slots(spellLevel);
            }
        }
    }

    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.CalcSlotsLimit))]
    internal static class KruneSchoolSlotPatch
    {
        private static void Postfix(Spellbook __instance, SpellSlotType slotType, ref int __result)
        {
            if (slotType == SpellSlotType.Favorite && KruneSpellbook.IsKrunes(__instance))
            {
                __result = 0;
            }
        }
    }
}
