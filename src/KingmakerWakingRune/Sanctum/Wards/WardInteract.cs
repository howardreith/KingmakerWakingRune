using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Commands;
using KingmakerWakingRune.Blueprints;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>One answer that touches a fixture. <see cref="StaysOpen"/> keeps the dialogue up for another choice.</summary>
    internal sealed class WardAction
    {
        public string Key;
        public string Label;
        public bool StaysOpen;
        public Func<bool> Show;
        public Action<UnitEntityData> Run;
    }

    /// <summary>The manipulate/attack answers for each fixture. Reading the cue never reaches any of these.</summary>
    internal static class WardActions
    {
        private const string FlailBlueprint = "bce3d01712a2959468b57f5bce2f0a0f";

        /// <summary>
        /// Physical answers never resolve inside the paused dialogue: they are deferred until it closes, then the character
        /// who opened it walks into reach and acts. Another character acts by clicking the fixture themselves.
        /// </summary>
        public static IEnumerable<WardAction> For(string id)
        {
            if (KruneProjection.Foci.Any(f => f.Id == id))
            {
                RuneFocus focus = KruneProjection.Focus(id);
                yield return new WardAction { Key = "trickery", Label = "[Trickery] Tamper with the plate", Show = () => !IsDisabled(id), Run = user => Physical(user, id, "tamper with the plate", u => !IsDisabled(id), u => UseRuneSkill(u, focus, RuneSkill.Trickery)) };
                yield return new WardAction { Key = "umd", Label = "[Use Magic Device] Try to command the rune", Show = () => !IsDisabled(id), Run = user => Physical(user, id, "command the rune", u => !IsDisabled(id), u => UseRuneSkill(u, focus, RuneSkill.UseMagicDevice)) };
                yield return new WardAction { Key = "attack", Label = "Attack the plate", Show = () => !IsDisabled(id), Run = user => WardInteract.Defer(() => WardInteract.Attack(WardFixtures.Find(id))) };
                // Offered at every active plate whenever the speaker has summons, so it never singles out the rune it suits.
                yield return new WardAction { Key = "summons", Label = "Direct my summoned creatures to attack this rune", Show = () => !IsDisabled(id) && SummonOrders.SummonsOf(Speaker()).Count > 0, Run = user => WardInteract.Defer(() => SummonOrders.Direct(user, id)) };
                yield return new WardAction { Key = "recall", Label = "Call the summoned creatures off this rune", Show = () => SummonOrders.HasOrders(id), Run = user => WardInteract.Defer(() => SummonOrders.Recall(id)) };
            }
            else if (id == WardText.DoorId)
            {
                yield return new WardAction { Key = "present", Label = "Press the Sihedron impression to the handprint", Show = () => !WardRuntime.State.Door.Destroyed && WardDoor.IsClosed && WardCredentials.HoldsImpression(), Run = user => PresentAtDoor(user, CredentialKind.Impression) };
                yield return new WardAction { Key = "presenthand", Label = "Press the branded hand to the handprint", Show = () => !WardRuntime.State.Door.Destroyed && WardDoor.IsClosed && HoldsHand(), Run = user => PresentAtDoor(user, CredentialKind.Hand) };
                yield return new WardAction { Key = "press", Label = "Press your own hand to the handprint", Show = () => !WardRuntime.State.Door.Destroyed, Run = user => WardInteract.Defer(() => WardDoor.Order(user, "press the handprint", u => !WardRuntime.State.Door.Destroyed, WardDoor.Press)) };
                yield return new WardAction { Key = "force", Label = "[Strength] Force the door", Show = () => WardRuntime.State.Door.Latched && !WardRuntime.State.Door.Destroyed, Run = user => WardInteract.Defer(() => WardDoor.Order(user, "force the door", u => WardRuntime.State.Door.Latched && !WardRuntime.State.Door.Destroyed, WardDoor.Force)) };
                yield return new WardAction { Key = "disarm", Label = "[Trickery] Disarm the feeder", Show = WardDoor.CanDisarm, Run = user => WardInteract.Defer(() => WardDoor.Order(user, "disarm the feeder", u => WardDoor.CanDisarm(), WardDoor.Disarm)) };
                yield return new WardAction { Key = "attack", Label = "Attack the door", Show = () => !WardRuntime.State.Door.Destroyed, Run = user => WardInteract.Defer(() => WardInteract.Attack(WardFixtures.Door)) };
            }
            else if (id == WardText.GemId)
            {
                yield return new WardAction { Key = "break", Label = "[Strength] Force the emerald from the altar", Show = () => !WardRuntime.State.Gem.Destroyed, Run = user => WardInteract.Defer(WardGem.Order) };
                yield return new WardAction { Key = "attack", Label = "Attack the emerald", Show = () => !WardRuntime.State.Gem.Destroyed, Run = user => WardInteract.Defer(() => WardInteract.Attack(WardFixtures.Gem)) };
            }
            else if (id == WardText.PedestalId)
            {
                // The way home (WR-FINAL): no roll stands between the party and the shard its own token matches.
                yield return new WardAction { Key = "lookhome", Label = "Hold up the shard that matches your refuge token, and look through the frame", StaysOpen = true, Show = () => WardRuntime.State.Conclusion.ReturnedAt < 0d, Run = u => LookThroughPortal() };
                yield return new WardAction { Key = "gohome", Label = "Step through to the lodge (one way, the whole party)", Show = () => WardRuntime.State.Conclusion.ReturnedAt < 0d, Run = u => WardInteract.Defer(ConfirmPortal) };
            }
            else if (id == WardText.ToolsId)
            {
                // An aggressive act against the rite, through the one commit every engagement uses: never a second trigger.
                yield return new WardAction { Key = "interrupt", Label = "Strike at the ritual tools (an attack on the rite: its guardians and the countdown answer at once)", StaysOpen = true, Show = () => !WardRuntime.State.Ritual.ConfrontationStarted, Run = u => Lashmistress.LashmistressEncounter.RitualAttacked(u, "the ritual tools were struck") };
            }
            else if (id == WardText.FlailId)
            {
                yield return new WardAction { Key = "draw", Label = "Draw the flail free (good-aligned character)", Show = () => !WardRuntime.State.FlailAcquired && IsGood(Speaker()), Run = user => Physical(user, id, "draw the flail", u => !WardRuntime.State.FlailAcquired && IsGood(u), u => Flail(u, false)) };
                yield return new WardAction { Key = "chip", Label = "Chip the flail out (10 minutes of work)", Show = () => !WardRuntime.State.FlailAcquired, Run = user => Physical(user, id, "chip out the flail", u => !WardRuntime.State.FlailAcquired, u => Flail(u, true), outOfCombatOnly: true) };
            }
            else if (CredentialRules.IsMarked(id))
            {
                yield return new WardAction { Key = "impression", Label = "Take an impression of the marked palm (1 minute)", Show = () => WardCredentials.CanTakeImpression(id), Run = user => Physical(user, id, "take a palm impression", u => WardCredentials.CanTakeImpression(id), u => WardCredentials.TakeImpression(u, id), outOfCombatOnly: true) };
                yield return new WardAction { Key = "hand", Label = "Recover the marked hand", Show = () => WardCredentials.CanRecoverHand(id), Run = user => Physical(user, id, "recover the marked hand", u => WardCredentials.CanRecoverHand(id), u => WardCredentials.RecoverHand(u, id), outOfCombatOnly: true) };
            }
        }

        /// <summary>What the matching shard shows: the lodge, economically described; the other shards lead nowhere this draft goes.</summary>
        private static void LookThroughPortal()
        {
            SanctumState state = WardRuntime.State;
            string mark = string.IsNullOrEmpty(state.TokenMotif) ? string.Empty : $" ({state.TokenMotif})";
            string text = $"One shard bears the same mark as your refuge token{mark}. Held up to the iron mosaic, it fills the frame with a shimmering view: Sheila Heidmarch's study in Magnimar, lamplit and quiet, exactly as you left it.\n\nThe other shards carry marks you do not know. Through them the frame shows only grey distance: places no one in the Society has seen.\n\nThe passage would carry the whole expedition, once, and close behind it.";
            EventBus.RaiseEvent(delegate(IDialogMessageBoxUIHandler h) { h.HandleOpen(text, DialogMessageBoxBase.BoxType.Message, button => { }); });
        }

        /// <summary>One way home: a clear warning of what is abandoned or left behind, then the guarded journey.</summary>
        private static void ConfirmPortal()
        {
            string refusal = PortalTransit.Refusal();
            if (refusal != null)
            {
                EventBus.RaiseEvent(delegate(IWarningNotificationUIHandler h) { h.HandleWarning(refusal); });
                return;
            }
            var carried = Lodge.DebriefService.Artifacts().ToList();
            string warning = Krune.ConclusionRules.ReturnWarning(WardRuntime.State.Conclusion, carried.Any(Krune.KruneLoot.IsSpear), carried.Any(Krune.KruneLoot.IsRod));
            string text = (warning.Length > 0 ? warning + "\n\n" : string.Empty) + "Step through the portal to the lodge? The journey is one way: the whole party goes, and the sanctum cannot be reached again.";
            EventBus.RaiseEvent(delegate(IDialogMessageBoxUIHandler h)
            {
                h.HandleOpen(text, DialogMessageBoxBase.BoxType.Dialog, button =>
                {
                    if (button == DialogMessageBoxBase.BoxButton.Yes)
                    {
                        PortalTransit.Request();
                    }
                });
            });
        }

        private static UnitEntityData Speaker()
        {
            return Game.Instance.DialogController.Initiator ?? Game.Instance.Player.MainCharacter.Value;
        }

        private static bool HoldsHand()
        {
            CredentialRecord held = WardCredentials.HeldCredential();
            return held != null && held.Kind == CredentialKind.Hand && !WardCredentials.HoldsImpression();
        }

        /// <summary>Defers one body's action at a fixture until the dialogue has closed; it then resolves only in reach.</summary>
        private static void Physical(UnitEntityData user, string id, string label, Func<UnitEntityData, bool> valid, Action<UnitEntityData> body, bool outOfCombatOnly = false)
        {
            WardInteract.Defer(() =>
            {
                UnitEntityData proxy = WardFixtures.Find(id);
                if (user != null && proxy != null)
                {
                    KwrFixtureCommand.Issue(user, KwrFixtureCommand.AtUnit(proxy, WardProfile.ContactReachMeters, label, outOfCombatOnly, valid, body));
                }
            });
        }

        /// <summary>Shared inventory may supply the item, but the presenting body must reach the door and make the contact.</summary>
        private static void PresentAtDoor(UnitEntityData user, CredentialKind kind)
        {
            WardInteract.Defer(() => WardDoor.Order(user, "present the credential", u => Credential(kind) != null && !WardRuntime.State.Door.Destroyed, u => WardDoor.Present(u, Credential(kind))));
        }

        private static CredentialRecord Credential(CredentialKind kind)
        {
            CredentialRecord held = WardCredentials.HeldCredential();
            if (kind == CredentialKind.Impression)
            {
                return held != null && held.Kind == CredentialKind.Impression ? held : null;
            }
            SanctumState state = WardRuntime.State;
            return Game.Instance.Player.Inventory.Any(i => i.Blueprint == WardBlueprints.BrandedHand) ? state.Credentials.Items.FirstOrDefault(r => r.Kind == CredentialKind.Hand) : null;
        }

        private static bool IsDisabled(string id)
        {
            return WardRuntime.State.Focus(id).Disabled;
        }

        public static IEnumerable<UnitEntityData> Party()
        {
            return Game.Instance.Player.Party.Where(u => u.Descriptor.State.CanAct);
        }

        internal static UnitEntityData BestParticipant(StatType stat, Func<UnitEntityData, bool> eligible = null)
        {
            return Participants().Where(u => eligible == null || eligible(u))
                .OrderByDescending(u => u.Stats.GetStat(stat).ModifiedValue).FirstOrDefault();
        }

        public static IEnumerable<UnitEntityData> Participants()
        {
            return Party().Where(u => u.IsInGame && u.View != null && u.IsDirectlyControllable
                && u.Descriptor.State.IsConscious && !u.Descriptor.IsPet && !u.HasControlLossEffects());
        }

        private static bool IsGood(UnitEntityData unit)
        {
            if (unit == null)
            {
                return false;
            }
            Alignment value = unit.Descriptor.Alignment.Value;
            return value == Alignment.LawfulGood || value == Alignment.NeutralGood || value == Alignment.ChaoticGood;
        }

        /// <summary>The body at the plate makes its own attempt; a failure brands that same body and no other.</summary>
        private static void UseRuneSkill(UnitEntityData actor, RuneFocus focus, RuneSkill skill)
        {
            StatType stat = skill == RuneSkill.Trickery ? StatType.SkillThievery : StatType.SkillUseMagicDevice;
            RuleSkillCheck check = Rulebook.Trigger(new RuleSkillCheck(actor, stat, RuneRules.SkillDC));
            RuneOutcome outcome = RuneRules.ApplySkill(focus, WardRuntime.State.Focus(focus.Id), WardRuntime.Now, actor.UniqueId, skill, check.RollResult);
            WardRuntime.ReportRune(focus, outcome, $"{skill} check {check.RollResult} by {actor.CharacterName}");
            WardInteract.Refresh(focus.Id);
        }

        private static void Flail(UnitEntityData actor, bool chipping)
        {
            SanctumState state = WardRuntime.State;
            if (state.FlailAcquired)
            {
                return;
            }
            if (chipping)
            {
                if (!WardRuntime.CanSpendWork(out string refusal))
                {
                    WardRuntime.Say(refusal);
                    return;
                }
                if (!WardRuntime.SpendWork(WardProfile.FlailWorkSeconds, $"{actor.CharacterName} chipping out the flail"))
                {
                    WardRuntime.Say("The work is cut short before the flail comes free.");
                    return;
                }
            }
            state.FlailAcquired = true;
            WardRuntime.ApplyFlailVisual(state);
            var item = ResourcesLibrary.TryGetBlueprint<Kingmaker.Blueprints.Items.BlueprintItem>(FlailBlueprint);
            Game.Instance.Player.Inventory.Add(item);
            WardRuntime.Say($"The bound deva's +3 flail comes free{(chipping ? " after ten minutes of work" : string.Empty)}.");
            Log.Info($"Flail acquired by {(chipping ? "chipping" : "a good-aligned character")}; total flails {Game.Instance.Player.Inventory.Count(i => i.Blueprint == item)}.");
            WardInteract.Refresh(WardText.FlailId);
        }
    }

    /// <summary>Runs the dialogues: real skill rolls, kept knowledge, and the deferred commands that a dialogue cannot issue.</summary>
    internal static class WardInteract
    {
        private static readonly Dictionary<WardSkill, StatType> Stats = new Dictionary<WardSkill, StatType>
        {
            { WardSkill.Arcana, StatType.SkillKnowledgeArcana },
            { WardSkill.Religion, StatType.SkillLoreReligion },
            { WardSkill.Nature, StatType.SkillLoreNature },
            { WardSkill.Perception, StatType.SkillPerception },
        };

        private static Action s_Pending;
        private static string s_Note;

        /// <summary>Whom a click on a fixture belongs to: the nearest selected character, as a native click would choose.</summary>
        public static void Open(string id, UnitEntityData user)
        {
            if (!WardDialogs.Dialogs.TryGetValue(id, out Kingmaker.DialogSystem.Blueprints.BlueprintDialog dialog))
            {
                return;
            }
            s_Pending = null;
            s_Note = null;
            WardCredentials.NoteMarkSeen(id);
            Refresh(id);
            Game.Instance.DialogController.StartDialogWithoutTarget(dialog, null, user);
        }

        public static void Defer(Action action)
        {
            s_Pending = action;
        }

        public static void RunPending()
        {
            Action action = s_Pending;
            s_Pending = null;
            action?.Invoke();
        }

        public static void Perform(string id, WardAction action)
        {
            action.Run(Game.Instance.DialogController.Initiator ?? Game.Instance.Player.MainCharacter.Value);
        }

        public static void Attack(UnitEntityData target)
        {
            foreach (UnitEntityData unit in Game.Instance.UI.SelectionManager.SelectedUnits.ToList())
            {
                unit.Commands.Run(UnitAttack.CreateAttackCommand(unit, target));
            }
        }

        private static Tuple<UnitEntityData, WardSkill> StudyChoice(InspectCheck check)
        {
            SanctumState state = WardRuntime.State;
            if (InspectionRules.BestResult(state.Knowledge, check.Key) >= check.DC && !IsRuneCheck(check))
            {
                return null;
            }
            if (BearerManifest.IsBearer(check.Key) && state.Statue(check.Key).Outcome != StatueOutcome.Stone)
            {
                return null;
            }
            if (check.Key == WardText.MistId && !WardRuntime.HasBeenExposedToMist())
            {
                return null;
            }
            var best = InspectionRules.BestStudy(WardActions.Participants(), check, (u, skill) => u.Stats.GetStat(Stats[skill]).ModifiedValue,
                (u, skill) => InspectionRules.RankAvailable(state.StudyRanks, InspectionRules.Opportunity(check.Key, u.UniqueId, skill),
                    u.Stats.GetStat(Stats[skill]).BaseValue));
            if (best == null) return null;
            var actor = best.Item1;
            var skillChosen = best.Item2;
            return InspectionRules.CanAttempt(state.Knowledge, check.Key, actor.UniqueId, skillChosen.ToString(),
                actor.Stats.GetStat(Stats[skillChosen]).BaseValue) ? best : null;
        }

        internal static bool CanStudy(InspectCheck check) => StudyChoice(check) != null;

        /// <summary>
        /// One native roll by the best eligible character, kept out of the battle log: the cue shows only what was earned, and
        /// a neutral line when nothing new was. A repeat click cannot reroll at the same rank. Diagnostics keep the numbers.
        /// </summary>
        public static void Study(string id, InspectCheck check)
        {
            var choice = StudyChoice(check);
            if (choice == null) return;
            var state = WardRuntime.State;
            var actor = choice.Item1;
            var skill = choice.Item2;
            var stat = Stats[skill];
            int before = Learned(check, InspectionRules.BestResult(state.Knowledge, check.Key));
            RuleSkillCheck roll = Rulebook.Trigger(new RuleSkillCheck(actor, stat, check.DC) { Silent = true });
            foreach (var member in WardActions.Participants())
            foreach (var subject in check.Skills)
            {
                string key = InspectionRules.Opportunity(check.Key, member.UniqueId, subject);
                int rank = member.Stats.GetStat(Stats[subject]).BaseValue;
                if (InspectionRules.RankAvailable(state.StudyRanks, key, rank)) state.StudyRanks[key] = rank;
            }
            InspectionRules.Record(state.Knowledge, check.Key, actor.UniqueId, skill.ToString(), actor.Stats.GetStat(stat).BaseValue, roll.RollResult);
            Log.Info($"Study {check.Key}: {actor.CharacterName} {skill} modifier {actor.Stats.GetStat(stat).ModifiedValue}, d20 {roll.D20}, total {roll.RollResult} against DC {check.DC}.");
            if (Learned(check, InspectionRules.BestResult(state.Knowledge, check.Key)) <= before)
            {
                s_Note = check.Skills.Contains(WardSkill.Perception)
                    ? "You look it over carefully but notice nothing beyond what is already plain to see."
                    : "You study the markings but can make out nothing beyond the details already visible.";
            }
            Refresh(id);
        }

        /// <summary>Rebuilds the cue from saved knowledge, so a reload shows exactly what was already learned.</summary>
        public static void Refresh(string id)
        {
            InspectDef def = WardText.Find(id);
            if (def == null)
            {
                return;
            }
            SanctumState state = WardRuntime.State;
            string body = BearerManifest.IsBearer(id) ? WardText.BodyLine(id, state.Statue(id).Outcome, WardStatues.BodyIsCorpse(id), state.Credentials.HandsRecovered.Contains(id)) : null;
            var text = new System.Text.StringBuilder();
            if (KruneProjection.Foci.Any(f => f.Id == id) && state.Focus(id).Disabled)
            {
                // The current state comes first and is authoritative; seeing that the magic has stopped needs no roll.
                text.Append(RuneFeedback.DisabledStatus(KruneProjection.Focus(id), RuneIdentified(id, state))).Append("\n\n");
            }
            text.Append(body ?? def.Observe);
            AppendStatus(text, id, state);
            foreach (InspectCheck check in def.Checks)
            {
                int best = InspectionRules.BestResult(state.Knowledge, check.Key);
                if (best == int.MinValue)
                {
                    continue;
                }
                if (IsRuneCheck(check))
                {
                    AppendRune(text, id, state, best);
                }
                else if (best >= check.DC)
                {
                    text.Append("\n\n").Append(CheckText(check, state));
                }
            }
            if (!string.IsNullOrEmpty(s_Note))
            {
                text.Append("\n\n").Append(s_Note);
            }
            WardDialogs.TextKey(id, text.ToString());
            LocalizedText.InjectInto(Kingmaker.Localization.LocalizationManager.CurrentPack);
        }

        /// <summary>Whether the party has already identified this plate's rune (its first study tier), from saved knowledge.</summary>
        public static bool RuneIdentified(string id, SanctumState state)
        {
            InspectDef def = WardText.Find(id);
            return def != null && def.Checks.Where(IsRuneCheck).Any(c => InspectionRules.RuneTier(InspectionRules.BestResult(state.Knowledge, c.Key)) >= 1);
        }

        private static bool IsRuneCheck(InspectCheck check)
        {
            return check.Result == "rune";
        }

        private static string CheckText(InspectCheck check, SanctumState state)
        {
            switch (check.Result)
            {
                case "ritual":
                    return WardRitual.AssessmentText(state);
                case "motif":
                    string motif = string.IsNullOrEmpty(state.TokenMotif) ? "your own token's" : state.TokenMotif;
                    return $"Each shard answers to the runes of one refuge token; the one bearing {motif} motif would lead back the way it came. No shard here can be used yet.";
                case "inscription":
                    string clue = "One word has been recut, crudely and deeper than its neighbours. It once read: \"Only those who bear my sign shall pass.\"";
                    return state.Credentials.MarksSeen.Count > 0
                        ? clue + " The star cut into this door is the same seven-pointed sign branded into a petrified palm in the outer chamber."
                        : clue;
                default:
                    return check.Result;
            }
        }

        /// <summary>How much a best result reveals: rune tiers, or simply whether an ordinary study's text is earned.</summary>
        private static int Learned(InspectCheck check, int best)
        {
            if (best == int.MinValue)
            {
                return 0;
            }
            return IsRuneCheck(check) ? InspectionRules.RuneTier(best) : best >= check.DC ? 1 : 0;
        }

        private static void AppendStatus(System.Text.StringBuilder text, string id, SanctumState state)
        {
            if (KruneProjection.Foci.Any(f => f.Id == id) && state.Focus(id).Disabled)
            {
                text.Append("\n\nThe plate is cold and cracked; the blue-green glyph above it has gone dark.");
            }
            if (id == WardText.DoorId)
            {
                text.Append(state.Door.Destroyed ? "\n\nNothing remains of it but rubble in the doorway." : state.Door.Latched ? string.Empty : "\n\nIt is no longer held fast.");
            }
            bool aftermath = state.Conclusion.AftermathAt >= 0d;
            if (id == WardText.GemId && (state.Gem.Destroyed || aftermath))
            {
                text.Append(state.Gem.Destroyed ? "\n\nThe emerald is shattered." : "\n\nThe emptied emerald has cracked through and gone dark.");
            }
            if (id == WardText.SarcophagusId && aftermath)
            {
                text.Append("\n\nWith its master's death it crumbled: only rubble and slack chains remain.");
            }
            else if (id == WardText.SarcophagusId && state.Krune.Stage >= Krune.AwakeningStage.Emerged)
            {
                text.Append("\n\nIts occupant is gone: the runelord has stepped out of it.");
            }
            if (id == WardText.FlailId && state.FlailAcquired)
            {
                text.Append("\n\nThe flail is gone.");
            }
        }

        private static void AppendRune(System.Text.StringBuilder text, string id, SanctumState state, int best)
        {
            int tier = InspectionRules.RuneTier(best);
            if (tier == 0)
            {
                return;
            }
            RuneFocus focus = KruneProjection.Focus(id);
            // The deeper reading replaces the identification: it opens with the rune's identity and state itself.
            text.Append("\n\n").Append(tier == 2 ? WardText.RuneUnderstanding(focus.Kind, state.Focus(id).Disabled) : WardText.RuneIdentity(focus, state.Focus(id)));
        }
    }
}
