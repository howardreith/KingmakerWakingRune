using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>What the saved departure record means for the next use of the Refuge Token.</summary>
    internal enum DepartureReadiness
    {
        /// <summary>No record: ready the expedition now, once.</summary>
        Refresh,

        /// <summary>This departure was already readied (a failed transfer is retried, or the checkpoint reloaded): reconcile only what changed since.</summary>
        Reconcile,

        /// <summary>A record of the earlier two-use flow ("rested, awaiting the second use"): dropped, then ready once.</summary>
        Legacy,
    }

    /// <summary>
    /// One member's preparation as readied: a key for each prepared slot ("s:" book, level, type, index, with the chosen
    /// spell as its value, empty for an empty slot) and for each spontaneous level ("d:" book, level, with the spells per day
    /// as its value).
    /// </summary>
    internal sealed class DepartureSnapshot
    {
        public readonly string Unit;
        public readonly Dictionary<string, string> Entries;

        public DepartureSnapshot(string unit, Dictionary<string, string> entries)
        {
            Unit = unit;
            Entries = entries;
        }

        public static string SlotKey(string book, int level, int type, int index)
        {
            return $"s:{book}/{level}/{type}/{index}";
        }

        public static string DailyKey(string book, int level)
        {
            return $"d:{book}/{level}";
        }
    }

    /// <summary>What a reconciliation does for one member: everything (not readied before), or only what changed.</summary>
    internal sealed class DepartureAction
    {
        public string Unit;

        /// <summary>A member who was not in the readied expedition (a new recruit, or no snapshot was saved): readied in full.</summary>
        public bool Full;

        /// <summary>Prepared slots whose spell was chosen after the readiness (or which a level-up added): made ready.</summary>
        public List<string> Slots = new List<string>();

        /// <summary>Spontaneous levels whose spells per day grew since (a level-up): the key and the number of spells added.</summary>
        public Dictionary<string, int> Daily = new Dictionary<string, int>();

        public bool Nothing => !Full && Slots.Count == 0 && Daily.Count == 0;
    }

    /// <summary>
    /// The departure's saved record (WR-PLAYTEST-01). Using the token readies the expedition once and writes a record of
    /// what was readied: each member and each prepared choice. The record stays until the arrival is committed. A retry after
    /// a failed transfer, or a reload of the checkpoint written before leaving, reconciles against it: an unchanged
    /// expedition departs as it stands (nothing is refreshed twice, so spells cast since stay spent); a member who was not
    /// readied is readied in full; a slot whose spell was changed, a slot a level-up added, and spontaneous spells a level-up
    /// added are made ready, as is a slot prepared again since (even with the same spell, which the native preparation leaves
    /// unready until a rest), and nothing else. Any other payload is the earlier flow's selections record, dropped without
    /// reopening its dialogues. Pure, so the decisions are unit-tested.
    /// </summary>
    internal static class DepartureRecord
    {
        public const string ReadyPrefix = "ready:";

        public static string Ready(double gameTimeSeconds, IEnumerable<DepartureSnapshot> members)
        {
            var text = new System.Text.StringBuilder(ReadyPrefix);
            text.Append(gameTimeSeconds.ToString("0", CultureInfo.InvariantCulture));
            foreach (DepartureSnapshot member in members)
            {
                text.Append('|').Append(member.Unit).Append('#');
                text.Append(string.Join(";", member.Entries.OrderBy(e => e.Key, StringComparer.Ordinal).Select(e => e.Key + "=" + e.Value).ToArray()));
            }
            return text.ToString();
        }

        public static DepartureReadiness Classify(string payload)
        {
            if (payload == null)
            {
                return DepartureReadiness.Refresh;
            }
            return payload.StartsWith(ReadyPrefix, StringComparison.Ordinal) ? DepartureReadiness.Reconcile : DepartureReadiness.Legacy;
        }

        /// <summary>The members of a readied record; a bare marker (the unreleased first 0.10.0 candidate) has none.</summary>
        public static Dictionary<string, DepartureSnapshot> Parse(string payload)
        {
            var members = new Dictionary<string, DepartureSnapshot>();
            string[] parts = payload.Split('|');
            for (int i = 1; i < parts.Length; i++)
            {
                int hash = parts[i].IndexOf('#');
                if (hash <= 0)
                {
                    continue;
                }
                var entries = new Dictionary<string, string>();
                foreach (string entry in parts[i].Substring(hash + 1).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int eq = entry.IndexOf('=');
                    if (eq > 0)
                    {
                        entries[entry.Substring(0, eq)] = entry.Substring(eq + 1);
                    }
                }
                string unit = parts[i].Substring(0, hash);
                members[unit] = new DepartureSnapshot(unit, entries);
            }
            return members;
        }

        /// <summary>What to ready now; <paramref name="preparedSince"/> lists slots the player prepared after the record was written.</summary>
        public static List<DepartureAction> Reconcile(Dictionary<string, DepartureSnapshot> readied, IEnumerable<DepartureSnapshot> current, ICollection<string> preparedSince = null)
        {
            var actions = new List<DepartureAction>();
            foreach (DepartureSnapshot member in current)
            {
                var action = new DepartureAction { Unit = member.Unit };
                if (!readied.TryGetValue(member.Unit, out DepartureSnapshot before))
                {
                    action.Full = true;
                    actions.Add(action);
                    continue;
                }
                foreach (KeyValuePair<string, string> entry in member.Entries.OrderBy(e => e.Key, StringComparer.Ordinal))
                {
                    before.Entries.TryGetValue(entry.Key, out string was);
                    if (entry.Key.StartsWith("s:", StringComparison.Ordinal))
                    {
                        if (entry.Value.Length > 0 && (entry.Value != was || (preparedSince != null && preparedSince.Contains(entry.Key))))
                        {
                            action.Slots.Add(entry.Key);
                        }
                    }
                    else if (entry.Key.StartsWith("d:", StringComparison.Ordinal))
                    {
                        int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int now);
                        int.TryParse(was ?? "0", NumberStyles.Integer, CultureInfo.InvariantCulture, out int then);
                        if (now > then)
                        {
                            action.Daily[entry.Key] = now - then;
                        }
                    }
                }
                actions.Add(action);
            }
            return actions;
        }
    }
}
