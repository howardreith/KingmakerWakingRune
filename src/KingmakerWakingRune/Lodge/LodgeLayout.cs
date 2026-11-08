using UnityEngine;

namespace KingmakerWakingRune.Lodge
{
    /// <summary>Prototype room layout, relative to the donor office entry pose (tuned in-game; see docs/RECON.md).</summary>
    internal static class LodgeLayout
    {
        /// <summary>
        /// Sheila's position in entry-local space (x right, z forward), in metres: just in front of the
        /// reading desk (Capital Library desk at x≈-13.8, entry at x≈-21.9 facing +x), clear of the aisle.
        /// </summary>
        public static readonly Vector3 SheilaOffset = new Vector3(0f, 0f, 6f);

        /// <summary>Sheila's facing relative to the entry facing; 180 faces the arriving hero.</summary>
        public const float SheilaRelativeYaw = 180f;
    }
}
