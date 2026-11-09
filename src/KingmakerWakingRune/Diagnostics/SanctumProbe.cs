using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual;
using Kingmaker.Visual.FogOfWar;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Sanctum;
using UnityEngine;

namespace KingmakerWakingRune.Diagnostics
{
    /// <summary>
    /// Read-only sanctum facts for the test harness: door state, each unit's footprint, live fog and sight at fixed
    /// points of each room, and an audit of the real upper surface meshes. Reached only through <see cref="HarnessProbe"/>.
    /// </summary>
    internal static class SanctumProbe
    {
        private const float SightRange = 11.7f;

        private static readonly (string Name, float X, float Y)[] Samples =
        {
            ("A3-south", 13f, 28f), ("A3-sw", 6f, 21f), ("A3-se", 20f, 21f), ("A3-west", 3.5f, 12f), ("A3-east", 22.5f, 12f), ("A3-north", 13f, 2.5f),
            ("A4-passage", 19.5f, 14.6f), ("A4-centre", 18.2f, 9.5f), ("A4-altar", 14f, 7.5f), ("A5", 8f, 12f),
            ("A1-floor", 13f, 15f), ("A2-stairs", 13.15f, 25f), ("terrace-sw", 10.3f, 22.7f), ("terrace-se", 15.7f, 22.7f), ("apex", 13f, 19.6f),
            ("behind-A2", 13.15f, 27.6f), ("behind-east", 19.8f, 14.6f), ("before-east", 22.2f, 14.6f),
        };

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"sanctum: built={SanctumScope.IsBuilt} doorsReady={SanctumDoors.AreReady} statues={SanctumScope.StatueCount}");
            foreach (SanctumDoor door in Object.FindObjectsOfType<SanctumDoor>())
            {
                UnitEntityData blocker = door.Data != null ? SanctumDoors.FindInSwing(door.Plan) : null;
                sb.AppendLine($"door {door.Plan.Id}: open={door.IsOpen} sightBlocker={door.Blocker.enabled} closeRefusedBy={blocker?.CharacterName ?? "-"}");
            }
            foreach (UnitEntityData unit in Game.Instance.State.Units.Where(u => u.IsInGame && u.View != null))
            {
                Vector3 p = unit.Position;
                sb.AppendLine($"unit '{unit.CharacterName}' grid=({Gx(p):0.00},{Gy(p):0.00}) h={p.y:0.00} corpulence={unit.View.Corpulence:0.00} space={SanctumPlan.SpaceAt(Gx(p), Gy(p))} inFog={unit.IsInFogOfWar}");
            }
            AppendFog(sb);
            AppendSurfaceAudit(sb);
            return sb.ToString();
        }

        private static float Gx(Vector3 p)
        {
            return p.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX;
        }

        private static float Gy(Vector3 p)
        {
            return SanctumPlan.OriginY - p.z / SanctumPlan.MetersPerSquare;
        }

        private static void AppendFog(StringBuilder sb)
        {
            LocationMaskRenderer mask = LocationMaskRenderer.Instance;
            if (mask == null)
            {
                sb.AppendLine("fog: no mask renderer");
                return;
            }
            Texture2D fog = mask.GetDynamicFogOfWarMask();
            Bounds bounds = mask.Bounds;
            Vector4 st = mask.CalculateMaskST();
            float eye = mask.FogOfWar.ShadowCullingHeightOffset;
            List<Transform> revealers = Game.Instance.FogOfWar.Revealers;
            sb.AppendLine($"fog: bounds={bounds} tex={fog?.width} revealers={revealers.Count} blockers={FogOfWarBlocker.All.Count}");
            foreach ((string name, float x, float y) in Samples)
            {
                float? h = SanctumPlan.HeightAt(x, y);
                var world = new Vector3(SanctumPlan.ToWorldX(x), h ?? 0f, SanctumPlan.ToWorldZ(y));
                int clear = 0, inRange = 0;
                foreach (Transform revealer in revealers)
                {
                    if (revealer == null || (new Vector2(revealer.position.x - world.x, revealer.position.z - world.z)).magnitude > SightRange)
                    {
                        continue;
                    }
                    inRange++;
                    if (!LineOfSightGeometry.Instance.HasObstacle(revealer.position + Vector3.up * eye, world + Vector3.up * eye))
                    {
                        clear++;
                    }
                }
                Color pixel = fog != null ? fog.GetPixelBilinear(world.x * st.x + st.z, world.z * st.y + st.w) : Color.clear;
                sb.AppendLine($"fog {name}: visibleNow={pixel.r:0.00} explored={pixel.g:0.00} revealersInRange={inRange} withClearLine={clear}");
            }
            if (fog != null)
            {
                Object.Destroy(fog);
            }
        }

        private struct FlatTriangle
        {
            public Vector2 A, B, C;
            public float Height;
            public string Owner;
        }

        /// <summary>Counts positive-area coplanar overlaps among the real, generated upward surfaces above the terrace.</summary>
        private static void AppendSurfaceAudit(StringBuilder sb)
        {
            var triangles = new List<FlatTriangle>();
            foreach (MeshFilter filter in Object.FindObjectsOfType<MeshFilter>())
            {
                if (!filter.name.StartsWith("Floor_") && !filter.name.StartsWith("Ashlar_"))
                {
                    continue;
                }
                Mesh mesh = filter.sharedMesh;
                Vector3[] v = mesh.vertices;
                int[] t = mesh.triangles;
                for (int i = 0; i < t.Length; i += 3)
                {
                    Vector3 a = filter.transform.TransformPoint(v[t[i]]), b = filter.transform.TransformPoint(v[t[i + 1]]), c = filter.transform.TransformPoint(v[t[i + 2]]);
                    Vector3 n = Vector3.Cross(b - a, c - a);
                    if (n.y > 0f && n.y > 0.99f * n.magnitude && a.y >= SanctumPlan.RingHeight - 0.01f && n.magnitude > 0.000001f)
                    {
                        triangles.Add(new FlatTriangle { A = new Vector2(a.x, a.z), B = new Vector2(b.x, b.z), C = new Vector2(c.x, c.z), Height = a.y, Owner = filter.name });
                    }
                }
            }
            int overlaps = 0;
            float area = 0f;
            for (int i = 0; i < triangles.Count; i++)
            {
                for (int j = i + 1; j < triangles.Count; j++)
                {
                    if (Mathf.Abs(triangles[i].Height - triangles[j].Height) < 0.002f)
                    {
                        float shared = OverlapArea(triangles[i], triangles[j]);
                        if (shared > 0.00005f)
                        {
                            overlaps++;
                            area += shared;
                            if (overlaps <= 6)
                            {
                                sb.AppendLine($"overlap {triangles[i].Owner}/{triangles[j].Owner} h={triangles[i].Height:0.000} area={shared:0.0000}");
                            }
                        }
                    }
                }
            }
            sb.AppendLine($"surfaceAudit: triangles={triangles.Count} coplanarOverlaps={overlaps} sharedArea={area:0.0000}");
        }

        private static float OverlapArea(FlatTriangle p, FlatTriangle q)
        {
            var polygon = new List<Vector2> { p.A, p.B, p.C };
            var clip = new[] { q.A, q.B, q.C };
            float sign = Mathf.Sign(Cross(clip[1] - clip[0], clip[2] - clip[0]));
            for (int e = 0; e < 3 && polygon.Count > 0; e++)
            {
                Vector2 a = clip[e], b = clip[(e + 1) % 3];
                var kept = new List<Vector2>();
                for (int i = 0; i < polygon.Count; i++)
                {
                    Vector2 s = polygon[i], f = polygon[(i + 1) % polygon.Count];
                    float ds = sign * Cross(b - a, s - a), df = sign * Cross(b - a, f - a);
                    if (ds >= 0f)
                    {
                        kept.Add(s);
                    }
                    if (ds * df < 0f)
                    {
                        kept.Add(s + (f - s) * (ds / (ds - df)));
                    }
                }
                polygon = kept;
            }
            float total = 0f;
            for (int i = 1; i + 1 < polygon.Count; i++)
            {
                total += Cross(polygon[i] - polygon[0], polygon[i + 1] - polygon[0]);
            }
            return Mathf.Abs(total) / 2f;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }
}
