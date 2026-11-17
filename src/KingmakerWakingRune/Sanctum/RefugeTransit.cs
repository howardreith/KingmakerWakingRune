using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UI.SettingsUI;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Diagnostics;
using KingmakerWakingRune.Lodge;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// The one transfer service behind both ways of using the token, and the game side of <see cref="RefugeTransferFlow"/>.
    /// Ticks on unscaled time so a paused game cannot stall it.
    /// </summary>
    internal sealed class RefugeTransit : MonoBehaviour, ITransferHost
    {
        public const string PreDepartureSaveName = "Before the Sanctum of Sloth";
        private const float EscapePressSeconds = 0.25f;
        private const float PendingNoticeSeconds = 2f;
        private const string SaveDisallowed = "The token's magic falters: the game cannot save here, and the Sanctum has no way back. Nothing was spent.";
        private const string SaveBusy = "The token's magic falters: the game is still saving. Nothing was spent; try again in a moment.";
        private const string IronmanSave = "The token's magic falters: this game keeps a single save, so no separate one can be made before leaving. Nothing was spent.";

        private static RefugeTransit s_Instance;

        private RefugeTransferFlow m_Flow;
        private DeparturePreparation m_Preparation;
        private CheckpointOperation m_Operation;
        private SaveInfo m_Checkpoint;
        private string m_Party;
        private bool m_PendingNoticed;
        private float m_NextEscape;

        public static bool InProgress => s_Instance != null && (s_Instance.m_Flow.InProgress || s_Instance.m_Preparation.InProgress);

        /// <summary>The ability's single entry point.</summary>
        public static void Request()
        {
            Game game = Game.Instance;
            var flags = game.Player.UnlockableFlags;
            TravelRejection rejection = SanctumPolicy.CheckTravel(
                game.CurrentlyLoadedArea == ScenarioBlueprints.Area,
                flags.IsUnlocked(ScenarioBlueprints.BriefingCompleteFlag),
                game.Player.IsInCombat,
                game.IsModeActive(GameModeType.Dialog),
                game.UI.CharacterBuildController != null && game.UI.CharacterBuildController.IsShow,
                flags.IsUnlocked(ScenarioBlueprints.SanctumEnteredFlag),
                InProgress,
                ScenarioBlueprints.SanctumEnter != null);
            if (rejection == TravelRejection.None && CheckpointOperation.AnyPending)
            {
                // A save given up earlier is still in the native queue: no second one starts beside it.
                Log.Info("Refuge Token use rejected: an earlier checkpoint is still being written.");
                EventBus.RaiseEvent(delegate(IWarningNotificationUIHandler h) { h.HandleWarning(SaveBusy); });
                return;
            }
            if (rejection != TravelRejection.None)
            {
                Log.Info($"Refuge Token use rejected: {rejection}.");
                string text = SanctumPolicy.RejectionText(rejection);
                if (text != null)
                {
                    EventBus.RaiseEvent(delegate(IWarningNotificationUIHandler h) { h.HandleWarning(text); });
                }
                return;
            }
            if (s_Instance == null)
            {
                var host = new GameObject("KWR_RefugeTransit");
                DontDestroyOnLoad(host);
                s_Instance = host.AddComponent<RefugeTransit>();
                s_Instance.m_Flow = new RefugeTransferFlow(s_Instance);
                s_Instance.m_Preparation = new DeparturePreparation(s_Instance.Depart, s_Instance.Warn);
            }
            Log.Info("Refuge Token accepted; preparing the party to depart.");
            s_Instance.m_Preparation.Begin();
        }

        /// <summary>The prepared party leaves: checkpoint, fade and transfer, exactly as before preparation existed.</summary>
        private void Depart()
        {
            m_Operation = null;
            m_Checkpoint = null;
            m_Party = PartyIds();
            Log.Info("Party prepared; beginning transfer.");
            m_Flow.Begin();
        }

        private void Update()
        {
            m_Preparation?.Tick(Now);
            m_Flow?.Tick();
        }

        /// <summary>Called when the sanctum has finished loading. Commits exactly one successful transfer, into a ready area.</summary>
        public static void OnSanctumLoaded()
        {
            if (!InProgress)
            {
                return;
            }
            bool ready = SanctumScope.CheckArrival(out string problem);
            s_Instance.m_Flow.OnArrived(ready, Game.Instance.Player.UnlockableFlags.IsUnlocked(ScenarioBlueprints.SanctumEnteredFlag), problem);
        }

        public static void OnSanctumLoadFailed(string problem)
        {
            if (InProgress)
            {
                s_Instance.m_Flow.OnArrived(false, false, problem);
            }
        }

        /// <summary>
        /// A save load replaces the world; any in-flight transfer ends with it. The departure rest's record is part of the save
        /// and travels with it, so it is left alone here.
        /// </summary>
        public static void Reset(string reason)
        {
            if (s_Instance != null)
            {
                s_Instance.m_Preparation.Cancel(reason);
                s_Instance.m_Flow.Cancel(reason);
            }
        }

        public float Now => Time.realtimeSinceStartup;

        public float FadeSeconds => FadeCanvas.Instance.FadeTimer;

        public bool LoadIdle => !LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.IsLoadingScreenActive;

        public bool WindowsClosed()
        {
            if (!Game.Instance.IsModeActive(GameModeType.FullScreenUi))
            {
                return true;
            }
            if (Now >= m_NextEscape)
            {
                m_NextEscape = Now + EscapePressSeconds;
                Game.Instance.UI.EscManager.OnEscPressed();
            }
            return false;
        }

        public string StartCheckpoint()
        {
            m_Checkpoint = null;
            m_PendingNoticed = false;
            m_Operation = CheckpointOperation.Start(PreDepartureSaveName, out CheckpointRefusal refusal);
            switch (refusal)
            {
                case CheckpointRefusal.Disallowed:
                    return SaveDisallowed;
                case CheckpointRefusal.SingleSave:
                    return IronmanSave;
                case CheckpointRefusal.Busy:
                    return SaveBusy;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Success is this departure's own checkpoint, committed and read back (see <see cref="CheckpointOperation"/>), with the
        /// party that was readied still the party; a save another mod holds back before writing is simply still pending.
        /// </summary>
        public CheckpointProgress PollCheckpoint(out string failure)
        {
            failure = null;
            switch (m_Operation.Poll())
            {
                case CheckpointProgress.Failed:
                    failure = $"The token's magic falters: {CheckpointRules.Describe(m_Operation.Failure, "before leaving")}. Nothing was spent; try again.";
                    return CheckpointProgress.Failed;
                case CheckpointProgress.Running:
                    if (!m_PendingNoticed && m_Operation.Age >= PendingNoticeSeconds)
                    {
                        m_PendingNoticed = true;
                        Warn("The token waits for the save before leaving to finish.");
                    }
                    return CheckpointProgress.Running;
            }
            string party = PartyIds();
            if (party != m_Party)
            {
                Log.Error($"The party changed while the pre-departure save was written (readied [{m_Party}], now [{party}]); not departing.");
                failure = "The token's magic falters: the party changed while the save before leaving was written. Nothing was spent; use the token again.";
                return CheckpointProgress.Failed;
            }
            m_Checkpoint = m_Operation.Checkpoint;
            Log.Info($"Pre-departure save verified: {m_Checkpoint.Name} ({Path.GetFileName(m_Checkpoint.FolderName)}, {new FileInfo(m_Checkpoint.FolderName).Length} bytes).");
            return CheckpointProgress.Succeeded;
        }

        public string AbandonCheckpoint(string reason)
        {
            if (m_Operation == null)
            {
                return null;
            }
            Log.Error($"Pre-departure save given up: {m_Operation.Status}; {reason}.");
            m_Operation.Abandon(reason);
            return $"The token's magic falters: {CheckpointRules.Describe(CheckpointFailure.TimedOut, "before leaving")}. Nothing was spent; try again in a moment.";
        }

        private static string PartyIds()
        {
            return string.Join(",", Game.Instance.Player.Party.Select(u => u.UniqueId).ToArray());
        }

        public void FadeOut()
        {
            FadeCanvas.Instance.ShowLoadingScreen();
        }

        public void LoadDestination()
        {
            // The briefing is complete by validation; the driver must not act during the unload.
            BriefingService.Disarm();
            Game.Instance.LoadArea(ScenarioBlueprints.SanctumEnter, AutoSaveMode.None);
        }

        public void CommitArrival()
        {
            RefugeToken.Consume();
            DeparturePreparation.Forget("arrival committed");
            Game.Instance.Player.UnlockableFlags.Unlock(ScenarioBlueprints.SanctumEnteredFlag);
            Log.Info($"Arrival committed: party [{string.Join(", ", Game.Instance.Player.Party.Select(u => u.CharacterName).ToArray())}]; tokens left {RefugeToken.CountOwned()}.");
        }

        public void FadeIn()
        {
            FadeCanvas.Instance?.HideLoadingScreen();
        }

        public void Warn(string text)
        {
            EventBus.RaiseEvent(delegate(IWarningNotificationUIHandler h) { h.HandleWarning(text); });
        }

        public void ReportStranded(string text)
        {
            SaveInfo checkpoint = m_Checkpoint;
            Log.Error($"Refuge transfer stranded: {text} The token was not spent; recovery save '{checkpoint?.Name}'.");
            string message = checkpoint == null
                ? $"{text} The Refuge Token was not spent."
                : $"{text} The Refuge Token was not spent. Load the save \"{checkpoint.Name}\" to return to the lodge?";
            EventBus.RaiseEvent(delegate(IDialogMessageBoxUIHandler h)
            {
                h.HandleOpen(message, checkpoint == null ? DialogMessageBoxBase.BoxType.Message : DialogMessageBoxBase.BoxType.Dialog, button =>
                {
                    if (checkpoint != null && button == DialogMessageBoxBase.BoxButton.Yes)
                    {
                        Game.Instance.LoadGame(checkpoint);
                    }
                });
            });
        }

        public void Info(string text)
        {
            Log.Info(text);
        }

        public void Error(string text, Exception exception)
        {
            Log.Error(text, exception);
        }
    }
}
