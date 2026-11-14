using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KingmakerWakingRune.Blueprints
{
    /// <summary>
    /// The approved art embedded in the mod's own DLL (see the project file), so the portraits and icons work from any
    /// install: no loose files, no checkout, no custom-portrait folder. Build.ps1 checks the set and decodes each image.
    /// </summary>
    internal static class PackagedAssets
    {
        private static Dictionary<string, string> s_Names;

        /// <summary>The bytes of an embedded file such as "Portraits/SheilaHeidmarch/Small.png", or null.</summary>
        public static byte[] Read(string path)
        {
            Assembly assembly = typeof(PackagedAssets).Assembly;
            if (s_Names == null)
            {
                // MSBuild keeps the folder separator of the wildcard match, so names are normalized once here.
                s_Names = assembly.GetManifestResourceNames().ToDictionary(n => n.Replace('\\', '/'), n => n);
            }
            if (!s_Names.TryGetValue("KWR/" + path, out string name))
            {
                return null;
            }
            using (Stream stream = assembly.GetManifestResourceStream(name))
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
        }

        /// <summary>A sprite from an embedded image, or null (logged) if it is missing or does not decode.</summary>
        public static Sprite Sprite(string path, float pixelsPerUnit = 100f, FilterMode filter = FilterMode.Bilinear)
        {
            byte[] bytes = Read(path);
            if (bytes == null)
            {
                Log.Error($"Embedded image {path} is missing from the mod's DLL.");
                return null;
            }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = filter };
            if (!ImageConversion.LoadImage(texture, bytes))
            {
                Log.Error($"Could not decode embedded image {path}.");
                return null;
            }
            return UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
    }
}
