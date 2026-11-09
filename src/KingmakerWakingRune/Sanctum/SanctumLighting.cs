using UnityEngine;
using UnityEngine.Rendering;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// Restrained fill for CRPG readability in a chamber the source leaves dark, plus the local lights it describes.
    /// Visual only; none of this is a mechanical light source.
    /// </summary>
    /// <summary>The short-lived flashes of energy over the sarcophagus: visual only, never a hazard.</summary>
    internal sealed class SanctumFlash : MonoBehaviour
    {
        private const float Dim = 0.35f;
        private const float Bright = 1.5f;

        private Light m_Light;
        private float m_NextFlash;
        private float m_FlashEnds;

        private void Awake()
        {
            m_Light = GetComponent<Light>();
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (now >= m_NextFlash)
            {
                m_FlashEnds = now + Random.Range(0.06f, 0.2f);
                m_NextFlash = now + Random.Range(0.8f, 3.2f);
            }
            m_Light.intensity = now < m_FlashEnds ? Bright : Dim;
        }
    }

    internal static class SanctumLighting
    {
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

        public static void ApplyEnvironment()
        {
            // The donor's baked probes describe a different room, so characters use the ambient fill instead.
            if (!s_ProbesReplaced)
            {
                s_SavedProbes = LightmapSettings.lightProbes;
                s_ProbesReplaced = true;
            }
            LightmapSettings.lightProbes = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.26f, 0.26f, 0.33f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.21f, 0.27f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.15f, 0.19f);
            RenderSettings.fog = false;
        }

        public static void Apply(Transform root)
        {
            // Straight down: the isometric camera is never in its mirror direction, so glossy stone shows no hotspots,
            // and one light cannot overrun the per-object light limit that seams many small fills between mesh chunks.
            var fill = new GameObject("Fill");
            fill.transform.SetParent(root, false);
            fill.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            var directional = fill.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.color = new Color(0.78f, 0.76f, 0.80f);
            directional.intensity = 0.58f;
            directional.shadows = LightShadows.None;
            directional.renderMode = LightRenderMode.ForcePixel;

            // A weaker fill from the camera's side lights the faces the top-down fill cannot reach. Its half-vector
            // with the view is far from vertical, so it adds no highlight on the floor.
            var faces = new GameObject("FaceFill");
            faces.transform.SetParent(root, false);
            faces.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -0.62f, -0.78f), Vector3.up);
            var faceLight = faces.AddComponent<Light>();
            faceLight.type = LightType.Directional;
            faceLight.color = new Color(0.74f, 0.72f, 0.78f);
            faceLight.intensity = 0.42f;
            faceLight.shadows = LightShadows.None;
            faceLight.renderMode = LightRenderMode.ForcePixel;

            // The faint tan light through the A4 dome, and the sickly green-purple from A5, as the source describes them.
            Point(root, 18.4f, 9.6f, 6.5f, new Color(0.86f, 0.72f, 0.50f), 1.7f, 13f);
            Point(root, 9f, 11.5f, 6.5f, new Color(0.58f, 0.46f, 0.72f), 1.15f, 13f);
            Light flash = Point(root, 8.9f, 9.5f, 2.6f, new Color(0.62f, 0.86f, 0.46f), 0.7f, 8f);
            flash.gameObject.AddComponent<SanctumFlash>();
        }

        public static Light Point(Transform root, float x, float y, float height, Color color, float intensity, float range)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(SanctumPlan.ToWorldX(x), height, SanctumPlan.ToWorldZ(y));
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            return light;
        }
    }
}
