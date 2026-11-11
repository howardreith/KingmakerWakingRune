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
        private const string SaveDisallowed = "The token's magic falters: the game cannot save here, and the Sanctum has no way back. Nothing was spent.";
        private const string SaveBusy = "The token's magic falters: the game is still saving. Nothing was spent; try again in a moment.";
        private const string SaveFailed = "The token's magic falters: the save before leaving failed. Nothing was spent; check the disk and try again.";
        private const string IronmanSave = "The token's magic falters: this game keeps a single save, so no separate one can be made before leaving. Nothing was spent.";

        private static RefugeTransit s_Instance;

        private RefugeTransferFlow m_Flow;
        private DeparturePreparation m_Preparation;
        private SaveInfo m_Requested;
        private SaveInfo m_Checkpoint;
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
            m_Requested = null;
            m_Checkpoint = null;
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
            SaveManager saves = Game.Instance.SaveManager;
            if (FaultInjection.Take("save-refused"))
            {
                Log.Info("INJECTED FAULT save-refused: reporting saving as disallowed.");
                return SaveDisallowed;
            }
            if (SettingsRoot.Instance.OnlyOneSave.CurrentValue)
            {
                return IronmanSave;
            }
            if (!saves.IsSaveAllowed())
            {
                return SaveDisallowed;
            }
            if (LoadingProcess.Instance.IsLoadingInProcess || saves.CommitInProgress)
            {
                return SaveBusy;
            }
            m_Requested = saves.CreateNewSave(PreDepartureSaveName);
            m_Checkpoint = null;
            if (FaultInjection.Take("save-failed"))
            {
                Log.Info("INJECTED FAULT save-failed: no save is written and the poll will find none.");
                return null;
            }
            if (FaultInjection.Take("save-timeout"))
            {
                Log.Info("INJECTED FAULT save-timeout: the poll will keep reporting a running save.");
                m_Requested = null;
                return null;
            }
            LoadingProcess.Instance.StartLoadingProcess(saves.SaveRoutine(m_Requested), null, LoadingProcessTag.Save);
            return null;
        }

        /// <summary>
        /// The routine replaces the requested record with a new one, can end while the file is still being committed, and
        /// removes the record if the write fails. So success is only that record, fully committed, with a readable file behind it.
        /// </summary>
        public CheckpointProgress PollCheckpoint(out string failure)
        {
            failure = null;
            if (m_Requested == null)
            {
                return CheckpointProgress.Running;
            }
            SaveManager saves = Game.Instance.SaveManager;
            if (LoadingProcess.Instance.IsLoadingInProcess || saves.CommitInProgress)
            {
                return CheckpointProgress.Running;
            }
            SaveInfo written = saves.FirstOrDefault(s => s != m_Requested && s.Name == m_Requested.Name && s.Type == m_Requested.Type && s.GameId == Game.Instance.Player.GameId);
            if (written == null)
            {
                Log.Error($"Pre-departure save '{m_Requested.Name}' has no record after its routine ended.");
                failure = SaveFailed;
                return CheckpointProgress.Failed;
            }
            if (written.OperationState != SaveInfo.StateType.None)
            {
                return CheckpointProgress.Running;
            }
            if (!IsReadable(written))
            {
                Log.Error($"Pre-departure save '{written.Name}' could not be read back from '{written.FolderName}'.");
                failure = SaveFailed;
                return CheckpointProgress.Failed;
            }
            m_Checkpoint = written;
            Log.Info($"Pre-departure save verified: {written.Name} ({Path.GetFileName(written.FolderName)}, {new FileInfo(written.FolderName).Length} bytes).");
            return CheckpointProgress.Succeeded;
        }

        private static bool IsReadable(SaveInfo save)
        {
            try
            {
                return save.IsActuallySaved && File.Exists(save.FolderName) && new FileInfo(save.FolderName).Length > 0 && !string.IsNullOrEmpty(save.Saver?.ReadHeader());
            }
            catch (Exception ex)
            {
                Log.Error("Pre-departure save could not be read back.", ex);
                return false;
            }
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
