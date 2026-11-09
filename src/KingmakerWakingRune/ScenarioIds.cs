using System;
using System.Security.Cryptography;
using System.Text;
using Kingmaker.Blueprints.Root;

namespace KingmakerWakingRune
{
    /// <summary>
    /// Stable identities for every mod-owned blueprint. Saves reference these GUIDs,
    /// so they must never change once a build has been played.
    /// </summary>
    internal static class ScenarioIds
    {
        public const string PresetGuid = "296a34c4b43b4ea1979e8b0eb1eb6814";
        public const string AreaGuid = "c4afc93588db4638a08ae4f8c460207c";
        public const string EnterPointGuid = "6d784149ae074844b8a5425cb98bc262";
        public const string SheilaUnitGuid = "7a71bd693b8346779e8e7f50654abd7e";
        public const string SessionFlagGuid = "349dfb6a1a074793b98f20001fe89d99";
        public const string SetupFlagGuid = "2a40464005fa49a49f7ac53e7e86df3c";
        public const string SheilaNameGuid = "59f6be05f3d343b4838599ef21875978";

        /// <summary>Save-bound scenario schema version, stored as the session flag value.</summary>
        public const int SessionVersion = 1;

        /// <summary>Level the protagonist and every recruit are brought to (by XP, never by assigned levels).</summary>
        public const int PartyLevelTarget = 11;

        /// <summary>The shared party treasury set once for each new Waking Rune game.</summary>
        public const long StartingTreasury = 450000;

        /// <summary>
        /// Out-of-range DlcType identifying the Waking Rune as its own campaign: the selector entry,
        /// the preset's DlcCampaign and therefore the save header's CampaignDLC. DlcRoot.Get maps it
        /// to the mod's DLC data; every native type is untouched.
        /// </summary>
        public const DlcType StoryType = (DlcType)0x4B57;

        /// <summary>
        /// Donor room: the base-game Capital Library (a self-contained area, not an area part, so it
        /// carries its own navmesh). Its static, light, audio and mechanics scenes are loaded, but
        /// none of the mechanics scene's entity views are ever turned into entities.
        /// </summary>
        public const string DonorStaticScene = "CapitalLibrary_Static";

        /// <summary>Technical shell and material source for the sanctum: an underground tomb with its own navmesh.</summary>
        public const string SanctumDonorStaticScene = "DunswardTomb_Static";

        /// <summary>
        /// Stable GUID for a named mod blueprint. The name is the identity: renaming one orphans
        /// it in existing saves, so names of shipped blueprints must not change.
        /// </summary>
        public static string Derive(string name)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes("KingmakerWakingRune:" + name));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
