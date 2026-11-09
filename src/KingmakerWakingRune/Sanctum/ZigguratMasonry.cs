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
        /// The part of a side flight's run standing clear of the first course. Inside it the course's own channel
        /// wall is the visible stone, so a flank drawn there would share its plane.
        /// </summary>
        public static bool ExposedFlank(Ramp ramp, float from, float to, out float start, out float end)
        {
            float low = Math.Min(from, to), high = Math.Max(from, to);
            bool west = ramp.Name == "ZigguratWestStairs";
            start = west ? low : Math.Max(low, SanctumPlan.RingInnerEast);
            end = west ? Math.Min(high, SanctumPlan.RingInnerWest) : high;
            return end - start > 0.0001f;
        }
    }
}
