using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker.Visual.FogOfWar;
using Pathfinding.Poly2Tri;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>Base-game materials taken from the donor scene; the donor's own objects stay hidden.</summary>
    internal sealed class SanctumMaterials
    {
        public Material Floor;
        public Material Rock;
        public Material Black;
        public SanctumPalette Palette;
    }

    /// <summary>Builds the walkable surfaces, walls and sight blockers of the plan as runtime meshes.</summary>
    internal static class SanctumBuilder
    {
        public const int GroundLayer = 8;
        private const float LowWall = 0.45f;
        /// <summary>The chamber's near wall stays a curb, so it cannot cover the feet of a party standing at the arrival.</summary>
        private const float ArrivalCurb = 0.25f;
        private const float StepRise = 0.17f;
        private const float TreadSink = 0.04f;
        private const float HighWall = 4.6f;
        private const float WallCap = 0.3f;
        private const float FloorTile = 3f;
        private const float WallTile = 2.4f;
        private const float BlockerHeight = 12f;

        public static void Build(Transform root, SanctumMaterials materials)
        {
            var floor = new ChunkedMesh();
            var collider = new ChunkedMesh();
            var rock = new ChunkedMesh();
            var black = new ChunkedMesh();
            var ashlar = new ChunkedMesh();

            foreach (WalkRegion region in SanctumPlan.Regions)
            {
                AddRegion(region, floor, collider);
            }
            foreach (Ramp ramp in SanctumPlan.Ramps)
            {
                AddRampCollider(ramp, collider);
            }
            // The sarcophagus hangs clear of the floor: its shadow is unwalkable but the floor is still there.
            GridPoint[] shadow = SanctumPlan.SarcophagusShadow;
            floor.PlanarTriangle(World(shadow[0], 0f), World(shadow[1], 0f), World(shadow[2], 0f), FloorTile);
            floor.PlanarTriangle(World(shadow[0], 0f), World(shadow[2], 0f), World(shadow[3], 0f), FloorTile);
            foreach (WallRun wall in SanctumPlan.Walls)
            {
                AddWall(wall, rock, black);
                AddBlockers(root, wall);
            }
            AddZiggurat(ashlar, floor);
            AddStairs(ashlar);
            AddZigguratBlocker(root);

            floor.Instantiate(root, "Floor", materials.Floor);
            rock.Instantiate(root, "Rock", materials.Rock);
            ashlar.Instantiate(root, "Ashlar", materials.Palette.Ashlar);
            black.Instantiate(root, "RockTop", materials.Black);

            var walkable = new GameObject("WalkableSurface") { layer = GroundLayer };
            walkable.transform.SetParent(root, false);
            Mesh surface = collider.ToSingleMesh("WalkableSurface");
            walkable.AddComponent<MeshCollider>().sharedMesh = surface;

            // This build's recast rasterizer ignores mesh colliders; a RecastMeshObj feeds it the same surface.
            var navSource = new GameObject("NavSurface");
            navSource.transform.SetParent(root, false);
            navSource.AddComponent<MeshFilter>().sharedMesh = surface;
            // Enabled with no materials: it draws nothing, but a disabled renderer reports empty bounds to the voxelizer.
            navSource.AddComponent<MeshRenderer>().sharedMaterials = new Material[0];
            navSource.AddComponent<Pathfinding.RecastMeshObj>();
        }

        private static Vector3 World(float x, float y, float height)
        {
            return new Vector3(SanctumPlan.ToWorldX(x), height, SanctumPlan.ToWorldZ(y));
        }

        private static Vector3 World(GridPoint p, float height)
        {
            return World(p.X, p.Y, height);
        }

        private static void AddRegion(WalkRegion region, ChunkedMesh floor, ChunkedMesh collider)
        {
            AddSurface(region, region.Outer, region.Holes, floor);
            AddSurface(region, region.EffectiveWalkOuter, region.EffectiveWalkHoles, collider);
        }

        private static void AddSurface(WalkRegion region, GridPoint[] outer, List<GridPoint[]> holes, ChunkedMesh mesh)
        {
            var polygon = new Polygon(outer.Select(p => new PolygonPoint(p.X, p.Y)).ToList());
            foreach (GridPoint[] hole in holes)
            {
                polygon.AddHole(new Polygon(hole.Select(p => new PolygonPoint(p.X, p.Y)).ToList()));
            }
            P2T.Triangulate(polygon);
            foreach (DelaunayTriangle triangle in polygon.Triangles)
            {
                Vector3 a = WorldOf(triangle.Points[0], region.Height);
                Vector3 b = WorldOf(triangle.Points[1], region.Height);
                Vector3 c = WorldOf(triangle.Points[2], region.Height);
                mesh.PlanarTriangle(a, b, c, FloorTile);
            }
        }

        private static Vector3 WorldOf(TriangulationPoint p, float height)
        {
            return World((float)p.X, (float)p.Y, height);
        }

        private static void AddRampCollider(Ramp ramp, ChunkedMesh collider)
        {
            Vector3 a = RampPoint(ramp, ramp.WalkX0, ramp.Y0);
            Vector3 b = RampPoint(ramp, ramp.WalkX1, ramp.Y0);
            Vector3 c = RampPoint(ramp, ramp.WalkX1, ramp.Y1);
            Vector3 d = RampPoint(ramp, ramp.WalkX0, ramp.Y1);
            collider.PlanarTriangle(a, b, c, FloorTile);
            collider.PlanarTriangle(a, c, d, FloorTile);
        }

        private static Vector3 RampPoint(Ramp ramp, float x, float y)
        {
            return World(x, y, ramp.HeightAt(x, y));
        }

        private static void AddWall(WallRun wall, ChunkedMesh rock, ChunkedMesh black)
        {
            int count = wall.Closed ? wall.Points.Length : wall.Points.Length - 1;
            for (int i = 0; i < count; i++)
            {
                if (wall.Openings.Contains(i))
                {
                    continue;
                }
                GridPoint p0 = wall.Points[i];
                GridPoint p1 = wall.Points[(i + 1) % wall.Points.Length];
                float height = wall.Name.StartsWith("A2") ? SanctumPlan.RingHeight + 1.2f : SanctumPlan.CutawayHeight(p0, p1, wall.Name == "A1" ? ArrivalCurb : LowWall, HighWall);
                AddWallSegment(p0, p1, height, rock, black);
            }
        }

        private static void AddWallSegment(GridPoint p0, GridPoint p1, float height, ChunkedMesh rock, ChunkedMesh black)
        {
            Vector3 a = World(p0, 0f);
            Vector3 b = World(p1, 0f);
            Vector3 along = b - a;
            float length = along.magnitude;
            Vector3 normal = new Vector3(-along.z, 0f, along.x).normalized;
            float midX = (p0.X + p1.X) / 2f, midY = (p0.Y + p1.Y) / 2f;
            float dx = (p1.Y - p0.Y), dy = -(p1.X - p0.X);
            float n = Mathf.Sqrt(dx * dx + dy * dy);
            bool floorOnPositive = SanctumPlan.HeightAt(midX + dx / n * 0.2f, midY + dy / n * 0.2f).HasValue;
            Vector3 toFloor = normal * (floorOnPositive ? 1f : -1f);
            // Quad winding faces the floor side; the first point order follows the walk direction.
            Vector3 up = Vector3.up * height;
            Vector2 u0 = new Vector2(0f, 0f), u1 = new Vector2(length / WallTile, 0f), u2 = new Vector2(length / WallTile, height / WallTile), u3 = new Vector2(0f, height / WallTile);
            bool flip = Vector3.Dot(Vector3.Cross(b - a, up), toFloor) < 0f;
            if (flip)
            {
                rock.Quad(a, a + up, b + up, b, u0, u3, u2, u1);
                rock.Quad(a, b, b + up, a + up, u0, u1, u2, u3);
            }
            else
            {
                rock.Quad(a, b, b + up, a + up, u0, u1, u2, u3);
                rock.Quad(a, a + up, b + up, b, u0, u3, u2, u1);
            }
            Vector3 cap = -toFloor * WallCap;
            Vector3 ta = a + up, tb = b + up;
            if (Vector3.Dot(Vector3.Cross(tb - ta, cap), Vector3.up) >= 0f)
            {
                black.Quad(ta, tb, tb + cap, ta + cap, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            else
            {
                black.Quad(ta, ta + cap, tb + cap, tb, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
            }
        }

        private static void AddZiggurat(ChunkedMesh block, ChunkedMesh floor)
        {
            float w = SanctumPlan.ZigguratWest, e = SanctumPlan.ZigguratEast, n = SanctumPlan.ZigguratNorth, s = SanctumPlan.ZigguratSouth;
            float ring = SanctumPlan.RingHeight;
            float stairWest = SanctumPlan.NorthStairWest, stairEast = SanctumPlan.NorthStairEast, head = SanctumPlan.NorthStairHead;
            Box(block, w, n, e, s, 0f, ring, east: true, west: true);
            // The north face, parted by the flight that runs back into the first tier.
            float rows = ring / WallTile;
            Face(block, World(stairWest, n, 0f), World(w, n, 0f), World(w, n, ring), World(stairWest, n, ring), rows);
            Face(block, World(e, n, 0f), World(stairEast, n, 0f), World(stairEast, n, ring), World(e, n, ring), rows);
            Face(block, World(stairWest, head, 0f), World(stairWest, n, 0f), World(stairWest, n, ring), World(stairWest, head, ring), rows);
            Face(block, World(stairEast, n, 0f), World(stairEast, head, 0f), World(stairEast, head, ring), World(stairEast, n, ring), rows);
            const int courses = 3;
            float channelNorth = SanctumPlan.SideStairNorth, channelSouth = SanctumPlan.SideStairSouth;
            for (int i = 0; i < courses; i++)
            {
                float t0 = (float)i / courses, t1 = (float)(i + 1) / courses;
                float bottom = Mathf.Lerp(ring, SanctumPlan.ApexHeight, t0), top = Mathf.Lerp(ring, SanctumPlan.ApexHeight, t1);
                float rw = Mathf.Lerp(SanctumPlan.RingInnerWest, SanctumPlan.ApexWest, t0);
                float re = Mathf.Lerp(SanctumPlan.RingInnerEast, SanctumPlan.ApexEast, t0);
                float rn = Mathf.Lerp(SanctumPlan.RingInnerNorth, SanctumPlan.ApexNorth, t0);
                float rs = Mathf.Lerp(SanctumPlan.RingInnerSouth, SanctumPlan.ApexSouth, t0);
                // Channels are left open for the east and west flights, so nobody climbing them walks inside a course.
                Box(block, rw, rn, re, channelNorth, bottom, top, north: true, east: true, west: true, south: true, topFace: true);
                Box(block, rw, channelSouth, re, rs, bottom, top, north: true, east: true, west: true, south: true, topFace: true);
                Box(block, SanctumPlan.ApexWest, channelNorth, SanctumPlan.ApexEast, channelSouth, bottom, top, east: true, west: true, topFace: true);
            }
            float aw = SanctumPlan.ApexWest, ae = SanctumPlan.ApexEast, an = SanctumPlan.ApexNorth, asouth = SanctumPlan.ApexSouth;
            Vector3 p0 = World(aw, an, SanctumPlan.ApexHeight), p1 = World(ae, an, SanctumPlan.ApexHeight), p2 = World(ae, asouth, SanctumPlan.ApexHeight), p3 = World(aw, asouth, SanctumPlan.ApexHeight);
            floor.PlanarTriangle(p0, p1, p2, FloorTile);
            floor.PlanarTriangle(p0, p2, p3, FloorTile);
        }

        private static void Box(ChunkedMesh mesh, float w, float n, float e, float s, float bottom, float top, bool north = false, bool east = false, bool west = false, bool south = false, bool topFace = false)
        {
            float h = (top - bottom) / WallTile;
            if (north)
            {
                Face(mesh, World(e, n, bottom), World(w, n, bottom), World(w, n, top), World(e, n, top), h);
            }
            if (south)
            {
                Face(mesh, World(w, s, bottom), World(e, s, bottom), World(e, s, top), World(w, s, top), h);
            }
            if (east)
            {
                Face(mesh, World(e, s, bottom), World(e, n, bottom), World(e, n, top), World(e, s, top), h);
            }
            if (west)
            {
                Face(mesh, World(w, n, bottom), World(w, s, bottom), World(w, s, top), World(w, n, top), h);
            }
            if (topFace)
            {
                Vector3 a = World(w, n, top), b = World(e, n, top), c = World(e, s, top), d = World(w, s, top);
                mesh.PlanarTriangle(a, b, c, WallTile);
                mesh.PlanarTriangle(a, c, d, WallTile);
            }
        }

        private static void Face(ChunkedMesh mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float vRepeat)
        {
            float u = (b - a).magnitude / WallTile;
            mesh.Quad(a, d, c, b, Vector2.zero, new Vector2(0f, vRepeat), new Vector2(u, vRepeat), new Vector2(u, 0f));
        }

        private static void AddStairs(ChunkedMesh block)
        {
            foreach (Ramp ramp in SanctumPlan.Ramps)
            {
                int steps = Mathf.Max(3, Mathf.RoundToInt(Mathf.Abs(ramp.HeightTo - ramp.HeightFrom) / StepRise));
                float below = ramp.HeightFrom;
                float foot = Mathf.Min(ramp.HeightFrom, ramp.HeightTo);
                for (int i = 0; i < steps; i++)
                {
                    float t0 = (float)i / steps, t1 = (float)(i + 1) / steps;
                    float from = Mathf.Lerp(ramp.From, ramp.To, t0), to = Mathf.Lerp(ramp.From, ramp.To, t1);
                    // Each tread sits a hair under the walking slope at its middle, so feet rest on the stone instead of in it.
                    float height = Mathf.Lerp(ramp.HeightFrom, ramp.HeightTo, (t0 + t1) / 2f) - TreadSink;
                    AddStep(block, ramp, from, to, below, height, foot);
                    below = height;
                }
            }
        }

        private static void AddStep(ChunkedMesh block, Ramp ramp, float from, float to, float below, float height, float foot)
        {
            Vector3 P(float along, float cross, float h)
            {
                return ramp.AlongX ? World(along, cross, h) : World(cross, along, h);
            }
            float c0 = ramp.AlongX ? Mathf.Min(ramp.Y0, ramp.Y1) : Mathf.Min(ramp.X0, ramp.X1);
            float c1 = ramp.AlongX ? Mathf.Max(ramp.Y0, ramp.Y1) : Mathf.Max(ramp.X0, ramp.X1);
            Vector3 a = P(from, c0, height), b = P(from, c1, height), c = P(to, c1, height), d = P(to, c0, height);
            block.PlanarTriangle(a, b, c, WallTile);
            block.PlanarTriangle(a, c, d, WallTile);
            Vector3 riseA = P(from, c0, below), riseB = P(from, c1, below);
            float v = Mathf.Abs(height - below) / WallTile;
            float u = (b - a).magnitude / WallTile;
            block.Quad(riseA, riseB, b, a, Vector2.zero, new Vector2(u, 0f), new Vector2(u, v), new Vector2(0f, v));
            block.Quad(riseA, a, b, riseB, Vector2.zero, new Vector2(0f, v), new Vector2(u, v), new Vector2(u, 0f));
            // Closed flanks: a flight seen from the side is solid stone, not a row of floating treads.
            AddStepFlank(block, P(from, c0, foot), P(to, c0, foot), P(to, c0, height), P(from, c0, height));
            AddStepFlank(block, P(from, c1, foot), P(to, c1, foot), P(to, c1, height), P(from, c1, height));
        }

        private static void AddStepFlank(ChunkedMesh block, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            float u = (b - a).magnitude / WallTile, v = (d - a).magnitude / WallTile;
            block.Quad(a, b, c, d, Vector2.zero, new Vector2(u, 0f), new Vector2(u, v), new Vector2(0f, v));
            block.Quad(a, d, c, b, Vector2.zero, new Vector2(0f, v), new Vector2(u, v), new Vector2(u, 0f));
        }

        private static void AddBlockers(Transform root, WallRun wall)
        {
            int count = wall.Closed ? wall.Points.Length : wall.Points.Length - 1;
            var run = new List<GridPoint>();
            int started = -1;
            for (int i = 0; i < count; i++)
            {
                if (wall.Openings.Contains(i))
                {
                    if (run.Count > 1)
                    {
                        CreateBlocker(root, $"{wall.Name}_{started}", run);
                    }
                    run = new List<GridPoint>();
                    started = -1;
                    continue;
                }
                if (run.Count == 0)
                {
                    run.Add(wall.Points[i]);
                    started = i;
                }
                run.Add(wall.Points[(i + 1) % wall.Points.Length]);
            }
            if (run.Count > 1)
            {
                CreateBlocker(root, $"{wall.Name}_{started}", run);
            }
        }

        private static void AddZigguratBlocker(Transform root)
        {
            // The block itself hides what is behind it from anyone on the chamber floor.
            CreateBlocker(root, "ZigguratBase", new List<GridPoint>
            {
                new GridPoint(SanctumPlan.ZigguratWest, SanctumPlan.ZigguratSouth),
                new GridPoint(SanctumPlan.ZigguratWest, SanctumPlan.ZigguratNorth),
                new GridPoint(SanctumPlan.ZigguratEast, SanctumPlan.ZigguratNorth),
                new GridPoint(SanctumPlan.ZigguratEast, SanctumPlan.ZigguratSouth),
            }, SanctumPlan.RingHeight * 0.5f, 1.4f);
        }

        private static void CreateBlocker(Transform root, string name, List<GridPoint> points, float y = 0f, float height = BlockerHeight)
        {
            var go = new GameObject("Blocker_" + name);
            go.SetActive(false);
            go.transform.SetParent(root, false);
            var polygon = go.AddComponent<FogOfWarBlockerPolygon>();
            polygon.Height = height;
            AccessTools.Field(typeof(Kingmaker.Utility.PolygonComponent), "m_Points").SetValue(polygon, points.Select(p => World(p, y)).ToList());
            AccessTools.Field(typeof(Kingmaker.Utility.PolygonComponent), "m_Closed").SetValue(polygon, false);
            go.SetActive(true);
        }
    }
}
