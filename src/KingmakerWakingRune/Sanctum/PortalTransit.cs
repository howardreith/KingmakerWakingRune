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
using KingmakerWakingRune.Lodge;
using KingmakerWakingRune.Sanctum.Krune;
using KingmakerWakingRune.Sanctum.Wards;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// The A4 portal home (page 10–11 adapted): the shard matching the expedition's own refuge token opens a one-way passage
    /// for the whole party to the lodge. The same guarded journey as the token (RefugeTransferFlow): a verified save first, a
    /// native area load, and a return recorded only when the lodge reports itself ready. A failed save or load leaves the
    /// expedition where it was, with nothing recorded and the save named. One way: once home, nothing reopens the sanctum.
    /// </summary>
    internal sealed class PortalTransit : MonoBehaviour, ITransferHost
    {
        public const string PreReturnSaveName = "Before the portal home";
        private const string SaveDisallowed = "The portal will not open while the game cannot save here. Nothing has changed.";
        private const string SaveBusy = "The portal will not open while the game is still saving. Try again in a moment.";
        private const string SaveFailed = "The save before stepping through failed. Nothing has changed; check the disk and try again.";
        private const string IronmanSave = "This game keeps a single save, so no separate one can be made before the portal. Nothing has changed.";

        private static PortalTransit s_Instance;
        private RefugeTransferFlow m_Flow;
        private SaveInfo m_Requested;
        private SaveInfo m_Checkpoint;
        private float m_NextEscape;

        public static bool InProgress => s_Instance != null && s_Instance.m_Flow.InProgress;

        /// <summary>Why the portal may not be used now, or null when it may.</summary>
        public static string Refusal()
        {
            Game game = Game.Instance;
            SanctumState state = WardRuntime.State;
            if (state == null || game.CurrentlyLoadedArea != ScenarioBlueprints.Sanctum)
            {
                return "The portal is not here.";
            }
            if (state.Conclusion.ReturnedAt >= 0d)
            {
                return "The expedition has already gone home.";
            }
            if (game.Player.IsInCombat)
            {
                return "Not with foes still fighting: the portal needs a calm moment to open.";
            }
            if (InProgress || RefugeTransit.InProgress)
            {
                return "The passage is already opening.";
            }
            return null;
        }

        public static void Request()
        {
            string refusal = Refusal();
            if (refusal != null)
            {
                Log.Info($"Portal home refused: {refusal}");
                EventBus.RaiseEvent(delegate(IWarningNotificationUIHandler h) { h.HandleWarning(refusal); });
                return;
            }
            if (s_Instance == null)
            {
                var host = new GameObject("KWR_PortalTransit");
                DontDestroyOnLoad(host);
                s_Instance = host.AddComponent<PortalTransit>();
                s_Instance.m_Flow = new RefugeTransferFlow(s_Instance);
            }
            s_Instance.m_Requested = null;
            s_Instance.m_Checkpoint = null;
            Log.Info("Portal home: the expedition steps through.");
            s_Instance.m_Flow.Begin();
        }

        private void Update()
        {
            m_Flow?.Tick();
        }

        /// <summary>The lodge finished loading: commit exactly one return, into a ready lodge.</summary>
        public static void OnLodgeLoaded()
        {
            if (!InProgress)
            {
                return;
            }
            bool ready = Game.Instance.CurrentlyLoadedArea == ScenarioBlueprints.Area && Game.Instance.Player.MainCharacter.Value != null;
            ConclusionState c = WardStore.Existing?.Conclusion;
            s_Instance.m_Flow.OnArrived(ready, c != null && c.ReturnedAt >= 0d, ready ? null : "the lodge did not load");
        }

        public static void Reset(string reason)
        {
            s_Instance?.m_Flow.Cancel(reason);
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
                m_NextEscape = Now + 0.25f;
                Game.Instance.UI.EscManager.OnEscPressed();
            }
            return false;
        }

        public string StartCheckpoint()
        {
            SaveManager saves = Game.Instance.SaveManager;
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
            m_Requested = saves.CreateNewSave(PreReturnSaveName);
            m_Checkpoint = null;
            LoadingProcess.Instance.StartLoadingProcess(saves.SaveRoutine(m_Requested), null, LoadingProcessTag.Save);
            return null;
        }

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
                failure = SaveFailed;
                return CheckpointProgress.Failed;
            }
            if (written.OperationState != SaveInfo.StateType.None)
            {
                return CheckpointProgress.Running;
            }
            try
            {
                if (!written.IsActuallySaved || !File.Exists(written.FolderName) || new FileInfo(written.FolderName).Length == 0 || string.IsNullOrEmpty(written.Saver?.ReadHeader()))
                {
                    failure = SaveFailed;
                    return CheckpointProgress.Failed;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Pre-return save could not be read back.", ex);
                failure = SaveFailed;
                return CheckpointProgress.Failed;
            }
            m_Checkpoint = written;
            Log.Info($"Pre-return save verified: {written.Name}.");
            return CheckpointProgress.Succeeded;
        }

        public void FadeOut()
        {
            FadeCanvas.Instance.ShowLoadingScreen();
        }

        public void LoadDestination()
        {
            Game.Instance.LoadArea(ScenarioBlueprints.EnterPoint, AutoSaveMode.None);
        }

        /// <summary>The return, once: what the party actually carries home is what the lodge will count.</summary>
        public void CommitArrival()
        {
            ConclusionState c = WardStore.Existing?.Conclusion;
            if (c == null)
            {
                return;
            }
            var carried = DebriefService.Artifacts().ToList();
            bool spear = carried.Any(KruneLoot.IsSpear);
            bool rod = carried.Any(KruneLoot.IsRod);
            if (ConclusionRules.RecordReturn(c, Game.Instance.TimeController.GameTime.TotalSeconds, spear, rod))
            {
                Log.Info($"Return committed: Krune {(c.ReturnedAfterVictory ? "slain" : "alive")}, spear carried {spear}, rod carried {rod}; party [{string.Join(", ", Game.Instance.Player.Party.Select(u => u.CharacterName).ToArray())}].");
            }
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
            Log.Error($"Portal return stranded: {text}; recovery save '{checkpoint?.Name}'.");
            string message = checkpoint == null ? text : $"{text} Load the save \"{checkpoint.Name}\" to stand before the portal again?";
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
