using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Interaction;
using KingmakerWakingRune.Blueprints;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>A blueprint condition backed by mod code; lives only in memory, so it is never serialized.</summary>
    internal sealed class KwrCondition : Condition
    {
        public Func<bool> Body;
        public string Caption;

        protected override string GetConditionCaption()
        {
            return Caption;
        }

        protected override bool CheckCondition()
        {
            return Body != null && Body();
        }
    }

    /// <summary>
    /// A native dialogue for each fixture: the cue carries what is observable and what has been learned, answers run real
    /// checks and actions, and leaving costs nothing. Every answer that touches the fixture is separate from reading it.
    /// </summary>
    internal static class WardDialogs
    {
        public static readonly Dictionary<string, BlueprintDialog> Dialogs = new Dictionary<string, BlueprintDialog>();

        private static readonly Dictionary<WardSkill, string> SkillLabels = new Dictionary<WardSkill, string>
        {
            { WardSkill.Arcana, "Knowledge: Arcana" },
            { WardSkill.Religion, "Lore: Religion" },
            { WardSkill.Nature, "Lore: Nature" },
            { WardSkill.Perception, "Perception" },
        };

        private static Action<BlueprintScriptableObject> s_Register;

        public static void Build(Action<BlueprintScriptableObject> register)
        {
            s_Register = register;
            foreach (InspectDef def in WardText.All)
            {
                Dialogs[def.Id] = BuildOne(def);
            }
        }

        public static void TextKey(string id, string text)
        {
            LocalizedText.Define(CueKey(id), text);
        }

        private static string CueKey(string id)
        {
            return "kwr.ward.cue." + id;
        }

        private static BlueprintDialog BuildOne(InspectDef def)
        {
            LocalizedText.Define(CueKey(def.Id), def.Observe);
            var cue = Create<BlueprintCue>("KWR_WardCue_" + def.Id);
            cue.Text = LocalizedText.Create(CueKey(def.Id));
            cue.Speaker = new DialogSpeaker { NoSpeaker = true, MoveCamera = false };
            cue.TurnSpeaker = false;
            cue.Conditions = new ConditionsChecker();
            cue.OnShow = new ActionList();
            cue.OnStop = new ActionList();
            cue.Continue = new CueSelection();

            var answers = new List<BlueprintAnswerBase>();
            foreach (InspectCheck check in def.Checks)
            {
                foreach (WardSkill skill in check.Skills)
                {
                    answers.Add(CheckAnswer(def, check, skill, cue));
                }
            }
            foreach (WardAction action in WardActions.For(def.Id))
            {
                answers.Add(ActionAnswer(def, action, cue));
            }
            answers.Add(Answer($"{def.Id}.leave", "Leave it be.", null, null, null));
            cue.Answers = answers;

            var dialog = Create<BlueprintDialog>("KWR_WardDialog_" + def.Id);
            dialog.FirstCue = new CueSelection { Cues = new List<BlueprintCueBase> { cue } };
            dialog.StartActions = Actions(Callback("Refresh inspection text", () => WardInteract.Refresh(def.Id)));
            dialog.FinishActions = Actions(Callback("Run the chosen action", WardInteract.RunPending));
            dialog.ReplaceActions = new ActionList();
            return dialog;
        }

        private static BlueprintAnswer CheckAnswer(InspectDef def, InspectCheck check, WardSkill skill, BlueprintCue cue)
        {
            string id = $"{def.Id}.{check.Key}.{skill}";
            GameAction roll = Callback($"Roll {skill} at {def.Id}", () => WardInteract.Study(def.Id, check, skill));
            string label = $"[{SkillLabels[skill]}] {check.Label}";
            return Answer(id, label, cue, roll, () => WardInteract.CanStudy(check, skill));
        }

        private static BlueprintAnswer ActionAnswer(InspectDef def, WardAction action, BlueprintCue cue)
        {
            string id = $"{def.Id}.action.{action.Key}";
            GameAction run = Callback($"{action.Key} at {def.Id}", () => WardInteract.Perform(def.Id, action));
            return Answer(id, action.Label, action.StaysOpen ? cue : null, run, action.Show);
        }

        private static BlueprintAnswer Answer(string id, string text, BlueprintCue next, GameAction onSelect, Func<bool> show)
        {
            string key = "kwr.ward.answer." + id;
            LocalizedText.Define(key, text);
            var answer = Create<BlueprintAnswer>("KWR_WardAnswer_" + id);
            answer.Text = LocalizedText.Create(key);
            answer.NextCue = next == null ? new CueSelection() : new CueSelection { Cues = new List<BlueprintCueBase> { next } };
            answer.ShowCheck = new ShowCheck();
            answer.CharacterSelection = new CharacterSelection { ComparisonStats = new StatType[0] };
            answer.ShowConditions = show == null ? new ConditionsChecker() : new ConditionsChecker { Conditions = new Condition[] { Condition(show, id) } };
            answer.SelectConditions = new ConditionsChecker();
            answer.OnSelect = onSelect == null ? new ActionList() : Actions(onSelect);
            answer.FakeChecks = new CheckData[0];
            answer.AlignmentRequirement = AlignmentComponent.None;
            return answer;
        }


        private static KwrCondition Condition(Func<bool> show, string id)
        {
            var condition = ScriptableObject.CreateInstance<KwrCondition>();
            condition.name = "KWR_WardCondition_" + id;
            condition.Caption = id;
            condition.Body = show;
            return condition;
        }

        private static GameAction Callback(string caption, Action body)
        {
            var callback = ScriptableObject.CreateInstance<KwrCallback>();
            callback.name = "KWR_WardCallback";
            callback.Caption = caption;
            callback.Body = body;
            return callback;
        }

        private static ActionList Actions(params GameAction[] actions)
        {
            return new ActionList { Actions = actions };
        }

        private static T Create<T>(string name) where T : BlueprintScriptableObject
        {
            var blueprint = ScriptableObject.CreateInstance<T>();
            blueprint.name = name;
            blueprint.AssetGuid = ScenarioIds.Derive("warddialog." + name);
            s_Register(blueprint);
            return blueprint;
        }
    }
}
