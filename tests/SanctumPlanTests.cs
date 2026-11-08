using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Tests
{
    public static class SanctumPlanTests
    {
        private const float Step = 0.1f;
        private const float MaxStepRise = 0.6f;

        private static HashSet<long> Flood()
        {
            var seen = new HashSet<long>();
            var open = new Queue<long>();
            long start = Key((int)Math.Round(SanctumPlan.Arrival.X / Step), (int)Math.Round(SanctumPlan.Arrival.Y / Step));
            seen.Add(start);
            open.Enqueue(start);
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            while (open.Count > 0)
            {
                long k = open.Dequeue();
                int x = (int)(k / 100000), y = (int)(k % 100000);
                float? here = SanctumPlan.HeightAt(x * Step, y * Step);
                for (int d = 0; d < 4; d++)
                {
                    long next = Key(x + dx[d], y + dy[d]);
                    float? there = SanctumPlan.HeightAt((x + dx[d]) * Step, (y + dy[d]) * Step);
                    if (there.HasValue && here.HasValue && !seen.Contains(next) && Math.Abs(there.Value - here.Value) <= MaxStepRise)
                    {
                        seen.Add(next);
                        open.Enqueue(next);
                    }
                }
            }
            return seen;
        }

        private static long Key(int x, int y)
        {
            return x * 100000L + y;
        }

        private static bool Reached(HashSet<long> seen, float x, float y)
        {
            return seen.Contains(Key((int)Math.Round(x / Step), (int)Math.Round(y / Step)));
        }

        public static void TestScaleIsFiveFootSquaresOnNorthUpAxes()
        {
            Assert.Equal(1.524f, SanctumPlan.MetersPerSquare, "metres per square");
            Assert.Equal(true, SanctumPlan.ToWorldZ(1f) > SanctumPlan.ToWorldZ(30f), "north is +Z");
            Assert.Equal(true, SanctumPlan.ToWorldX(24f) > SanctumPlan.ToWorldX(2f), "east is +X");
        }

        public static void TestPerimeterHasSevenSides()
        {
            Assert.Equal(7, SanctumPlan.Outer.Length, "outer vertices");
        }

        public static void TestCorridorIsFifteenFeetWideOnEverySide()
        {
            // Perpendicular distance from each core wall to the matching outer wall, in five-foot squares.
            Assert.Equal(3f, Math.Round(Distance(SanctumPlan.Outer[0], SanctumPlan.Outer[1], new GridPoint(13, 4)), 2) == 3.0 ? 3f : -1f, "north");
            Assert.Equal(true, Math.Abs(Distance(SanctumPlan.Outer[2], SanctumPlan.Outer[3], new GridPoint(21, 12)) - 3f) < 0.05f, "east");
            Assert.Equal(true, Math.Abs(Distance(SanctumPlan.Outer[1], SanctumPlan.Outer[2], new GridPoint(18.88f, 6.12f)) - 3f) < 0.06f, "north-east");
            Assert.Equal(true, Math.Abs(Distance(SanctumPlan.Outer[3], SanctumPlan.Outer[4], new GridPoint(17.65f, 20.49f)) - 3f) < 0.06f, "south-east");
        }

        private static double Distance(GridPoint a, GridPoint b, GridPoint p)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            return Math.Abs(dy * (p.X - a.X) - dx * (p.Y - a.Y)) / Math.Sqrt(dx * dx + dy * dy);
        }

        public static void TestRequiredWalkingGraphIsConnected()
        {
            HashSet<long> seen = Flood();
            Assert.Equal(true, Reached(seen, 13.1f, 14.95f), "A1 arrival");
            Assert.Equal(true, Reached(seen, 13.1f, 16.8f), "ziggurat ring north");
            Assert.Equal(true, Reached(seen, 13f, 19.6f), "ziggurat apex");
            Assert.Equal(true, Reached(seen, 13.1f, 24.6f), "A2 stairs");
            Assert.Equal(true, Reached(seen, 13f, 28f), "A3 south junction");
            Assert.Equal(true, Reached(seen, 22.5f, 12f), "A3 east arm");
            Assert.Equal(true, Reached(seen, 3.5f, 12f), "A3 west arm");
            Assert.Equal(true, Reached(seen, 13f, 2.5f), "A3 north side");
            Assert.Equal(true, Reached(seen, 19.8f, 14.6f), "A4 approach");
            Assert.Equal(true, Reached(seen, 18f, 9.5f), "A4 atrium");
            Assert.Equal(true, Reached(seen, 13f, 8f), "A5 around the altar");
            Assert.Equal(true, Reached(seen, 7f, 9.5f), "A5 west of the sarcophagus");
        }

        public static void TestRockAndHolesAreNotWalkable()
        {
            Assert.Equal(false, SanctumPlan.HeightAt(13f, 13.6f).HasValue, "rock between A5 and A1");
            Assert.Equal(false, SanctumPlan.HeightAt(14.8f, 9.5f).HasValue, "altar");
            Assert.Equal(false, SanctumPlan.HeightAt(8.87f, 9.46f).HasValue, "under the sarcophagus");
            Assert.Equal(false, SanctumPlan.HeightAt(12f, 18.2f).HasValue, "ziggurat face");
            Assert.Equal(false, SanctumPlan.HeightAt(13f, 31f).HasValue, "beyond the south vertex");
        }

        public static void TestThereIsNoInventedDirectRouteBetweenRooms()
        {
            HashSet<long> seen = Flood();
            int y = (int)Math.Round(13.6f / Step);
            bool throughRock = Enumerable.Range((int)Math.Round(9f / Step), (int)Math.Round(8f / Step)).Any(x => seen.Contains(Key(x, y)));
            Assert.Equal(false, throughRock, "A1 to A5 through rock");
        }

        public static void TestArrivalIsNorthOfTheZiggurat()
        {
            Assert.Equal(true, SanctumPlan.Arrival.Y < SanctumPlan.ZigguratNorth, "north of the steps");
            Assert.Equal(true, SanctumPlan.HeightAt(SanctumPlan.Arrival.X, SanctumPlan.Arrival.Y) == 0f, "on the chamber floor");
        }

        public static void TestArrivalSlotsStayOnTheChamberFloor()
        {
            GridPoint[] all = new[] { SanctumPlan.Arrival }.Concat(SanctumPlan.ArrivalSlots).ToArray();
            Assert.Equal(13, all.Length, "six characters, six pets and a spare");
            foreach (GridPoint slot in all)
            {
                Assert.Equal(true, SanctumPlan.HeightAt(slot.X, slot.Y) == 0f, $"floor at ({slot.X}, {slot.Y})");
                Assert.Equal(true, slot.Y < SanctumPlan.ArrivalSouth, $"north of the stairs at ({slot.X}, {slot.Y})");
            }
            foreach (GridPoint slot in all.Take(9))
            {
                Assert.Equal(true, slot.X >= SanctumPlan.ArrivalWest && slot.X <= SanctumPlan.ArrivalEast && slot.Y >= SanctumPlan.ArrivalNorth, $"inside the printed rectangle at ({slot.X}, {slot.Y})");
            }
            for (int i = 0; i < all.Length; i++)
            {
                for (int j = i + 1; j < all.Length; j++)
                {
                    double distance = Math.Sqrt(Math.Pow(all[i].X - all[j].X, 2) + Math.Pow(all[i].Y - all[j].Y, 2));
                    Assert.Equal(true, distance >= 0.65, $"slots {i} and {j} are apart");
                }
            }
        }

        public static void TestApexIsTwentyFeetUp()
        {
            Assert.Equal(true, Math.Abs(SanctumPlan.HeightAt(13f, 19.6f).Value - 6.096f) < 0.001f, "apex height");
        }

        public static void TestSevenRunePlatesAtMappedWalls()
        {
            string[] ids = SanctumPlan.RunePlates.Select(p => p.Id).ToArray();
            Assert.Equal("A3a,A3b,A3c,A3d,A3e,A3f,A3g", string.Join(",", ids), "plate order");
            Assert.Equal(3, SanctumPlan.RunePlates.Count(p => p.Rune == "Life"), "three Life runes");
        }

        public static void TestWallsOpenExactlyAtTheTwoDoors()
        {
            WallRun core = SanctumPlan.Walls.Single(w => w.Name == "A3Core");
            Assert.Equal(2, core.Openings.Count, "A3 core openings");
            Assert.Equal(1, SanctumPlan.Walls.Single(w => w.Name == "A4A5").Openings.Count, "A4 mouth");
        }

        public static void TestCutawayNeverHidesTheArrivalFloor()
        {
            // The wall behind A5's south side faces the camera; it must stay low enough not to cover A1's arrival floor.
            float height = SanctumPlan.CutawayHeight(new GridPoint(12f, 13.08f), new GridPoint(10f, 13.08f), 0.4f, 4.5f);
            Assert.Equal(true, height <= 1.6f, $"height {height}");
            float nearWall = SanctumPlan.CutawayHeight(new GridPoint(10f, 14.1f), new GridPoint(16f, 14.1f), 0.4f, 4.5f);
            Assert.Equal(0.4f, nearWall, "wall in front of its own floor");
        }
    }
}
