using System;
using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Enums;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Sanctum.Krune;
using KingmakerWakingRune.Sanctum.Wards;
using UnityEngine;

namespace KingmakerWakingRune.Scenario
{
    /// <summary>
    /// The adventure's journal in the game's own quest book: one quest, seven objectives, brought up to date from the
    /// recorded facts (flags, the sanctum state, the conclusion) every few seconds in the lodge and the sanctum, and at once
    /// when the debrief ends. Only Waking Rune sessions get it; it never moves an objective back.
    /// </summary>
    internal static class ScenarioJournal
    {
        private const float SyncSeconds = 2f;
        private static readonly Dictionary<string, BlueprintQuestObjective> s_Objectives = new Dictionary<string, BlueprintQuestObjective>();
        private static float s_NextSync;

        public static BlueprintQuest Quest { get; private set; }

        private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            [JournalRules.Prepare] = new[] { "Prepare and assemble the expedition", "Hear the Venture-Captain, gather a party of six, equip them from the Quartermaster and rest. Breaking the Refuge Token carries the party to the sanctum: there is no coming back by it." },
            [JournalRules.Defenses] = new[] { "Overcome the sanctum's defenses", "Find a way through the sanctum's wards and guardians to where the runelord sleeps." },
            [JournalRules.Revival] = new[] { "Confront the revival", "The runelord's revival is under way. Face what wakes." },
            [JournalRules.Krune] = new[] { "Defeat Runelord Krune", "Krune, Runelord of Sloth, is awake. He must not leave his sanctum alive." },
            [JournalRules.Artifacts] = new[] { "Recover the two artifacts", "Carry the runelord's dragon-tooth spear and rune-carved rod home for the Society." },
            [JournalRules.Report] = new[] { "Return and report", "Report to the Venture-Captain at the lodge." },
        };

        public static void Build(Action<BlueprintScriptableObject> register)
        {
            LocalizedText.Define("kwr.quest.waking", "The Waking Rune");
            LocalizedText.Define("kwr.quest.waking.desc", "The Pathfinder Society has learned that an ancient runelord stirs in a sanctum beneath the ice. Venture-Captain Sheila Heidmarch sends your team to see that he never wakes to rule again.");
            LocalizedText.Define("kwr.quest.waking.done", "The expedition is over and the Venture-Captain has your report.");
            Quest = ScriptableObject.CreateInstance<BlueprintQuest>();
            Quest.name = "KWR_Quest_WakingRune";
            Quest.AssetGuid = ScenarioIds.Derive("quest.waking");
            Quest.Title = LocalizedText.Create("kwr.quest.waking");
            Quest.Description = LocalizedText.Create("kwr.quest.waking.desc");
            Quest.CompletionText = LocalizedText.Create("kwr.quest.waking.done");
            Quest.ComponentsArray = new BlueprintComponent[0];
            var objectives = new List<BlueprintQuestObjective>();
            s_Objectives.Clear();
            foreach (string key in JournalRules.Order)
            {
                var objective = ScriptableObject.CreateInstance<BlueprintQuestObjective>();
                objective.name = "KWR_Objective_" + key;
                objective.AssetGuid = ScenarioIds.Derive("quest.objective." + key);
                LocalizedText.Define("kwr.objective." + key, Texts[key][0]);
                LocalizedText.Define("kwr.objective." + key + ".desc", Texts[key][1]);
                objective.Title = LocalizedText.Create("kwr.objective." + key);
                objective.Description = LocalizedText.Create("kwr.objective." + key + ".desc");
                objective.Locations = new List<Kingmaker.Globalmap.Blueprints.BlueprintLocation>();
                objective.ComponentsArray = new BlueprintComponent[0];
                AccessTools.Field(typeof(BlueprintQuestObjective), "m_Quest").SetValue(objective, Quest);
                AccessTools.Field(typeof(BlueprintQuestObjective), "m_FinishParent").SetValue(objective, key == JournalRules.Report);
                objectives.Add(objective);
                s_Objectives[key] = objective;
                register(objective);
            }
            AccessTools.Field(typeof(BlueprintQuest), "m_Objectives").SetValue(Quest, objectives);
            register(Quest);
        }

        /// <summary>Every frame from the lodge driver and the sanctum runtime; acts every few seconds.</summary>
        public static void Tick()
        {
            if (Time.unscaledTime < s_NextSync)
            {
                return;
            }
            s_NextSync = Time.unscaledTime + SyncSeconds;
            Sync();
        }

        public static void Sync()
        {
            try
            {
                if (Quest == null || Game.Instance?.Player == null || !Game.Instance.Player.UnlockableFlags.IsUnlocked(ScenarioBlueprints.SessionFlag))
                {
                    return;
                }
                Dictionary<string, JournalMark> marks = JournalRules.Marks(Facts());
                QuestBook book = Game.Instance.Player.QuestBook;
                var changed = new List<string>();
                foreach (string key in JournalRules.Order)
                {
                    BlueprintQuestObjective objective = s_Objectives[key];
                    JournalMark wanted = marks[key];
                    if (!JournalRules.Advances(Current(book, objective), wanted))
                    {
                        continue;
                    }
                    switch (wanted)
                    {
                        case JournalMark.Started:
                            book.GiveObjective(objective);
                            break;
                        case JournalMark.Completed:
                            book.CompleteObjective(objective);
                            break;
                        case JournalMark.Failed:
                            book.FailObjective(objective);
                            break;
                    }
                    changed.Add(key + " " + wanted);
                }
                if (changed.Count > 0)
                {
                    Log.Info($"Journal: {string.Join(", ", changed.ToArray())}; quest {book.GetQuestState(Quest)}.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Journal sync failed.", ex);
            }
        }

        private static JournalMark Current(QuestBook book, BlueprintQuestObjective objective)
        {
            switch (book.GetObjectiveState(objective))
            {
                case QuestObjectiveState.Started:
                    return JournalMark.Started;
                case QuestObjectiveState.Completed:
                    return JournalMark.Completed;
                case QuestObjectiveState.Failed:
                    return JournalMark.Failed;
                default:
                    return JournalMark.None;
            }
        }

        private static JournalFacts Facts()
        {
            var flags = Game.Instance.Player.UnlockableFlags;
            SanctumState s = WardStore.Existing;
            ConclusionState c = s?.Conclusion;
            return new JournalFacts
            {
                SanctumEntered = flags.IsUnlocked(ScenarioBlueprints.SanctumEnteredFlag),
                DefendersDefeated = s != null && s.Lashmistress.AllDefeated,
                KruneAwake = s != null && s.Krune.Stage >= AwakeningStage.Emerged,
                KruneSlain = c != null && c.KruneDeadAt >= 0d && !c.DiagnosticDeath,
                Returned = c != null && c.ReturnedAt >= 0d,
                SpearHome = c != null && c.SpearCarriedHome,
                RodHome = c != null && c.RodCarriedHome,
                Debriefed = c != null && c.DebriefAt >= 0d,
                Withdrawn = c != null && c.Outcome == ScenarioOutcome.Withdrawn,
            };
        }
    }
}
