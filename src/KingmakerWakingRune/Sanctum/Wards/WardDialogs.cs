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
            Prophecy = Notice("Prophecy", true, "You may have bled me, mortal, but Krune has mastered the rune of armor and your blades shall never pierce his flesh! Alacrity bestows him with a sixth sense with which he anticipates intentions and actions. Still other runes feed him divine power, strengthening his body and mind, as well as assuring him immortality.");
            Blood = Notice("Blood", false, "Kurshu’s blood spills across the stone. The droplets gather into Thassilonian runes.",
                "Enjoy your victory, and I shall enjoy mine. In your righteousness you have sacrificed my life to assure the rise of another. May Runelord Krune skin you alive and use your flesh as the canvas for the scriptures of Lissala’s second coming!",
                "Krune will awaken in 90 minutes. The expedition must act quickly.");
            foreach (var slot in BearerManifest.Roles.Keys) BuildSurvivor(slot);
            foreach (InspectDef def in WardText.All)
            {
                Dialogs[def.Id] = BuildOne(def);
            }
        }

        private static void BuildSurvivor(string id)
        {
            string key = "survivor." + id;
            Func<StatueState> state = () => WardRuntime.State.Statue(id);
            var cue = Create<BlueprintCue>("KWR_Survivor_" + id);
            cue.Text = LocalizedText.Create(CueKey(key));
            cue.Speaker = new DialogSpeaker();
            cue.TurnSpeaker = false;
            cue.Conditions = new ConditionsChecker();
            cue.OnShow = cue.OnStop = new ActionList();
            cue.Continue = new CueSelection();
            Action refresh = () =>
            {
                var s = state();
                string line = s.Role != BearerRole.Deceive ? "The cultist glances past you, looking for a way out."
                    : s.Conversation == "questioned" ? "“The woman beyond the gate tends the runes. She alone can put them out. Let me speak to her first.”"
                    : s.Conversation == "blocked" ? "The cultist waits, watching the door."
                    : s.Conversation == "released" ? "“You gave me leave to pass.”"
                    : "“A prisoner, like the others. Open the gate for me and I will beg its keeper to quench the runes.”";
                if (s.Role == BearerRole.Deceive && s.DeceptionDetected && s.Conversation != "released")
                    line += " The speaker’s hand forms the seven-pointed sign at the mention of the keeper.";
                TextKey(key, line);
            };
            cue.Answers = new List<BlueprintAnswerBase>
            {
                Answer(key + ".question", "Who keeps the gate?", cue, Callback("Question the cultist", () => { state().Conversation = "questioned"; refresh(); }),
                    () => state().Role == BearerRole.Deceive && state().Conversation == "met"),
                Answer(key + ".expose", "I saw your sign. Stay here.", null, Callback("Block the cultist", () => state().Conversation = "blocked"),
                    () => state().DeceptionDetected && state().Conversation != "blocked"),
                Answer(key + ".block", "Stay where you are.", null, Callback("Block the path", () => state().Conversation = "blocked"), () => state().Conversation != "blocked"),
                Answer(key + ".release", "Go.", null, Callback("Let the cultist leave", () => state().Conversation = "released"), null),
                Answer(key + ".attack", "[Attack]", null, Callback("Attack the cultist", () =>
                {
                    var body = WardStatues.Body(state());
                    var actor = Kingmaker.Game.Instance.DialogController.Initiator ?? Kingmaker.Game.Instance.Player.MainCharacter.Value;
                    WardInteract.Defer(() =>
                    {
                        WardStatues.Provoke(actor, body);
                        if (body != null) actor.Commands.Run(Kingmaker.UnitLogic.Commands.UnitAttack.CreateAttackCommand(actor, body));
                    });
                }), null),
            };
            var dialog = Create<BlueprintDialog>("KWR_SurvivorDialog_" + id);
            dialog.FirstCue = new CueSelection { Cues = new List<BlueprintCueBase> { cue } };
            dialog.StartActions = Actions(Callback("Meet the cultist", () =>
            {
                if (state().Conversation.Length == 0) state().Conversation = "met";
                refresh();
            }));
            dialog.FinishActions = Actions(Callback("Resume expedition", WardInteract.RunPending));
            dialog.ReplaceActions = new ActionList();
            Dialogs[key] = dialog;
        }

        public static void OpenSurvivor(StatueState state, Kingmaker.EntitySystem.Entities.UnitEntityData body, Kingmaker.EntitySystem.Entities.UnitEntityData user)
        {
            Kingmaker.Game.Instance.DialogController.StartDialogWithUnit(Dialogs["survivor." + state.Id], body, user);
        }

        public static BlueprintDialog Prophecy { get; private set; }
        public static BlueprintDialog Blood { get; private set; }

        public static bool IsCombatStory(BlueprintDialog dialog)
        {
            return dialog != null && (dialog == Prophecy || dialog == Blood);
        }

        private static BlueprintDialog Notice(string id, bool spoken, params string[] lines)
        {
            BlueprintCue next = null;
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string key = id + i;
                TextKey(key, lines[i]);
                var cue = Create<BlueprintCue>("KWR_KurshuCue_" + key);
                cue.Text = LocalizedText.Create(CueKey(key));
                cue.Speaker = new DialogSpeaker { NoSpeaker = !spoken, MoveCamera = spoken };
                cue.TurnSpeaker = false;
                cue.Conditions = new ConditionsChecker();
                cue.OnShow = new ActionList();
                cue.OnStop = new ActionList();
                cue.Continue = new CueSelection();
                cue.Answers = new List<BlueprintAnswerBase> { Answer(key, "Continue", next, null, null) };
                next = cue;
            }
            var dialog = Create<BlueprintDialog>("KWR_KurshuDialog_" + id);
            dialog.TurnFirstSpeaker = dialog.TurnPlayer = false;
            dialog.FirstCue = new CueSelection { Cues = new List<BlueprintCueBase> { next } };
            dialog.StartActions = Actions(Callback("Kurshu's message displayed", () =>
            {
                if (spoken) WardRuntime.State.Kurshu.WoundDialoguePending = false;
                else
                {
                    WardRuntime.State.Kurshu.BloodDialoguePending = false;
                    WardRuntime.State.Kurshu.BloodMessageShown = true;
                }
            }));
            dialog.FinishActions = new ActionList();
            dialog.ReplaceActions = new ActionList();
            return dialog;
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
    // Dialog mode still ticks movement and unit actions. Acknowledging these two panels
    // must preserve the command queues, including attacks already in their animation.
    [Harmony12.HarmonyPatch(typeof(Kingmaker.Controllers.Units.UnitActionController), "TickOnUnit")]
    internal static class KurshuDialogCommands
    {
        internal static bool Prefix() => !WardDialogs.IsCombatStory(Kingmaker.Game.Instance.DialogController.Dialog);
    }

    [Harmony12.HarmonyPatch(typeof(Kingmaker.Controllers.Units.UnitMoveController), "Tick")]
    internal static class KurshuDialogMovement
    {
        private static bool Prefix() => KurshuDialogCommands.Prefix();
    }

    [Harmony12.HarmonyPatch(typeof(Kingmaker.Controllers.TimeController), "Tick")]
    internal static class KurshuDialogClock
    {
        private static void Postfix(Kingmaker.Controllers.TimeController __instance)
        {
            if (!WardDialogs.IsCombatStory(Kingmaker.Game.Instance.DialogController.Dialog)) return;
            __instance.SetDeltaTime(0f);
            Time.timeScale = 0f;
        }
    }

    // Ordinary conversations dismiss combat commands. These two notices only open the reader.
    [Harmony12.HarmonyPatch(typeof(Kingmaker.Controllers.Dialog.DialogController), "Start")]
    internal static class KurshuDialogStart
    {
        private static bool Prefix(Kingmaker.Controllers.Dialog.DialogController __instance)
        {
            if (!WardDialogs.IsCombatStory(__instance.Dialog)) return true;
            Kingmaker.PubSubSystem.EventBus.RaiseEvent<Kingmaker.PubSubSystem.IDialogStartHandler>(h => h.HandleDialogStarted());
            return false;
        }
    }

    [Harmony12.HarmonyPatch(typeof(Kingmaker.Controllers.Dialog.DialogController), "AddInvolvedUnit")]
    internal static class KurshuDialogInvolved
    {
        private static bool Prefix(Kingmaker.Controllers.Dialog.DialogController __instance)
        {
            return !WardDialogs.IsCombatStory(__instance.Dialog);
        }
    }
}
