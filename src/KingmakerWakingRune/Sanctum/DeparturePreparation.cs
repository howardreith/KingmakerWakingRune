using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Newtonsoft.Json;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>The departure rest's record, saved on the main character while the party prepares in the lodge.</summary>
    public sealed class DepartureRecordPart : UnitPart
    {
        [JsonProperty]
        public string Payload;
    }

    /// <summary>
    /// The lodge side of a departure, in two uses of the token. The first: the party is told that leaving begins with a
    /// night's rest, reviews its spellbooks, is warned about any prepared slot left empty, and takes the native scripted rest;
    /// then control returns in the lodge for final preparation (protective spells, potions), which stays spent. The second:
    /// an explicit confirmation, and the party departs as it stands, with no second rest. The rest readies what each caster
    /// chose; it never chooses for them, and a sanctum load never reaches this code.
    /// </summary>
    internal sealed class DeparturePreparation
    {
        private const float WindowOpenSeconds = 3f;
        private const float RestStartSeconds = 5f;

        private enum Stage
        {
            Idle,
            Confirming,
            OpeningBook,
            Reviewing,
            Warning,
            StartingRest,
            Resting,
        }

        private readonly Action m_Depart;
        private readonly Action<string> m_Cancel;
        private Stage m_Stage;
        private float m_Deadline;
        private bool m_SawRest;
        private float m_Now;

        public DeparturePreparation(Action depart, Action<string> cancel)
        {
            m_Depart = depart;
            m_Cancel = cancel;
        }

        public bool InProgress => m_Stage != Stage.Idle;

        /// <summary>
        /// The rest's record, as the loaded game carries it: a save made in the lodge after the rest keeps it, so a reload
        /// can still depart without resting. A completed arrival removes it.
        /// </summary>
        private static Dictionary<string, string> RestedAs
        {
            get
            {
                string payload = Game.Instance.Player.MainCharacter.Value?.Descriptor.Get<DepartureRecordPart>()?.Payload;
                return payload == null ? null : DepartureRecord.Read(payload);
            }
        }

        public static void Forget(string reason)
        {
            UnitEntityData main = Game.Instance.Player.MainCharacter.Value;
            if (main?.Descriptor.Get<DepartureRecordPart>() != null)
            {
                main.Descriptor.Remove<DepartureRecordPart>();
                Log.Info($"Departure rest record removed: {reason}.");
            }
        }

        private static void Remember()
        {
            UnitEntityData main = Game.Instance.Player.MainCharacter.Value;
            if (main != null)
            {
                main.Descriptor.Ensure<DepartureRecordPart>().Payload = DepartureRecord.Write(Game.Instance.Player.Party.ToDictionary(u => u.UniqueId, Selections));
            }
        }

        public void Begin()
        {
            Dictionary<string, string> rested = RestedAs;
            if (rested != null)
            {
                Dictionary<string, string> now = Game.Instance.Player.Party.ToDictionary(u => u.UniqueId, Selections);
                List<string> changed = DepartureRecord.Changes(rested, now, id => Game.Instance.Player.Party.FirstOrDefault(u => u.UniqueId == id)?.CharacterName ?? "a member");
                m_Stage = Stage.Confirming;
                if (changed.Count == 0)
                {
                    Log.Info("Departure: the party rested for this journey and its selections are unchanged; asking to depart without a second rest.");
                    Ask("The party has rested for this journey. Spells cast and items used since then stay spent, and nothing is restored.\n\nDepart for the Sanctum of Sloth now?", yes =>
                    {
                        if (yes)
                        {
                            Go("explicit departure after the rest");
                        }
                        else
                        {
                            Stop("Departure put off. Make any further preparations, then use the Refuge Token again.");
                        }
                    });
                    return;
                }
                string list = string.Join("; ", changed.ToArray());
                Log.Info($"Departure: since the departure rest: {list}.");
                Ask("Since the party's rest: " + list + ". Spells chosen since then are not ready.\n\nDepart now as the party stands? (Choose No to rest again instead.)", yes =>
                {
                    if (yes)
                    {
                        Go("explicit departure without resting again, after changes: " + list);
                        return;
                    }
                    Ask("Rest again before departing? Spells and effects cast since the last rest will run out during the night; spent spell slots and abilities return with the rest.", rest =>
                    {
                        if (rest)
                        {
                            OpenBook();
                        }
                        else
                        {
                            Stop("Departure put off. Nothing was spent.");
                        }
                    });
                });
                return;
            }
            m_Stage = Stage.Confirming;
            Ask("Before the token carries you away, the party takes a full night's rest here in the lodge and prepares its spells. Spells and effects already cast will run out during the night. Spent scrolls, potions and wand charges are not restored. Spells you choose in the spellbook become ready with the rest.\n\nAfter the rest you stay in the lodge to make final preparations, and use the token again to depart.\n\nReview your spellbooks now, then rest?", yes =>
            {
                if (!yes)
                {
                    Stop("Departure put off before the rest. Nothing was spent.");
                    return;
                }
                OpenBook();
            });
        }

        private void Go(string why)
        {
            m_Stage = Stage.Idle;
            Log.Info($"Departure: {why}; party as it stands: {string.Join("; ", Game.Instance.Player.Party.Select(Readiness).ToArray())}.");
            m_Depart();
        }

        public void Cancel(string reason)
        {
            if (m_Stage != Stage.Idle)
            {
                Log.Info($"Departure preparation cancelled: {reason}.");
                m_Stage = Stage.Idle;
            }
        }

        public void Tick(float now)
        {
            switch (m_Stage)
            {
                case Stage.OpeningBook:
                    if (BookShown)
                    {
                        m_Stage = Stage.Reviewing;
                    }
                    else if (now >= m_Deadline)
                    {
                        // No window to wait on (no caster, or the UI refused): the slots are still checked before resting.
                        Log.Info("Departure: the spellbook did not open; checking preparation directly.");
                        Review();
                    }
                    break;
                case Stage.Reviewing:
                    if (!BookShown && !Game.Instance.IsModeActive(GameModeType.FullScreenUi))
                    {
                        Review();
                    }
                    break;
                case Stage.StartingRest:
                    if (Game.Instance.CurrentMode == GameModeType.Rest)
                    {
                        m_SawRest = true;
                        m_Stage = Stage.Resting;
                    }
                    else if (now >= m_Deadline)
                    {
                        Stop("The party could not settle to rest, so the token was not used. Nothing was spent.");
                    }
                    break;
                case Stage.Resting:
                    // The native rest ends with its own autosave; the departure checkpoint waits for that to be committed.
                    if (m_SawRest && Game.Instance.CurrentMode == GameModeType.Default && !LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.IsLoadingScreenActive && !Game.Instance.SaveManager.CommitInProgress)
                    {
                        Remember();
                        m_Stage = Stage.Idle;
                        Log.Info($"Departure rest complete; control returns in the lodge for final preparation: {string.Join("; ", Game.Instance.Player.Party.Select(Readiness).ToArray())}.");
                        EventBus.RaiseEvent(delegate(IDialogMessageBoxUIHandler h)
                        {
                            h.HandleOpen("The party has rested and its chosen spells are ready.\n\nMake your final preparations here: cast protective spells, drink potions, ready your gear. What you spend stays spent.\n\nWhen you are ready, use the Refuge Token again to depart. The party will not rest again.", DialogMessageBoxBase.BoxType.Message, button => { });
                        });
                    }
                    break;
            }
            m_Now = now;
        }

        private static bool BookShown => Game.Instance.UI.ServiceWindow?.WindowTabs != null && Game.Instance.UI.ServiceWindow.WindowTabs.IsShow;

        private void OpenBook()
        {
            m_Stage = Stage.OpeningBook;
            m_Deadline = m_Now + WindowOpenSeconds;
            EventBus.RaiseEvent(delegate(IServiceWindowUIHandler h) { h.HandleOpenSpellbook(); });
        }

        private void Review()
        {
            List<string> empty = Game.Instance.Player.Party.Select(EmptySlots).Where(s => s != null).ToList();
            if (empty.Count == 0)
            {
                Rest();
                return;
            }
            m_Stage = Stage.Warning;
            Log.Info($"Departure: empty prepared slots: {string.Join("; ", empty.ToArray())}.");
            Ask("Some prepared spell slots are still empty, and resting will not fill them:\n\n" + string.Join("\n", empty.ToArray()) + "\n\nRest anyway? Choose Cancel to return to the spellbooks.", yes =>
            {
                if (yes)
                {
                    Rest();
                }
                else
                {
                    OpenBook();
                }
            });
        }

        private void Rest()
        {
            if (Game.Instance.Player.IsInCombat)
            {
                Stop("The party cannot rest while in combat. Nothing was spent.");
                return;
            }
            m_Stage = Stage.StartingRest;
            m_SawRest = false;
            m_Deadline = m_Now + RestStartSeconds;
            Log.Info($"Departure rest: native scripted party rest started; before: {string.Join("; ", Game.Instance.Player.Party.Select(Readiness).ToArray())}.");
            Game.Instance.RestController.StartScripted(immediate: false);
        }

        private void Stop(string text)
        {
            m_Stage = Stage.Idle;
            m_Cancel(text);
        }

        private static void Ask(string text, Action<bool> answer)
        {
            EventBus.RaiseEvent(delegate(IDialogMessageBoxUIHandler h)
            {
                h.HandleOpen(text, DialogMessageBoxBase.BoxType.Dialog, button => answer(button == DialogMessageBoxBase.BoxButton.Yes));
            });
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

        /// <summary>
        /// What a rest would make ready, for one member: spellbooks, caster levels, daily slot counts, and each prepared slot's
        /// chosen spell. Not whether a slot or resource has been spent, nor wounds: those are the party's preparation, and
        /// stay as they are.
        /// </summary>
        private static string Selections(UnitEntityData unit)
        {
            var sb = new System.Text.StringBuilder();
            foreach (Spellbook book in unit.Descriptor.Spellbooks)
            {
                sb.Append($"| {book.Blueprint.AssetGuid} cl {book.CasterLevel}");
                for (int level = 0; level <= book.MaxSpellLevel; level++)
                {
                    if (book.Blueprint.Spontaneous)
                    {
                        sb.Append($" {level}:{book.GetSpellsPerDay(level)}");
                    }
                    else
                    {
                        sb.Append($" {level}:[{string.Join(",", book.GetMemorizedSpellSlots(level).Select(s => s.Spell?.Blueprint.AssetGuid ?? "-").ToArray())}]");
                    }
                }
            }
            return sb.ToString();
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
