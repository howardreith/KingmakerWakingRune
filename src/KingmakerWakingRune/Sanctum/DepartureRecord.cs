using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// The party as its departure rest left it, member by member: what each one chose to be ready with (spellbooks, caster
    /// levels and every prepared slot's spell), never what has been spent since. A rested party may then prepare in the
    /// lodge, casting protections and using items, and leave without a second rest; only a member who joined or left, or a
    /// spell selection or caster level that changed, makes the token ask again. Pure, so the comparison is unit-tested.
    /// </summary>
    internal static class DepartureRecord
    {
        public static string Write(IDictionary<string, string> members)
        {
            return string.Join("\n", members.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + "\t" + p.Value).ToArray());
        }

        public static Dictionary<string, string> Read(string payload)
        {
            var members = new Dictionary<string, string>();
            foreach (string line in (payload ?? string.Empty).Split('\n'))
            {
                int tab = line.IndexOf('\t');
                if (tab > 0)
                {
                    members[line.Substring(0, tab)] = line.Substring(tab + 1);
                }
            }
            return members;
        }

        /// <summary>Each way the party differs from the one that rested, by name; empty when it is the same party with the same selections.</summary>
        public static List<string> Changes(IDictionary<string, string> rested, IDictionary<string, string> now, Func<string, string> nameOf)
        {
            var changes = new List<string>();
            foreach (var member in now.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (!rested.TryGetValue(member.Key, out string before))
                {
                    changes.Add($"{nameOf(member.Key)} joined");
                }
                else if (before != member.Value)
                {
                    changes.Add($"{nameOf(member.Key)}'s prepared spells or caster level changed");
                }
            }
            if (rested.Keys.Any(id => !now.ContainsKey(id)))
            {
                changes.Add("a member left");
            }
            return changes;
        }
    }
}
