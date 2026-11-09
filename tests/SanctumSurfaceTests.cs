using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Tests
{
    /// <summary>Checks the surface lists SanctumBuilder draws from. They are not the meshes; those are audited in the running game.</summary>
    public static class SanctumSurfaceTests
    {
        private static readonly PlanPatch Apex = new PlanPatch(SanctumPlan.ApexWest, SanctumPlan.ApexNorth, SanctumPlan.ApexEast, SanctumPlan.ApexSouth, SanctumPlan.ApexHeight);

        private static IEnumerable<PlanPatch> ExposedSurfaces()
        {
            return ZigguratMasonry.Tops.Concat(new[] { Apex });
        }

        public static void TestApexHasOneOwner()
        {
            WalkRegion region = SanctumPlan.Regions.Single(r => r.Name == "ZigguratApex");
            Assert.Equal(SanctumPlan.ApexHeight, region.Height, "apex region height");
            float overlap = ZigguratMasonry.Tops.Where(t => Math.Abs(t.Height - SanctumPlan.ApexHeight) < 0.001f).Sum(t => t.OverlapArea(Apex));
            Assert.Equal(0f, overlap, "course tops over the walkable apex");
        }

        public static void TestNoTwoUpperSurfacesShareAPatch()
        {
            List<PlanPatch> all = ExposedSurfaces().ToList();
            for (int i = 0; i < all.Count; i++)
            {
                for (int j = i + 1; j < all.Count; j++)
                {
                    if (Math.Abs(all[i].Height - all[j].Height) < 0.001f && all[i].OverlapArea(all[j]) > 0.00001f)
                    {
                        throw new Exception($"Surfaces {i} and {j} overlap at height {all[i].Height}.");
                    }
                }
            }
        }

        public static void TestUpperSurfacesHaveRealArea()
        {
            foreach (PlanPatch top in ZigguratMasonry.Tops)
            {
                if (top.Area <= 0.0001f)
                {
                    throw new Exception($"Degenerate top at ({top.West}, {top.North}).");
                }
            }
        }

        public static void TestTopsLeaveTheFlightsOpen()
        {
            foreach (PlanPatch top in ZigguratMasonry.Tops)
            {
                bool inChannel = top.South > SanctumPlan.SideStairNorth + 0.001f && top.North < SanctumPlan.SideStairSouth - 0.001f;
                bool besideApex = top.West >= SanctumPlan.ApexEast - 0.001f || top.East <= SanctumPlan.ApexWest + 0.001f;
                if (inChannel && besideApex)
                {
                    throw new Exception($"A top caps a flight at ({top.West}, {top.North}).");
                }
            }
        }

        public static void TestEveryCourseKeepsItsVisibleTop()
        {
            for (int course = 0; course < ZigguratMasonry.Courses; course++)
            {
                float height = ZigguratMasonry.CourseTop(course);
                Assert.Equal(true, ZigguratMasonry.Tops.Any(t => Math.Abs(t.Height - height) < 0.001f), $"course {course} top");
            }
        }

        public static void TestFacesAreSolidAndOnTheirEdges()
        {
            foreach (PlanFace face in ZigguratMasonry.Faces)
            {
                Assert.Equal(true, face.Top > face.Bottom, "face has height");
                PlanPatch f = face.Footprint;
                Assert.Equal(true, f.East > f.West && f.South > f.North, "face footprint has area");
            }
        }

        public static void TestSideFlankStopsAtFirstCourse()
        {
            Ramp west = SanctumPlan.Ramps.Single(r => r.Name == "ZigguratWestStairs");
            Ramp east = SanctumPlan.Ramps.Single(r => r.Name == "ZigguratEastStairs");
            float below = ZigguratMasonry.CourseTop(0) - 0.1f;
            Assert.Equal(0, ZigguratMasonry.ExposedFlank(west, SanctumPlan.RingInnerWest + 0.1f, SanctumPlan.RingInnerWest + 0.3f, below, SanctumPlan.RingHeight).Count(), "inside the course, west");
            Assert.Equal(0, ZigguratMasonry.ExposedFlank(east, SanctumPlan.RingInnerEast - 0.3f, SanctumPlan.RingInnerEast - 0.1f, below, SanctumPlan.RingHeight).Count(), "inside the course, east");
            FlankSpan[] straddling = ZigguratMasonry.ExposedFlank(west, SanctumPlan.RingInnerWest - 0.1f, SanctumPlan.RingInnerWest + 0.1f, below, SanctumPlan.RingHeight).ToArray();
            Assert.Equal(1, straddling.Length, "only the part clear of the course");
            Assert.Equal(SanctumPlan.RingInnerWest - 0.1f, straddling[0].Start, "straddling start");
            Assert.Equal(SanctumPlan.RingInnerWest, straddling[0].End, "straddling end");
        }

        public static void TestSideFlankRisesFromTheChannelWallWithoutGapOrOverlap()
        {
            foreach (Ramp ramp in SanctumPlan.Ramps.Where(r => r.Name == "ZigguratWestStairs" || r.Name == "ZigguratEastStairs"))
            {
                const int steps = 13;
                int exposedAboveCourse = 0;
                for (int i = 0; i < steps; i++)
                {
                    float t0 = (float)i / steps, t1 = (float)(i + 1) / steps;
                    float from = ramp.From + (ramp.To - ramp.From) * t0, to = ramp.From + (ramp.To - ramp.From) * t1;
                    float height = ramp.HeightFrom + (ramp.HeightTo - ramp.HeightFrom) * (t0 + t1) / 2f;
                    FlankSpan[] spans = ZigguratMasonry.ExposedFlank(ramp, from, to, height, ramp.HeightFrom).ToArray();
                    float low = Math.Min(from, to), high = Math.Max(from, to);
                    for (int sample = 0; sample < 40; sample++)
                    {
                        float x = low + (high - low) * (sample + 0.5f) / 40f;
                        float wall = Math.Max(ramp.HeightFrom, ZigguratMasonry.ChannelWallTop(ramp, x));
                        FlankSpan[] here = spans.Where(s => x >= s.Start && x <= s.End).ToArray();
                        if (height > wall + 0.001f)
                        {
                            Assert.Equal(1, here.Length, $"{ramp.Name} flank covers the stone above the wall at {x}");
                            Assert.Equal(wall, here[0].Bottom, $"{ramp.Name} flank starts at the wall top at {x}");
                            exposedAboveCourse += wall > ramp.HeightFrom ? 1 : 0;
                        }
                        else
                        {
                            Assert.Equal(0, here.Length, $"{ramp.Name} flank does not double the wall at {x}");
                        }
                    }
                }
                Assert.Equal(true, exposedAboveCourse > 0, $"{ramp.Name} climbs past a course, the case that left a gap");
            }
        }

        public static void TestOtherFlightsKeepTheirWholeFlank()
        {
            Ramp north = SanctumPlan.Ramps.Single(r => r.Name == "ZigguratNorthStairs");
            FlankSpan span = ZigguratMasonry.ExposedFlank(north, 16f, 15.5f, 1f, 0f).Single();
            Assert.Equal(15.5f, span.Start, "start");
            Assert.Equal(16f, span.End, "end");
            Assert.Equal(0f, span.Bottom, "foot");
        }

        public static void TestThresholdDrawsOnlyToTheDoorLine()
        {
            WalkRegion threshold = SanctumPlan.Regions.Single(r => r.Name == "A2Threshold");
            Assert.Equal(26.4f, threshold.Outer.Max(p => p.Y), "threshold floor stops at the door");
            Assert.Equal(26.6f, threshold.EffectiveWalkOuter.Max(p => p.Y), "walking continues through it");
        }
    }
}
