using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Tests
{
    /// <summary>Checks the authored slot table. How the figures actually look is judged in the running game.</summary>
    public static class SanctumStatueTests
    {
        private static IEnumerable<StatueSlot> Supports => StatueManifest.Slots.Where(s => s.Id.StartsWith("Support"));
        private static IEnumerable<StatueSlot> Attendants => StatueManifest.Slots.Where(s => s.Id.StartsWith("A3"));

        public static void TestEveryPlacementHasASlot()
        {
            Assert.Equal(31, StatueManifest.Slots.Length, "ten supports and twenty-one attendants");
            Assert.Equal(10, Supports.Count(), "supports");
            Assert.Equal(21, Attendants.Count(), "attendants");
        }

        public static void TestEachPlateKeepsThreeFigures()
        {
            foreach (RunePlate plate in SanctumPlan.RunePlates)
            {
                Assert.Equal(3, Attendants.Count(s => s.Id.StartsWith(plate.Id + "-")), plate.Id);
            }
        }

        public static void TestRunePlatesAreUnchanged()
        {
            Assert.Equal(7, SanctumPlan.RunePlates.Count, "seven plates");
            Assert.Equal(4, SanctumPlan.RunePlates.Select(p => p.Rune).Distinct().Count() - 1, "five rune types, counting the three Life plates once");
        }

        public static void TestEverySlotResolves()
        {
            foreach (StatueSlot slot in StatueManifest.Slots)
            {
                StatueManifest.Model(slot.Model);
                StatueManifest.Pose(slot.Pose);
                Assert.Equal(true, slot.Height > 0.5f, slot.Id);
            }
        }

        public static void TestNoTwoFiguresAreTheSameCombination()
        {
            Assert.Equal(31, StatueManifest.Slots.Select(s => s.Model + "/" + s.Pose).Distinct().Count(), "model and pose pairs");
        }

        public static void TestNoPlateRepeatsAnotherPlatesCast()
        {
            var seen = new HashSet<string>();
            foreach (RunePlate plate in SanctumPlan.RunePlates)
            {
                string cast = string.Join("+", Attendants.Where(s => s.Id.StartsWith(plate.Id + "-")).Select(s => s.Model).OrderBy(m => m).ToArray());
                Assert.Equal(true, seen.Add(cast), $"{plate.Id} has its own trio");
            }
        }

        public static void TestA3IsNotOneKindOfBeing()
        {
            Assert.Equal(true, Attendants.Select(s => s.Model).Distinct().Count() >= 10, "a varied host");
            Assert.Equal(true, StatueManifest.Slots.Select(s => s.Model).Distinct().Count() >= 20, "library in use");
        }

        public static void TestA3PosesStayInTheSourceRegister()
        {
            foreach (StatueSlot slot in Attendants)
            {
                Assert.Equal(true, slot.Pose.StartsWith("Anger") || slot.Pose.StartsWith("Agony") || slot.Pose.StartsWith("Despair"), slot.Id);
            }
        }

        public static void TestSupportsAreHumansBearingStone()
        {
            var humans = new[] { "ManInLeathers", "Labourer", "Merchant", "SickOldMan", "Cultist", "Priest", "Townswoman", "Citizen", "Noblewoman", "GrievingWoman" };
            foreach (StatueSlot slot in Supports)
            {
                Assert.Equal(true, humans.Contains(slot.Model), slot.Id);
                Assert.Equal(true, slot.Pose.StartsWith("Bearing"), slot.Id);
                Assert.Equal(true, slot.FitToHeight, slot.Id);
            }
            Assert.Equal(10, Supports.Select(s => s.Model).Distinct().Count(), "ten different people");
        }

        public static void TestA3dKeepsTheDeva()
        {
            Assert.Equal("MovanicDeva", StatueManifest.Slot("A3d-L").Model, "the bound deva slot");
        }

        public static void TestPosesAreCompleteDirections()
        {
            foreach (StatuePose pose in StatueManifest.Poses)
            {
                foreach (float[] v in new[] { pose.Spine, pose.Chest, pose.Neck, pose.LeftUpperArm, pose.LeftForearm, pose.RightUpperArm, pose.RightForearm, pose.LeftThigh, pose.LeftShin, pose.RightThigh, pose.RightShin })
                {
                    Assert.Equal(3, v.Length, pose.Name);
                    Assert.Equal(true, v[0] * v[0] + v[1] * v[1] + v[2] * v[2] > 0.25f, pose.Name);
                }
            }
        }
    }
}
