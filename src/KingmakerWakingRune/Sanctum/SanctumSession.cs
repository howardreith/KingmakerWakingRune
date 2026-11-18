using System;
using Kingmaker;
using Kingmaker.PubSubSystem;
using KingmakerWakingRune.Blueprints;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>Builds the environment whenever the sanctum loads and commits token arrival once it has.</summary>
    internal sealed class SanctumSession : IAreaLoadingStagesHandler, ISceneHandler, IGlobalSubscriber
    {
        private static SanctumSession s_Instance;

        public static void Subscribe()
        {
            if (s_Instance != null)
            {
                return;
            }
            s_Instance = new SanctumSession();
            EventBus.Subscribe(s_Instance);
        }

        public void OnAreaScenesLoaded()
        {
            if (SanctumScope.IsSanctumLoaded)
            {
                SanctumScope.EnsureBuilt();
            }
        }

        public void OnAreaLoadingComplete()
        {
        }

        public void OnAreaBeginUnloading()
        {
            Krune.KruneSession.Reset("area unloading");
            if (SanctumScope.IsSanctumLoaded)
            {
                SanctumLighting.Restore();
                Wards.WardRuntime.OnLeavingSanctum();
            }
        }

        public void OnAreaDidLoad()
        {
            Krune.KruneSession.Reset("area loaded");
            if (!SanctumScope.IsSanctumLoaded)
            {
                return;
            }
            try
            {
                SanctumScope.EnsureBuilt();
                // The party is already placed by now: door cuts applied during placement stack everyone on the stairs.
                var saved = Wards.WardStore.Existing;
                bool previousLayout = !RefugeTransit.InProgress && saved != null && saved.LayoutVersion < SanctumPlan.LayoutVersion;
                if (!previousLayout) SanctumDoors.ReleaseOccupied();
                SanctumScope.ApplyDoorCuts();
                bool positionsReady = RefugeTransit.InProgress || SanctumScope.RepairRestoredPositions(previousLayout);
                // Read before arrival commits: a save that already entered the empty sanctum is the only legacy case.
                bool legacyEntered = Game.Instance.Player.UnlockableFlags.IsUnlocked(ScenarioBlueprints.SanctumEnteredFlag);
                Wards.WardRuntime.OnSanctumLoaded(legacyEntered, !previousLayout || positionsReady);
                RefugeTransit.OnSanctumLoaded();
                Log.Info($"Sanctum loaded: entered={Game.Instance.Player.UnlockableFlags.IsUnlocked(ScenarioBlueprints.SanctumEnteredFlag)} party={Game.Instance.Player.Party.Count} loot={Game.Instance.CurrentlyLoadedArea.LootSetting} indoor={Game.Instance.CurrentlyLoadedArea.IsIndoor}.");
            }
            catch (Exception ex)
            {
                Log.Error("Sanctum session failed.", ex);
                RefugeTransit.OnSanctumLoadFailed(ex.Message);
            }
        }
    }
}
