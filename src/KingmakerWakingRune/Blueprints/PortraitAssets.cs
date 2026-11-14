using Kingmaker.Blueprints;
using UnityEngine;

namespace KingmakerWakingRune.Blueprints
{
    /// <summary>
    /// Mod-owned portrait blueprints built from the PNGs embedded in the mod's DLL. Saves and NPC blueprints
    /// reference the blueprint GUIDs; nothing is read from or written to the player's custom-portrait folder.
    /// </summary>
    internal static class PortraitAssets
    {
        public static BlueprintPortrait Create(string id)
        {
            Sprite small = PackagedAssets.Sprite($"Portraits/{id}/Small.png");
            Sprite medium = PackagedAssets.Sprite($"Portraits/{id}/Medium.png");
            Sprite full = PackagedAssets.Sprite($"Portraits/{id}/Fulllength.png");
            if (small == null || medium == null || full == null)
            {
                Log.Error($"Portrait set '{id}' is incomplete in the mod's DLL; the NPC falls back to the native placeholder.");
                return null;
            }
            var portrait = ScriptableObject.CreateInstance<BlueprintPortrait>();
            portrait.name = "KWR_Portrait_" + id;
            portrait.AssetGuid = ScenarioIds.Derive("portrait." + id);
            portrait.Data = new PortraitData(null, small, medium, full);
            return portrait;
        }
    }
}
