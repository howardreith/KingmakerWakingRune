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
            Vector3 node = ObstacleAnalyzer.GetNearestNode(p).clampedPosition;
            return (new Vector2(node.x, node.z) - new Vector2(p.x, p.z)).magnitude < 0.3f;
        }

        /// <summary>A teleport destination: floor, and clear of every creature but the mover (corpulence plus a margin).</summary>
        public static bool LegalDestination(Vector3 p, UnitEntityData mover)
        {
            if (!OnFloor(p))
            {
                return false;
            }
            float margin = mover != null ? mover.Corpulence : 0.4f;
            return Game.Instance.State.Units.All(u => u == mover || !u.IsInGame || u.Descriptor.State.IsDead || WardFixtures.IdOf(u) != null
                || Vector2.Distance(new Vector2(u.Position.x, u.Position.z), new Vector2(p.x, p.z)) > u.Corpulence + margin + 0.2f);
        }

        /// <summary>
        /// The legal floor point farthest from every foe (and from where he stands), from a fixed grid over the sanctum, within
        /// a maximum distance from him (0: anywhere in the sanctum) and, when asked, in his line of sight. Deterministic; null when nowhere is legal.
        /// </summary>
        public static Vector3? Retreat(UnitEntityData krune, IList<UnitEntityData> foes, float maxMeters, bool needSight = false)
        {
            Vector3? best = null;
            float bestScore = float.MinValue;
            for (float y = 1f; y <= 34f; y += 1f)
            {
                for (float x = 1f; x <= 26f; x += 1f)
                {
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
                    float score = nearestFoe + 0.1f * fromHim;
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
