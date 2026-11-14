using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.View;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Diagnostics;
using KingmakerWakingRune.Party;
using KingmakerWakingRune.Sanctum;
using UnityEngine;

namespace KingmakerWakingRune.Lodge
{
    /// <summary>
    /// Applies lodge setup through native persistence: authored NPCs are spawned into the area's saved
    /// entity state exactly once, recorded by save-bound flags, and only verified afterwards.
    /// A WR-01 save (Sheila only) is upgraded once by the same path.
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
            BriefingService.Disarm();
            LodgeDriver.Detach();
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
            // After the lodge's own setup: a party arriving through the sanctum's portal is committed here, once.
            Sanctum.PortalTransit.OnLodgeLoaded();
        }

        private static void Apply()
        {
            if (Game.Instance.CurrentlyLoadedArea == ScenarioBlueprints.Sanctum)
            {
                return;
            }
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
                    UnitEntityData sheila = Spawn(ScenarioBlueprints.Sheila, LodgeLayout.Sheila, LodgeLayout.SheilaYaw);
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
                    return;
            }
            if (inLodge)
            {
                EnsureCast();
                StartingGrants.ApplyOnce();
                RefugeToken.EnsureGranted();
                BriefingService.ArmIfPending();
                LodgeDriver.Attach();
                Recon.DumpLodge();
            }
        }

        private static void EnsureCast()
        {
            var flags = Game.Instance.Player.UnlockableFlags;
            bool recorded = flags.IsUnlocked(ScenarioBlueprints.CastSetupFlag);
            foreach (CastMember member in LodgeCast.Members)
            {
                int present = Game.Instance.State.Units.Count(u => u.Blueprint == member.Unit);
                switch (WakingPolicies.DecideMember(recorded, present))
                {
                    case WakingPolicies.CastAction.Spawn:
                        UnitEntityData spawned = Spawn(member.Unit, member.Position, member.Yaw);
                        if (spawned == null)
                        {
                            Log.Error($"Cast member {member.Id} could not be spawned; setup stays unrecorded.");
                            return;
                        }
                        break;
                    case WakingPolicies.CastAction.Report:
                        Log.Error($"Cast member {member.Id} has {present} instances but setup recorded={recorded}; not changed.");
                        break;
                }
            }
            if (!recorded)
            {
                flags.Unlock(ScenarioBlueprints.CastSetupFlag);
                Log.Info($"Lodge cast setup recorded: {LodgeCast.Members.Count} NPCs plus Sheila.");
            }
        }

        private static UnitEntityData Spawn(Kingmaker.Blueprints.BlueprintUnit blueprint, Vector3 position, float yaw)
        {
            Vector3 placed = ObstacleAnalyzer.GetNearestNode(position).clampedPosition;
            return Game.Instance.EntityCreator.SpawnUnit(blueprint, placed, Quaternion.Euler(0f, yaw, 0f), Game.Instance.State.LoadedAreaState.MainState);
        }

        public static UnitEntityData FindCastMember(string id)
        {
            var blueprint = LodgeCast.Find(id).Unit;
            return Game.Instance.State.Units.FirstOrDefault(u => u.Blueprint == blueprint);
        }

        public static UnitEntityData FindSheila()
        {
            return Game.Instance.State.Units.FirstOrDefault(u => u.Blueprint == ScenarioBlueprints.Sheila);
        }

        public static IEnumerable<UnitEntityData> CurrentAgents()
        {
            HashSet<Kingmaker.Blueprints.BlueprintUnit> blueprints = new HashSet<Kingmaker.Blueprints.BlueprintUnit>(LodgeCast.Agents.Select(a => a.Unit));
            return Game.Instance.State.Units.Where(u => blueprints.Contains(u.Blueprint));
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
