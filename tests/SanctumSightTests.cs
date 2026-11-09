using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Tests
{
    /// <summary>
    /// A model of the native two-dimensional sight test over the plan's blockers: no walkable point may see into a
    /// neighbouring space while the door between them is closed. Runtime fog is proved separately in the game.
    /// </summary>
    public static class SanctumSightTests
    {
        private struct Segment
        {
            public GridPoint A, B;
        }

        private static List<Segment> Blockers(bool includeDoors)
        {
            var segments = new List<Segment>();
            foreach (WallRun line in SanctumPlan.SightLines)
            {
                int count = line.Closed ? line.Points.Length : line.Points.Length - 1;
                for (int i = 0; i < count; i++)
                {
                    if (!line.Openings.Contains(i))
                    {
                        segments.Add(new Segment { A = line.Points[i], B = line.Points[(i + 1) % line.Points.Length] });
                    }
                }
            }
            if (includeDoors)
            {
                segments.AddRange(SanctumPlan.Doors.Select(d => new Segment { A = d.Hinge, B = d.Latch }));
            }
            return segments;
        }

        private static bool Crosses(GridPoint p, GridPoint q, Segment s)
        {
            double d1 = Side(s.A, s.B, p), d2 = Side(s.A, s.B, q), d3 = Side(p, q, s.A), d4 = Side(p, q, s.B);
            return d1 * d2 <= 0.0 && d3 * d4 <= 0.0 && !(d1 == 0.0 && d2 == 0.0);
        }

        private static double Side(GridPoint a, GridPoint b, GridPoint p)
        {
            return (double)(b.X - a.X) * (p.Y - a.Y) - (double)(b.Y - a.Y) * (p.X - a.X);
        }

        private static List<GridPoint> Walkable(SanctumSpace space, float step)
        {
            var points = new List<GridPoint>();
            for (float x = 2f; x <= 24f; x += step)
            {
                for (float y = 1f; y <= 30f; y += step)
                {
                    // Nudged off the grid so samples do not land exactly on plan vertices.
                    float px = x + 0.013f, py = y + 0.017f;
                    if (SanctumPlan.HeightAt(px, py).HasValue && SanctumPlan.SpaceAt(px, py) == space)
                    {
                        points.Add(new GridPoint(px, py));
                    }
                }
            }
            return points;
        }

        private static GridPoint? FindLeak(List<GridPoint> viewers, List<GridPoint> targets, List<Segment> blockers)
        {
            foreach (GridPoint viewer in viewers)
            {
                foreach (GridPoint target in targets)
                {
                    bool blocked = false;
                    foreach (Segment s in blockers)
                    {
                        if (Crosses(viewer, target, s))
                        {
                            blocked = true;
                            break;
                        }
                    }
                    if (!blocked)
                    {
                        return viewer;
                    }
                }
            }
            return null;
        }

        private static void AssertSealed(SanctumSpace a, SanctumSpace b)
        {
            GridPoint? leak = FindLeak(Walkable(a, 0.6f), Walkable(b, 0.9f), Blockers(true));
            if (leak.HasValue)
            {
                throw new Exception($"{a} sees into {b} from ({leak.Value.X}, {leak.Value.Y}).");
            }
        }

        public static void TestInteriorCannotSeeHallThroughClosedDoors()
        {
            AssertSealed(SanctumSpace.Interior, SanctumSpace.Hall);
        }

        public static void TestHallCannotSeeAtriumThroughClosedDoors()
        {
            AssertSealed(SanctumSpace.Hall, SanctumSpace.Atrium);
        }

        public static void TestInteriorCannotSeeAtrium()
        {
            AssertSealed(SanctumSpace.Interior, SanctumSpace.Atrium);
        }

        public static void TestTerraceCornersAreInsideTheSightLine()
        {
            List<Segment> blockers = Blockers(true);
            List<GridPoint> hall = Walkable(SanctumSpace.Hall, 0.5f);
            var corners = new[] { new GridPoint(10.36f, 22.64f), new GridPoint(15.64f, 22.64f), new GridPoint(10.2f, 22.8f), new GridPoint(15.8f, 22.8f) };
            GridPoint? leak = FindLeak(corners.ToList(), hall, blockers);
            Assert.Equal(false, leak.HasValue, "terrace corner sees the hall");
        }

        public static void TestTheDrawnWallAloneWouldLeakAtTheCorners()
        {
            var drawn = new List<Segment>();
            GridPoint[] core = SanctumPlan.Core;
            for (int i = 0; i < core.Length; i++)
            {
                drawn.Add(new Segment { A = core[i], B = core[(i + 1) % core.Length] });
            }
            var corners = new List<GridPoint> { new GridPoint(10.2f, 22.8f) };
            Assert.Equal(true, FindLeak(corners, Walkable(SanctumSpace.Hall, 0.5f), drawn).HasValue, "the cause of the owner's report");
        }

        public static void TestSightLinesAreTracedInBothDirections()
        {
            var run = new[] { new GridPoint(1f, 1f), new GridPoint(2f, 1f), new GridPoint(2f, 3f) };
            List<GridPoint> traced = SanctumPlan.TracedBothWays(run);
            Assert.Equal(5, traced.Count, "out and back share the turning point");
            var edges = new HashSet<string>();
            for (int i = 0; i + 1 < traced.Count; i++)
            {
                edges.Add($"{traced[i].X},{traced[i].Y}>{traced[i + 1].X},{traced[i + 1].Y}");
            }
            Assert.Equal(true, edges.Contains("1,1>2,1") && edges.Contains("2,1>1,1"), "first edge both ways");
            Assert.Equal(true, edges.Contains("2,1>2,3") && edges.Contains("2,3>2,1"), "second edge both ways");
        }

        public static void TestOpenDoorsDoLetSightThrough()
        {
            var viewer = new List<GridPoint> { new GridPoint(13.15f, 25.9f) };
            var target = new List<GridPoint> { new GridPoint(13.15f, 27.5f) };
            Assert.Equal(false, FindLeak(viewer, target, Blockers(true)).HasValue, "closed A2 blocks");
            Assert.Equal(true, FindLeak(viewer, target, Blockers(false)).HasValue, "open A2 does not");
        }
    }
}
