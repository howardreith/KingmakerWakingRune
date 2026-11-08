using UnityEngine;
using UnityEngine.Rendering;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// Restrained fill for CRPG readability in a chamber the source leaves dark, plus the local lights it describes.
    /// Visual only; none of this is a mechanical light source.
    /// </summary>
    internal static class SanctumLighting
    {
        private static readonly Color Fill = new Color(0.62f, 0.60f, 0.66f);
        private static LightProbes s_SavedProbes;
        private static bool s_ProbesReplaced;

        public static void Restore()
        {
            if (s_ProbesReplaced)
            {
                LightmapSettings.lightProbes = s_SavedProbes;
                s_SavedProbes = null;
                s_ProbesReplaced = false;
            }
        }

        public static void Apply(Transform root)
        {
            // The donor's baked probes describe a different room, so characters use the ambient fill instead.
            s_SavedProbes = LightmapSettings.lightProbes;
            s_ProbesReplaced = true;
            LightmapSettings.lightProbes = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.42f, 0.50f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.36f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.26f, 0.30f);
            RenderSettings.fog = false;

            // A1 and the ziggurat.
            Point(root, 13f, 14.8f, 4.2f, Fill, 1.5f, 13f);
            Point(root, 9.5f, 18.5f, 3.4f, Fill, 1.0f, 10f);
            Point(root, 16.5f, 18.5f, 3.4f, Fill, 1.0f, 10f);
            Point(root, 13f, 19.6f, 8.6f, Fill, 1.0f, 12f);
            // A3, around the perimeter.
            foreach (GridPoint p in new[]
            {
                new GridPoint(13f, 2.5f), new GridPoint(19f, 3.4f), new GridPoint(22.5f, 8f), new GridPoint(22.5f, 14f),
                new GridPoint(20.5f, 21.5f), new GridPoint(15.5f, 27f), new GridPoint(10.5f, 27f), new GridPoint(5.5f, 21.5f),
                new GridPoint(3.5f, 14f), new GridPoint(3.5f, 8f), new GridPoint(7f, 3.4f),
            })
            {
                Point(root, p.X, p.Y, 3.4f, Fill, 1.1f, 9.5f);
            }
            // The A4 approach, and the faint tan light through the dome.
            Point(root, 19.5f, 14.6f, 3f, Fill, 0.9f, 7f);
            Point(root, 18.4f, 9.6f, 5f, new Color(0.86f, 0.72f, 0.50f), 1.5f, 12f);
            // Sickly green-purple from A5, as the source describes it.
            Point(root, 9f, 9.5f, 3.6f, new Color(0.58f, 0.46f, 0.72f), 1.4f, 13f);
            Point(root, 12.5f, 9.5f, 3f, new Color(0.46f, 0.62f, 0.42f), 0.9f, 9f);
        }

        public static Light Point(Transform root, float x, float y, float height, Color color, float intensity, float range)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(SanctumPlan.ToWorldX(x), height, SanctumPlan.ToWorldZ(y));
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity * 2.2f;
            light.range = range;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            return light;
        }
    }
}
