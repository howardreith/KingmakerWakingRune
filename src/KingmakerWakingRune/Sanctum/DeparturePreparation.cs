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
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// The lodge side of a departure: the party is told that leaving includes a night's rest, reviews its spellbooks, is
    /// warned about any prepared slot left empty, and takes the native scripted rest before the checkpoint is written. The
    /// rest readies what each caster chose; it never chooses for them, and a sanctum load never reaches this code.
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

        /// <summary>
        /// The party has rested for the departure in progress. A failed checkpoint or load leaves the party in the lodge and
        /// a retry departs without resting again; a save load or a completed arrival clears it.
        /// </summary>
        private static bool s_Rested;

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

        public static void Forget(string reason)
        {
            if (s_Rested)
            {
                Log.Info($"Departure rest record cleared: {reason}.");
            }
            s_Rested = false;
        }

        public void Begin()
        {
            if (s_Rested)
            {
                Log.Info("Departure: the party already rested for this departure; leaving without a second rest.");
                m_Depart();
                return;
            }
            m_Stage = Stage.Confirming;
            Ask("Before the token carries you away, the party takes a full night's rest here in the lodge and prepares its spells.\n\nSpells and effects already cast will run out during the night. Spent scrolls, potions and wand charges are not restored. Spells you choose in the spellbook become ready with the rest.\n\nReview your spellbooks now, then rest and depart?", yes =>
            {
                if (!yes)
                {
                    Stop("Departure put off before the rest. Nothing was spent.");
                    return;
                }
                OpenBook();
            });
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
                        s_Rested = true;
                        m_Stage = Stage.Idle;
                        Log.Info($"Departure rest complete: {string.Join("; ", Game.Instance.Player.Party.Select(Readiness).ToArray())}.");
                        m_Depart();
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
            Ask("Some prepared spell slots are still empty, and resting will not fill them:\n\n" + string.Join("\n", empty.ToArray()) + "\n\nRest and depart anyway? Choose No to return to the spellbooks.", yes =>
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
