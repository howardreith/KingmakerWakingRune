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
        /// <summary>When set, replaces <see cref="Holes"/> for walking only: the floor is still drawn up to the holes.</summary>
        public List<GridPoint[]> WalkHoles;
        /// <summary>When set, replaces <see cref="Outer"/> for walking only.</summary>
        public GridPoint[] WalkOuter;
        public float Height;

        public List<GridPoint[]> EffectiveWalkHoles => WalkHoles ?? Holes;
        public GridPoint[] EffectiveWalkOuter => WalkOuter ?? Outer;
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

    /// <summary>One of the two mapped doors. Seen from above, positive degrees turn the leaf clockwise about its hinge as it opens.</summary>
    internal sealed class DoorPlan
    {
        public string Id;
        public GridPoint Hinge;
        public GridPoint Latch;
        public float OpenDegrees;
        public bool Sihedron;
        /// <summary>Metres of closed leaf a click can land on, measured through the door.</summary>
        public float ClosedHitDepth = 0.6f;

        public GridPoint Center => new GridPoint((Hinge.X + Latch.X) / 2f, (Hinge.Y + Latch.Y) / 2f);
    }

    /// <summary>Spaces separated by the mapped doors and walls; sight and safe ground do not cross between them.</summary>
    internal enum SanctumSpace
    {
        None,
        /// <summary>A1, the ziggurat and the A2 stairs.</summary>
        Interior,
        /// <summary>The A3 circuit.</summary>
        Hall,
        /// <summary>A4 and A5.</summary>
        Atrium,
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
        public const int LayoutVersion = 2;
        public const float HorizontalScale = 1.15f;
        public const float PrintedSquareMeters = MetersPerFoot * FeetPerSquare;
        public const float MetersPerSquare = PrintedSquareMeters * HorizontalScale;
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
        /// Pets take slots from the sixth on, so the first of them stand in the row clear of the chamber's near wall.
        /// </summary>
        public static readonly GridPoint[] ArrivalSlots = Points(
            12.2f, 15.1f, 14f, 15.1f, 12.65f, 14.62f, 13.55f, 14.62f, 11.75f, 14.62f, 14.9f, 15.1f, 11.3f, 15.1f, 14.45f, 14.62f,
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

        /// <summary>Squares of chamber floor left unwalkable before a tier face, so characters cannot step up through it.</summary>
        public const float FaceGap = 0.35f;

        /// <summary>Half the depth of a door's sealed strip, in squares.</summary>
        public const float DoorwayHalfDepth = 0.2f;

        /// <summary>Metres from the floor to the top of a door leaf.</summary>
        public const float DoorLeafHeight = 2.9f;

        /// <summary>How far, in metres, a restored unit may sit from the plan's walking surface and still be standing on it.</summary>
        public const float GroundTolerance = 0.8f;

        public static readonly DoorPlan[] Doors =
        {
            new DoorPlan { Id = "A2", Hinge = new GridPoint(12.4f, 26.4f), Latch = new GridPoint(13.9f, 26.4f), OpenDegrees = 100f, Sihedron = true },
            // Hinged south: open, the leaf lies against the passage's far wall, where it cannot stand between the camera and the floor.
            // The default camera sees this door edge-on, so its closed click target is deeper than the leaf.
            new DoorPlan { Id = "A4Approach", Hinge = new GridPoint(21f, 15.4f), Latch = new GridPoint(21f, 13.9f), OpenDegrees = -90f, ClosedHitDepth = 1.25f },
        };

        public static readonly GridPoint[] Outer = Points(8, 1, 18, 1, 24, 7, 24, 18, 13, 30, 2, 18, 2, 7);

        /// <summary>The solid core inside the A3 ring, with the east door to A4 and the A2 stair block drawn into it.</summary>
        public static readonly GridPoint[] Core = Points(9.24f, 4, 16.76f, 4, 21, 8.24f, 21, 13.9f, 21, 15.4f, 21, 16.83f, 14.3f, 24.14f, 14.3f, 26.4f, 13.9f, 26.4f, 12.4f, 26.4f, 12, 26.4f, 12, 24.14f, 5, 16.83f, 5, 8.24f);

        /// <summary>
        /// The core for walking purposes: it bulges around the ziggurat's two south corners, which overhang A3's floor
        /// by less than the navmesh's step allowance would otherwise respect, along both sides of the A2 stairs, and
        /// on the north-east diagonal, where A4's lobes come within inches of the A3 floor behind a zero-thickness wall.
        /// </summary>
        public static readonly GridPoint[] CoreForWalking = Points(9.24f, 4, 16.76f, 4, 18.16f, 5.4f, 19.18f, 5.58f, 20.88f, 7.28f, 21.25f, 8.24f, 21.25f, 13.6f, 21, 13.9f, 21, 15.4f, 21, 16.83f, 18.15f, 20.4f, 17.2f, 21.65f, 16.45f, 23.45f, 14.9f, 23.49f, 14.6f, 24.3f, 14.6f, 26.7f, 13.9f, 26.7f, 13.9f, 26.4f, 12.4f, 26.4f, 12.4f, 26.7f, 11.7f, 26.7f, 11.7f, 24.3f, 11.35f, 23.46f, 9.55f, 23.45f, 8.95f, 21.65f, 7.85f, 20.4f, 5, 16.83f, 5, 8.24f);

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
            new WalkRegion { Name = "A3", Outer = Outer, Holes = { Core }, WalkHoles = new List<GridPoint[]> { CoreForWalking }, Height = 0f },
            new WalkRegion { Name = "A4A5", Outer = Atrium, Holes = { Altar, BrazierNorth, BrazierSouth, Pedestal, SarcophagusShadow }, Height = 0f },
            new WalkRegion { Name = "A1", Outer = ArrivalChamber, WalkOuter = BuildArrivalChamber(FaceGap), Height = 0f },
            new WalkRegion { Name = "ZigguratRing", Outer = Points(ZigguratWest, ZigguratNorth, NorthStairWest, ZigguratNorth, NorthStairWest, NorthStairHead, NorthStairEast, NorthStairHead, NorthStairEast, ZigguratNorth, ZigguratEast, ZigguratNorth, ZigguratEast, ZigguratSouth, ZigguratWest, ZigguratSouth), Holes = { Rect(RingInnerWest, RingInnerNorth, RingInnerEast, RingInnerSouth) }, Height = RingHeight },
            new WalkRegion { Name = "ZigguratApex", Outer = Rect(ApexWest, ApexNorth, ApexEast, ApexSouth), Height = ApexHeight },
            // Walking reaches through the door line; the floor beyond it already belongs to A3.
            new WalkRegion { Name = "A2Threshold", Outer = Rect(12.4f, 26.2f, 13.9f, 26.4f), WalkOuter = Rect(12.4f, 26.2f, 13.9f, 26.6f), Height = 0f },
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

        /// <summary>
        /// The core as fog of war must see it. Fog is two-dimensional, and the ziggurat's two south corners overhang the
        /// drawn core wall: anyone standing on those terrace corners would be past it, looking straight into A3. This line
        /// runs around the corners instead, while the walls themselves stay where they are drawn.
        /// </summary>
        public static readonly GridPoint[] CoreSight = BuildCoreSight();

        /// <summary>The runs that stop sight: the drawn walls, with the core following <see cref="CoreSight"/>.</summary>
        public static readonly List<WallRun> SightLines = BuildSightLines();

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
                if (Inside(region.EffectiveWalkOuter, x, y) && !region.EffectiveWalkHoles.Any(h => Inside(h, x, y)))
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

        /// <summary>
        /// Whether a unit's circular footprint touches the strip a closed door seals. Elevation matters: someone on the
        /// flights or the terrace above is not in the doorway however close their plan position is.
        /// </summary>
        public static bool DoorwayOverlap(DoorPlan door, float x, float y, float height, float radius)
        {
            if (height < -1f || height > DoorLeafHeight)
            {
                return false;
            }
            float x0 = Math.Min(door.Hinge.X, door.Latch.X), x1 = Math.Max(door.Hinge.X, door.Latch.X);
            float y0 = Math.Min(door.Hinge.Y, door.Latch.Y), y1 = Math.Max(door.Hinge.Y, door.Latch.Y);
            bool alongX = door.Hinge.Y == door.Latch.Y;
            float halfX = alongX ? 0f : DoorwayHalfDepth, halfY = alongX ? DoorwayHalfDepth : 0f;
            float dx = Math.Max(Math.Max(x0 - halfX - x, x - x1 - halfX), 0f);
            float dy = Math.Max(Math.Max(y0 - halfY - y, y - y1 - halfY), 0f);
            return dx * dx + dy * dy <= radius * radius;
        }

        /// <summary>Whether a unit's footprint touches the quarter-circle an open leaf sweeps as it closes.</summary>
        public static bool LeafSweepOverlap(DoorPlan door, float x, float y, float height, float radius)
        {
            if (height < -1f || height > DoorLeafHeight)
            {
                return false;
            }
            float leafX = door.Latch.X - door.Hinge.X, leafY = door.Latch.Y - door.Hinge.Y;
            float length = (float)Math.Sqrt(leafX * leafX + leafY * leafY);
            double open = door.OpenDegrees * Math.PI / 180.0;
            var openLatch = new GridPoint(door.Hinge.X + (float)(leafX * Math.Cos(open) - leafY * Math.Sin(open)), door.Hinge.Y + (float)(leafX * Math.Sin(open) + leafY * Math.Cos(open)));
            float px = x - door.Hinge.X, py = y - door.Hinge.Y;
            float reach = (float)Math.Sqrt(px * px + py * py);
            // Y runs south, so an increasing angle here is clockwise on the page, the sense OpenDegrees is given in.
            double between = Math.Atan2(leafX * py - leafY * px, leafX * px + leafY * py);
            if (door.OpenDegrees < 0f)
            {
                between = -between;
            }
            if (between >= 0.0 && between <= Math.Abs(open))
            {
                return reach - length <= radius;
            }
            return DistanceToSegment(x, y, door.Hinge, door.Latch) <= radius || DistanceToSegment(x, y, door.Hinge, openLatch) <= radius;
        }

        /// <summary>Whether closing the door now would pass through a unit of the given footprint.</summary>
        public static bool ClosureBlocked(DoorPlan door, float x, float y, float height, float radius)
        {
            return DoorwayOverlap(door, x, y, height, radius) || LeafSweepOverlap(door, x, y, height, radius);
        }

        public static bool SameDoorSide(DoorPlan door, GridPoint before, GridPoint after)
        {
            // Classify only a move whose segment crosses the physical span of this door.
            bool horizontal = door.Hinge.Y == door.Latch.Y;
            float a = horizontal ? before.Y - door.Center.Y : before.X - door.Center.X;
            float b = horizontal ? after.Y - door.Center.Y : after.X - door.Center.X;
            if (a * b >= 0f) return true;
            float t = a / (a - b);
            float cross = horizontal ? before.X + t * (after.X - before.X) : before.Y + t * (after.Y - before.Y);
            float start = horizontal ? door.Hinge.X : door.Hinge.Y;
            float end = horizontal ? door.Latch.X : door.Latch.Y;
            return cross < Math.Min(start, end) - DoorwayHalfDepth || cross > Math.Max(start, end) + DoorwayHalfDepth;
        }

        private static float DistanceToSegment(float x, float y, GridPoint a, GridPoint b)
        {
            float vx = b.X - a.X, vy = b.Y - a.Y;
            float t = Math.Max(0f, Math.Min(1f, ((x - a.X) * vx + (y - a.Y) * vy) / (vx * vx + vy * vy)));
            float dx = x - (a.X + vx * t), dy = y - (a.Y + vy * t);
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static SanctumSpace SpaceAt(float x, float y)
        {
            if (Inside(Atrium, x, y))
            {
                return SanctumSpace.Atrium;
            }
            if (x >= ZigguratWest && x <= ZigguratEast && y >= ZigguratNorth && y <= ZigguratSouth)
            {
                return SanctumSpace.Interior;
            }
            if (Inside(Core, x, y))
            {
                return SanctumSpace.Interior;
            }
            return Inside(Outer, x, y) ? SanctumSpace.Hall : SanctumSpace.None;
        }

        /// <summary>
        /// A sight line traced out and back again. The fog's shadow volume takes each edge's direction into account, as
        /// native blockers are solid outlines; a wall with no thickness needs both directions to stop sight from both sides.
        /// </summary>
        public static List<GridPoint> TracedBothWays(IList<GridPoint> run)
        {
            var traced = new List<GridPoint>(run);
            for (int i = run.Count - 2; i >= 0; i--)
            {
                traced.Add(run[i]);
            }
            return traced;
        }

        /// <summary>Whether a restored unit is standing on the plan's walking surface at its own height.</summary>
        public static bool IsValidGround(float x, float y, float height)
        {
            float? ground = HeightAt(x, y);
            return ground.HasValue && Math.Abs(ground.Value - height) <= GroundTolerance;
        }

        /// <summary>Plan points on rings spreading out from a position, nearest first, for finding ground close to it.</summary>
        public static IEnumerable<GridPoint> RingsAround(float x, float y, float step = 0.2f, float reach = 6f)
        {
            for (float radius = step; radius <= reach; radius += step)
            {
                int bearings = Math.Max(8, (int)(radius * 12f));
                for (int i = 0; i < bearings; i++)
                {
                    double angle = i * Math.PI * 2.0 / bearings;
                    yield return new GridPoint(x + radius * (float)Math.Cos(angle), y + radius * (float)Math.Sin(angle));
                }
            }
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
        public static float CutawayHeight(GridPoint a, GridPoint b, float low, float high, float dx = 0f, float dy = 1f, float pitchTangent = 0.9325f)
        {
            float best = high;
            for (int i = 0; i <= 4; i++)
            {
                float t = i / 4f;
                float x = a.X + (b.X - a.X) * t;
                float y = a.Y + (b.Y - a.Y) * t;
                if (HeightAt(x + dx * 0.08f, y + dy * 0.08f).HasValue)
                {
                    return low;
                }
                float distance = high;
                for (float d = 0.25f; d < 8f; d += 0.25f)
                {
                    if (HeightAt(x + dx * d, y + dy * d).HasValue)
                    {
                        distance = Math.Max(0f, d - 0.25f) * MetersPerSquare * pitchTangent * 0.9f;
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

        private static GridPoint[] BuildCoreSight()
        {
            GridPoint northEast = new GridPoint(21f, 16.83f), southEast = new GridPoint(14.3f, 24.14f), southWest = new GridPoint(12f, 24.14f), northWest = new GridPoint(5f, 16.83f);
            var points = new List<GridPoint>();
            foreach (GridPoint p in Core)
            {
                points.Add(p);
                if (p.X == northEast.X && p.Y == northEast.Y)
                {
                    points.Add(new GridPoint(ZigguratEast, YAt(northEast, southEast, ZigguratEast)));
                    points.Add(new GridPoint(ZigguratEast, ZigguratSouth));
                    points.Add(new GridPoint(XAt(northEast, southEast, ZigguratSouth), ZigguratSouth));
                }
                else if (p.X == southWest.X && p.Y == southWest.Y)
                {
                    points.Add(new GridPoint(XAt(southWest, northWest, ZigguratSouth), ZigguratSouth));
                    points.Add(new GridPoint(ZigguratWest, ZigguratSouth));
                    points.Add(new GridPoint(ZigguratWest, YAt(southWest, northWest, ZigguratWest)));
                }
            }
            return points.ToArray();
        }

        private static float YAt(GridPoint a, GridPoint b, float x)
        {
            return a.Y + (b.Y - a.Y) * (x - a.X) / (b.X - a.X);
        }

        private static float XAt(GridPoint a, GridPoint b, float y)
        {
            return a.X + (b.X - a.X) * (y - a.Y) / (b.Y - a.Y);
        }

        private static List<WallRun> BuildSightLines()
        {
            var lines = new List<WallRun>();
            foreach (WallRun wall in Walls)
            {
                lines.Add(wall.Name == "A3Core"
                    ? new WallRun { Name = wall.Name, Points = CoreSight, Closed = true, Openings = { IndexOf(CoreSight, 21, 13.9f), IndexOf(CoreSight, 13.9f, 26.4f) } }
                    : wall);
            }
            return lines;
        }

        /// <summary>The chamber floor; a gap held back from the ziggurat's faces gives the outline characters may walk.</summary>
        private static GridPoint[] BuildArrivalChamber(float gap = 0f)
        {
            var points = new List<GridPoint>();
            points.AddRange(ArrivalChamberRock());
            float east = 16f + gap, west = 10f - gap, face = 16.2f - gap;
            if (gap <= 0f)
            {
                points.Add(new GridPoint(east, 21.1f));
            }
            points.AddRange(Points(east, face, 14.16f, face, 14.16f, 15.4f, 12.07f, 15.4f, 12.07f, face, west, face));
            if (gap <= 0f)
            {
                points.Add(new GridPoint(west, 21.1f));
            }
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
