using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using UnityEngine;

namespace KingmakerWakingRune.Diagnostics
{
    /// <summary>
    /// WR-04 reconnaissance, inert unless 'recon.flag' exists: writes the real contents of the spells, conditions, unit
    /// types and summons the sanctum's wards and runes have to work with, so no rule relies on a guessed blueprint.
    /// </summary>
    internal static class RulesRecon
    {
        private const int MaxDepth = 7;

        private static readonly Regex Abilities = new Regex(
            "StoneToFlesh|FleshToStone|BreakEnchantment|^DispelMagic|Dispel.*Greater|^RemoveCurse|RemoveDisease|^Restoration|LesserRestoration|GreaterRestoration|^HoldPerson|^HoldMonster|^Web|^Entangle|^Slow$|^Slay|FingerOfDeath|Destruction|^Harm$|^Inflict|^Enervation|EnergyDrain|ConeOfCold|^Disintegrate|^Dominate|^Confusion|^Geas|^Charm|^Poison|^Contagion|^VampiricTouch|^Knock|^Silence|TanglefootBomb|AlchemistBomb|^SummonMonster[IVX]+|^SummonNaturesAlly[IVX]+|Touch(Attack)?Item",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex Buffs = new Regex(
            "Stagger|Fatigue|Exhaust|Sleep|Slowed|^Slow|Entangl|Tanglefoot|Paraly|HoldPerson|HoldMonster|Petrif|Stone|Curse|Darkvision|CatsGrace|FalseLife|MageArmor|SummonedUnit|^Web|Poison|Toxic|Venom|^Contagion|Disease",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex Features = new Regex(
            "Lissala|Sihedron|Thassilon|^(Aberration|Construct|Humanoid|Undead|Outsider|MagicalBeast|Vermin|Plant|Ooze|Animal)Type$|ImmunityToCritical|ImmunityToPrecision|^Deity|ChannelNegative|Evasion$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex Units = new Regex(
            "InvisibleKitten|^CultistMale1$|^Cultist$|Dretch|GiantSpiderSummoned|SpiderSwarmSummoned|MonitorLizardSummoned|PurpleWormSummoned|Priest.*Level|Cleric.*Level[1-5]$|^Skeleton.*Summon",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static void Dump(LibraryScriptableObject library, Func<bool> enabled)
        {
            if (!enabled())
            {
                return;
            }
            var all = library.GetAllBlueprints();
            WriteGroup("abilities.txt", all.OfType<BlueprintAbility>().Where(a => Abilities.IsMatch(a.name)), a => $"range={a.Range} self={a.CanTargetSelf} friends={a.CanTargetFriends} enemies={a.CanTargetEnemies} point={a.CanTargetPoint} type={a.Type} action={a.ActionType} variants={a.HasVariants}");
            WriteGroup("buffs.txt", all.OfType<BlueprintBuff>().Where(b => Buffs.IsMatch(b.name)), b => $"stacking={b.Stacking} hidden={b.IsHiddenInUI}");
            WriteGroup("features.txt", all.OfType<BlueprintFeature>().Where(f => Features.IsMatch(f.name)), f => string.Empty);
            WriteGroup("recon-units.txt", all.OfType<BlueprintUnit>().Where(u => Units.IsMatch(u.name)), u => $"size={u.Size} str={u.Strength} dex={u.Dexterity} con={u.Constitution} int={u.Intelligence} wis={u.Wisdom} cha={u.Charisma} faction={u.Faction?.name} brain={u.Brain?.name} facts=[{string.Join(",", (u.AddFacts ?? new BlueprintUnitFact[0]).Select(f => f == null ? "null" : f.name).ToArray())}]");
        }

        private static void WriteGroup<T>(string file, System.Collections.Generic.IEnumerable<T> items, Func<T, string> summary) where T : BlueprintScriptableObject
        {
            try
            {
                var sb = new StringBuilder();
                foreach (T item in items.OrderBy(i => i.name))
                {
                    sb.AppendLine($"## {item.name} {item.AssetGuid} {SafeSummary(item, summary)}");
                    foreach (BlueprintComponent component in item.ComponentsArray ?? new BlueprintComponent[0])
                    {
                        Append(sb, component, 1, new System.Collections.Generic.HashSet<object>());
                    }
                }
                string dir = Path.Combine(Main.Mod.Path, "recon");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, file), sb.ToString());
                Log.Info($"Recon written: {file}");
            }
            catch (Exception ex)
            {
                Log.Error($"Recon {file} failed.", ex);
            }
        }

        private static string SafeSummary<T>(T item, Func<T, string> summary)
        {
            try
            {
                return summary(item);
            }
            catch (Exception ex)
            {
                return "err=" + ex.GetType().Name;
            }
        }

        private static void Append(StringBuilder sb, object value, int depth, System.Collections.Generic.HashSet<object> seen)
        {
            if (value == null || depth > MaxDepth || !seen.Add(value))
            {
                return;
            }
            string pad = new string(' ', depth * 2);
            Type type = value.GetType();
            sb.AppendLine($"{pad}{type.Name}");
            foreach (FieldInfo field in Fields(type))
            {
                object member;
                try
                {
                    member = field.GetValue(value);
                }
                catch
                {
                    continue;
                }
                if (member == null)
                {
                    continue;
                }
                AppendMember(sb, field.Name, member, depth + 1, seen);
            }
        }

        private static void AppendMember(StringBuilder sb, string name, object member, int depth, System.Collections.Generic.HashSet<object> seen)
        {
            string pad = new string(' ', depth * 2);
            switch (member)
            {
                case BlueprintScriptableObject bp:
                    sb.AppendLine($"{pad}{name}=@{bp.name}/{bp.AssetGuid}");
                    return;
                case Element element:
                    sb.AppendLine($"{pad}{name}:");
                    Append(sb, element, depth + 1, seen);
                    return;
                case UnityEngine.Object unity:
                    sb.AppendLine($"{pad}{name}=unity:{unity.name}");
                    return;
                case string text:
                    if (text.Length > 0)
                    {
                        sb.AppendLine($"{pad}{name}='{text}'");
                    }
                    return;
                case IList list when !(member is string):
                    sb.AppendLine($"{pad}{name}[{list.Count}]");
                    foreach (object element in list)
                    {
                        if (element is Element || element is BlueprintScriptableObject || element is UnityEngine.Object || element == null || element.GetType().IsPrimitive || element is string || element.GetType().IsEnum)
                        {
                            AppendMember(sb, "-", element ?? "null", depth + 1, seen);
                        }
                        else
                        {
                            Append(sb, element, depth + 1, seen);
                        }
                    }
                    return;
            }
            Type type = member.GetType();
            if (type.IsPrimitive || type.IsEnum || member is decimal)
            {
                if (!Equals(member, Default(type)))
                {
                    sb.AppendLine($"{pad}{name}={member}");
                }
                return;
            }
            if (type.Namespace != null && type.Namespace.StartsWith("Kingmaker") || member is GameAction || member is Condition)
            {
                sb.AppendLine($"{pad}{name}:");
                Append(sb, member, depth + 1, seen);
            }
        }

        private static object Default(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static System.Collections.Generic.IEnumerable<FieldInfo> Fields(Type type)
        {
            for (Type t = type; t != null && t != typeof(object) && t != typeof(ScriptableObject) && t != typeof(BlueprintComponent) && t != typeof(Element); t = t.BaseType)
            {
                foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), true).Length > 0)
                    {
                        yield return field;
                    }
                }
            }
        }
    }
}
