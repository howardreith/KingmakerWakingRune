using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using KingmakerWakingRune.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Every ability Krune's book names, by blueprint name: the game's own spells, his encounter-local spells
    /// (KruneLocalSpells) and his held slots. Built once with the scenario's blueprints.
    /// </summary>
    internal static class KruneSpells
    {
        private static readonly Dictionary<string, BlueprintAbility> s_ByName = new Dictionary<string, BlueprintAbility>();
        private static Action<BlueprintScriptableObject> s_Register;

        public static IEnumerable<string> Problems => s_Problems;
        private static readonly List<string> s_Problems = new List<string>();

        public static BlueprintAbility Resolve(string name)
        {
            return name != null && s_ByName.TryGetValue(name, out BlueprintAbility ability) ? ability : null;
        }

        public static void Build(IEnumerable<BlueprintScriptableObject> all, Action<BlueprintScriptableObject> register)
        {
            s_Register = register;
            s_ByName.Clear();
            s_Problems.Clear();
            var abilities = all.OfType<BlueprintAbility>().GroupBy(a => a.name).ToDictionary(g => g.Key, g => g.First());
            foreach (BookEntry entry in KruneBook.Plan().Where(e => e.Kind == PrepKind.Native || e.Kind == PrepKind.Spent))
            {
                if (abilities.TryGetValue(entry.Blueprint, out BlueprintAbility native))
                {
                    s_ByName[entry.Blueprint] = native;
                    if (entry.Variant.Length > 0 && abilities.TryGetValue(entry.Variant, out BlueprintAbility variant))
                    {
                        s_ByName[entry.Variant] = variant;
                    }
                }
                else
                {
                    s_Problems.Add("native spell " + entry.Blueprint);
                }
            }
            foreach (string spell in KruneBook.HeldSpells())
            {
                s_ByName[KruneBook.HeldName(spell)] = Held(spell, abilities);
            }
            foreach (KeyValuePair<string, BlueprintAbility> local in KruneLocalSpells.Build(abilities, register))
            {
                s_ByName[local.Key] = local.Value;
            }
            foreach (string local in KruneBook.LocalBlueprints().Where(n => !s_ByName.ContainsKey(n)))
            {
                s_Problems.Add("local spell " + local);
            }
            Log.Info($"Krune spells: {s_ByName.Count} abilities resolved{(s_Problems.Count == 0 ? string.Empty : "; MISSING " + string.Join(", ", s_Problems.ToArray()))}.");
        }

        /// <summary>A held slot: shown in his book under its printed name with the reason it has no use here; never castable.</summary>
        private static BlueprintAbility Held(string spell, Dictionary<string, BlueprintAbility> abilities)
        {
            string name = KruneBook.HeldName(spell);
            var never = ScriptableObject.CreateInstance<Lashmistress.KwrNeverCastable>();
            never.name = name + "_Never";
            never.Reason = "Held: " + KruneBook.HeldReason(spell) + ".";
            var ability = ScriptableObject.CreateInstance<BlueprintAbility>();
            ability.name = name;
            ability.AssetGuid = ScenarioIds.Derive("krune." + name);
            LocalizedText.Define("kwr.ability." + name, spell);
            LocalizedText.Define("kwr.ability." + name + ".desc", spell + " (prepared). " + never.Reason);
            Harmony12.AccessTools.Field(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact), "m_DisplayName").SetValue(ability, LocalizedText.Create("kwr.ability." + name));
            Harmony12.AccessTools.Field(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact), "m_Description").SetValue(ability, LocalizedText.Create("kwr.ability." + name + ".desc"));
            ability.Type = AbilityType.Spell;
            ability.ActionType = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard;
            ability.Range = AbilityRange.Personal;
            ability.CanTargetSelf = true;
            ability.LocalizedDuration = new Kingmaker.Localization.LocalizedString();
            ability.LocalizedSavingThrow = new Kingmaker.Localization.LocalizedString();
            ability.ComponentsArray = new BlueprintComponent[] { never };
            s_Register(ability);
            return ability;
        }
    }
}
