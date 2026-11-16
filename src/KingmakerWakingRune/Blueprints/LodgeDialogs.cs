using System;
using System.Collections.Generic;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Interaction;
using Kingmaker.Visual.Animation.Kingmaker;
using KingmakerWakingRune.Lodge;
using KingmakerWakingRune.Party;
using UnityEngine;

namespace KingmakerWakingRune.Blueprints
{
    /// <summary>A blueprint action that runs mod code; lives only in memory, so it is never serialized.</summary>
    internal sealed class KwrCallback : GameAction
    {
        public Action Body;
        public string Caption;

        public override string GetCaption()
        {
            return Caption;
        }

        public override void RunAction()
        {
            Body?.Invoke();
        }
    }

    /// <summary>
    /// The supplied briefing script and the service conversations, as native dialogue blueprints with
    /// stable identities. The script text is the owner's, unchanged except for the "Heidmarch" correction.
    /// </summary>
    internal static class LodgeDialogs
    {
        public static BlueprintDialog Briefing { get; private set; }
        public static BlueprintDialog FollowUp { get; private set; }

        private static Action<BlueprintScriptableObject> s_Register;

        public static void Build(BlueprintUnit sheila, Action<BlueprintScriptableObject> register, BlueprintUnlockableFlag briefingComplete)
        {
            s_Register = register;
            BlueprintUnit ambrus = LodgeCast.Find("ambrus").Unit;
            BlueprintUnit masked = LodgeCast.Find("masked").Unit;
            BlueprintUnit quartermaster = LodgeCast.Find("quartermaster").Unit;
            BlueprintUnit coordinator = LodgeCast.Find("coordinator").Unit;

            // Closing line and the four hub answers are shared by the opening and later follow-ups.
            BlueprintCue closing = Cue("closing", ambrus, "I don't need to stress to you the importance and danger of this assignment. Speak with the quartermaster and coordinator and make sure your team is fully equipped for the battle ahead. When you are ready, use your tokens. Good luck Pathfinders.");
            var hub = new List<BlueprintAnswerBase>();
            BlueprintCue krune = Cue("krune", sheila, "Krune is one of the Runelords, powerful rulers of ancient Thassilon who partnered with dragons and employed rune magic to increase their power. Krune was the Runelord of Sloth and an extraordinarily powerful conjurer.");
            BlueprintCue defeat = Cue("defeat", masked, "The Master of Scrolls speculates that harnessing as much Rune magic as Krune supposedly can would require powerful foci. If you can identify what these are and disrupt them, Krune might lose his greatest weapon. If you cannot find such foci, your battle will be the harder. Do not fail.");
            BlueprintCue returnCue = Cue("return", ambrus, "The tokens have a minor enchantment that tracks where they were triggered, so I have good reason to believe that you will be able to return. Pay careful attention to what your refuge token looks like before you break it; you might need it to come back.");
            BlueprintCue fail = Cue("fail", sheila, "The Decemvirate has contingencies for just such an outcome, but I am sure that with you leading the charge, we will end this threat today.");
            hub.Add(Answer("hub.krune", "Who is Krune?", krune));
            hub.Add(Answer("hub.defeat", "How are we supposed to defeat a Runelord? Aren't they extremely powerful?", defeat));
            hub.Add(Answer("hub.return", "How will we return?", returnCue));
            hub.Add(Answer("hub.fail", "What if we fail?", fail));
            hub.Add(Answer("hub.understand", "I understand.", closing));
            krune.Answers = hub;
            defeat.Answers = hub;
            returnCue.Answers = hub;
            fail.Answers = hub;

            BlueprintCue questions = Cue("sheila.brief", sheila, "We recently acquired these refuge tokens and their command words. When broken, the tokens will take a handful of agents directly to Krune's sanctuary. If he has yet to revive, do whatever you can to ensure he never does. If he does awaken, pray that he is too dazed and addled to command his full power. Whatever it takes, end this threat to Varisia-permanently. The Society has recovered Thassilonian relics, perhaps some through your deeds, and I would rather they be in your hands as you face Krune; they will do us little good in a museum if the runelord returns. Once you defeat him, take special care to recover his two artifacts: a dragon-tooth spear and a rune-inscribed rod. Once you are ready, I will show you how to use the refuge tokens. Do you have any questions?");
            questions.Answers = hub;
            BlueprintCue plans = Cue("ambrus.plans", ambrus, "Over the past year, we've received increasingly concerning reports from this region that the Lissalans have plans in motion to revive Krune, the Runelord of Sloth and one of the most powerful wizards of ancient Thassilon. We have set our own plans in motion to keep him in check if the Lissalans succeed, but Sheila Heidmarch has an ambitious plan regarding this threat.");
            Continue(plans, questions);
            BlueprintCue nods = StageDirection("stage.nods", masked, "The masked figure listens and nods silently.");
            nods.OnShow = Actions(Callback("Focus the masked figure", BriefingService.FocusMaskedFigure));
            Continue(nods, plans);
            BlueprintCue opening = Cue("ambrus.opening", ambrus, "My fellow Pathfinders, as you can see, the task that lies ahead transcends the interests of Magnimar's increasingly esteemed lodge; it's a mission that will affect the entire Pathfinder Society and merits the direct attention of the Decemvirate.");
            Continue(opening, nods);

            Briefing = Dialog("briefing", opening, Actions(Callback("Record briefing complete", BriefingService.MarkComplete)));

            BlueprintCue followUp = Cue("sheila.followup", sheila, "Do you have any questions?");
            followUp.Answers = hub;
            FollowUp = Dialog("followup", followUp, new ActionList());

            ConditionsChecker afterBriefing = Conditions(Flag(briefingComplete));
            AddInteraction(sheila, FollowUp, afterBriefing);
            AddInteraction(ambrus, FollowUp, afterBriefing);

            BlueprintCue shopPrompt = Cue("qm.prompt", quartermaster, "The Society's stores are open to the team. What do you need?");
            shopPrompt.Answers = new List<BlueprintAnswerBase>
            {
                Answer("qm.shop", "Show me what you have.", null, Callback("Open the Quartermaster's shop", QuartermasterService.RequestOpen)),
                Answer("qm.rest", "We need a night's rest to prepare before we leave.", null, Callback("Rest the party at the lodge", LodgeRestService.RequestRest)),
                Answer("qm.leave", "Nothing for now.", null),
            };
            AddInteraction(quartermaster, Dialog("quartermaster", shopPrompt, new ActionList()), new ConditionsChecker());

            BlueprintCue recruitPrompt = Cue("coordinator.prompt", coordinator, "Need more hands for this assignment? The Society will sponsor them.");
            recruitPrompt.Answers = new List<BlueprintAnswerBase>
            {
                Answer("coordinator.recruit", "I need to recruit a new adventurer.", null, Callback("Open native recruitment", Recruitment.RequestRecruit)),
                Answer("coordinator.party", "I need to change who goes with me.", null, Callback("Open native party selection", Recruitment.RequestPartySelection)),
                Answer("coordinator.retrain.self", "I want to retrain myself. (Free: rebuild from level 1 to my current experience.)", null, Callback("Open free retraining of the protagonist", Retraining.RequestProtagonist)),
                Answer("coordinator.retrain.companion", "I want one of my companions retrained. (Free: choose who, then rebuild from level 1.)", null, Callback("Open free retraining of a chosen companion", Retraining.RequestCompanion)),
                Answer("coordinator.leave", "Nothing for now.", null),
            };
            AddInteraction(coordinator, Dialog("coordinator", recruitPrompt, new ActionList()), new ConditionsChecker());

            BuildDebriefs(sheila, ambrus, masked);
        }

        public static BlueprintDialog DebriefSuccess { get; private set; }
        public static BlueprintDialog DebriefPartial { get; private set; }
        public static BlueprintDialog DebriefWithdrawn { get; private set; }

        /// <summary>
        /// The return to the lodge (WR-FINAL): one short exchange for each way the expedition can end. Original wording in the
        /// briefing's voice. The handover and the outcome are recorded by <see cref="Lodge.DebriefService"/>, once.
        /// </summary>
        private static void BuildDebriefs(BlueprintUnit sheila, BlueprintUnit ambrus, BlueprintUnit masked)
        {
            BlueprintCue s1 = Cue("debrief.success.ambrus", ambrus, "You are back, all of you that could be, and I can read it in your faces before you say a word. Krune is dead?");
            BlueprintCue s2 = Cue("debrief.success.sheila", sheila, "Slain in his own sanctum, before he could take back the full weight of his power. Varisia will never know how close it came, and that is as it should be. The Society owes you more than it will ever say aloud.");
            BlueprintCue s3 = Cue("debrief.success.masked", masked, "The spear and the rod. Give them to me. They will not stay in Magnimar.");
            BlueprintCue s4 = StageDirection("debrief.success.handover", masked, "The masked figure takes the dragon-tooth spear and the rune-carved rod, speaks a single word, and is gone.");
            BlueprintCue s5 = Cue("debrief.success.rest", ambrus, "Rest now; you have earned it. The Lissalans have lost their high priest and their runelord in a single night. Whatever comes next can wait for another day.");
            Continue(s1, s2);
            Continue(s2, s3);
            Continue(s3, s4);
            Continue(s4, s5);
            s4.OnShow = new ActionList { Actions = new[] { Callback("Hand over the artifacts", Lodge.DebriefService.HandOver) } };
            DebriefSuccess = Dialog("debrief.success", s1, new ActionList { Actions = new[] { Callback("Record the outcome", Lodge.DebriefService.Finish) } });

            BlueprintCue p1 = Cue("debrief.partial.ambrus", ambrus, "Krune is dead? Then the worst of it is behind us. But you were sent for two things, and I do not see them both.");
            BlueprintCue p2 = Cue("debrief.partial.masked", masked, "A runelord's weapon left in a broken sanctum is a seed for the next revival. What you brought, I will take. What you left, we will see to ourselves, in our own time.");
            BlueprintCue p3 = Cue("debrief.partial.sheila", sheila, "You did what you went to do: the runelord is slain. What was left behind is the Decemvirate's concern now. Rest, Pathfinders.");
            Continue(p1, p2);
            Continue(p2, p3);
            p2.OnShow = new ActionList { Actions = new[] { Callback("Hand over what was carried home", Lodge.DebriefService.HandOver) } };
            DebriefPartial = Dialog("debrief.partial", p1, new ActionList { Actions = new[] { Callback("Record the outcome", Lodge.DebriefService.Finish) } });

            BlueprintCue w1 = Cue("debrief.withdrawn.sheila", sheila, "You are back early. And from your faces, Krune still lives.");
            BlueprintCue w2 = Cue("debrief.withdrawn.ambrus", ambrus, "Then the Decemvirate's contingencies must do what they were made for. They will slow him; they will not end him. That task falls to another team now.");
            BlueprintCue w3 = Cue("debrief.withdrawn.masked", masked, "Your tokens are spent and the sanctum is lost to us. Live, learn from it, and do not speak of this to anyone.");
            Continue(w1, w2);
            Continue(w2, w3);
            DebriefWithdrawn = Dialog("debrief.withdrawn", w1, new ActionList { Actions = new[] { Callback("Record the outcome", Lodge.DebriefService.Finish) } });
        }


        private static BlueprintCue Cue(string id, BlueprintUnit speaker, string text)
        {
            string key = "kwr.dialog.cue." + id;
            LocalizedText.Define(key, text);
            var cue = Create<BlueprintCue>("KWR_Cue_" + id);
            cue.Text = LocalizedText.Create(key);
            cue.Speaker = new DialogSpeaker { Blueprint = speaker };
            cue.Conditions = new ConditionsChecker();
            cue.OnShow = new ActionList();
            cue.OnStop = new ActionList();
            cue.Continue = new CueSelection();
            return cue;
        }

        /// <summary>
        /// Narration about one character: nobody speaks (no speaker entity, no turn or camera move), but the panel shows
        /// that character's portrait through the cue's native SpeakerPortrait, and the text is in the narrator's colour.
        /// </summary>
        private static BlueprintCue StageDirection(string id, BlueprintUnit subject, string text)
        {
            BlueprintCue cue = Cue(id, null, "{n}" + text + "{/n}");
            cue.Speaker = new DialogSpeaker { NoSpeaker = true, MoveCamera = false, SpeakerPortrait = subject };
            cue.TurnSpeaker = false;
            return cue;
        }

        private static void Continue(BlueprintCue cue, BlueprintCue next)
        {
            cue.Continue = new CueSelection { Cues = new List<BlueprintCueBase> { next } };
        }

        private static BlueprintAnswer Answer(string id, string text, BlueprintCue next, GameAction onSelect = null)
        {
            string key = "kwr.dialog.answer." + id;
            LocalizedText.Define(key, text);
            var answer = Create<BlueprintAnswer>("KWR_Answer_" + id);
            answer.Text = LocalizedText.Create(key);
            answer.NextCue = next == null ? new CueSelection() : new CueSelection { Cues = new List<BlueprintCueBase> { next } };
            answer.ShowCheck = new ShowCheck();
            answer.CharacterSelection = new CharacterSelection { ComparisonStats = new StatType[0] };
            answer.ShowConditions = new ConditionsChecker();
            answer.SelectConditions = new ConditionsChecker();
            answer.OnSelect = onSelect == null ? new ActionList() : Actions(onSelect);
            answer.FakeChecks = new CheckData[0];
            answer.AlignmentRequirement = AlignmentComponent.None;
            return answer;
        }

        private static BlueprintDialog Dialog(string id, BlueprintCue first, ActionList finishActions)
        {
            var dialog = Create<BlueprintDialog>("KWR_Dialog_" + id);
            dialog.FirstCue = new CueSelection { Cues = new List<BlueprintCueBase> { first } };
            dialog.FinishActions = finishActions;
            dialog.StartActions = new ActionList();
            dialog.ReplaceActions = new ActionList();
            return dialog;
        }

        private static void AddInteraction(BlueprintUnit unit, BlueprintDialog dialog, ConditionsChecker conditions)
        {
            var interaction = ScriptableObject.CreateInstance<DialogOnClick>();
            interaction.name = "KWR_Interact_" + unit.name;
            interaction.Dialog = dialog;
            interaction.NoDialogActions = new ActionList();
            interaction.Conditions = conditions;
            unit.ComponentsArray = new BlueprintComponent[] { interaction };
        }

        private static ConditionsChecker Conditions(params Condition[] conditions)
        {
            return new ConditionsChecker { Conditions = conditions };
        }

        private static Condition Flag(BlueprintUnlockableFlag flag)
        {
            var condition = ScriptableObject.CreateInstance<FlagUnlocked>();
            condition.name = "KWR_Flag_" + flag.name;
            condition.ConditionFlag = flag;
            return condition;
        }

        private static GameAction Callback(string caption, Action body)
        {
            var callback = ScriptableObject.CreateInstance<KwrCallback>();
            callback.name = "KWR_Callback";
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
            blueprint.AssetGuid = ScenarioIds.Derive("dialog." + name);
            s_Register(blueprint);
            return blueprint;
        }
    }
}
