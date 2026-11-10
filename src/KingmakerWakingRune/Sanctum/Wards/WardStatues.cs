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
        private const float WaypointMeters = 1.2f;
        private static readonly Color Ash = new Color(0.28f, 0.27f, 0.26f, 1f);

        /// <summary>
        /// A reporter's printed route to Vandiana, on foot: up to A2, through it with its own hand, round to the east door,
        /// through that, and on to a point in her chamber where she can hear it. Progress is saved per leg.
        /// </summary>
        private static readonly RunnerLeg[] RunnerRoute =
        {
            new RunnerLeg(13.15f, 25.0f, RunnerStep.Walk),
            new RunnerLeg(13.15f, 25.0f, RunnerStep.OpenA2),
            new RunnerLeg(13.15f, 27.6f, RunnerStep.Walk),
            new RunnerLeg(22.2f, 14.65f, RunnerStep.Walk),
            new RunnerLeg(22.2f, 14.65f, RunnerStep.OpenEast),
            new RunnerLeg(19.6f, 14.65f, RunnerStep.Walk),
            new RunnerLeg(15.2f, 10.6f, RunnerStep.Report),
        };

        private enum RunnerStep
        {
            Walk,
            OpenA2,
            OpenEast,
            Report,
        }

        private struct RunnerLeg
        {
            public readonly GridPoint Point;
            public readonly RunnerStep Step;

            public RunnerLeg(float x, float y, RunnerStep step)
            {
                Point = new GridPoint(x, y);
                Step = step;
            }
        }

        public static bool IsStoneToFlesh(Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility ability)
        {
            return ability.AssetGuid == StoneToFlesh;
        }

        public static UnitEntityData Body(StatueState statue)
        {
            return string.IsNullOrEmpty(statue.EntityId) ? null : Game.Instance.State.Units.FirstOrDefault(u => u.UniqueId == statue.EntityId);
        }

        /// <summary>A real dead body stands where the figure was: a failed survival save, a later death, or an old save's remnant.</summary>
        public static bool BodyIsCorpse(string slot)
        {
            SanctumState state = WardRuntime.State;
            if (state == null || !state.Statues.TryGetValue(slot, out StatueState statue))
            {
                return false;
            }
            if (state.Credentials.RemnantSlots.Contains(slot))
            {
                return true;
            }
            if (statue.Outcome != StatueOutcome.Corpse && statue.Outcome != StatueOutcome.Living)
            {
                return false;
            }
            UnitEntityData body = Body(statue);
            return body != null && body.Descriptor.State.IsDead;
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
            Vector3 placed = StepOut(id);
            BearerRole role = BearerManifest.Roles[id];
            BlueprintUnit blueprint = WardBlueprints.Cultists[id];
            UnitEntityData cultist = Game.Instance.EntityCreator.SpawnUnit(blueprint, placed, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
            LogStats(id, cultist, "spawned");
            cultist.Descriptor.Ensure<WardRestoredPart>().StatueId = id;
            statue.EntityId = cultist.UniqueId;
            statue.Credentialed = CredentialRules.IsMarked(id);
            statue.RouteLeg = 0;
            LogStats(id, cultist, "before survival save");
            RuleSavingThrow save = Rulebook.Trigger(new RuleSavingThrow(cultist, SavingThrowType.Fortitude, WardProfile.StoneToFleshSurvivalDC));
            Log.Info($"Stone to Flesh on {id}: survival Fortitude {save.RollResult} vs {WardProfile.StoneToFleshSurvivalDC}: {(save.IsPassed ? "survived" : "failed")}; role {role}; credential {statue.Credentialed}.");
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

        /// <summary>
        /// Where a freed body stands: a step out from the figure along its facing, on the same floor, so it never lands on
        /// the ziggurat tier the figure was holding up.
        /// </summary>
        private static Vector3 StepOut(string id)
        {
            Vector3 foot = SanctumDressing.StatueFeet[id];
            Vector3 facing = SanctumDressing.StatuePalms.TryGetValue(id, out Vector3[] palm) ? palm[2] : Vector3.zero;
            Vector3 front = foot + new Vector3(facing.x, 0f, facing.z).normalized * 0.7f;
            Vector3 node = ObstacleAnalyzer.GetNearestNode(front).clampedPosition;
            return Mathf.Abs(node.y - foot.y) < 0.5f ? node : front;
        }

        /// <summary>
        /// Brings marked bodies from older saves up to date: a body restored before credentials existed gains its brand once,
        /// and a marked body that no longer exists at all leaves one documented remnant at its figure, never regenerated.
        /// </summary>
        public static void MigrateMarked(SanctumState state)
        {
            foreach (string slot in CredentialRules.MarkedSources)
            {
                if (!state.Statues.TryGetValue(slot, out StatueState statue) || statue.Outcome == StatueOutcome.Stone)
                {
                    continue;
                }
                UnitEntityData body = Body(statue);
                if (body != null && !statue.Credentialed)
                {
                    if (!body.Descriptor.HasFact(WardBlueprints.SihedronBrand))
                    {
                        body.Descriptor.AddFact(WardBlueprints.SihedronBrand);
                    }
                    statue.Credentialed = true;
                    Log.Info($"Migration: {slot}'s restored body gained its Sihedron credential.");
                }
                else if (body == null && !state.Credentials.HandsRecovered.Contains(slot) && !state.Credentials.RemnantSlots.Contains(slot))
                {
                    state.Credentials.RemnantSlots.Add(slot);
                    Log.Info($"Migration: {slot}'s body is gone from this save; one remnant hand is left at the figure.");
                }
            }
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
            if (SanctumDressing.PalmMarks.TryGetValue(id, out GameObject mark) && mark != null)
            {
                mark.SetActive(!hidden);
            }
            UnitEntityData fixture = WardFixtures.Find(id);
            if (fixture == null)
            {
                return;
            }
            // A marked corpse keeps a readable target on the body itself until its hand is taken.
            bool corpseTarget = hidden && CredentialRules.IsMarked(id) && BodyIsCorpse(id) && !WardRuntime.State.Credentials.HandsRecovered.Contains(id);
            if (corpseTarget)
            {
                UnitEntityData body = Body(statue);
                Vector3 at = body != null ? body.Position : SanctumDressing.StatueFeet[id];
                if ((fixture.Position - at).sqrMagnitude > 0.04f)
                {
                    fixture.Position = at;
                }
            }
            bool wanted = !hidden || corpseTarget;
            if (fixture.IsInGame != wanted)
            {
                fixture.IsInGame = wanted;
            }
        }

        /// <summary>Restored bodies whose statistics were logged since the last load; a reload logs each once again.</summary>
        private static readonly System.Collections.Generic.HashSet<string> s_StatsLogged = new System.Collections.Generic.HashSet<string>();

        public static void ForgetStatsProbe()
        {
            s_StatsLogged.Clear();
        }

        /// <summary>
        /// Where a restored body's numbers come from: the blueprint's score, the initialized base, every modifier with its
        /// source, damage and drain, race, classes and facts. Read only; it never writes a statistic.
        /// </summary>
        public static void LogStats(string slot, UnitEntityData unit, string when)
        {
            if (unit == null)
            {
                return;
            }
            var sb = new System.Text.StringBuilder();
            sb.Append($"Cultist stats {slot} [{when}]: blueprint {unit.Blueprint.name} race {unit.Descriptor.Progression.Race?.name} classes [{string.Join(",", unit.Descriptor.Progression.Classes.Select(c => c.CharacterClass.name + " " + c.Level).ToArray())}] prefab {unit.View?.name}");
            foreach (StatType type in new[] { StatType.Strength, StatType.Dexterity, StatType.Constitution, StatType.Intelligence, StatType.Wisdom, StatType.Charisma })
            {
                var stat = (ModifiableValueAttributeStat)unit.Stats.GetStat(type);
                int printed = type == StatType.Strength ? unit.Blueprint.Strength : type == StatType.Dexterity ? unit.Blueprint.Dexterity : type == StatType.Constitution ? unit.Blueprint.Constitution : type == StatType.Intelligence ? unit.Blueprint.Intelligence : type == StatType.Wisdom ? unit.Blueprint.Wisdom : unit.Blueprint.Charisma;
                sb.Append($"; {type} bp {printed} base {stat.BaseValue} mod {stat.ModifiedValue} dmg {stat.Damage} drain {stat.Drain} [{string.Join(",", stat.Modifiers.Select(m => $"{m.ModDescriptor} {m.ModValue:+0;-0} {m.Source?.Blueprint?.name ?? m.ItemSource?.Blueprint?.name ?? "?"}").ToArray())}]");
            }
            sb.Append($"; AC {unit.Stats.AC.ModifiedValue} [{string.Join(",", unit.Stats.AC.Modifiers.Select(m => $"{m.ModDescriptor} {m.ModValue:+0;-0}").ToArray())}]");
            sb.Append($"; Fort {unit.Stats.SaveFortitude.ModifiedValue} Ref {unit.Stats.SaveReflex.ModifiedValue} Will {unit.Stats.SaveWill.ModifiedValue} hp {unit.Descriptor.HPLeft}/{unit.Stats.HitPoints.ModifiedValue}");
            sb.Append($"; hands {unit.Body.PrimaryHand.MaybeItem?.Blueprint.name}/{unit.Body.SecondaryHand.MaybeItem?.Blueprint.name} armor {unit.Body.Armor.MaybeItem?.Blueprint.name}");
            sb.Append($"; facts [{string.Join(",", unit.Descriptor.Progression.Features.Enumerable.Select(f => f.Blueprint.name).Concat(unit.Buffs.Enumerable.Select(b => "buff:" + b.Blueprint.name)).ToArray())}]");
            Log.Info(sb.ToString());
        }

        public static void Tick(SanctumState state, double now)
        {
            foreach (StatueState statue in state.Statues.Values.ToList())
            {
                if (!string.IsNullOrEmpty(statue.EntityId) && s_StatsLogged.Add(statue.EntityId))
                {
                    LogStats(statue.Id, Body(statue), "first tick since load or spawn");
                }
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
                else if (statue.Outcome == StatueOutcome.Corpse && CredentialRules.IsMarked(statue.Id))
                {
                    // A failed survival save dies a moment after the cast: keep the hand's target on the body as it falls.
                    ApplyVisual(statue.Id, statue, now);
                }
                else if (statue.Outcome == StatueOutcome.Living)
                {
                    UnitEntityData body = Body(statue);
                    if (body != null && body.Descriptor.State.IsDead && CredentialRules.IsMarked(statue.Id))
                    {
                        ApplyVisual(statue.Id, statue, now);
                    }
                    if (!statue.Reported && BearerManifest.Roles.TryGetValue(statue.Id, out BearerRole role) && role == BearerRole.Report)
                    {
                        TickRunner(statue, body, now);
                    }
                }
            }
        }

        /// <summary>
        /// A runner really walks its saved route: each leg ends only on arrival or a completed door action, so an
        /// interruption or reload resumes where it was. Only a living, able runner standing in Vandiana's chamber reports.
        /// </summary>
        private static void TickRunner(StatueState statue, UnitEntityData runner, double now)
        {
            if (runner == null || runner.Descriptor.State.IsDead || runner.IsInCombat || !runner.Descriptor.State.CanAct || !runner.Descriptor.State.CanMove)
            {
                return;
            }
            if (statue.RouteLeg >= RunnerRoute.Length)
            {
                return;
            }
            RunnerLeg leg = RunnerRoute[statue.RouteLeg];
            Vector3 point = World(leg.Point);
            bool arrived = Vector2.Distance(new Vector2(runner.Position.x, runner.Position.z), new Vector2(point.x, point.z)) <= WaypointMeters;
            switch (leg.Step)
            {
                case RunnerStep.Walk:
                    if (arrived)
                    {
                        Advance(statue, runner);
                    }
                    else if (runner.Commands.Empty)
                    {
                        runner.Commands.Run(new UnitMoveTo(point, 0.3f));
                    }
                    break;
                case RunnerStep.OpenA2:
                    if (WardRuntime.State.Door.Destroyed || (WardDoor.A2 != null && WardDoor.A2.IsOpen))
                    {
                        Advance(statue, runner);
                    }
                    else if (WardRuntime.State.Door.Latched && !runner.Descriptor.HasFact(WardBlueprints.SihedronBrand))
                    {
                        // An unmarked cultist has no sign the door knows: it is stopped here and turns on the nearest intruder.
                        UnitEntityData nearest = Game.Instance.Player.Party.Where(u => !u.Descriptor.State.IsDead).OrderBy(u => u.DistanceTo(runner)).FirstOrDefault();
                        if (nearest != null && runner.Commands.Empty && nearest.DistanceTo(runner) < 25f)
                        {
                            runner.Commands.Run(UnitAttack.CreateAttackCommand(runner, nearest));
                        }
                    }
                    else if (runner.Commands.Empty)
                    {
                        // The cultist's own brand, through the real door interaction at arm's length.
                        WardDoor.Order(runner, "open A2 with its own brand", u => !WardRuntime.State.Door.Destroyed, u =>
                        {
                            WardDoor.PresentOwnBrand(u, statue.Id);
                            Advance(statue, u);
                        });
                    }
                    break;
                case RunnerStep.OpenEast:
                    SanctumDoor east = UnityEngine.Object.FindObjectsOfType<SanctumDoor>().FirstOrDefault(d => d.Plan.Id == "A4Approach");
                    if (east == null || east.IsOpen)
                    {
                        Advance(statue, runner);
                    }
                    else if (runner.Commands.Empty)
                    {
                        KwrFixtureCommand.Issue(runner, KwrFixtureCommand.AtPoint(east.transform.position, east.ProximityRadius, "open the east door", false, u => !east.IsOpen, u =>
                        {
                            east.Toggle(u);
                            Advance(statue, u);
                        }));
                    }
                    break;
                case RunnerStep.Report:
                    if (!arrived)
                    {
                        if (runner.Commands.Empty)
                        {
                            runner.Commands.Run(new UnitMoveTo(point, 0.3f));
                        }
                        break;
                    }
                    if (!InVandianasChamber(runner))
                    {
                        break;
                    }
                    statue.Reported = true;
                    statue.ReportedAt = now;
                    Advance(statue, runner);
                    WardRitual.Raise(AwarenessCause.Messenger, runner.CharacterName + " reached Vandiana and reported");
                    break;
            }
        }

        private static void Advance(StatueState statue, UnitEntityData runner)
        {
            statue.RouteLeg++;
            Log.Info($"Runner {statue.Id} ({runner.CharacterName}) completed leg {statue.RouteLeg}/{RunnerRoute.Length}.");
        }

        /// <summary>The same enclosed space as Vandiana, on her floor: never through the rock between A1 and A5.</summary>
        private static bool InVandianasChamber(UnitEntityData runner)
        {
            UnitEntityData vandiana = WardFixtures.Vandiana;
            if (vandiana == null)
            {
                return false;
            }
            float rx = runner.Position.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX, ry = SanctumPlan.OriginY - runner.Position.z / SanctumPlan.MetersPerSquare;
            float vx = vandiana.Position.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX, vy = SanctumPlan.OriginY - vandiana.Position.z / SanctumPlan.MetersPerSquare;
            return SanctumPlan.SpaceAt(rx, ry) == SanctumPlan.SpaceAt(vx, vy) && Mathf.Abs(runner.Position.y - vandiana.Position.y) < 2f && runner.DistanceTo(vandiana) <= 8f;
        }

        private static Vector3 World(GridPoint p)
        {
            return new Vector3(SanctumPlan.ToWorldX(p.X), SanctumPlan.HeightAt(p.X, p.Y) ?? 0f, SanctumPlan.ToWorldZ(p.Y));
        }
    }
}
