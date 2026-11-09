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
            Assert.Equal(false, ZigguratMasonry.ExposedFlank(west, SanctumPlan.RingInnerWest + 0.1f, SanctumPlan.RingInnerWest + 0.3f, out _, out _), "inside the course, west");
            Assert.Equal(false, ZigguratMasonry.ExposedFlank(east, SanctumPlan.RingInnerEast - 0.3f, SanctumPlan.RingInnerEast - 0.1f, out _, out _), "inside the course, east");
            Assert.Equal(true, ZigguratMasonry.ExposedFlank(west, SanctumPlan.RingInnerWest - 0.1f, SanctumPlan.RingInnerWest + 0.1f, out float start, out float end), "straddling step");
            Assert.Equal(SanctumPlan.RingInnerWest - 0.1f, start, "straddling start");
            Assert.Equal(SanctumPlan.RingInnerWest, end, "straddling end");
        }

        public static void TestThresholdDrawsOnlyToTheDoorLine()
        {
            WalkRegion threshold = SanctumPlan.Regions.Single(r => r.Name == "A2Threshold");
            Assert.Equal(26.4f, threshold.Outer.Max(p => p.Y), "threshold floor stops at the door");
            Assert.Equal(26.6f, threshold.EffectiveWalkOuter.Max(p => p.Y), "walking continues through it");
        }
    }
}
