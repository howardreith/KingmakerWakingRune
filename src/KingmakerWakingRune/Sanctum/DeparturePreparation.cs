using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Controllers.Rest;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Newtonsoft.Json;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>The departure's record (see <see cref="DepartureRecord"/>), saved on the main character until the arrival is committed.</summary>
    public sealed class DepartureRecordPart : UnitPart
    {
        [JsonProperty]
        public string Payload;
    }

    /// <summary>
    /// The lodge side of a departure, in one use of the token (WR-PLAYTEST-01, the owner's decision; it supersedes the
    /// two-use review, rest and confirmation). The expedition is readied once, as preparation finished before leaving, not as
    /// a night spent in the running scenario: the native per-unit rest (RestController.ApplyRest) readies each caster's chosen
    /// spells, refills spontaneous slots and rest-recovered abilities and item powers, clears what a native rest clears, and
    /// wounds are healed; the game clock does not move, so protective spells already cast keep their remaining time, and
    /// nothing consumed (scrolls, potions, wand charges, ammunition, components) comes back. Empty prepared slots are noted
    /// quietly and never filled. Then the existing checkpoint and transfer run unchanged. The saved record lists each member
    /// and each prepared choice as readied, and stays until the arrival is committed: a retry after a failed transfer, or a
    /// reload of the checkpoint, reconciles against it (see <see cref="DepartureRecord"/>), so an unchanged expedition is
    /// never refreshed twice, while a new member, a changed preparation or a level-up is readied before leaving. A sanctum
    /// load never reaches this code.
    /// </summary>
    internal sealed class DeparturePreparation
    {
        private readonly Action m_Depart;

        public DeparturePreparation(Action depart, Action<string> cancel)
        {
            m_Depart = depart;
        }

        /// <summary>Synchronous: nothing waits between the token's use and the transfer's own stages.</summary>
        public bool InProgress => false;

        public static void Forget(string reason)
        {
            UnitEntityData main = Game.Instance.Player.MainCharacter.Value;
            if (main?.Descriptor.Get<DepartureRecordPart>() != null)
            {
                main.Descriptor.Remove<DepartureRecordPart>();
                Log.Info($"Departure record removed: {reason}.");
            }
        }

        public void Begin()
        {
            UnitEntityData main = Game.Instance.Player.MainCharacter.Value;
            string payload = main?.Descriptor.Get<DepartureRecordPart>()?.Payload;
            switch (DepartureRecord.Classify(payload))
            {
                case DepartureReadiness.Reconcile:
                    Reconcile(main, payload);
                    break;
                case DepartureReadiness.Legacy:
                    Forget("a record of the earlier two-use departure, migrated without its dialogues");
                    Ready(main);
                    break;
                default:
                    Ready(main);
                    break;
            }
            m_Depart();
        }

        public void Cancel(string reason)
        {
        }

        public void Tick(float now)
        {
        }

        /// <summary>Every living member and companion of the party, recruits and animal companions included.</summary>
        private static List<UnitEntityData> Members()
        {
            var members = new List<UnitEntityData>();
            foreach (UnitEntityData unit in Game.Instance.Player.Party)
            {
                members.Add(unit);
                UnitEntityData pet = unit.Descriptor.Pet;
                if (pet != null && !members.Contains(pet))
                {
                    members.Add(pet);
                }
            }
            return members.Where(u => !u.Descriptor.State.IsDead).ToList();
        }

        private static void Ready(UnitEntityData main)
        {
            List<UnitEntityData> members = Members();
            Log.Info($"Departure readiness, before: {string.Join("; ", members.Select(Readiness).ToArray())}.");
            foreach (UnitEntityData unit in members)
            {
                ReadyUnit(unit);
            }
            Record(main, members);
            Log.Info($"Departure readiness, after: {string.Join("; ", members.Select(Readiness).ToArray())}.");
            Announce("The expedition is rested and ready.", "The expedition is rested and ready.");
        }

        /// <summary>
        /// A retry or a checkpoint reload: what was readied stays as it stands (spent since is spent); a member who was not
        /// readied is readied in full; a slot prepared with a different spell since, or added by a level-up, and spontaneous
        /// spells a level-up added, are made ready. The record then describes the expedition as it leaves.
        /// </summary>
        private static void Reconcile(UnitEntityData main, string payload)
        {
            List<UnitEntityData> members = Members();
            List<DepartureAction> actions = DepartureRecord.Reconcile(DepartureRecord.Parse(payload), members.Select(Snapshot)).Where(a => !a.Nothing).ToList();
            if (actions.Count == 0)
            {
                Log.Info($"Departure: this expedition was already readied and is unchanged ({payload.Split('|')[0]}); departing as it stands: {string.Join("; ", members.Select(Readiness).ToArray())}.");
                return;
            }
            Log.Info($"Departure reconciliation, before: {string.Join("; ", members.Select(Readiness).ToArray())}.");
            var notes = new List<string>();
            foreach (DepartureAction action in actions)
            {
                UnitEntityData unit = members.First(u => u.UniqueId == action.Unit);
                if (action.Full)
                {
                    ReadyUnit(unit);
                    notes.Add($"{unit.CharacterName} readied");
                    continue;
                }
                Dictionary<string, SpellSlot> slots = PreparedSlots(unit);
                foreach (string key in action.Slots)
                {
                    slots[key].Available = true;
                }
                foreach (KeyValuePair<string, int> daily in action.Daily)
                {
                    string[] parts = daily.Key.Substring(2).Split('/');
                    Spellbook book = unit.Descriptor.Spellbooks.First(b => b.Blueprint.AssetGuid == parts[0]);
                    book.RestoreSpontaneousSlots(int.Parse(parts[1], CultureInfo.InvariantCulture), daily.Value);
                }
                var what = new List<string>();
                if (action.Slots.Count > 0)
                {
                    string spells = string.Join(", ", action.Slots.Select(k => slots[k].Spell.Name).Distinct().ToArray());
                    what.Add($"{action.Slots.Count} newly prepared slot{(action.Slots.Count == 1 ? "" : "s")} ({spells})");
                }
                int gained = action.Daily.Values.Sum();
                if (gained > 0)
                {
                    what.Add($"{gained} spontaneous spell{(gained == 1 ? "" : "s")} gained by level");
                }
                notes.Add($"{unit.CharacterName}: {string.Join(" and ", what.ToArray())}");
            }
            Record(main, members);
            Log.Info($"Departure reconciliation: {string.Join("; ", notes.ToArray())}. After: {string.Join("; ", members.Select(Readiness).ToArray())}.");
            Announce("Readied since the last preparation: " + string.Join("; ", notes.ToArray()) + ".", "Changes since the last preparation are readied.");
        }

        private static void ReadyUnit(UnitEntityData unit)
        {
            var info = new UnitRestInfo(unit.Descriptor, new RestStatus());
            if (unit.Damage > 0)
            {
                Rulebook.Trigger(new RuleHealDamage(unit, unit, unit.Damage));
            }
            foreach (StatType stat in StatTypeHelper.Attributes)
            {
                if (info.HealedStatDamage > 0)
                {
                    Rulebook.Trigger(new RuleHealStatDamage(unit, unit, stat, info.HealedStatDamage));
                }
            }
            RestController.ApplyRest(unit.Descriptor);
            unit.Descriptor.LastRestTime = Game.Instance.TimeController.GameTime;
        }

        private static void Record(UnitEntityData main, List<UnitEntityData> members)
        {
            if (main != null)
            {
                main.Descriptor.Ensure<DepartureRecordPart>().Payload = DepartureRecord.Ready(Game.Instance.TimeController.GameTime.TotalSeconds, members.Select(Snapshot));
            }
        }

        private static void Announce(string line, string warning)
        {
            List<string> empty = Game.Instance.Player.Party.Select(EmptySlots).Where(e => e != null).ToList();
            if (empty.Count > 0)
            {
                Log.Info($"Departure: empty prepared slots, left empty: {string.Join("; ", empty.ToArray())}.");
                line += " Unfilled prepared slots: " + string.Join("; ", empty.ToArray()) + ".";
            }
            Game.Instance.UI.BattleLogManager?.HandleOnDialogHistory(line);
            EventBus.RaiseEvent(delegate(IWarningNotificationUIHandler h) { h.HandleWarning(warning); });
        }

        /// <summary>Each slot of a member's prepared books, by its record key.</summary>
        private static Dictionary<string, SpellSlot> PreparedSlots(UnitEntityData unit)
        {
            var slots = new Dictionary<string, SpellSlot>();
            foreach (Spellbook book in unit.Descriptor.Spellbooks.Where(b => !b.Blueprint.Spontaneous))
            {
                book.UpdateAllSlotsSize();
                for (int level = 1; level <= book.MaxSpellLevel; level++)
                {
                    foreach (SpellSlot slot in book.GetMemorizedSpellSlots(level))
                    {
                        slots[DepartureSnapshot.SlotKey(book.Blueprint.AssetGuid, level, (int)slot.Type, slot.Index)] = slot;
                    }
                }
            }
            return slots;
        }

        /// <summary>A member's preparation for the record: each prepared slot's chosen spell, and each spontaneous level's spells per day.</summary>
        private static DepartureSnapshot Snapshot(UnitEntityData unit)
        {
            var entries = new Dictionary<string, string>();
            foreach (KeyValuePair<string, SpellSlot> slot in PreparedSlots(unit))
            {
                AbilityData spell = slot.Value.Spell;
                string value = spell == null ? "" : spell.Blueprint.AssetGuid;
                if (spell?.MetamagicData != null && spell.MetamagicData.NotEmpty)
                {
                    value += "+" + ((int)spell.MetamagicData.MetamagicMask).ToString(CultureInfo.InvariantCulture);
                }
                entries[slot.Key] = value;
            }
            foreach (Spellbook book in unit.Descriptor.Spellbooks.Where(b => b.Blueprint.Spontaneous))
            {
                for (int level = 1; level <= book.MaxSpellLevel; level++)
                {
                    entries[DepartureSnapshot.DailyKey(book.Blueprint.AssetGuid, level)] = book.GetSpellsPerDay(level).ToString(CultureInfo.InvariantCulture);
                }
            }
            return new DepartureSnapshot(unit.UniqueId, entries);
        }

        /// <summary>One character's empty prepared slots by level, or null when every slot holds a chosen spell.</summary>
        private static string EmptySlots(UnitEntityData unit)
        {
            var parts = new List<string>();
            foreach (Spellbook book in unit.Descriptor.Spellbooks)
            {
                if (book.Blueprint.Spontaneous)
                {
                    continue;
                }
                book.UpdateAllSlotsSize();
                for (int level = book.MaxSpellLevel; level >= 1; level--)
                {
                    int count = book.GetMemorizedSpellSlots(level).Count(s => s.Spell == null);
                    if (count > 0)
                    {
                        parts.Add($"{Ordinal(level)} level ({count})");
                    }
                }
            }
            return parts.Count == 0 ? null : $"{unit.CharacterName}: {string.Join(", ", parts.ToArray())}";
        }

        /// <summary>For the log: each prepared book's ready and total slots at its highest level, and current hit points.</summary>
        private static string Readiness(UnitEntityData unit)
        {
            var books = unit.Descriptor.Spellbooks.Where(b => !b.Blueprint.Spontaneous && b.MaxSpellLevel > 0).Select(b =>
            {
                List<SpellSlot> top = b.GetMemorizedSpellSlots(b.MaxSpellLevel).ToList();
                return $"L{b.MaxSpellLevel} ready {top.Count(s => s.Spell != null && s.Available)}/{top.Count}";
            });
            return $"{unit.CharacterName} hp {unit.Descriptor.HPLeft}/{unit.Stats.HitPoints.ModifiedValue} {string.Join(" ", books.ToArray())}".Trim();
        }

        private static string Ordinal(int level)
        {
            switch (level)
            {
                case 1:
                    return "1st";
                case 2:
                    return "2nd";
                case 3:
                    return "3rd";
                default:
                    return level + "th";
            }
        }
    }
}
