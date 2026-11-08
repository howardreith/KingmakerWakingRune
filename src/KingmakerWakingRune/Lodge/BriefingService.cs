using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.Visual.Animation.Kingmaker;
using KingmakerWakingRune.Blueprints;
using UnityEngine;

namespace KingmakerWakingRune.Lodge
{
    /// <summary>
    /// Opens the scripted briefing as a native dialogue once the lodge is ready, and records its
    /// completion in the save. An interrupted briefing simply reopens from the start on the next load.
    /// Follow-up questions go through the Sheila/Ambrus click interaction and never touch this state.
    /// </summary>
    internal static class BriefingService
    {
        private const string BriefingFlagLog = "Briefing complete recorded.";
        private static bool s_Armed;

        public static bool IsArmed => s_Armed;

        public static void ArmIfPending()
        {
            s_Armed = !Game.Instance.Player.UnlockableFlags.IsUnlocked(ScenarioBlueprints.BriefingCompleteFlag);
        }

        public static void Disarm()
        {
            s_Armed = false;
        }

        /// <summary>Called by the lodge driver every frame while armed.</summary>
        public static void TryOpen()
        {
            if (!s_Armed)
            {
                return;
            }
            UnitEntityData ambrus = LodgeSession.FindCastMember("ambrus");
            bool cueOwnersReady = ambrus?.View != null
                                  && LodgeSession.FindCastMember("masked")?.View != null
                                  && LodgeSession.FindSheila()?.View != null;
            if (!WakingPolicies.ShouldOpenBriefing(true, true, false, cueOwnersReady, IsBusy()))
            {
                return;
            }
            Game game = Game.Instance;
            s_Armed = false;
            Log.Info("Starting the lodge briefing.");
            game.DialogController.StartDialogWithUnit(LodgeDialogs.Briefing, ambrus, game.Player.MainCharacter.Value);
        }

        private static bool IsBusy()
        {
            Game game = Game.Instance;
            if (game.CurrentMode != GameModeType.Default || game.Player.Dialog.Scheduled != null || game.DialogController.Dialog != null)
            {
                return true;
            }
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.IsLoadingScreenActive)
            {
                return true;
            }
            var build = game.UI.CharacterBuildController;
            return build != null && build.IsShow;
        }

        public static void MarkComplete()
        {
            var flags = Game.Instance.Player.UnlockableFlags;
            if (!flags.IsUnlocked(ScenarioBlueprints.BriefingCompleteFlag))
            {
                flags.Unlock(ScenarioBlueprints.BriefingCompleteFlag);
                Log.Info(BriefingFlagLog);
            }
        }

        /// <summary>Stage direction: move focus to the masked representative and nod, using a native gesture.</summary>
        public static void FocusMaskedFigure()
        {
            UnitEntityData masked = LodgeSession.FindCastMember("masked");
            if (masked == null)
            {
                return;
            }
            Game.Instance.UI.GetCameraRig()?.ScrollDialogTo(masked.Position);
            masked.View?.AnimationManager?.Execute(UnitAnimationType.DialogBow);
        }
    }
}
