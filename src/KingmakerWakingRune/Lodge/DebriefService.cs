using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Sanctum.Krune;
using KingmakerWakingRune.Sanctum.Wards;

namespace KingmakerWakingRune.Lodge
{
    /// <summary>
    /// The lodge's return mode (WR-FINAL). A party that came home through the sanctum's portal is met once by the debrief that
    /// fits what happened; the opening briefing, grants, stock, token and preparation are never replayed (their own flags
    /// stand). The handover takes exactly the two quest artifacts, wherever the party carries them, once; the outcome is
    /// recorded once, in the saved conclusion and in a native flag. After it the lodge stays open for inspection and saving.
    /// </summary>
    internal static class DebriefService
    {
        public static ConclusionState Conclusion => WardStore.Existing?.Conclusion;

        /// <summary>The masked figure took the artifacts and teleported away: once the handover is recorded it is gone, on every load.</summary>
        public static void ApplyDeparture()
        {
            ConclusionState c = Conclusion;
            UnitEntityData masked = c != null && c.HandoverAt >= 0d ? LodgeSession.FindCastMember("masked") : null;
            if (masked != null && masked.IsInGame)
            {
                masked.IsInGame = false;
                Log.Info("The masked figure has gone with the artifacts.");
            }
        }

        public static void TryOpen()
        {
            ApplyDeparture();
            ConclusionState c = Conclusion;
            if (c == null || c.ReturnedAt < 0d || c.DebriefAt >= 0d || IsBusy())
            {
                return;
            }
            UnitEntityData ambrus = LodgeSession.FindCastMember("ambrus");
            if (ambrus?.View == null || LodgeSession.FindCastMember("masked")?.View == null || LodgeSession.FindSheila()?.View == null)
            {
                return;
            }
            ScenarioOutcome outcome = ConclusionRules.Decide(c);
            Kingmaker.DialogSystem.Blueprints.BlueprintDialog dialog =
                outcome == ScenarioOutcome.Success ? LodgeDialogs.DebriefSuccess :
                outcome == ScenarioOutcome.Withdrawn ? LodgeDialogs.DebriefWithdrawn : LodgeDialogs.DebriefPartial;
            Log.Info($"Starting the lodge debrief ({outcome}).");
            Game.Instance.DialogController.StartDialogWithUnit(dialog, ambrus, Game.Instance.Player.MainCharacter.Value);
        }

        /// <summary>Exactly the artifacts, from the shared inventory and every character's equipment; recorded before removal.</summary>
        public static void HandOver()
        {
            ConclusionState c = Conclusion;
            if (c == null || !ConclusionRules.MayHandOver(c))
            {
                return;
            }
            c.HandoverAt = Game.Instance.TimeController.GameTime.TotalSeconds;
            var taken = new List<string>();
            foreach (ItemEntity item in Artifacts().ToList())
            {
                if (item.HoldingSlot != null)
                {
                    item.HoldingSlot.RemoveItem();
                }
                item.Collection?.Remove(item);
                taken.Add(item.Name);
            }
            Log.Info($"Debrief handover recorded: [{string.Join(", ", taken.ToArray())}].");
            ApplyDeparture();
        }

        /// <summary>Every ItemEntity of the two artifact blueprints the party holds, equipped or carried. Nothing else matches.</summary>
        public static IEnumerable<ItemEntity> Artifacts()
        {
            var seen = new HashSet<ItemEntity>();
            foreach (ItemEntity item in Game.Instance.Player.Inventory.Items.Where(KruneLoot.IsArtifact))
            {
                if (seen.Add(item))
                {
                    yield return item;
                }
            }
            foreach (UnitEntityData unit in Game.Instance.Player.AllCharacters)
            {
                foreach (ItemSlot slot in unit.Body.AllSlots.Where(s => s.HasItem && KruneLoot.IsArtifact(s.Item)))
                {
                    if (seen.Add(slot.Item))
                    {
                        yield return slot.Item;
                    }
                }
            }
        }

        public static void Finish()
        {
            ConclusionState c = Conclusion;
            if (c == null || c.DebriefAt >= 0d)
            {
                return;
            }
            c.DebriefAt = Game.Instance.TimeController.GameTime.TotalSeconds;
            c.Outcome = ConclusionRules.Decide(c);
            Game.Instance.Player.UnlockableFlags.SetFlagValue(ScenarioBlueprints.OutcomeFlag, (int)c.Outcome);
            Log.Info($"Scenario outcome recorded: {c.Outcome}.");
            string text;
            switch (c.Outcome)
            {
                case ScenarioOutcome.Success:
                    text = "The Waking Rune: success.\n\nRunelord Krune is slain and his two artifacts are in the Society's hands.\n\nThe adventure is complete. You may stay in the lodge, save, or return to the main menu.";
                    break;
                case ScenarioOutcome.KruneSlainArtifactsMissing:
                    text = "The Waking Rune: Krune slain.\n\nThe runelord is dead, but not all of his artifacts came home: the Society's task is only partly done.\n\nThe adventure is over. You may stay in the lodge, save, or return to the main menu.";
                    break;
                case ScenarioOutcome.Withdrawn:
                    text = "The Waking Rune: withdrawn.\n\nThe expedition came home before Krune was slain. The Decemvirate's contingencies will slow his return; another team must finish what you began.\n\nThe adventure is over. You may stay in the lodge, save, or return to the main menu.";
                    break;
                default:
                    text = "The Waking Rune: a test save.\n\nThis expedition comes from a diagnostic save of an earlier build: it is not a completed adventure.";
                    break;
            }
            EventBus.RaiseEvent(delegate(IDialogMessageBoxUIHandler h) { h.HandleOpen(text, DialogMessageBoxBase.BoxType.Message, button => { }); });
        }

        private static bool IsBusy()
        {
            Game game = Game.Instance;
            return game.CurrentMode != GameModeType.Default || game.Player.Dialog.Scheduled != null || game.DialogController.Dialog != null
                || LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.IsLoadingScreenActive;
        }
    }
}
