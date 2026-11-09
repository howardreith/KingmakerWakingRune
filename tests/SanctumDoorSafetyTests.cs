using System.Linq;
using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Tests
{
    public static class SanctumDoorSafetyTests
    {
        private const float Medium = 0.5f / SanctumPlan.MetersPerSquare;
        private const float Large = 1.1f / SanctumPlan.MetersPerSquare;

        private static DoorPlan A2 => SanctumPlan.Doors.Single(d => d.Id == "A2");
        private static DoorPlan East => SanctumPlan.Doors.Single(d => d.Id == "A4Approach");

        public static void TestUnitInDoorwayBlocksClosing()
        {
            Assert.Equal(true, SanctumPlan.ClosureBlocked(A2, 13.15f, 26.4f, 0f, Medium), "in the opening");
            Assert.Equal(true, SanctumPlan.ClosureBlocked(East, 21f, 14.6f, 0f, Medium), "in the east opening");
        }

        public static void TestLargeBodyOverlapsWhileCentreIsOutside()
        {
            const float x = 13.15f, y = 25.7f;
            Assert.Equal(false, SanctumPlan.InDoorway(A2, x, y, 0.15f), "the old centre test misses it");
            Assert.Equal(false, SanctumPlan.DoorwayOverlap(A2, x, y, 0f, Medium), "a medium body there is clear");
            Assert.Equal(true, SanctumPlan.DoorwayOverlap(A2, x, y, 0f, Large), "a large body there reaches the doorway");
            Assert.Equal(true, SanctumPlan.ClosureBlocked(A2, x, y, 0f, Large), "so closing is refused");
        }

        public static void TestLeafSweepIsGuardedOnItsOpeningSideOnly()
        {
            // A2 opens south into A3: a point in the swing, and its mirror on the stairs side.
            Assert.Equal(true, SanctumPlan.LeafSweepOverlap(A2, 13.2f, 27.2f, 0f, Medium), "inside the swing");
            Assert.Equal(false, SanctumPlan.LeafSweepOverlap(A2, 13.2f, 25.2f, 0f, Medium), "behind the door");
            // The east door opens west into the passage.
            Assert.Equal(true, SanctumPlan.LeafSweepOverlap(East, 20.2f, 14.9f, 0f, Medium), "inside the east swing");
            Assert.Equal(false, SanctumPlan.LeafSweepOverlap(East, 21.8f, 14.9f, 0f, Medium), "outside the east door");
        }

        public static void TestSomeoneBeyondTheSwingDoesNotBlock()
        {
            Assert.Equal(false, SanctumPlan.ClosureBlocked(A2, 13.15f, 29f, 0f, Medium), "well clear in A3");
            Assert.Equal(false, SanctumPlan.ClosureBlocked(East, 18.5f, 14.6f, 0f, Medium), "well clear in the passage");
        }

        public static void TestElevationSeparatesUnits()
        {
            Assert.Equal(true, SanctumPlan.ClosureBlocked(A2, 13.15f, 26.4f, 0.4f, Medium), "low on the last step");
            Assert.Equal(false, SanctumPlan.ClosureBlocked(A2, 13.15f, 26.4f, SanctumPlan.DoorLeafHeight + 0.5f, Medium), "far above the leaf");
        }

        public static void TestUsersCanStandClearOfTheSwing()
        {
            // The approach distance puts a medium user straight in front of the door outside its swing.
            float away = SanctumPlan.Doors.Length > 0 ? 3.2f / SanctumPlan.MetersPerSquare : 0f;
            Assert.Equal(false, SanctumPlan.ClosureBlocked(A2, A2.Center.X, A2.Center.Y + away, 0f, Medium), "A3 side");
            Assert.Equal(false, SanctumPlan.ClosureBlocked(East, East.Center.X - away, East.Center.Y, 0f, Medium), "passage side");
        }

        public static void TestSpacesAndGround()
        {
            Assert.Equal(SanctumSpace.Interior, SanctumPlan.SpaceAt(13f, 15f), "A1");
            Assert.Equal(SanctumSpace.Interior, SanctumPlan.SpaceAt(10.2f, 22.8f), "a terrace corner past the drawn wall");
            Assert.Equal(SanctumSpace.Hall, SanctumPlan.SpaceAt(13f, 28f), "A3");
            Assert.Equal(SanctumSpace.Atrium, SanctumPlan.SpaceAt(19f, 9.5f), "A4");
            Assert.Equal(true, SanctumPlan.IsValidGround(13f, 15f, 0.1f), "on the floor");
            Assert.Equal(false, SanctumPlan.IsValidGround(13f, 15f, 3f), "floating");
            Assert.Equal(false, SanctumPlan.IsValidGround(13f, 19.6f, SanctumPlan.RingHeight), "inside the courses");
        }

        public static void TestRingsSpreadOutward()
        {
            GridPoint first = SanctumPlan.RingsAround(5f, 5f).First();
            float distance = (float)System.Math.Sqrt((first.X - 5f) * (first.X - 5f) + (first.Y - 5f) * (first.Y - 5f));
            Assert.Equal(true, distance < 0.25f, "starts close");
            Assert.Equal(true, SanctumPlan.RingsAround(5f, 5f).Last().X != 5f, "reaches outward");
        }
    }
}
