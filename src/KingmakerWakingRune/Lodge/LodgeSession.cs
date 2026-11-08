using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.View;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Diagnostics;
using UnityEngine;

namespace KingmakerWakingRune.Lodge
{
    /// <summary>
    /// Applies lodge setup through native persistence: Sheila is spawned into the area's saved
    /// entity state exactly once, recorded by a save-bound flag, and only verified afterwards.
    /// </summary>
    internal sealed class LodgeSession : ISceneHandler
    {
        private static LodgeSession s_Instance;

        public static void Subscribe()
        {
            if (s_Instance != null)
            {
                return;
            }
            s_Instance = new LodgeSession();
            EventBus.Subscribe(s_Instance);
        }

        public void OnAreaBeginUnloading()
        {
        }

        public void OnAreaDidLoad()
        {
            if (!ScenarioBlueprints.IsRegistered)
            {
                return;
            }
            try
            {
                Apply();
            }
            catch (Exception ex)
            {
                Log.Error("Lodge setup failed.", ex);
            }
        }

        private static void Apply()
        {
            Player player = Game.Instance.Player;
            var flags = player.UnlockableFlags;
            bool inLodge = Game.Instance.CurrentlyLoadedArea == ScenarioBlueprints.Area;
            bool sessionMarked = flags.IsUnlocked(ScenarioBlueprints.SessionFlag);
            int sessionVersion = sessionMarked ? flags.GetFlagValue(ScenarioBlueprints.SessionFlag) : 0;
            bool setupDone = flags.IsUnlocked(ScenarioBlueprints.SetupFlag);
            var sheilas = Game.Instance.State.Units.Where(u => u.Blueprint == ScenarioBlueprints.Sheila).ToList();

            LodgeDecision decision = LodgePolicy.Decide(inLodge, sessionMarked, sessionVersion, ScenarioIds.SessionVersion, setupDone, sheilas.Count);
            if (decision.Action == LodgeAction.None)
            {
                return;
            }

            UnitEntityData main = player.MainCharacter.Value;
            Log.Info($"Lodge area loaded: area={Game.Instance.CurrentlyLoadedArea?.name} decision={decision.Action} ({decision.Reason}) " +
                     $"gameId={player.GameId} player={Describe(main)} position={main?.Position}");

            switch (decision.Action)
            {
                case LodgeAction.CreateInitialSetup:
                    UnitEntityData sheila = SpawnSheila();
                    if (sheila == null)
                    {
                        Log.Error("Sheila could not be spawned; setup is not recorded and will be retried on next load.");
                        return;
                    }
                    flags.Unlock(ScenarioBlueprints.SetupFlag);
                    Log.Info($"Initial lodge setup recorded: sheila={Describe(sheila)} position={sheila.Position} flag={ScenarioBlueprints.SetupFlag.AssetGuid}");
                    break;
                case LodgeAction.Restored:
                    Log.Info($"Lodge session restored: sheila={Describe(sheilas[0])} position={sheilas[0].Position} " +
                             $"difficulty={DifficultyReport.Describe()}");
                    break;
                case LodgeAction.Error:
                    Log.Error($"Lodge state inconsistent: {decision.Reason}; sheilas=[{string.Join(", ", sheilas.Select(Describe).ToArray())}]");
                    break;
            }
            if (inLodge)
            {
                Recon.DumpLodge();
            }
        }

        private static UnitEntityData SpawnSheila()
        {
            Vector3 position = ObstacleAnalyzer.GetNearestNode(LodgeLayout.SheilaPosition).clampedPosition;
            return Game.Instance.EntityCreator.SpawnUnit(
                ScenarioBlueprints.Sheila,
                position,
                Quaternion.Euler(0f, LodgeLayout.SheilaYaw, 0f),
                Game.Instance.State.LoadedAreaState.MainState);
        }

        internal static string Describe(UnitEntityData unit)
        {
            if (unit == null)
            {
                return "<none>";
            }
            var d = unit.Descriptor;
            string classes = string.Join("/", d.Progression.Classes.Select(c => $"{c.CharacterClass.name}{c.Level}").ToArray());
            return $"'{unit.CharacterName}' id={unit.UniqueId} bp={unit.Blueprint.name} race={d.Progression.Race?.name} " +
                   $"gender={d.Gender} classes={classes} portrait={d.UISettings.PortraitBlueprint?.name ?? d.UISettings.CustomPortraitRaw?.CustomId}";
        }
    }
}
