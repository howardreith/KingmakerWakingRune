using System;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Interaction;
using KingmakerWakingRune.Blueprints;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// The short opening exchange (page 14 gives behaviour, not a script; this wording is the adaptation's own). He demands
    /// submission and names; the party may identify itself, tell him Thassilon fell, or challenge him. Every path ends with
    /// him hostile: there is no persuasion, recruitment, reward or bypass of the fight.
    /// </summary>
    internal static class KruneDialog
    {
        public static BlueprintDialog Opening { get; private set; }

        private static Action<BlueprintScriptableObject> s_Register;

        public static void Build(Action<BlueprintScriptableObject> register)
        {
            if (!KruneBlueprints.Ready)
            {
                return;
            }
            s_Register = register;
            BlueprintCue end = Cue("end", "(In Thassilonian) \"Enough. You have told me what I needed. Kneel, or be made to.\"");
            BlueprintCue challenge = Cue("challenge", "(In Thassilonian) \"Then you will be the first to learn what Sloth has kept for ten thousand years.\"");
            BlueprintCue silence = Cue("silence", "(In Thassilonian) \"Silence. You waste the first hour of my return. Very well: I will have your names from what is left of you.\"");
            BlueprintCue greet = Cue("greet", "Runes crawl over his skin as he studies you. (In Thassilonian) \"Kneel. You stand before Krune, Runelord of Sloth, master of Haruka. You wear no mark of Lissala that I know. Prostrate yourselves, and name yourselves and your masters, while I still permit you breath.\"");
            BlueprintCue identified = Cue("identified", "(In Thassilonian) \"Pathfinders. Absalom. Names without weight. Some upstart order of Xin-Edasseril's, or a peacock's errand for Alaznist? It does not matter whose. Kneel.\"");
            BlueprintCue disbelief = Cue("disbelief", "He laughs, once. (In Thassilonian) \"Ten thousand years? A clumsy lie. My sleep was to be a century. Haruka endures, my rivals scheme on their thrones, and the stars—\"", Do("He was told of Thassilon's fall", () => KruneAwakening.OnParleyBeat("told")));
            BlueprintCue calculating = Cue("calculating", "He falls silent, eyes moving over the thinned vapour and the walls that hide his rune foci. (In Thassilonian) \"...So the stars did fall, and the others with them. Then no rival runelord stands between me and every throne in Varisia. An interesting age to wake into.\"", Do("He has weighed the elapsed millennia", () => KruneAwakening.OnParleyBeat("calculated")));

            var hub = new List<BlueprintAnswerBase>
            {
                Answer("identify", "\"We are Pathfinders, of the Pathfinder Society in Absalom.\"", identified, Do("The party identified itself", () => KruneAwakening.OnParleyBeat("identified"))),
                Answer("fall", "\"Thassilon is gone. It fell ten thousand years ago.\"", disbelief, null),
                Answer("challenge", "\"We have come to end you, Runelord.\"", challenge, Do("The party challenged him", () => KruneAwakening.OnParleyBeat("challenged"))),
                Answer("silence", "[Say nothing.]", silence, null),
            };
            greet.Answers = hub;
            identified.Answers = hub;
            disbelief.Answers = new List<BlueprintAnswerBase>
            {
                Answer("earthfall", "\"Earthfall ended your age. Your rivals and your empire are dust.\"", calculating, null),
                Answer("disbelief.challenge", "\"Believe what you like. We have come to end you.\"", challenge, Do("The party challenged him", () => KruneAwakening.OnParleyBeat("challenged"))),
            };
            calculating.Answers = new List<BlueprintAnswerBase>
            {
                Answer("refuse", "\"We will not kneel.\"", end, null),
                Answer("calculating.challenge", "\"Your new age ends here.\"", challenge, Do("The party challenged him", () => KruneAwakening.OnParleyBeat("challenged"))),
            };

            var dialog = Create<BlueprintDialog>("KWR_KruneOpening");
            dialog.FirstCue = new CueSelection { Cues = new List<BlueprintCueBase> { greet } };
            dialog.StartActions = new ActionList { Actions = new[] { Do("The opening exchange begins", KruneAwakening.OnParleyStarted) } };
            dialog.FinishActions = new ActionList { Actions = new[] { Do("The opening exchange ends: he is hostile", KruneAwakening.OnParleyFinished) } };
            dialog.ReplaceActions = new ActionList();
            Opening = dialog;
        }

        private static BlueprintCue Cue(string id, string text, GameAction onShow = null)
        {
            string key = "kwr.krune.cue." + id;
            LocalizedText.Define(key, text);
            var cue = Create<BlueprintCue>("KWR_KruneCue_" + id);
            cue.Text = LocalizedText.Create(key);
            cue.Speaker = new DialogSpeaker { Blueprint = KruneBlueprints.Unit, MoveCamera = false };
            cue.TurnSpeaker = true;
            cue.Conditions = new ConditionsChecker();
            cue.OnShow = onShow == null ? new ActionList() : new ActionList { Actions = new[] { onShow } };
            cue.OnStop = new ActionList();
            cue.Continue = new CueSelection();
            cue.Answers = new List<BlueprintAnswerBase>();
            return cue;
        }

        private static BlueprintAnswer Answer(string id, string text, BlueprintCue next, GameAction onSelect)
        {
            string key = "kwr.krune.answer." + id;
            LocalizedText.Define(key, text);
            var answer = Create<BlueprintAnswer>("KWR_KruneAnswer_" + id);
            answer.Text = LocalizedText.Create(key);
            answer.NextCue = next == null ? new CueSelection() : new CueSelection { Cues = new List<BlueprintCueBase> { next } };
            answer.ShowCheck = new ShowCheck();
            answer.CharacterSelection = new CharacterSelection { ComparisonStats = new StatType[0] };
            answer.ShowConditions = new ConditionsChecker();
            answer.SelectConditions = new ConditionsChecker();
            answer.OnSelect = onSelect == null ? new ActionList() : new ActionList { Actions = new[] { onSelect } };
            answer.FakeChecks = new CheckData[0];
            answer.AlignmentRequirement = AlignmentComponent.None;
            return answer;
        }

        private static GameAction Do(string caption, Action body)
        {
            var callback = ScriptableObject.CreateInstance<KwrCallback>();
            callback.name = "KWR_KruneCallback";
            callback.Caption = caption;
            callback.Body = body;
            return callback;
        }

        private static T Create<T>(string name) where T : BlueprintScriptableObject
        {
            var blueprint = ScriptableObject.CreateInstance<T>();
            blueprint.name = name;
            blueprint.AssetGuid = ScenarioIds.Derive("krunedialog." + name);
            s_Register(blueprint);
            return blueprint;
        }
    }
}
