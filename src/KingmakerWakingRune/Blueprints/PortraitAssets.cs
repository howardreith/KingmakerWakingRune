using System.IO;
using Kingmaker.Blueprints;
using UnityEngine;

namespace KingmakerWakingRune.Blueprints
{
    /// <summary>
    /// Mod-owned portrait blueprints built from the PNGs packaged with the mod. Saves and NPC blueprints
    /// reference the blueprint GUIDs; nothing is read from or written to the player's custom-portrait folder.
    /// </summary>
    internal static class PortraitAssets
    {
        public static BlueprintPortrait Create(string id)
        {
            string folder = Path.Combine(Path.Combine(Main.Mod.Path, "Portraits"), id);
            Sprite small = Load(folder, "Small.png");
            Sprite medium = Load(folder, "Medium.png");
            Sprite full = Load(folder, "Fulllength.png");
            if (small == null || medium == null || full == null)
            {
                Log.Error($"Portrait set '{id}' is incomplete in {folder}; the NPC falls back to the native placeholder.");
                return null;
            }
            var portrait = ScriptableObject.CreateInstance<BlueprintPortrait>();
            portrait.name = "KWR_Portrait_" + id;
            portrait.AssetGuid = ScenarioIds.Derive("portrait." + id);
            portrait.Data = new PortraitData(null, small, medium, full);
            return portrait;
        }

        private static Sprite Load(string folder, string file)
        {
            string path = Path.Combine(folder, file);
            if (!File.Exists(path))
            {
                return null;
            }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path)))
            {
                Log.Error($"Could not decode portrait image {path}.");
                return null;
            }
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
