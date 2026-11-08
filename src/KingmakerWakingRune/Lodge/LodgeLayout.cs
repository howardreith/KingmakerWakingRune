using UnityEngine;

namespace KingmakerWakingRune.Lodge
{
    /// <summary>
    /// Room layout in world coordinates of the donor Capital Library, tuned against its navmesh (see docs/RECON.md).
    /// The party enters at (-21.9, -0.7) facing +x, towards the reading desk at x=-14. Yaw 0 faces +z.
    /// </summary>
    internal static class LodgeLayout
    {
        public static readonly Vector3 Sheila = new Vector3(-15.9f, 0f, -0.6f);
        public const float SheilaYaw = 269f;

        public const float LeaderYaw = 270f;
        public static readonly Vector3 Ambrus = new Vector3(-17.5f, 0f, 1.3f);
        public static readonly Vector3 Masked = new Vector3(-17.5f, 0f, -2.4f);

        public const float ServiceYaw = 90f;
        public static readonly Vector3 Quartermaster = new Vector3(-21.6f, 0f, 2.9f);
        public static readonly Vector3 Coordinator = new Vector3(-20.7f, 0f, -3.0f);

        /// <summary>Warding agents line the bay's two sides and face the alcoves behind them; west of x=-22.5 the door wall hides them.</summary>
        public const float NorthAgentYaw = 0f;
        public const float SouthAgentYaw = 180f;
        public static readonly Vector3[] NorthAgents =
        {
            new Vector3(-22.2f, 0f, 4.3f),
            new Vector3(-20.7f, 0f, 4.3f),
            new Vector3(-19.2f, 0f, 4.3f),
        };
        public static readonly Vector3[] SouthAgents =
        {
            new Vector3(-22.2f, 0f, -4.6f),
            new Vector3(-20.7f, 0f, -4.6f),
            new Vector3(-19.2f, 0f, -4.6f),
        };
    }
}
