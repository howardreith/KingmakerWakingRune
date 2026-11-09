using System;
using System.Collections.Generic;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>A flat axis-aligned surface in plan squares. X runs east, Y south, as in <see cref="SanctumPlan"/>.</summary>
    internal struct PlanPatch
    {
        public readonly float West, North, East, South, Height;

        public PlanPatch(float west, float north, float east, float south, float height)
        {
            West = west;
            North = north;
            East = east;
            South = south;
            Height = height;
        }

        public float Area => Math.Max(0f, East - West) * Math.Max(0f, South - North);

        public float OverlapArea(PlanPatch other)
        {
            float width = Math.Min(East, other.East) - Math.Max(West, other.West);
            float depth = Math.Min(South, other.South) - Math.Max(North, other.North);
            return width > 0f && depth > 0f ? width * depth : 0f;
        }
    }

    internal enum FaceDirection
    {
        North,
        South,
        East,
        West,
    }

    /// <summary>A vertical stone face on one edge of a plan rectangle, looking outward in <see cref="Direction"/>.</summary>
    internal struct PlanFace
    {
        public readonly PlanPatch Footprint;
        public readonly float Bottom, Top;
        public readonly FaceDirection Direction;

        public PlanFace(PlanPatch footprint, float bottom, float top, FaceDirection direction)
        {
            Footprint = footprint;
            Bottom = bottom;
            Top = top;
            Direction = direction;
        }
    }

    /// <summary>A length of a stair's side wall to draw: from <see cref="Start"/> to <see cref="End"/> along the run, rising from <see cref="Bottom"/>.</summary>
    internal struct FlankSpan
    {
        public readonly float Start, End, Bottom;

        public FlankSpan(float start, float end, float bottom)
        {
            Start = start;
            End = end;
            Bottom = bottom;
        }
    }

    /// <summary>
    /// The upper courses of the ziggurat as data, so every exposed patch has exactly one owner and that can be tested
    /// without the game. A course's top keeps only what the next course up, or the walkable apex, leaves exposed.
    /// </summary>
    internal static class ZigguratMasonry
    {
        public const int Courses = 3;

        public static readonly List<PlanPatch> Tops = BuildTops();
        public static readonly List<PlanFace> Faces = BuildFaces();

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static float CourseTop(int course)
        {
            return Lerp(SanctumPlan.RingHeight, SanctumPlan.ApexHeight, (course + 1f) / Courses);
        }

        private static float CourseBottom(int course)
        {
            return Lerp(SanctumPlan.RingHeight, SanctumPlan.ApexHeight, (float)course / Courses);
        }

        /// <summary>The solid parts of a course: the blocks either side of the flights' channel and the column between the flights.</summary>
        private static PlanPatch[] Solids(int course)
        {
            float t = (float)course / Courses;
            float west = Lerp(SanctumPlan.RingInnerWest, SanctumPlan.ApexWest, t), east = Lerp(SanctumPlan.RingInnerEast, SanctumPlan.ApexEast, t);
            float north = Lerp(SanctumPlan.RingInnerNorth, SanctumPlan.ApexNorth, t), south = Lerp(SanctumPlan.RingInnerSouth, SanctumPlan.ApexSouth, t);
            float top = CourseTop(course);
            return new[]
            {
                new PlanPatch(west, north, east, SanctumPlan.SideStairNorth, top),
                new PlanPatch(west, SanctumPlan.SideStairSouth, east, south, top),
                new PlanPatch(SanctumPlan.ApexWest, SanctumPlan.SideStairNorth, SanctumPlan.ApexEast, SanctumPlan.SideStairSouth, top),
            };
        }

        private static List<PlanPatch> BuildTops()
        {
            var tops = new List<PlanPatch>();
            var apex = new PlanPatch(SanctumPlan.ApexWest, SanctumPlan.ApexNorth, SanctumPlan.ApexEast, SanctumPlan.ApexSouth, SanctumPlan.ApexHeight);
            for (int course = 0; course < Courses; course++)
            {
                PlanPatch[] covers = course + 1 < Courses ? Solids(course + 1) : new[] { apex };
                foreach (PlanPatch solid in Solids(course))
                {
                    var exposed = new List<PlanPatch> { solid };
                    foreach (PlanPatch cover in covers)
                    {
                        exposed = Subtract(exposed, cover);
                    }
                    tops.AddRange(exposed);
                }
            }
            return tops;
        }

        private static List<PlanPatch> Subtract(List<PlanPatch> patches, PlanPatch cover)
        {
            var result = new List<PlanPatch>();
            foreach (PlanPatch p in patches)
            {
                if (p.OverlapArea(cover) <= 0f)
                {
                    result.Add(p);
                    continue;
                }
                float north = Math.Max(p.North, cover.North), south = Math.Min(p.South, cover.South);
                Keep(result, new PlanPatch(p.West, p.North, p.East, north, p.Height));
                Keep(result, new PlanPatch(p.West, south, p.East, p.South, p.Height));
                Keep(result, new PlanPatch(p.West, north, Math.Max(p.West, cover.West), south, p.Height));
                Keep(result, new PlanPatch(Math.Min(p.East, cover.East), north, p.East, south, p.Height));
            }
            return result;
        }

        private static void Keep(List<PlanPatch> patches, PlanPatch patch)
        {
            if (patch.Area > 0.0001f)
            {
                patches.Add(patch);
            }
        }

        private static List<PlanFace> BuildFaces()
        {
            var faces = new List<PlanFace>();
            for (int course = 0; course < Courses; course++)
            {
                float bottom = CourseBottom(course), top = CourseTop(course);
                PlanPatch[] solids = Solids(course);
                // The channels stay open for the east and west flights, so nobody climbing them walks inside a course.
                foreach (PlanPatch block in new[] { solids[0], solids[1] })
                {
                    foreach (FaceDirection direction in new[] { FaceDirection.North, FaceDirection.South, FaceDirection.East, FaceDirection.West })
                    {
                        faces.Add(new PlanFace(block, bottom, top, direction));
                    }
                }
                faces.Add(new PlanFace(solids[2], bottom, top, FaceDirection.East));
                faces.Add(new PlanFace(solids[2], bottom, top, FaceDirection.West));
            }
            return faces;
        }

        /// <summary>
        /// The height to which a side flight's flank is already stone at a point along its run: the top of the highest
        /// course whose channel wall reaches that point, or the terrace where none does.
        /// </summary>
        public static float ChannelWallTop(Ramp ramp, float along)
        {
            bool west = ramp.Name == "ZigguratWestStairs";
            float top = SanctumPlan.RingHeight;
            for (int course = 0; course < Courses; course++)
            {
                float t = (float)course / Courses;
                bool reached = west ? along >= Lerp(SanctumPlan.RingInnerWest, SanctumPlan.ApexWest, t) : along <= Lerp(SanctumPlan.RingInnerEast, SanctumPlan.ApexEast, t);
                if (reached)
                {
                    top = CourseTop(course);
                }
            }
            return top;
        }

        /// <summary>
        /// The parts of one step's flank that need drawing. On a side flight a channel wall is already the visible stone
        /// up to <see cref="ChannelWallTop"/>, so the flank starts there: lower and it would share the wall's plane, higher
        /// and a gap would open where the steps climb past a course. Other flights have no channel, so their flank is whole.
        /// </summary>
        public static IEnumerable<FlankSpan> ExposedFlank(Ramp ramp, float from, float to, float height, float foot)
        {
            float low = Math.Min(from, to), high = Math.Max(from, to);
            if (ramp.Name != "ZigguratWestStairs" && ramp.Name != "ZigguratEastStairs")
            {
                yield return new FlankSpan(low, high, foot);
                yield break;
            }
            bool west = ramp.Name == "ZigguratWestStairs";
            var cuts = new List<float> { low, high };
            for (int course = 0; course < Courses; course++)
            {
                float t = (float)course / Courses;
                float edge = west ? Lerp(SanctumPlan.RingInnerWest, SanctumPlan.ApexWest, t) : Lerp(SanctumPlan.RingInnerEast, SanctumPlan.ApexEast, t);
                if (edge > low && edge < high)
                {
                    cuts.Add(edge);
                }
            }
            cuts.Sort();
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float bottom = Math.Max(foot, ChannelWallTop(ramp, (cuts[i] + cuts[i + 1]) / 2f));
                if (height - bottom > 0.0001f && cuts[i + 1] - cuts[i] > 0.0001f)
                {
                    yield return new FlankSpan(cuts[i], cuts[i + 1], bottom);
                }
            }
        }
    }
}
