using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using KingmakerWakingRune.Sanctum.Wards;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>Where Krune (and his walls) may legally be: sanctum floor, on the navmesh, outside the coffin, clear of creatures.</summary>
    internal static class KruneMoves
    {
        public static float GridX(Vector3 p)
        {
            return p.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX;
        }

        public static float GridY(Vector3 p)
        {
            return SanctumPlan.OriginY - p.z / SanctumPlan.MetersPerSquare;
        }

        public static Vector3 World(float x, float y)
        {
            return new Vector3(SanctumPlan.ToWorldX(x), SanctumPlan.HeightAt(x, y) ?? 0f, SanctumPlan.ToWorldZ(y));
        }

        /// <summary>Real sanctum floor at this point: a mapped space with a floor height, outside the coffin, on the navmesh.</summary>
        public static bool OnFloor(Vector3 p)
        {
            float x = GridX(p);
            float y = GridY(p);
            if (SanctumPlan.SpaceAt(x, y) == SanctumSpace.None || !SanctumPlan.HeightAt(x, y).HasValue || KruneRules.InCoffin(x, y))
            {
                return false;
            }
            var nearest = ObstacleAnalyzer.GetNearestNode(p);
            if (nearest.node == null || !nearest.node.Walkable || Mathf.Abs(nearest.clampedPosition.y - p.y) > 0.45f) return false;
            Vector3 node = nearest.clampedPosition;
            return (new Vector2(node.x, node.z) - new Vector2(p.x, p.z)).magnitude < 0.3f;
        }

        /// <summary>A teleport destination: floor, and clear of every creature but the mover (corpulence plus a margin).</summary>
        public static bool LegalDestination(Vector3 p, UnitEntityData mover)
        {
            if (!OnFloor(p))
            {
                return false;
            }
            // Reserve a large creature's footprint for a summon before its exact native variant is constructed.
            float margin = mover != null ? mover.Corpulence : 1.05f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 edge = p + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * margin;
                if (!OnFloor(edge)) return false;
            }
            if (SanctumPlan.Doors.Any(d => SanctumPlan.DoorwayOverlap(d, GridX(p), GridY(p), p.y, (margin + 0.8f) / SanctumPlan.MetersPerSquare))) return false;
            return Game.Instance.State.Units.All(u => u == mover || !u.IsInGame || u.Descriptor.State.IsDead || WardFixtures.IdOf(u) != null
                || Vector2.Distance(new Vector2(u.Position.x, u.Position.z), new Vector2(p.x, p.z)) > u.Corpulence + margin + 0.2f);
        }

        // Centres in A1, the north/east/south/west arcs of A3, the A4 altar chamber and the A5 coffin chamber.
        // Nine candidate landings per centre keep searches bounded even when summons crowd one room.
        private static readonly GridPoint[] Anchors =
        {
            new GridPoint(12f, 14.8f), new GridPoint(14.7f, 15f), new GridPoint(13f, 2.5f),
            new GridPoint(22.5f, 12f), new GridPoint(13f, 28f), new GridPoint(3.5f, 12f),
            new GridPoint(16.7f, 10.2f), new GridPoint(9.8f, 11f)
        };

        public static Vector3? Retreat(UnitEntityData krune, IList<UnitEntityData> foes, float maxMeters, bool needSight = false)
        {
            Vector3? best = null;
            float bestScore = float.MinValue;
            float startDistance = foes.Count == 0 ? 0f : foes.Min(f => f.DistanceTo(krune));
            foreach (GridPoint anchor in Anchors)
            {
                for (int i = 0; i <= 8; i++)
                {
                    float angle = i * Mathf.PI / 4f;
                    float x = anchor.X + (i == 8 ? 0f : Mathf.Cos(angle));
                    float y = anchor.Y + (i == 8 ? 0f : Mathf.Sin(angle));
                    if (!SanctumPlan.HeightAt(x, y).HasValue || SanctumPlan.SpaceAt(x, y) == SanctumSpace.None)
                    {
                        continue;
                    }
                    Vector3 p = World(x, y);
                    float fromHim = Vector3.Distance(p, krune.Position);
                    if (maxMeters > 0f && fromHim > maxMeters)
                    {
                        continue;
                    }
                    if (needSight && Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance != null && Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance.HasObstacle(krune.EyePosition, p))
                    {
                        continue;
                    }
                    float nearestFoe = foes.Count == 0 ? 50f : foes.Min(f => Vector3.Distance(f.Position, p));
                    if (fromHim < 2f || nearestFoe <= startDistance + 1f) continue;
                    var previous = WardRuntime.State.Krune;
                    float repeatPenalty = previous.HasRetreat && Mathf.Abs(GridX(p) - previous.RetreatX) < 2f && Mathf.Abs(GridY(p) - previous.RetreatY) < 2f ? 10f : 0f;
                    float score = nearestFoe + 0.1f * fromHim - repeatPenalty;
                    if (score > bestScore && LegalDestination(p, krune))
                    {
                        best = p;
                        bestScore = score;
                    }
                }
            }
            return best;
        }
    }
}
