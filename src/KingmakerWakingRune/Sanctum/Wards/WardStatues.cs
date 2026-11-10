using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.View;
using Newtonsoft.Json;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>Ties a restored or dead body to the statue it came from, so a reload re-binds rather than recreates it.</summary>
    public sealed class WardRestoredPart : UnitPart
    {
        [JsonProperty]
        public string StatueId;
    }

    /// <summary>
    /// The figures' fates. Stone to Flesh is observed from the real cast, one-to-one per statue: humans return (or die on the
    /// survival save), the dead outsiders crumble to ash over a simulated minute. Nothing is ever granted twice.
    /// </summary>
    internal static class WardStatues
    {
        private const string StoneToFlesh = "e243740dfdb17a246b116b334ed0b165";
        private static readonly Color Ash = new Color(0.28f, 0.27f, 0.26f, 1f);

        // Printed route for a report: through both doors to Vandiana, on foot.
        private static readonly GridPoint[] RunnerRoute =
        {
            new GridPoint(13.15f, 25.9f),
            new GridPoint(13.15f, 27.6f),
            new GridPoint(22.2f, 14.65f),
            new GridPoint(19.6f, 14.65f),
            new GridPoint(13.9f, 9.6f),
        };

        public static bool IsStoneToFlesh(Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility ability)
        {
            return ability.AssetGuid == StoneToFlesh;
        }

        public static void OnRestored(string statueId, UnitEntityData caster)
        {
            SanctumState state = WardRuntime.State;
            StatueState statue = state.Statue(statueId);
            if (statue.Outcome != StatueOutcome.Stone)
            {
                Log.Info($"Stone to Flesh on {statueId}: already {statue.Outcome}; nothing more happens.");
                return;
            }
            if (BearerManifest.IsBearer(statueId))
            {
                RestoreBearer(statueId, statue, caster);
            }
            else
            {
                statue.Outcome = StatueOutcome.Crumbling;
                statue.CrumbleStartedAt = WardRuntime.Now;
                Log.Info($"Stone to Flesh on {statueId}: long dead; crumbling begins.");
                WardRuntime.Say("Flesh returns for a moment, and begins to crumble to ash.");
            }
        }

        private static void RestoreBearer(string id, StatueState statue, UnitEntityData caster)
        {
            Vector3 foot = SanctumDressing.StatueFeet[id];
            Vector3 placed = ObstacleAnalyzer.GetNearestNode(foot).clampedPosition;
            BearerRole role = BearerManifest.Roles[id];
            BlueprintUnit blueprint = role == BearerRole.Attack ? WardBlueprints.CultistAttacker : WardBlueprints.CultistReporter;
            UnitEntityData cultist = Game.Instance.EntityCreator.SpawnUnit(blueprint, placed, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
            cultist.Descriptor.Ensure<WardRestoredPart>().StatueId = id;
            statue.EntityId = cultist.UniqueId;
            RuleSavingThrow save = Rulebook.Trigger(new RuleSavingThrow(cultist, SavingThrowType.Fortitude, WardProfile.StoneToFleshSurvivalDC));
            Log.Info($"Stone to Flesh on {id}: survival Fortitude {save.RollResult} vs {WardProfile.StoneToFleshSurvivalDC}: {(save.IsPassed ? "survived" : "failed")}; role {role}.");
            if (save.IsPassed)
            {
                statue.Outcome = StatueOutcome.Living;
                WardRuntime.Say("Stone softens to flesh; a cultist stumbles free of the support.");
            }
            else
            {
                statue.Outcome = StatueOutcome.Corpse;
                cultist.Descriptor.State.MarkedForDeath = true;
                WardRuntime.Say("Stone softens to flesh, but the body beneath is already gone.");
            }
            ApplyVisual(id, statue, WardRuntime.Now);
        }

        public static void ApplyAll(SanctumState state, double now)
        {
            foreach (StatueSlot slot in StatueManifest.Slots)
            {
                ApplyVisual(slot.Id, state.Statue(slot.Id), now);
            }
        }

        private static void ApplyVisual(string id, StatueState statue, double now)
        {
            if (!SanctumDressing.StatueFigures.TryGetValue(id, out Renderer[] renderers))
            {
                return;
            }
            bool hidden = statue.Outcome == StatueOutcome.Living || statue.Outcome == StatueOutcome.Corpse || statue.Outcome == StatueOutcome.Ash;
            float progress = statue.Outcome == StatueOutcome.Crumbling ? Mathf.Clamp01((float)((now - statue.CrumbleStartedAt) / WardProfile.HuskCrumbleSeconds)) : 0f;
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = !hidden;
                if (!hidden && statue.Outcome == StatueOutcome.Crumbling)
                {
                    renderer.material.color = Color.Lerp(Color.white, Ash, progress);
                }
            }
            UnitEntityData fixture = WardFixtures.Find(id);
            if (fixture != null && hidden && fixture.IsInGame)
            {
                fixture.IsInGame = false;
            }
        }

        public static void Tick(SanctumState state, double now)
        {
            foreach (StatueState statue in state.Statues.Values.ToList())
            {
                if (statue.Outcome == StatueOutcome.Crumbling)
                {
                    ApplyVisual(statue.Id, statue, now);
                    if (now - statue.CrumbleStartedAt >= WardProfile.HuskCrumbleSeconds)
                    {
                        statue.Outcome = StatueOutcome.Ash;
                        ApplyVisual(statue.Id, statue, now);
                        Log.Info($"{statue.Id} crumbled to ash.");
                    }
                }
                else if (statue.Outcome == StatueOutcome.Living && !statue.Reported && BearerManifest.Roles.TryGetValue(statue.Id, out BearerRole role) && role == BearerRole.Report)
                {
                    TickRunner(state, statue, now);
                }
            }
        }

        /// <summary>A runner really walks: it opens what it may, is stopped by what it cannot, and reports only on arrival.</summary>
        private static void TickRunner(SanctumState state, StatueState statue, double now)
        {
            UnitEntityData runner = Game.Instance.State.Units.FirstOrDefault(u => u.UniqueId == statue.EntityId);
            if (runner == null || runner.Descriptor.State.IsDead || runner.IsInCombat)
            {
                return;
            }
            UnitEntityData vandiana = WardFixtures.Vandiana;
            if (vandiana != null && runner.DistanceTo(vandiana) <= 8f)
            {
                statue.Reported = true;
                statue.ReportedAt = now;
                WardRitual.Raise(AwarenessCause.Messenger, runner.CharacterName + " reached Vandiana");
                return;
            }
            if (!runner.Commands.Empty)
            {
                return;
            }
            int leg = NextLeg(runner);
            if (leg < 0)
            {
                return;
            }
            GridPoint target = RunnerRoute[leg];
            if (leg == 1 && !PassA2(runner))
            {
                return;
            }
            if (leg == 3)
            {
                OpenEastDoor(runner);
            }
            runner.Commands.Run(new UnitMoveTo(new Vector3(SanctumPlan.ToWorldX(target.X), SanctumPlan.HeightAt(target.X, target.Y) ?? 0f, SanctumPlan.ToWorldZ(target.Y))));
        }

        private static int NextLeg(UnitEntityData runner)
        {
            float x = runner.Position.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX;
            float y = SanctumPlan.OriginY - runner.Position.z / SanctumPlan.MetersPerSquare;
            for (int i = RunnerRoute.Length - 1; i >= 0; i--)
            {
                float dx = x - RunnerRoute[i].X, dy = y - RunnerRoute[i].Y;
                if (dx * dx + dy * dy < 1.2f * 1.2f)
                {
                    return i + 1 < RunnerRoute.Length ? i + 1 : -1;
                }
            }
            return 0;
        }

        /// <summary>A loyal cultist has no brand; a latched door stops it, and it turns on whoever is nearest instead.</summary>
        private static bool PassA2(UnitEntityData runner)
        {
            DoorWardState door = WardRuntime.State.Door;
            SanctumDoor a2 = UnityEngine.Object.FindObjectsOfType<SanctumDoor>().FirstOrDefault(d => d.Plan.Id == "A2");
            if (door.Destroyed || a2 == null || a2.IsOpen)
            {
                return true;
            }
            if (door.Latched)
            {
                UnitEntityData nearest = Game.Instance.Player.Party.OrderBy(u => u.DistanceTo(runner)).FirstOrDefault();
                if (nearest != null)
                {
                    runner.Commands.Run(UnitAttack.CreateAttackCommand(runner, nearest));
                }
                return false;
            }
            a2.Toggle(runner);
            return true;
        }

        private static void OpenEastDoor(UnitEntityData runner)
        {
            SanctumDoor east = UnityEngine.Object.FindObjectsOfType<SanctumDoor>().FirstOrDefault(d => d.Plan.Id == "A4Approach");
            if (east != null && !east.IsOpen)
            {
                east.Toggle(runner);
            }
        }
    }
}
