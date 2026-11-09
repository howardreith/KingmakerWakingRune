using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerWakingRune.Sanctum
{
    internal struct GridPoint
    {
        public readonly float X;
        public readonly float Y;

        public GridPoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    internal sealed class WalkRegion
    {
        public string Name;
        public GridPoint[] Outer;
        public List<GridPoint[]> Holes = new List<GridPoint[]>();
        public float Height;
    }

    /// <summary>A sloped rectangle: height runs linearly from From to To along Y, or along X when AlongX is set.</summary>
    internal sealed class Ramp
    {
        public string Name;
        public float X0, Y0, X1, Y1;
        public bool AlongX;
        public float From, To, HeightFrom, HeightTo;
        /// <summary>Squares kept unwalkable along each side of a flight, though its steps are still drawn there.</summary>
        public float SideGap;

        public float WalkX0 => Math.Min(X0, X1) + (AlongX ? 0f : SideGap);
        public float WalkX1 => Math.Max(X0, X1) - (AlongX ? 0f : SideGap);

        public bool Contains(float x, float y)
        {
            return x >= WalkX0 && x <= WalkX1 && y >= Math.Min(Y0, Y1) && y <= Math.Max(Y0, Y1);
        }

        public float HeightAt(float x, float y)
        {
            float t = ((AlongX ? x : y) - From) / (To - From);
            t = Math.Max(0f, Math.Min(1f, t));
            return HeightFrom + (HeightTo - HeightFrom) * t;
        }
    }

    internal sealed class WallRun
    {
        public string Name;
        public GridPoint[] Points;
        public bool Closed;
        /// <summary>Segments left open (door and passage mouths), by the index of their first point.</summary>
        public HashSet<int> Openings = new HashSet<int>();
    }

    internal sealed class RunePlate
    {
        public string Id;
        public string Rune;
        public GridPoint Position;
        public GridPoint InwardNormal;
        /// <summary>Degrees the plate leans back into its wall so the fixed camera can read it.</summary>
        public float Tilt;
        /// <summary>Metres the foot of the plate's face stands forward of the wall line.</summary>
        public float Depth;
    }

    /// <summary>One of the two mapped doors. Seen from above, opening turns the leaf clockwise about its hinge.</summary>
    internal sealed class DoorPlan
    {
        public string Id;
        public GridPoint Hinge;
        public GridPoint Latch;
        public float OpenDegrees;
        public bool Sihedron;

        public GridPoint Center => new GridPoint((Hinge.X + Latch.X) / 2f, (Hinge.Y + Latch.Y) / 2f);
    }

    /// <summary>
    /// The page-6 plan in source grid squares (x east, y south, one square = five feet). The measured grid of
    /// references/sanctum-map-crop.png is 40.6 pixels per square with its origin at (20.1, 38.95). Corridor widths
    /// are the printed 15 feet; every deliberate difference from the drawing is listed in docs/WR-03-SANCTUM.md.
    /// No game types, so it is verified directly by the unit tests.
    /// </summary>
    internal static class SanctumPlan
    {
        public const float MetersPerFoot = 0.3048f;
        public const float FeetPerSquare = 5f;
        public const float MetersPerSquare = MetersPerFoot * FeetPerSquare;
        public const float OriginX = 13f;
        public const float OriginY = 15.5f;

        /// <summary>Inferred: the source gives only the 20-foot apex. Six feet keeps the north flight short and steep.</summary>
        public const float RingHeight = 6f * MetersPerFoot;
        public const float ApexHeight = 20f * MetersPerFoot;

        /// <summary>The printed "PCs Arrive Here" rectangle, cut off where the built stairs begin.</summary>
        public const float ArrivalWest = 11.09f;
        public const float ArrivalEast = 15.16f;
        public const float ArrivalNorth = 13.9f;
        public const float ArrivalSouth = 15.4f;

        public static readonly GridPoint Arrival = new GridPoint(13.1f, 15.1f);

        /// <summary>
        /// Native preset positions after <see cref="Arrival"/>, in party order: two staggered rows on the chamber floor.
        /// The first eight lie inside the printed rectangle; the rest spread onto the same floor for a party of six with six pets.
        /// </summary>
        public static readonly GridPoint[] ArrivalSlots = Points(
            12.2f, 15.1f, 14f, 15.1f, 12.65f, 14.62f, 13.55f, 14.62f, 11.75f, 14.62f, 14.45f, 14.62f, 11.3f, 15.1f, 14.9f, 15.1f,
            10.85f, 14.62f, 15.35f, 14.62f, 10.4f, 15.1f, 15.8f, 15.1f);

        public const float ZigguratWest = 10f;
        public const float ZigguratEast = 16f;
        public const float ZigguratNorth = 16.2f;
        public const float ZigguratSouth = 23f;
        public const float RingInnerWest = 11.3f;
        public const float RingInnerEast = 14.7f;
        public const float RingInnerNorth = 17.5f;
        public const float RingInnerSouth = 21.7f;
        public const float ApexWest = 12.5f;
        public const float ApexEast = 13.5f;
        public const float ApexNorth = 18.7f;
        public const float ApexSouth = 20.5f;

        public const float NorthStairWest = 12.07f;
        public const float NorthStairEast = 14.16f;
        public const float NorthStairFoot = 15.4f;
        /// <summary>The flight runs into the first tier, which eases it to 45 degrees and leaves room behind its head.</summary>
        public const float NorthStairHead = 16.6f;
        public const float SideStairNorth = 18.95f;
        public const float SideStairSouth = 20.25f;
        public const float WestStairFoot = 10.85f;
        public const float EastStairFoot = 15.15f;

        /// <summary>Half the depth of a door's sealed strip, in squares.</summary>
        public const float DoorwayHalfDepth = 0.2f;

        public static readonly DoorPlan[] Doors =
        {
            new DoorPlan { Id = "A2", Hinge = new GridPoint(12.4f, 26.4f), Latch = new GridPoint(13.9f, 26.4f), OpenDegrees = 100f, Sihedron = true },
            new DoorPlan { Id = "A4Approach", Hinge = new GridPoint(21f, 13.9f), Latch = new GridPoint(21f, 15.4f), OpenDegrees = 90f },
        };

        public static readonly GridPoint[] Outer = Points(8, 1, 18, 1, 24, 7, 24, 18, 13, 30, 2, 18, 2, 7);

        /// <summary>The solid core inside the A3 ring, with the east door to A4 and the A2 stair block drawn into it.</summary>
        public static readonly GridPoint[] Core = Points(9.24f, 4, 16.76f, 4, 21, 8.24f, 21, 13.9f, 21, 15.4f, 21, 16.83f, 14.3f, 24.14f, 14.3f, 26.4f, 13.9f, 26.4f, 12.4f, 26.4f, 12, 26.4f, 12, 24.14f, 5, 16.83f, 5, 8.24f);

        public static readonly GridPoint[] Atrium = BuildAtrium();

        public static readonly GridPoint[] ArrivalChamber = BuildArrivalChamber();

        public static readonly GridPoint[] Altar = Rect(14.4f, 8.84f, 15.2f, 10.3f);
        public static readonly GridPoint[] BrazierNorth = Octagon(15.62f, 9.07f, 0.22f);
        public static readonly GridPoint[] BrazierSouth = Octagon(15.62f, 10.1f, 0.22f);
        public static readonly GridPoint[] Pedestal = Octagon(19.7f, 10.9f, 0.27f);
        public static readonly GridPoint[] SarcophagusShadow = Rect(8.07f, 8.98f, 9.67f, 9.94f);

        public static readonly GridPoint PortalRecess = new GridPoint(20.6f, 9.65f);
        public static readonly GridPoint SarcophagusCenter = new GridPoint(8.87f, 9.46f);
        public static readonly GridPoint[] ChainAnchors = Points(6.8f, 7.3f, 12.6f, 6.06f, 6.65f, 11.75f, 12.8f, 13.08f);

        public static readonly List<WalkRegion> Regions = new List<WalkRegion>
        {
            new WalkRegion { Name = "A3", Outer = Outer, Holes = { Core }, Height = 0f },
            new WalkRegion { Name = "A4A5", Outer = Atrium, Holes = { Altar, BrazierNorth, BrazierSouth, Pedestal, SarcophagusShadow }, Height = 0f },
            new WalkRegion { Name = "A1", Outer = ArrivalChamber, Height = 0f },
            new WalkRegion { Name = "ZigguratRing", Outer = Points(ZigguratWest, ZigguratNorth, NorthStairWest, ZigguratNorth, NorthStairWest, NorthStairHead, NorthStairEast, NorthStairHead, NorthStairEast, ZigguratNorth, ZigguratEast, ZigguratNorth, ZigguratEast, ZigguratSouth, ZigguratWest, ZigguratSouth), Holes = { Rect(RingInnerWest, RingInnerNorth, RingInnerEast, RingInnerSouth) }, Height = RingHeight },
            new WalkRegion { Name = "ZigguratApex", Outer = Rect(ApexWest, ApexNorth, ApexEast, ApexSouth), Height = ApexHeight },
            new WalkRegion { Name = "A2Threshold", Outer = Rect(12.4f, 26.2f, 13.9f, 26.6f), Height = 0f },
        };

        public static readonly List<Ramp> Ramps = new List<Ramp>
        {
            new Ramp { Name = "ZigguratNorthStairs", X0 = NorthStairWest, Y0 = NorthStairFoot, X1 = NorthStairEast, Y1 = NorthStairHead, From = NorthStairFoot, To = NorthStairHead, HeightFrom = 0f, HeightTo = RingHeight },
            new Ramp { Name = "ZigguratWestStairs", X0 = WestStairFoot, Y0 = SideStairNorth, X1 = ApexWest, Y1 = SideStairSouth, AlongX = true, From = WestStairFoot, To = ApexWest, HeightFrom = RingHeight, HeightTo = ApexHeight },
            new Ramp { Name = "ZigguratEastStairs", X0 = ApexEast, Y0 = SideStairNorth, X1 = EastStairFoot, Y1 = SideStairSouth, AlongX = true, From = EastStairFoot, To = ApexEast, HeightFrom = RingHeight, HeightTo = ApexHeight },
            // The flight's lower half is within a step of the A3 floor beyond its walls; the gap keeps a closed door from being walked around.
            new Ramp { Name = "A2Stairs", X0 = 12f, Y0 = 23f, X1 = 14.3f, Y1 = 26.2f, From = 23f, To = 26.2f, HeightFrom = RingHeight, HeightTo = 0f, SideGap = 0.25f },
        };

        public static readonly List<RunePlate> RunePlates = new List<RunePlate>
        {
            new RunePlate { Id = "A3a", Rune = "Life", Position = new GridPoint(21.8f, 20.4f), InwardNormal = new GridPoint(-0.737f, -0.676f), Tilt = 14f, Depth = 0.72f },
            new RunePlate { Id = "A3b", Rune = "Alacrity", Position = new GridPoint(21f, 13.2f), InwardNormal = new GridPoint(1f, 0f), Tilt = 36f, Depth = 0.6f },
            new RunePlate { Id = "A3c", Rune = "Defense", Position = new GridPoint(22.3f, 5.3f), InwardNormal = new GridPoint(-0.707f, 0.707f), Tilt = 56f, Depth = 0.6f },
            new RunePlate { Id = "A3d", Rune = "Life", Position = new GridPoint(13f, 4f), InwardNormal = new GridPoint(0f, -1f), Tilt = 6f, Depth = 0.4f },
            new RunePlate { Id = "A3e", Rune = "Armor", Position = new GridPoint(3.7f, 5.3f), InwardNormal = new GridPoint(0.707f, 0.707f), Tilt = 56f, Depth = 0.6f },
            new RunePlate { Id = "A3f", Rune = "Control", Position = new GridPoint(5f, 13.2f), InwardNormal = new GridPoint(-1f, 0f), Tilt = 36f, Depth = 0.6f },
            new RunePlate { Id = "A3g", Rune = "Life", Position = new GridPoint(4.2f, 20.4f), InwardNormal = new GridPoint(0.737f, -0.676f), Tilt = 14f, Depth = 0.72f },
        };

        public static readonly List<WallRun> Walls = BuildWalls();

        public static float ToWorldX(float gridX)
        {
            return (gridX - OriginX) * MetersPerSquare;
        }

        /// <summary>North (smaller grid Y) is +Z in the world.</summary>
        public static float ToWorldZ(float gridY)
        {
            return (OriginY - gridY) * MetersPerSquare;
        }

        /// <summary>Walkable surface height at a grid point, or null over solid rock and non-walkable faces.</summary>
        public static float? HeightAt(float x, float y)
        {
            foreach (Ramp ramp in Ramps)
            {
                if (ramp.Contains(x, y))
                {
                    return ramp.HeightAt(x, y);
                }
            }
            float? best = null;
            foreach (WalkRegion region in Regions)
            {
                if (Inside(region.Outer, x, y) && !region.Holes.Any(h => Inside(h, x, y)))
                {
                    best = best.HasValue ? Math.Max(best.Value, region.Height) : region.Height;
                }
            }
            return best;
        }

        /// <summary>Whether a grid point lies in a door's sealed strip, widened by a margin in squares.</summary>
        public static bool InDoorway(DoorPlan door, float x, float y, float margin = 0f)
        {
            float halfDepth = DoorwayHalfDepth + margin;
            float x0 = Math.Min(door.Hinge.X, door.Latch.X), x1 = Math.Max(door.Hinge.X, door.Latch.X);
            float y0 = Math.Min(door.Hinge.Y, door.Latch.Y), y1 = Math.Max(door.Hinge.Y, door.Latch.Y);
            return door.Hinge.Y == door.Latch.Y
                ? x >= x0 - margin && x <= x1 + margin && Math.Abs(y - y0) <= halfDepth
                : y >= y0 - margin && y <= y1 + margin && Math.Abs(x - x0) <= halfDepth;
        }

        public static bool Inside(GridPoint[] polygon, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                GridPoint a = polygon[i];
                GridPoint b = polygon[j];
                if (a.Y > y != b.Y > y && x < (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// Cutaway height of a wall seen by a camera looking along +Y at the native 43-degree pitch: a wall in front
        /// of its own floor stays low; a wall behind a floor rises only as far as it can without hiding the next walkable floor.
        /// </summary>
        public static float CutawayHeight(GridPoint a, GridPoint b, float low, float high)
        {
            const float pitchTangent = 0.9325f;
            float best = high;
            for (int i = 0; i <= 4; i++)
            {
                float t = i / 4f;
                float x = a.X + (b.X - a.X) * t;
                float y = a.Y + (b.Y - a.Y) * t;
                if (HeightAt(x, y + 0.08f).HasValue)
                {
                    return low;
                }
                float distance = high;
                for (float d = 0.25f; d < 8f; d += 0.25f)
                {
                    if (HeightAt(x, y + d).HasValue)
                    {
                        distance = d * MetersPerSquare * pitchTangent * 0.9f;
                        break;
                    }
                }
                best = Math.Min(best, distance);
            }
            return Math.Max(low, Math.Min(high, best));
        }

        private static List<WallRun> BuildWalls()
        {
            var walls = new List<WallRun>
            {
                new WallRun { Name = "A3Outer", Points = Outer, Closed = true },
                new WallRun { Name = "A3Core", Points = Core, Closed = true, Openings = { IndexOf(Core, 21, 13.9f), IndexOf(Core, 13.9f, 26.4f) } },
                new WallRun { Name = "A4A5", Points = Atrium, Closed = true, Openings = { IndexOf(Atrium, 21.05f, 13.9f) } },
                new WallRun { Name = "A1", Points = ArrivalChamberRock(), Closed = false },
                new WallRun { Name = "A2West", Points = Points(12, 23, 12, 26.4f), Closed = false },
                new WallRun { Name = "A2East", Points = Points(14.3f, 23, 14.3f, 26.4f), Closed = false },
            };
            return walls;
        }

        private static GridPoint[] BuildArrivalChamber()
        {
            var points = new List<GridPoint>();
            points.AddRange(ArrivalChamberRock());
            points.AddRange(Points(16, 21.1f, 16, 16.2f, 14.16f, 16.2f, 14.16f, 15.4f, 12.07f, 15.4f, 12.07f, 16.2f, 10, 16.2f, 10, 21.1f));
            return points.ToArray();
        }

        /// <summary>The rounded rock outline of A1, from its south-west foot round the north to its south-east foot.</summary>
        private static GridPoint[] ArrivalChamberRock()
        {
            var points = new List<GridPoint>();
            points.Add(new GridPoint(9.7f, 21.1f));
            points.AddRange(Arc(9.7f, 20.4f, 0.7f, 0.7f, 90, 180, 4).Skip(1));
            points.AddRange(Arc(10f, 15.1f, 1f, 1f, 180, 270, 6));
            points.AddRange(Arc(16.1f, 15.1f, 1f, 1f, 270, 360, 6));
            points.AddRange(Arc(16.4f, 20.4f, 0.7f, 0.7f, 0, 90, 4));
            return points.ToArray();
        }

        private static GridPoint[] BuildAtrium()
        {
            var points = new List<GridPoint>();
            points.AddRange(Arc(9.5f, 9.57f, 3.51f, 3.51f, 90, 270, 20));
            points.AddRange(Arc(13.87f, 7.46f, 1.4f, 1.4f, 270, 360, 6));
            points.AddRange(Points(15.27f, 7.6f, 16.06f, 7.6f, 16.06f, 6.98f, 17.1f, 6.98f, 17.1f, 6.4f, 17.4f, 6.1f, 18.8f, 6.1f, 19.1f, 6.4f));
            points.AddRange(Arc(19.1f, 8.9f, 1.5f, 1.92f, 270, 360, 7));
            points.AddRange(Arc(19.1f, 10.4f, 1.5f, 1.7f, 0, 90, 7));
            points.AddRange(Points(19.1f, 13.9f, 21.05f, 13.9f, 21.05f, 15.4f, 18.1f, 15.4f, 17.6f, 14.9f, 17.6f, 12.1f, 16.06f, 12.1f, 16.06f, 11.5f, 15.27f, 11.5f));
            points.AddRange(Arc(13.87f, 11.68f, 1.4f, 1.4f, 0, 90, 6));
            return points.Distinct().ToArray();
        }

        private static int IndexOf(GridPoint[] points, float x, float y)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (Math.Abs(points[i].X - x) < 0.001f && Math.Abs(points[i].Y - y) < 0.001f)
                {
                    return i;
                }
            }
            throw new InvalidOperationException($"No plan vertex at ({x}, {y}).");
        }

        private static IEnumerable<GridPoint> Arc(float cx, float cy, float rx, float ry, float fromDegrees, float toDegrees, int steps)
        {
            for (int i = 0; i <= steps; i++)
            {
                double angle = (fromDegrees + (toDegrees - fromDegrees) * i / steps) * Math.PI / 180.0;
                yield return new GridPoint(cx + rx * (float)Math.Cos(angle), cy + ry * (float)Math.Sin(angle));
            }
        }

        private static GridPoint[] Rect(float x0, float y0, float x1, float y1)
        {
            return Points(x0, y0, x1, y0, x1, y1, x0, y1);
        }

        private static GridPoint[] Octagon(float cx, float cy, float radius)
        {
            return Arc(cx, cy, radius, radius, 0, 315, 7).ToArray();
        }

        private static GridPoint[] Points(params float[] xy)
        {
            var points = new GridPoint[xy.Length / 2];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new GridPoint(xy[2 * i], xy[2 * i + 1]);
            }
            return points;
        }
    }
}
