using System.Collections.Generic;
using System.Linq;

namespace KingmakerWakingRune.Sanctum
{
    internal enum StatueFinish
    {
        Limestone,
        Sandstone,
        Greywacke,
        Basalt,
    }

    /// <summary>An installed unit model cast as stone. <see cref="Stature"/> is its height in metres standing, at scale one.</summary>
    internal sealed class StatueModel
    {
        public string Name;
        public string PrefabId;
    }

    /// <summary>A limb aimed along a direction in the figure's own frame: X to its right, Y up, Z forward.</summary>
    internal sealed class StatuePose
    {
        public string Name;
        public float[] Spine, Chest, Neck, LeftUpperArm, LeftForearm, RightUpperArm, RightForearm, LeftThigh, LeftShin, RightThigh, RightShin;
    }

    /// <summary>One authored statue: which being, how it stands, and how tall its stone is.</summary>
    internal sealed class StatueSlot
    {
        public string Id;
        public string Model;
        public string Pose;
        public StatueFinish Finish;
        /// <summary>Metres. A standing height, or for bearers the height of the load their raised hands reach.</summary>
        public float Height;
        /// <summary>Scale the posed figure to exactly <see cref="Height"/>, for bearers whose hands must meet the stone above.</summary>
        public bool FitToHeight;
    }

    /// <summary>
    /// The sanctum's 31 petrified figures, authored one by one against a compact library of installed models and poses.
    /// A1's ten supports are humans bearing stone; A3's twenty-one are drained extraplanar beings in anger, agony or despair.
    /// No game types, so the arrangement is verified directly by the unit tests.
    /// </summary>
    internal static class StatueManifest
    {
        private const float FloorStature = 1.8f;
        private const float TopStature = 0.95f;

        public static readonly StatueModel[] Models =
        {
            // Petrified humans.
            M("ManInLeathers", "1d7921380b24cfd4abdb29a60fd8ba7a"), M("Labourer", "31d6b72d1b18b8444ad026f2c9ac4c05"), M("Merchant", "1da6131c0f3eb6d48a078a63f8a3ef14"),
            M("SickOldMan", "45410509d0a16394294e6353a8ba1521"), M("Cultist", "43eda759749c881419b9de6d13e9c910"), M("Priest", "4abf4e8605271b2428febc181b65d085"),
            M("Townswoman", "22e54bb2ad13e1b4aae7ea6c980c52df"), M("Citizen", "47305f912ee3b94439a62d915f61174e"), M("Noblewoman", "3a92dc45be151f44ab07e59351dc665f"), M("GrievingWoman", "4b0f6633e77ce2e4b8bc54ab1080a340"),
            // Extraplanar beings.
            M("MovanicDeva", "69aa28050e471024baa6f2150bcc0751"), M("Ghaele", "db4e061c2d26e01408a264cc7c569daf"), M("Bralani", "255c1c746b1c31b40b16add1bb6b783e"),
            M("Erinyes", "75b74b2aef360a1418dfa7f16f4c7c28"), M("ApostateDevil", "c78e19a2f6fa01343b4a188aacf38e50"), M("Thanadaemon", "b50fa8864626ccc4cb7c2cd69575f5d2"),
            M("Astradaemon", "40e548652ef6ef54a9708dc7752b200d"), M("Axiomite", "931e5a007f18d384a8abeb9d5ba06e63"), M("EarthElemental", "1be611909d97b56438a6c93593f2adfd"),
            M("Mephit", "876a02bbe68ce9a40a581e7d52f25cdf"), M("HornedMan", "20a4791a9c88b644cbe94c411fe6919b"), M("HornedWoman", "7aac2301509d0444ebb5d7abd463c30a"), M("HornedElder", "8abdd077a991763459478b1a152ef474"),
        };

        public static readonly StatuePose[] Poses =
        {
            P("BearingHigh", V(0, 1, .04f), V(0, 1, .08f), V(0, .93f, .36f), V(-.5f, .85f, .05f), V(-.1f, .99f, 0), V(.5f, .85f, .05f), V(.1f, .99f, 0), V(-.05f, -1, .02f), V(-.02f, -1, -.03f), V(.05f, -1, .02f), V(.02f, -1, -.03f)),
            P("BearingWide", V(0, 1, .05f), V(0, 1, .1f), V(0, .94f, .34f), V(-.85f, .5f, .1f), V(-.4f, .92f, .1f), V(.85f, .5f, .1f), V(.4f, .92f, .1f), V(-.22f, -.97f, .03f), V(-.08f, -1, -.03f), V(.22f, -.97f, .03f), V(.08f, -1, -.03f)),
            P("BearingBowed", V(0, .92f, .38f), V(0, .85f, .52f), V(0, .78f, .63f), V(-.35f, .9f, -.25f), V(-.1f, .95f, -.3f), V(.35f, .9f, -.25f), V(.1f, .95f, -.3f), V(-.06f, -.98f, .18f), V(-.03f, -.98f, -.2f), V(.06f, -.98f, .18f), V(.03f, -.98f, -.2f)),
            P("BearingOneArm", V(0, 1, .08f), V(0, .98f, .15f), V(0, .95f, .3f), V(-.4f, .9f, 0), V(-.05f, 1, 0), V(.35f, -.85f, .35f), V(.1f, -.9f, .4f), V(-.08f, -.99f, .1f), V(-.03f, -1, -.08f), V(.1f, -.99f, -.05f), V(.05f, -1, -.02f)),
            P("BearingStrained", V(0, 1, .15f), V(0, .98f, .2f), V(0, .9f, .43f), V(-.55f, .8f, .15f), V(-.15f, .98f, -.1f), V(.55f, .8f, .15f), V(.15f, .98f, -.1f), V(-.25f, -.75f, .6f), V(-.1f, -.95f, -.3f), V(.25f, -.75f, .6f), V(.1f, -.95f, -.3f)),
            P("BearingHunched", V(0, .96f, .28f), V(0, .9f, .42f), V(0, .82f, .57f), V(-.7f, .65f, .2f), V(-.2f, .95f, -.2f), V(.7f, .65f, .2f), V(.2f, .95f, -.2f), V(-.1f, -.95f, .3f), V(-.05f, -.95f, -.3f), V(.1f, -.95f, .3f), V(.05f, -.95f, -.3f)),

            P("AngerFists", V(0, 1, -.06f), V(0, 1, -.12f), V(0, .96f, .25f), V(-.72f, .68f, .12f), V(-.55f, .8f, .25f), V(.72f, .68f, .12f), V(.55f, .8f, .25f), V(-.2f, -.97f, .05f), V(-.08f, -1, -.03f), V(.2f, -.97f, .05f), V(.08f, -1, -.03f)),
            P("AngerAccusing", V(0, 1, .1f), V(0, .98f, .18f), V(0, .96f, .27f), V(-.35f, -.7f, -.6f), V(-.2f, -.2f, .95f), V(.25f, .1f, .96f), V(.1f, .05f, .99f), V(-.1f, -.95f, .25f), V(-.05f, -.95f, -.1f), V(.12f, -.98f, -.2f), V(.1f, -.99f, -.05f)),
            P("AngerRoaring", V(0, 1, -.18f), V(0, .95f, -.3f), V(0, .75f, -.65f), V(-.75f, -.25f, -.6f), V(-.55f, .4f, -.7f), V(.75f, -.25f, -.6f), V(.55f, .4f, -.7f), V(-.18f, -.97f, .15f), V(-.08f, -.98f, -.15f), V(.18f, -.97f, .15f), V(.08f, -.98f, -.15f)),
            P("AgonyClutchingHead", V(0, 1, .08f), V(0, .98f, .2f), V(0, .76f, .65f), V(-.75f, .6f, .3f), V(.45f, .6f, .6f), V(.75f, .6f, .3f), V(-.45f, .6f, .6f), V(-.08f, -.98f, .15f), V(-.04f, -.98f, -.15f), V(.08f, -.98f, .15f), V(.04f, -.98f, -.15f)),
            P("AgonyArched", V(0, .98f, -.2f), V(0, .9f, -.44f), V(0, .55f, -.84f), V(-.9f, -.1f, -.4f), V(-.75f, .55f, -.35f), V(.9f, -.1f, -.4f), V(.75f, .55f, -.35f), V(-.08f, -1, .08f), V(-.03f, -1, -.1f), V(.08f, -1, .08f), V(.03f, -1, -.1f)),
            P("AgonyClutchingChest", V(0, .96f, .25f), V(0, .92f, .38f), V(0, .84f, .54f), V(-.45f, -.55f, .7f), V(.6f, .3f, .72f), V(.45f, -.55f, .7f), V(-.6f, .3f, .72f), V(-.1f, -.96f, .25f), V(-.05f, -.96f, -.25f), V(.1f, -.96f, .1f), V(.05f, -.99f, -.1f)),
            P("DespairKneeling", V(0, .95f, .3f), V(0, .85f, .52f), V(0, .74f, .67f), V(-.3f, -.95f, .15f), V(-.15f, -.9f, .35f), V(.3f, -.95f, .15f), V(.15f, -.9f, .35f), V(-.1f, -1, .1f), V(-.05f, -.1f, -.99f), V(.1f, -1, .1f), V(.05f, -.1f, -.99f)),
            P("DespairHidingFace", V(0, .86f, .5f), V(0, .72f, .7f), V(0, .3f, .95f), V(-.4f, -.25f, .88f), V(.3f, .75f, .6f), V(.4f, -.25f, .88f), V(-.3f, .75f, .6f), V(-.08f, -.96f, .25f), V(-.04f, -.96f, -.25f), V(.08f, -.96f, .25f), V(.04f, -.96f, -.25f)),
            P("DespairSeated", V(0, .97f, .2f), V(0, .85f, .52f), V(0, .72f, .69f), V(-.3f, -.6f, .75f), V(-.1f, -.35f, .93f), V(.3f, -.6f, .75f), V(.1f, -.35f, .93f), V(-.15f, -.1f, .99f), V(-.1f, -.98f, .2f), V(.15f, -.1f, .99f), V(.1f, -.98f, .2f)),
            P("DespairCrouching", V(0, .7f, .7f), V(0, .55f, .83f), V(0, .2f, .97f), V(-.4f, -.4f, .83f), V(.4f, -.2f, .9f), V(.4f, -.4f, .83f), V(-.4f, -.2f, .9f), V(-.3f, .4f, .85f), V(-.05f, -.97f, -.2f), V(.3f, .4f, .85f), V(.05f, -.97f, -.2f)),
        };

        public static readonly StatueSlot[] Slots = BuildSlots();

        public static StatueSlot Slot(string id)
        {
            return Slots.First(s => s.Id == id);
        }

        public static StatueModel Model(string name)
        {
            return Models.First(m => m.Name == name);
        }

        public static StatuePose Pose(string name)
        {
            return Poses.First(p => p.Name == name);
        }

        private static StatueSlot[] BuildSlots()
        {
            float ring = SanctumPlan.RingHeight * 0.98f;
            const float terrace = 1.75f;
            var slots = new List<StatueSlot>
            {
                // A1: petrified humans under the first tier, north, west and east faces, then the second tier.
                Bearer("Support-N1", "ManInLeathers", "BearingHigh", StatueFinish.Limestone, ring),
                Bearer("Support-N2", "Citizen", "BearingBowed", StatueFinish.Sandstone, ring),
                Bearer("Support-N3", "Merchant", "BearingWide", StatueFinish.Greywacke, ring),
                Bearer("Support-N4", "SickOldMan", "BearingStrained", StatueFinish.Limestone, ring),
                Bearer("Support-T1", "Noblewoman", "BearingOneArm", StatueFinish.Sandstone, terrace),
                Bearer("Support-T2", "Cultist", "BearingHunched", StatueFinish.Greywacke, terrace),
                Bearer("Support-W1", "Labourer", "BearingWide", StatueFinish.Sandstone, ring),
                Bearer("Support-E1", "Townswoman", "BearingHigh", StatueFinish.Greywacke, ring),
                Bearer("Support-W2", "Priest", "BearingHunched", StatueFinish.Limestone, ring),
                Bearer("Support-E2", "GrievingWoman", "BearingStrained", StatueFinish.Sandstone, ring),
            };
            // A3: a left and right figure on the floor and a smaller one crouched above each plate.
            slots.AddRange(Plate("A3a", "Bralani", "AngerFists", StatueFinish.Limestone, "HornedMan", "AgonyClutchingHead", StatueFinish.Sandstone, "Mephit", "DespairCrouching", StatueFinish.Greywacke));
            slots.AddRange(Plate("A3b", "Erinyes", "AngerAccusing", StatueFinish.Greywacke, "Axiomite", "DespairHidingFace", StatueFinish.Limestone, "Mephit", "AgonyArched", StatueFinish.Basalt));
            slots.AddRange(Plate("A3c", "Thanadaemon", "AngerRoaring", StatueFinish.Basalt, "Ghaele", "AgonyArched", StatueFinish.Sandstone, "Mephit", "DespairSeated", StatueFinish.Limestone));
            // The bound monadic deva of A3d kneels on the left, with its metal flail lying inert before it.
            slots.AddRange(Plate("A3d", "MovanicDeva", "DespairKneeling", StatueFinish.Limestone, "HornedWoman", "AgonyClutchingChest", StatueFinish.Greywacke, "Mephit", "AngerFists", StatueFinish.Sandstone));
            slots.AddRange(Plate("A3e", "ApostateDevil", "AngerRoaring", StatueFinish.Greywacke, "Astradaemon", "AgonyClutchingChest", StatueFinish.Basalt, "Ghaele", "DespairCrouching", StatueFinish.Limestone));
            slots.AddRange(Plate("A3f", "HornedElder", "AngerAccusing", StatueFinish.Sandstone, "EarthElemental", "AgonyArched", StatueFinish.Greywacke, "Axiomite", "AgonyClutchingHead", StatueFinish.Basalt));
            slots.AddRange(Plate("A3g", "Thanadaemon", "DespairHidingFace", StatueFinish.Sandstone, "Erinyes", "AgonyClutchingHead", StatueFinish.Limestone, "Bralani", "DespairCrouching", StatueFinish.Greywacke));
            return slots.ToArray();
        }

        private static IEnumerable<StatueSlot> Plate(string plate, string left, string leftPose, StatueFinish leftFinish, string right, string rightPose, StatueFinish rightFinish, string top, string topPose, StatueFinish topFinish)
        {
            yield return new StatueSlot { Id = plate + "-L", Model = left, Pose = leftPose, Finish = leftFinish, Height = FloorStature };
            yield return new StatueSlot { Id = plate + "-R", Model = right, Pose = rightPose, Finish = rightFinish, Height = FloorStature };
            yield return new StatueSlot { Id = plate + "-T", Model = top, Pose = topPose, Finish = topFinish, Height = TopStature };
        }

        private static StatueSlot Bearer(string id, string model, string pose, StatueFinish finish, float height)
        {
            return new StatueSlot { Id = id, Model = model, Pose = pose, Finish = finish, Height = height, FitToHeight = true };
        }

        private static StatueModel M(string name, string prefabId)
        {
            return new StatueModel { Name = name, PrefabId = prefabId };
        }

        private static float[] V(float x, float y, float z)
        {
            return new[] { x, y, z };
        }

        private static StatuePose P(string name, float[] spine, float[] chest, float[] neck, float[] leftUpper, float[] leftFore, float[] rightUpper, float[] rightFore, float[] leftThigh, float[] leftShin, float[] rightThigh, float[] rightShin)
        {
            return new StatuePose { Name = name, Spine = spine, Chest = chest, Neck = neck, LeftUpperArm = leftUpper, LeftForearm = leftFore, RightUpperArm = rightUpper, RightForearm = rightFore, LeftThigh = leftThigh, LeftShin = leftShin, RightThigh = rightThigh, RightShin = rightShin };
        }
    }
}
