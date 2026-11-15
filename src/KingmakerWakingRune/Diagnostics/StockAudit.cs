using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Utility;
using KingmakerWakingRune.Blueprints;

namespace KingmakerWakingRune.Diagnostics
{
    /// <summary>
    /// Read only: the assembled Quartermaster catalogue as Markdown, from the live blueprints: every line with its count,
    /// price and source; the Wizard scroll audit (spell, level, scroll, caster level, price, source, or the recorded reason);
    /// and the skill and ability coverage, read from what each stocked item's enchantments actually grant.
    /// </summary>
    internal static class StockAudit
    {
        private static readonly StatType[] Skills =
        {
            StatType.SkillAthletics, StatType.SkillMobility, StatType.SkillThievery, StatType.SkillStealth,
            StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld, StatType.SkillLoreNature, StatType.SkillLoreReligion,
            StatType.SkillPerception, StatType.SkillPersuasion, StatType.SkillUseMagicDevice,
            StatType.CheckBluff, StatType.CheckDiplomacy, StatType.CheckIntimidate,
        };

        private static readonly StatType[] Abilities =
        {
            StatType.Strength, StatType.Dexterity, StatType.Constitution, StatType.Intelligence, StatType.Wisdom, StatType.Charisma,
        };

        public static string Write(string path)
        {
            QuartermasterStock.EnsureAssembled();
            var catalogue = QuartermasterStock.Catalogue;
            var md = new StringBuilder();
            List<ScrollAuditRow> audit = WizardScrolls.Audit;
            int failures = WizardScrolls.Failures().Count();
            md.AppendLine("# Quartermaster stock audit (generated in game)");
            md.AppendLine();
            md.AppendLine($"- Lines: {catalogue.Count}; items: {catalogue.Values.Sum()}.");
            md.AppendLine($"- Optional provider: {ProviderStock.Status}.");
            md.AppendLine($"- Wizard spells 0-{WizardScrolls.MaxLevel}: {audit.Count}; with scrolls: {audit.Count(r => r.Scrolls.Count > 0)}; " +
                          $"wrappers: {audit.Count(r => r.Source != null && r.Source.StartsWith("Waking Rune", StringComparison.Ordinal))}; recorded exceptions: {audit.Count(r => r.Exception != null)}; " +
                          $"unexplained omissions: {failures} ({(failures == 0 ? "PASS" : "FAIL")}).");
            md.AppendLine();

            md.AppendLine("## Ability and skill coverage (from enchantment effects)");
            md.AppendLine();
            md.AppendLine("| Stat | Stocked items (bonus, slot, price) |");
            md.AppendLine("|---|---|");
            foreach (StatType stat in Abilities.Concat(Skills))
            {
                var holders = catalogue.Keys.Where(i => !(i is BlueprintItemEquipmentUsable))
                    .Select(i => new { Item = i, Bonus = Bonuses(i).Where(b => b.Key == stat).Select(b => b.Value).DefaultIfEmpty(0).Max() })
                    .Where(x => x.Bonus > 0)
                    .OrderBy(x => x.Item.Cost)
                    .Select(x => $"{x.Item.Name} (+{x.Bonus}, {Slot(x.Item)}, {x.Item.Cost:N0})");
                md.AppendLine($"| {stat} | {string.Join("; ", holders.ToArray()).Replace("|", "/")} |");
            }
            md.AppendLine();

            md.AppendLine("## Wizard scrolls, levels 0-6");
            md.AppendLine();
            md.AppendLine("| Level | Spell | Scroll | SL | CL | Price | Count | Source |");
            md.AppendLine("|---:|---|---|---:|---:|---:|---:|---|");
            foreach (ScrollAuditRow row in audit.OrderBy(r => r.Level).ThenBy(r => Safe(() => r.Spell.Name)))
            {
                if (row.Scrolls.Count == 0)
                {
                    md.AppendLine($"| {row.Level} | {Safe(() => row.Spell.Name)} `{row.Spell.AssetGuid}` | none | | | | | {row.Exception ?? "UNEXPLAINED"} |");
                    continue;
                }
                foreach (BlueprintItemEquipmentUsable scroll in row.Scrolls)
                {
                    md.AppendLine($"| {row.Level} | {Safe(() => row.Spell.Name)} | {Safe(() => scroll.Name)} `{scroll.AssetGuid}` | {scroll.SpellLevel} | {scroll.CasterLevel} | {scroll.Cost:N0} | {catalogue.Get(scroll, 0)} | {row.Source} |");
                }
            }
            md.AppendLine();

            md.AppendLine("## Every line");
            md.AppendLine();
            md.AppendLine("| Item | Kind | Count | Price | Source | Blueprint |");
            md.AppendLine("|---|---|---:|---:|---|---|");
            foreach (var pair in catalogue.OrderBy(p => QuartermasterStock.Sources.Get(p.Key, "")).ThenBy(p => Slot(p.Key)).ThenBy(p => p.Key.Cost))
            {
                md.AppendLine($"| {Safe(() => pair.Key.Name)} | {Slot(pair.Key)} | {pair.Value} | {pair.Key.Cost:N0} | {QuartermasterStock.Sources.Get(pair.Key, "")} | `{pair.Key.AssetGuid}` |");
            }
            File.WriteAllText(path, md.ToString());
            return $"stock audit written to {path}: {catalogue.Count} lines, {catalogue.Values.Sum()} items; wizard spells {audit.Count}, " +
                   $"unexplained omissions {failures}; provider {ProviderStock.Status}";
        }

        /// <summary>Every stat bonus an item's enchantments grant (component fields Stat and Value), by stat.</summary>
        private static IEnumerable<KeyValuePair<StatType, int>> Bonuses(BlueprintItem item)
        {
            foreach (BlueprintItemEnchantment e in item.Enchantments.Where(e => e != null))
            {
                foreach (BlueprintComponent c in e.ComponentsArray.Where(c => c != null))
                {
                    var stat = c.GetType().GetField("Stat");
                    var value = c.GetType().GetField("Value");
                    if (stat != null && value != null && stat.FieldType == typeof(StatType) && value.FieldType == typeof(int))
                    {
                        yield return new KeyValuePair<StatType, int>((StatType)stat.GetValue(c), (int)value.GetValue(c));
                    }
                }
            }
        }

        private static string Slot(BlueprintItem item)
        {
            switch (item)
            {
                case BlueprintItemWeapon w:
                    return "weapon: " + w.Category;
                case BlueprintItemArmor a:
                    return "armor: " + a.ProficiencyGroup;
                case BlueprintItemShield _:
                    return "shield";
                case BlueprintItemEquipmentUsable u:
                    return u.Type.ToString().ToLowerInvariant();
                case BlueprintItemEquipment e:
                    return e.GetType().Name.Replace("BlueprintItemEquipment", "").ToLowerInvariant();
                default:
                    return "supplies";
            }
        }

        private static string Safe(Func<string> read)
        {
            try
            {
                return read();
            }
            catch (Exception)
            {
                return "?";
            }
        }
    }
}
