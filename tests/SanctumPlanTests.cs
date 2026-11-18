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

        private static HashSet<long> Flood(params DoorPlan[] closed)
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
                    // The navmesh cut overlaps each jamb by 0.15 m, which is 0.1 of a square.
                    bool sealedOff = closed.Any(door => SanctumPlan.InDoorway(door, (x + dx[d]) * Step, (y + dy[d]) * Step, 0.1f));
                    if (there.HasValue && here.HasValue && !sealedOff && !seen.Contains(next) && Math.Abs(there.Value - here.Value) <= MaxStepRise)
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
            Assert.Equal(1.524f, SanctumPlan.PrintedSquareMeters, "printed five-foot square");
            Assert.Equal(1.524f * 1.15f, SanctumPlan.MetersPerSquare, "horizontal traversal allowance");
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

        public static void TestClosedDoorsSealTheirRoutes()
        {
            DoorPlan a2 = SanctumPlan.Doors.Single(d => d.Id == "A2"), a4 = SanctumPlan.Doors.Single(d => d.Id == "A4Approach");
            HashSet<long> pastA2 = Flood(a2);
            Assert.Equal(true, Reached(pastA2, 13f, 19.6f), "the ziggurat stays reachable behind a closed A2 door");
            Assert.Equal(false, Reached(pastA2, 13f, 28f), "closed A2 door seals A3");
            HashSet<long> pastA4 = Flood(a4);
            Assert.Equal(true, Reached(pastA4, 22.5f, 12f), "A3 stays reachable behind a closed east door");
            Assert.Equal(false, Reached(pastA4, 18f, 9.5f), "closed east door seals A4");
            Assert.Equal(false, Reached(pastA4, 7f, 9.5f), "closed east door seals A5");
        }

        public static void TestA2StairsCannotBeSteppedOffIntoA3()
        {
            Ramp a2 = SanctumPlan.Ramps.Single(r => r.Name == "A2Stairs");
            foreach (float y in new[] { 24.5f, 25.5f, 26.1f })
            {
                Assert.Equal(false, SanctumPlan.HeightAt(a2.WalkX0 - 0.05f, y).HasValue, $"west side gap at y {y}");
                Assert.Equal(false, SanctumPlan.HeightAt(a2.WalkX1 + 0.05f, y).HasValue, $"east side gap at y {y}");
            }
            Assert.Equal(true, a2.WalkX0 <= 12.4f && a2.WalkX1 >= 13.9f, "the walkable flight still spans the doorway");
            // At least four 0.2 m navmesh cells must separate the flight from A3 on each side.
            Assert.Equal(false, SanctumPlan.HeightAt(14.5f, 25f).HasValue, "A3 held back east of the stairs");
            Assert.Equal(false, SanctumPlan.HeightAt(11.8f, 25f).HasValue, "A3 held back west of the stairs");
            Assert.Equal(true, (14.6f - a2.WalkX1) * SanctumPlan.MetersPerSquare >= 0.8f, "east gap width");
            Assert.Equal(true, (a2.WalkX0 - 11.7f) * SanctumPlan.MetersPerSquare >= 0.8f, "west gap width");
            Assert.Equal(true, SanctumPlan.HeightAt(13.1f, 26.9f) == 0f, "the doorway still opens onto A3");
        }

        public static void TestZigguratCornersCannotBeSteppedOffIntoA3()
        {
            Assert.Equal(false, SanctumPlan.HeightAt(16.2f, 22.9f).HasValue, "gap beyond the south-east corner");
            Assert.Equal(false, SanctumPlan.HeightAt(9.8f, 22.9f).HasValue, "gap beyond the south-west corner");
            Assert.Equal(true, SanctumPlan.HeightAt(15.9f, 22.9f) == SanctumPlan.RingHeight, "the terrace keeps its corner");
            Assert.Equal(true, SanctumPlan.HeightAt(18f, 24f) == 0f, "A3 stays walkable past the corner");
        }

        public static void TestChamberFloorStopsShortOfTheZigguratFaces()
        {
            Assert.Equal(false, SanctumPlan.HeightAt(11f, 16.1f).HasValue, "gap before the north face");
            Assert.Equal(false, SanctumPlan.HeightAt(9.85f, 19f).HasValue, "gap before the west face");
            Assert.Equal(false, SanctumPlan.HeightAt(16.15f, 19f).HasValue, "gap before the east face");
            Assert.Equal(true, SanctumPlan.HeightAt(13.1f, 15.3f) == 0f, "the foot of the flight stays connected");
            Assert.Equal(true, SanctumPlan.HeightAt(11f, 15.7f) == 0f, "chamber floor in front of the gap");
        }

        /// <summary>Walls have no thickness to the navmesh, so separate spaces need real distance between their walkable floors.</summary>
        public static void TestSeparateSpacesKeepClearOfEachOther()
        {
            const float clearance = 0.45f, step = 0.1f;
            var leaks = new List<string>();
            bool NearDoor(float x, float y) => SanctumPlan.Doors.Any(d => SanctumPlan.InDoorway(d, x, y, 0.9f));
            string Space(float x, float y)
            {
                if (!SanctumPlan.HeightAt(x, y).HasValue)
                {
                    return null;
                }
                if (SanctumPlan.Inside(SanctumPlan.Atrium, x, y))
                {
                    return "A4A5";
                }
                bool inCore = SanctumPlan.Inside(SanctumPlan.CoreForWalking, x, y);
                return inCore ? "Inner" : "A3";
            }
            for (float y = 1f; y < 30f; y += step)
            {
                for (float x = 2f; x < 24f; x += step)
                {
                    string here = Space(x, y);
                    if (here == null || NearDoor(x, y))
                    {
                        continue;
                    }
                    for (int a = 0; a < 16; a++)
                    {
                        double angle = a * Math.PI / 8.0;
                        float ox = x + clearance * (float)Math.Cos(angle), oy = y + clearance * (float)Math.Sin(angle);
                        string there = Space(ox, oy);
                        if (there != null && there != here && !NearDoor(ox, oy))
                        {
                            leaks.Add($"{here}({x:0.0},{y:0.0})~{there}");
                            break;
                        }
                    }
                }
            }
            Assert.Equal(0, leaks.Count, "spaces too close: " + string.Join(" ", leaks.Take(40).ToArray()));
        }

        public static void TestDoorsFillTheMappedOpenings()
        {
            foreach (DoorPlan door in SanctumPlan.Doors)
            {
                double width = Math.Sqrt(Math.Pow(door.Latch.X - door.Hinge.X, 2) + Math.Pow(door.Latch.Y - door.Hinge.Y, 2));
                Assert.Equal(true, Math.Abs(width - 1.5) < 0.01, $"{door.Id} is the mapped 7.5 feet wide");
                Assert.Equal(true, SanctumPlan.InDoorway(door, door.Center.X, door.Center.Y), $"{door.Id} centre is sealed");
            }
        }

        public static void TestArrivalIsClearOfBothDoors()
        {
            foreach (GridPoint slot in new[] { SanctumPlan.Arrival }.Concat(SanctumPlan.ArrivalSlots))
            {
                Assert.Equal(false, SanctumPlan.Doors.Any(d => SanctumPlan.InDoorway(d, slot.X, slot.Y, 1f)), $"slot ({slot.X}, {slot.Y})");
            }
        }

        public static void TestZigguratFlightsAreClimbableAndClearOfTheCourses()
        {
            Ramp north = SanctumPlan.Ramps.Single(r => r.Name == "ZigguratNorthStairs");
            double run = (north.To - north.From) * SanctumPlan.MetersPerSquare;
            Assert.Equal(true, Math.Atan2(SanctumPlan.RingHeight, run) * 180.0 / Math.PI <= 45.5, "north flight at most 45 degrees");
            // Both flights end on the apex and start on the first tier, inside the channels left through the courses.
            foreach (string name in new[] { "ZigguratWestStairs", "ZigguratEastStairs" })
            {
                Ramp flight = SanctumPlan.Ramps.Single(r => r.Name == name);
                Assert.Equal(true, flight.Y0 >= SanctumPlan.SideStairNorth - 0.001f && flight.Y1 <= SanctumPlan.SideStairSouth + 0.001f, $"{name} stays in its channel");
                Assert.Equal(true, Math.Atan2(SanctumPlan.ApexHeight - SanctumPlan.RingHeight, Math.Abs(flight.To - flight.From) * SanctumPlan.MetersPerSquare) * 180.0 / Math.PI < 60.0, $"{name} is under 60 degrees");
            }
            // Walkable terrace remains behind the head of the north flight.
            Assert.Equal(true, SanctumPlan.HeightAt(11f, SanctumPlan.NorthStairHead + 0.4f) == SanctumPlan.RingHeight, "terrace beside the flight head");
            Assert.Equal(true, SanctumPlan.RingInnerNorth - SanctumPlan.NorthStairHead >= 0.85f, "passage behind the flight head");
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

        public static void TestMigrationCannotCrossEitherClosedDoor()
        {
            foreach (var door in SanctumPlan.Doors)
            {
                bool horizontal = door.Hinge.Y == door.Latch.Y;
                var before = new GridPoint(door.Center.X - (horizontal ? 0 : 1), door.Center.Y - (horizontal ? 1 : 0));
                var across = new GridPoint(2 * door.Center.X - before.X, 2 * door.Center.Y - before.Y);
                Assert.Equal(false, SanctumPlan.SameDoorSide(door, before, across), door.Id);
                Assert.Equal(true, SanctumPlan.SameDoorSide(door, before, before), "stationary");
            }
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
