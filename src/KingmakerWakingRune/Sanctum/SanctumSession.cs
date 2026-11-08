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
            if (SanctumScope.IsSanctumLoaded)
            {
                SanctumLighting.Restore();
            }
        }

        public void OnAreaDidLoad()
        {
            if (!SanctumScope.IsSanctumLoaded)
            {
                return;
            }
            try
            {
                SanctumScope.EnsureBuilt();

                RefugeTransit.OnSanctumLoaded();
                Log.Info($"Sanctum loaded: entered={Game.Instance.Player.UnlockableFlags.IsUnlocked(ScenarioBlueprints.SanctumEnteredFlag)} party={Game.Instance.Player.Party.Count} loot={Game.Instance.CurrentlyLoadedArea.LootSetting} indoor={Game.Instance.CurrentlyLoadedArea.IsIndoor}.");
            }
            catch (Exception ex)
            {
                Log.Error("Sanctum session failed.", ex);
            }
        }
    }
}
