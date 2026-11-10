using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
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

        public static IEnumerable<WardAction> For(string id)
        {
            if (KruneProjection.Foci.Any(f => f.Id == id))
            {
                RuneFocus focus = KruneProjection.Focus(id);
                yield return new WardAction { Key = "trickery", Label = "[Trickery] Tamper with the plate", Show = () => !IsDisabled(id), Run = user => UseRuneSkill(focus, RuneSkill.Trickery) };
                yield return new WardAction { Key = "umd", Label = "[Use Magic Device] Try to command the rune", Show = () => !IsDisabled(id), Run = user => UseRuneSkill(focus, RuneSkill.UseMagicDevice) };
                yield return new WardAction { Key = "attack", Label = "Attack the plate", Show = () => !IsDisabled(id), Run = user => WardInteract.Defer(() => WardInteract.Attack(WardFixtures.Find(id))) };
            }
            else if (id == WardText.DoorId)
            {
                yield return new WardAction { Key = "press", Label = "Press the handprint", Show = () => !WardRuntime.State.Door.Destroyed, Run = WardDoor.Press };
                yield return new WardAction { Key = "force", Label = "[Strength] Force the door", Show = () => WardRuntime.State.Door.Latched && !WardRuntime.State.Door.Destroyed, Run = WardDoor.Force };
                yield return new WardAction { Key = "disarm", Label = "[Trickery] Disarm the feeder", Show = WardDoor.CanDisarm, Run = WardDoor.Disarm };
                yield return new WardAction { Key = "attack", Label = "Attack the door", Show = () => !WardRuntime.State.Door.Destroyed, Run = user => WardInteract.Defer(() => WardInteract.Attack(WardFixtures.Door)) };
            }
            else if (id == WardText.GemId)
            {
                yield return new WardAction { Key = "break", Label = "[Strength] Force the emerald from the altar", Show = () => !WardRuntime.State.Gem.Destroyed, Run = WardGem.Force };
                yield return new WardAction { Key = "attack", Label = "Attack the emerald", Show = () => !WardRuntime.State.Gem.Destroyed, Run = user => WardInteract.Defer(() => WardInteract.Attack(WardFixtures.Gem)) };
            }
            else if (id == WardText.ToolsId)
            {
                yield return new WardAction { Key = "interrupt", Label = "Interrupt the revival ritual (this will advance it)", StaysOpen = true, Show = () => !WardRuntime.State.Ritual.ConfrontationStarted, Run = WardRitual.CommitConfrontation };
            }
            else if (id == WardText.FlailId)
            {
                yield return new WardAction { Key = "draw", Label = "Draw the flail free (good-aligned character)", StaysOpen = true, Show = () => !WardRuntime.State.FlailAcquired && Party().Any(IsGood), Run = user => Flail(false) };
                yield return new WardAction { Key = "chip", Label = "Chip the flail out (10 minutes of work)", StaysOpen = true, Show = () => !WardRuntime.State.FlailAcquired && WardRuntime.CanSpendWork(), Run = user => Flail(true) };
            }
        }

        private static bool IsDisabled(string id)
        {
            return WardRuntime.State.Focus(id).Disabled;
        }

        public static IEnumerable<UnitEntityData> Party()
        {
            return Game.Instance.Player.Party.Where(u => u.Descriptor.State.CanAct);
        }

        private static bool IsGood(UnitEntityData unit)
        {
            Alignment value = unit.Descriptor.Alignment.Value;
            return value == Alignment.LawfulGood || value == Alignment.NeutralGood || value == Alignment.ChaoticGood;
        }

        private static void UseRuneSkill(RuneFocus focus, RuneSkill skill)
        {
            StatType stat = skill == RuneSkill.Trickery ? StatType.SkillThievery : StatType.SkillUseMagicDevice;
            UnitEntityData actor = Party().OrderByDescending(u => u.Stats.GetStat(stat).ModifiedValue).FirstOrDefault();
            if (actor == null)
            {
                return;
            }
            RuleSkillCheck check = Rulebook.Trigger(new RuleSkillCheck(actor, stat, RuneRules.SkillDC));
            RuneOutcome outcome = RuneRules.ApplySkill(focus, WardRuntime.State.Focus(focus.Id), WardRuntime.Now, actor.UniqueId, skill, check.RollResult);
            WardRuntime.ReportRune(focus, outcome, $"{skill} check {check.RollResult} by {actor.CharacterName}");
            WardInteract.Refresh(focus.Id);
        }

        private static void Flail(bool chipping)
        {
            SanctumState state = WardRuntime.State;
            if (state.FlailAcquired)
            {
                return;
            }
            if (chipping)
            {
                WardRuntime.SpendWork(WardProfile.FlailWorkSeconds, "chipping out the flail");
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

        /// <summary>Whom a click on a fixture belongs to: the nearest selected character, as a native click would choose.</summary>
        public static void Open(string id, UnitEntityData user)
        {
            if (!WardDialogs.Dialogs.TryGetValue(id, out Kingmaker.DialogSystem.Blueprints.BlueprintDialog dialog))
            {
                return;
            }
            s_Pending = null;
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

        public static bool CanStudy(InspectCheck check, WardSkill skill)
        {
            SanctumState state = WardRuntime.State;
            if (InspectionRules.BestResult(state.Knowledge, check.Key) >= check.DC && !IsRuneCheck(check))
            {
                return false;
            }
            StatType stat = Stats[skill];
            if (check.Key == WardText.MistId && !WardRuntime.HasBeenExposedToMist())
            {
                return false;
            }
            return WardActions.Party().Any(u => InspectionRules.CanAttempt(state.Knowledge, check.Key, u.UniqueId, skill.ToString(), u.Stats.GetStat(stat).BaseValue));
        }

        /// <summary>One native roll by the best eligible character. A repeat click cannot reroll at the same rank.</summary>
        public static void Study(string id, InspectCheck check, WardSkill skill)
        {
            SanctumState state = WardRuntime.State;
            StatType stat = Stats[skill];
            UnitEntityData actor = WardActions.Party()
                .Where(u => InspectionRules.CanAttempt(state.Knowledge, check.Key, u.UniqueId, skill.ToString(), u.Stats.GetStat(stat).BaseValue))
                .OrderByDescending(u => u.Stats.GetStat(stat).ModifiedValue)
                .FirstOrDefault();
            if (actor == null)
            {
                return;
            }
            RuleSkillCheck roll = Rulebook.Trigger(new RuleSkillCheck(actor, stat, check.DC));
            InspectionRules.Record(state.Knowledge, check.Key, actor.UniqueId, skill.ToString(), actor.Stats.GetStat(stat).BaseValue, roll.RollResult);
            Log.Info($"Study {check.Key}: {actor.CharacterName} {skill} {roll.RollResult} against DC {check.DC}.");
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
            var text = new System.Text.StringBuilder(def.Observe);
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
            WardDialogs.TextKey(id, text.ToString());
            LocalizedText.InjectInto(Kingmaker.Localization.LocalizationManager.CurrentPack);
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
                default:
                    return check.Result;
            }
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
            if (id == WardText.GemId && state.Gem.Destroyed)
            {
                text.Append("\n\nThe emerald is shattered.");
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
            text.Append("\n\n").Append(WardText.RuneIdentity(focus, state.Focus(id)));
            if (tier == 2)
            {
                text.Append("\n\n").Append(WardText.RuneRoutes(focus.Kind));
            }
        }
    }
}
