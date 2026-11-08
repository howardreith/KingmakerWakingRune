using System.Collections;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Lodge;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// The one transfer service behind both ways of using the token: validate, close native UI, checkpoint,
    /// fade, native area load, commit on real arrival, fade back. Runs on unscaled time so a paused game cannot stall it.
    /// </summary>
    internal sealed class RefugeTransit : MonoBehaviour
    {
        public const string PreDepartureSaveName = "Before the Sanctum of Sloth";
        private const float ArrivalTimeoutSeconds = 240f;
        private const float StepTimeoutSeconds = 30f;

        private static RefugeTransit s_Instance;

        public static bool InProgress { get; private set; }

        private static bool s_Arrived;

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
            }
            InProgress = true;
            s_Arrived = false;
            Log.Info("Refuge Token accepted; beginning transfer.");
            s_Instance.StartCoroutine(s_Instance.Run());
        }

        /// <summary>Called when the sanctum has finished loading. Commits exactly one successful transfer.</summary>
        public static void OnSanctumLoaded()
        {
            var flags = Game.Instance.Player.UnlockableFlags;
            if (!SanctumPolicy.ShouldCommitArrival(InProgress, true, flags.IsUnlocked(ScenarioBlueprints.SanctumEnteredFlag)))
            {
                return;
            }
            RefugeToken.Consume();
            flags.Unlock(ScenarioBlueprints.SanctumEnteredFlag);
            s_Arrived = true;
            Log.Info($"Arrival committed: party [{string.Join(", ", Game.Instance.Player.Party.Select(u => u.CharacterName).ToArray())}]; tokens left {RefugeToken.CountOwned()}.");
        }

        /// <summary>A save load replaces the world; any in-flight transfer ends with it.</summary>
        public static void Reset(string reason)
        {
            if (!InProgress)
            {
                return;
            }
            Log.Info($"Refuge transfer state cleared: {reason}.");
            if (s_Instance != null)
            {
                s_Instance.StopAllCoroutines();
            }
            InProgress = false;
            FadeIn();
        }

        private IEnumerator Run()
        {
            yield return CloseWindows();
            yield return Checkpoint();
            FadeCanvas.Instance.ShowLoadingScreen();
            yield return new WaitForSecondsRealtime(FadeCanvas.Instance.FadeTimer + 0.2f);
            LodgeBriefingSafety();
            Game.Instance.LoadArea(ScenarioBlueprints.SanctumEnter, AutoSaveMode.None);
            float deadline = Time.realtimeSinceStartup + ArrivalTimeoutSeconds;
            while (!s_Arrived && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            if (!s_Arrived)
            {
                Log.Error("Refuge transfer did not arrive in time; the token was not consumed.");
                InProgress = false;
                FadeIn();
                yield break;
            }
            deadline = Time.realtimeSinceStartup + StepTimeoutSeconds;
            while ((LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.IsLoadingScreenActive) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            FadeIn();
            InProgress = false;
            Log.Info("Refuge transfer complete; control restored.");
        }

        private static IEnumerator CloseWindows()
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (Game.Instance.IsModeActive(GameModeType.FullScreenUi) && Time.realtimeSinceStartup < deadline)
            {
                Game.Instance.UI.EscManager.OnEscPressed();
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        private static IEnumerator Checkpoint()
        {
            SaveManager saves = Game.Instance.SaveManager;
            if (!saves.IsSaveAllowed())
            {
                Log.Error("Pre-departure save was not allowed; continuing without one.");
                yield break;
            }
            SaveInfo info = saves.CreateNewSave(PreDepartureSaveName);
            LoadingProcess.Instance.StartLoadingProcess(saves.SaveRoutine(info), null, LoadingProcessTag.Save);
            float deadline = Time.realtimeSinceStartup + StepTimeoutSeconds;
            yield return null;
            while (LoadingProcess.Instance.IsLoadingInProcess && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Log.Info($"Pre-departure save written: {info.Name}.");
        }

        /// <summary>The briefing is complete by validation; the driver must not act during the unload.</summary>
        private static void LodgeBriefingSafety()
        {
            BriefingService.Disarm();
        }

        private static void FadeIn()
        {
            FadeCanvas.Instance?.HideLoadingScreen();
        }
    }
}
