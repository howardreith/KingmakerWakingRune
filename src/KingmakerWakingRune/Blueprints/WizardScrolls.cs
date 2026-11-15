using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using UnityEngine;

namespace KingmakerWakingRune.Blueprints
{
    /// <summary>One audited row: a Wizard spell, the scroll stocked for it (or why none is) and where the scroll comes from.</summary>
    internal sealed class ScrollAuditRow
    {
        public int Level;
        public BlueprintAbility Spell;
        public List<BlueprintItemEquipmentUsable> Scrolls = new List<BlueprintItemEquipmentUsable>();
        public string Source;
        public string Exception;
    }

    /// <summary>
    /// Every player-facing Wizard spell of levels 0-6 in the installed game (the Wizard spell list itself, so spells other
    /// enabled mods add to it are included), each with a scroll. A native scroll is used when one exists: the one written at
    /// the Wizard level, else the cheapest. A spell with variants (Resist Energy, Dispel Magic, the summonings) is stocked
    /// as the game stocks it, one scroll per variant, native where one exists; each is learned as the parent spell. A spell
    /// (or variant) with no native scroll gets a wrapper: a native scroll of the same school cloned onto the working spell,
    /// at the Wizard level and the minimum caster level, with the native price formula, so it casts, is learned and is priced
    /// like any scroll. The audit is the proof: an eligible spell without a scroll and without a recorded reason fails it.
    /// </summary>
    internal static class WizardScrolls
    {
        public const string WizardSpellListGuid = "ba0401fdeb4062f40a7aa95b6f07fe89";
        public const int MaxLevel = 6;

        public static readonly List<ScrollAuditRow> Audit = new List<ScrollAuditRow>();

        private static readonly Dictionary<BlueprintAbility, BlueprintItemEquipmentUsable> s_Wrappers = new Dictionary<BlueprintAbility, BlueprintItemEquipmentUsable>();

        /// <summary>
        /// Runs twice. At library load (register given) it builds and registers the wrappers, so a save holding one always
        /// resolves it. When the shop is assembled (register null) other mods have published their spells too; a spell that
        /// joined the list only then is stocked through its own scroll, or recorded as an exception, never wrapped late.
        /// </summary>
        public static List<BlueprintItemEquipmentUsable> Build(IEnumerable<BlueprintScriptableObject> all, System.Action<BlueprintScriptableObject> register)
        {
            Audit.Clear();
            var stocked = new List<BlueprintItemEquipmentUsable>();
            BlueprintSpellList wizard = all.OfType<BlueprintSpellList>().FirstOrDefault(l => l.AssetGuid == WizardSpellListGuid);
            if (wizard == null)
            {
                Log.Error("Wizard spell list not found; no Wizard scrolls are stocked.");
                return stocked;
            }
            List<BlueprintItemEquipmentUsable> scrolls = all.OfType<BlueprintItemEquipmentUsable>()
                .Where(u => u.Type == UsableItemType.Scroll && u.Ability != null && Copyable(u) && !s_Wrappers.ContainsValue(u)).ToList();
            ILookup<BlueprintAbility, BlueprintItemEquipmentUsable> byAbility = scrolls.ToLookup(u => u.Ability);
            var seenSpells = new HashSet<BlueprintAbility>();
            foreach (SpellLevelList list in wizard.SpellsByLevel.Where(l => l != null && l.SpellLevel <= MaxLevel).OrderBy(l => l.SpellLevel))
            {
                foreach (BlueprintAbility spell in list.Spells.Where(s => s != null))
                {
                    if (!seenSpells.Add(spell))
                    {
                        continue;
                    }
                    var row = new ScrollAuditRow { Level = list.SpellLevel, Spell = spell };
                    Audit.Add(row);
                    if (!SpellLevelList.SpellIsNotLocked(spell))
                    {
                        row.Exception = "locked: its DLC is not enabled in this installation";
                        continue;
                    }
                    BlueprintItemEquipmentUsable direct = Pick(byAbility[spell], list.SpellLevel);
                    if (direct != null)
                    {
                        row.Scrolls.Add(direct);
                        row.Source = Origin(direct);
                    }
                    else
                    {
                        BlueprintAbility[] variants = spell.GetComponent<AbilityVariants>()?.Variants?.Where(v => v != null && v.Parent == spell).ToArray() ?? new BlueprintAbility[0];
                        if (variants.Length > 0)
                        {
                            // As the game does it: one scroll per variant (an item's button has no variant choice, so a scroll
                            // of the parent cannot be cast), each learned as the parent spell. Native where one exists.
                            if (register != null)
                            {
                                // Kept registered, never stocked: the first 0.10 candidates sold a scroll of the parent, and a
                                // save that holds one must still resolve it.
                                Registered(spell, list.SpellLevel, scrolls, register);
                            }
                            foreach (BlueprintAbility variant in variants)
                            {
                                BlueprintItemEquipmentUsable scroll = Pick(byAbility[variant], list.SpellLevel) ?? Registered(variant, list.SpellLevel, scrolls, register);
                                if (scroll != null)
                                {
                                    row.Scrolls.Add(scroll);
                                }
                            }
                            if (row.Scrolls.Count < variants.Length)
                            {
                                row.Scrolls.Clear();
                                row.Exception = register == null
                                    ? "joined the Wizard list after loading (another mod) without a scroll for each variant"
                                    : "no native scroll and no donor scroll to wrap a variant";
                                continue;
                            }
                            row.Source = row.Scrolls.All(s => !s_Wrappers.ContainsValue(s)) ? "native variant scrolls" : "Waking Rune variant wrappers";
                        }
                        else
                        {
                            BlueprintItemEquipmentUsable wrapper = Registered(spell, list.SpellLevel, scrolls, register);
                            if (wrapper == null)
                            {
                                row.Exception = register == null
                                    ? "joined the Wizard list after loading (another mod) without a scroll of its own"
                                    : "no native scroll and no donor scroll to wrap it";
                                continue;
                            }
                            row.Scrolls.Add(wrapper);
                            row.Source = "Waking Rune wrapper";
                        }
                    }
                    foreach (BlueprintItemEquipmentUsable s in row.Scrolls.Where(s => !stocked.Contains(s)))
                    {
                        stocked.Add(s);
                    }
                }
            }
            int wrappers = Audit.Count(r => r.Source != null && r.Source.StartsWith("Waking Rune", System.StringComparison.Ordinal));
            List<ScrollAuditRow> failures = Failures().ToList();
            Log.Info($"Wizard scrolls 0-{MaxLevel} ({(register != null ? "load" : "shop")}): {Audit.Count} spells, {stocked.Count} scrolls ({wrappers} wrappers), {Audit.Count(r => r.Exception != null)} recorded exceptions.");
            foreach (ScrollAuditRow failure in failures)
            {
                Log.Error($"Wizard scroll audit: {failure.Spell.name} (level {failure.Level}) has no scroll and no recorded reason.");
            }
            return stocked;
        }

        /// <summary>Rows that fail the completeness check: an eligible spell with neither a scroll nor a recorded exception.</summary>
        public static IEnumerable<ScrollAuditRow> Failures()
        {
            return Audit.Where(r => r.Scrolls.Count == 0 && r.Exception == null);
        }

        /// <summary>The scroll written at the Wizard level if there is one, else the cheapest; never a free placeholder.</summary>
        private static BlueprintItemEquipmentUsable Pick(IEnumerable<BlueprintItemEquipmentUsable> candidates, int level)
        {
            return candidates.Where(c => c.Cost > 0)
                .OrderBy(c => c.SpellLevel == level ? 0 : 1)
                .ThenBy(c => c.Cost)
                .ThenBy(c => c.AssetGuid)
                .FirstOrDefault();
        }

        /// <summary>A scroll a Wizard can learn from: its copy component (if any) teaches the scroll's own spell, or the parent of its variant.</summary>
        private static bool Copyable(BlueprintItemEquipmentUsable scroll)
        {
            CopyScroll copy = scroll.GetComponent<CopyScroll>();
            return copy == null || copy.CustomSpell == null || copy.CustomSpell == scroll.Ability.Parent;
        }

        private static string Origin(BlueprintItem item)
        {
            return ProviderStock.Owns(item.AssetGuid) ? ProviderStock.DisplayName : "native";
        }

        /// <summary>The wrapper for a spell (or a variant): made and registered at load, looked up afterwards.</summary>
        private static BlueprintItemEquipmentUsable Registered(BlueprintAbility spell, int level, List<BlueprintItemEquipmentUsable> scrolls, System.Action<BlueprintScriptableObject> register)
        {
            if (!s_Wrappers.TryGetValue(spell, out BlueprintItemEquipmentUsable wrapper) && register != null)
            {
                wrapper = Wrap(spell, level, scrolls);
                if (wrapper != null)
                {
                    s_Wrappers[spell] = wrapper;
                    register(wrapper);
                }
            }
            return wrapper;
        }

        private static BlueprintItemEquipmentUsable Wrap(BlueprintAbility spell, int level, List<BlueprintItemEquipmentUsable> scrolls)
        {
            BlueprintItemEquipmentUsable donor = scrolls
                .Where(s => s.GetComponent<CopyScroll>() != null && s.Cost > 0)
                .OrderBy(s => s.Ability.School == spell.School ? 0 : 1)
                .ThenBy(s => s.AssetGuid)
                .FirstOrDefault();
            if (donor == null)
            {
                return null;
            }
            BlueprintItemEquipmentUsable scroll = Object.Instantiate(donor);
            scroll.name = "KWR_Scroll_" + spell.name;
            scroll.AssetGuid = ScenarioIds.Derive("scroll." + spell.AssetGuid);
            scroll.Ability = spell;
            scroll.SpellLevel = level;
            scroll.CasterLevel = level == 0 ? 1 : 2 * level - 1;
            scroll.Charges = 1;
            // An empty name gives the native "Scroll of <spell>"; the cost is the native CL x SL x 25 (a cantrip, 12 gp).
            AccessTools.Field(typeof(BlueprintItem), "m_DisplayNameText").SetValue(scroll, new LocalizedString());
            AccessTools.Field(typeof(BlueprintItem), "m_DescriptionText").SetValue(scroll, AccessTools.Field(typeof(BlueprintUnitFact), "m_Description").GetValue(spell));
            AccessTools.Field(typeof(BlueprintItem), "m_Cost").SetValue(scroll, level == 0 ? 12 : 0);
            return scroll;
        }
    }
}
