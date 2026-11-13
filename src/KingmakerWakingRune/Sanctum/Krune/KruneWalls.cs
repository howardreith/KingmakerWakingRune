using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.View;
using Kingmaker.Visual.FogOfWar;
using KingmakerWakingRune.Sanctum.Wards;
using Pathfinding;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Walls of stone in the world (WallRules for the rule). Each standing section is a real obstacle: a navmesh cut that
    /// stops movement, a sight blocker that stops sight and line of effect, and stone the party sees. One targetable proxy per
    /// wall takes attacks and spells through the breakable-object pipeline; a hit breaks the section nearest the attacker.
    /// Saved in SanctumState.Walls and rebuilt on every load; a broken section leaves no cut, blocker or stone behind.
    /// </summary>
    internal static class KruneWalls
    {
        public const string IdPrefix = "Wall-";
        private static readonly Dictionary<string, GameObject> s_Built = new Dictionary<string, GameObject>();

        public static bool IsWall(string fixtureId)
        {
            return fixtureId != null && fixtureId.StartsWith(IdPrefix, System.StringComparison.Ordinal);
        }

        public static void Raise(MechanicsContext context, Vector3 point)
        {
            SanctumState state = WardRuntime.State;
            UnitEntityData caster = context.MaybeCaster;
            if (state == null || caster == null)
            {
                return;
            }
            string id = IdPrefix + (state.Walls.Count + 1) + "-" + (int)WardRuntime.Now;
            var occupants = Game.Instance.State.Units.Where(u => u.IsInGame && !u.Descriptor.State.IsDead && WardFixtures.IdOf(u) == null).ToList();
            bool Clear(float x, float y)
            {
                Vector3 at = World(x, y);
                return KruneMoves.OnFloor(at) && occupants.All(u => Vector2.Distance(new Vector2(u.Position.x, u.Position.z), new Vector2(at.x, at.z)) > u.Corpulence + 0.8f);
            }
            WallRecord wall = WallRules.Layout(id, KruneMoves.GridX(caster.Position), KruneMoves.GridY(caster.Position), KruneMoves.GridX(point), KruneMoves.GridY(point), WallRules.MaxSections, Clear);
            wall.RaisedAt = WardRuntime.Now;
            wall.Caster = caster.UniqueId;
            if (wall.Sections.Count == 0)
            {
                KruneAwakening.Note($"wall of stone at ({wall.CenterX:0.0},{wall.CenterY:0.0}) could not form: no clear floor there.");
                WardRuntime.Say("The stone finds no place to rise.");
                return;
            }
            state.Walls.Add(wall);
            Build(wall);
            KruneAwakening.Note($"wall of stone {id} raised by {caster.CharacterName} at ({wall.CenterX:0.0},{wall.CenterY:0.0}): {wall.Sections.Count} sections of {WallRules.SectionHitPoints} hp, hardness {WallRules.Hardness}.");
        }

        /// <summary>Every load: the standing walls, their proxies, and nothing for broken sections.</summary>
        public static void Rebuild(SanctumState state)
        {
            foreach (GameObject go in s_Built.Values.Where(g => g != null))
            {
                Object.Destroy(go);
            }
            s_Built.Clear();
            foreach (WallRecord wall in state.Walls.Where(WallRules.Standing))
            {
                Build(wall);
            }
        }

        public static void TakeHit(string fixtureId, UnitEntityData attacker, int amount, bool disintegrated)
        {
            WallRecord wall = WardRuntime.State?.Walls.FirstOrDefault(w => w.Id == fixtureId);
            if (wall == null || !WallRules.Standing(wall))
            {
                return;
            }
            Vector3 from = attacker != null ? attacker.Position : World(wall.CenterX, wall.CenterY);
            if (disintegrated)
            {
                List<WallSection> gone = WallRules.Disintegrate(wall, KruneMoves.GridX(from), KruneMoves.GridY(from));
                KruneAwakening.Note($"wall of stone {wall.Id}: {gone.Count} sections disintegrated by {attacker?.CharacterName}.");
                WardRuntime.Say("A section of the wall is gone in a puff of dust.");
            }
            else
            {
                WallSection section = WallRules.Hit(wall, KruneMoves.GridX(from), KruneMoves.GridY(from), amount, out int dealt);
                KruneAwakening.Note($"wall of stone {wall.Id} hit by {attacker?.CharacterName}: {amount} - hardness {WallRules.Hardness} = {dealt}; section {section?.Index} {section?.HitPoints}/{WallRules.SectionHitPoints}.");
                WardRuntime.Say(dealt <= 0 ? "The blow leaves no more than a scratch." : section != null && section.HitPoints <= 0 ? "A section of the wall cracks and falls." : "Stone chips fly from the wall.");
            }
            Build(wall);
        }

        private static void Build(WallRecord wall)
        {
            if (s_Built.TryGetValue(wall.Id, out GameObject old) && old != null)
            {
                Object.Destroy(old);
            }
            s_Built.Remove(wall.Id);
            UnitEntityData proxy = WardFixtures.FindRetired(wall.Id);
            if (!WallRules.Standing(wall))
            {
                if (proxy != null && proxy.IsInGame)
                {
                    foreach (UnitEntityData unit in Game.Instance.State.Units.Where(u => u.Commands.Raw.Any(c => c != null && c.TargetUnit == proxy)).ToList())
                    {
                        unit.Commands.InterruptAll();
                    }
                    proxy.IsInGame = false;
                }
                KruneAwakening.Note($"wall of stone {wall.Id} is down.");
                return;
            }
            // Built inactive and switched on once complete, as the sanctum doors are, so every cut and blocker registers.
            var root = new GameObject("KWR_" + wall.Id);
            root.SetActive(false);
            if (SanctumDressing.Root != null)
            {
                root.transform.SetParent(SanctumDressing.Root, false);
            }
            Vector3 dir = Direction(wall);
            Quaternion along = Quaternion.LookRotation(dir, Vector3.up);
            float section = SanctumPlan.MetersPerSquare;
            Vector3 half = dir * (section / 2f);
            var mesh = new SceneryMesh();
            foreach (WallSection s in wall.Sections.Where(s => s.HitPoints > 0))
            {
                Vector3 at = World(s.X, s.Y);
                var part = new GameObject("Section" + s.Index);
                part.transform.SetParent(root.transform, false);
                part.transform.position = at;
                var blocker = part.AddComponent<FogOfWarBlockerPolygon>();
                blocker.Height = 3.2f;
                AccessTools.Field(typeof(Kingmaker.Utility.PolygonComponent), "m_Points").SetValue(blocker, new List<Vector3> { -half, half, -half });
                AccessTools.Field(typeof(Kingmaker.Utility.PolygonComponent), "m_Closed").SetValue(blocker, false);
                var cutObject = new GameObject("Cut");
                cutObject.transform.SetParent(part.transform, false);
                cutObject.transform.rotation = along;
                var cut = cutObject.AddComponent<NavmeshCut>();
                cut.type = NavmeshCut.MeshType.Rectangle;
                cut.rectangleSize = new Vector2(0.5f, section + 0.05f);
                cut.height = 5f;
                cut.useRotation = true;
                if (SanctumDressing.Palette != null)
                {
                    mesh.Box(SanctumDressing.Palette.Stone, at + Vector3.up * 1.5f, new Vector3(0.12f, 3f, section), along);
                }
            }
            mesh.Instantiate(root.transform, "KWR_" + wall.Id + "_Stone");
            root.SetActive(true);
            s_Built[wall.Id] = root;
            Vector3 middle = World(wall.CenterX, wall.CenterY);
            if (proxy == null)
            {
                proxy = Game.Instance.EntityCreator.SpawnUnit(WardBlueprints.WallFixture, ObstacleAnalyzer.GetNearestNode(middle).clampedPosition, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
                proxy.Descriptor.Ensure<WardFixturePart>().FixtureId = wall.Id;
                WardFixtures.Live[wall.Id] = proxy;
            }
            else if (!proxy.IsInGame)
            {
                proxy.IsInGame = true;
            }
        }

        private static Vector3 Direction(WallRecord wall)
        {
            return new Vector3(SanctumPlan.ToWorldX(wall.CenterX + wall.DirX) - SanctumPlan.ToWorldX(wall.CenterX), 0f, SanctumPlan.ToWorldZ(wall.CenterY + wall.DirY) - SanctumPlan.ToWorldZ(wall.CenterY)).normalized;
        }

        private static Vector3 World(float x, float y)
        {
            return new Vector3(SanctumPlan.ToWorldX(x), SanctumPlan.HeightAt(x, y) ?? 0f, SanctumPlan.ToWorldZ(y));
        }
    }
}
