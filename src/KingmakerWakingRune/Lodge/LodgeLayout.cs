using UnityEngine;

namespace KingmakerWakingRune.Lodge
{
    /// <summary>Prototype room layout in donor-scene coordinates (verified in-game; see docs/RECON.md).</summary>
    internal static class LodgeLayout
    {
        public static readonly Vector3 SpawnPosition = new Vector3(0f, 0f, 0f);
        public const float SpawnYaw = 0f;

        public static readonly Vector3 SheilaPosition = new Vector3(2f, 0f, 2f);
        public const float SheilaYaw = 180f;
    }
}
