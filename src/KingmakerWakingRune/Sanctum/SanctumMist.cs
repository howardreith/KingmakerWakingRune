using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>Cosmetic floor mist and brief energy flashes in A5. Particles only; no area effect, buff or rule exists.</summary>
    internal static class SanctumMist
    {
        public static void Create(Transform root, Material source, Vector3 center, Vector3 size)
        {
            Texture2D puff = SoftPuff();
            var mist = new GameObject("A5_Mist");
            mist.transform.SetParent(root, false);
            mist.transform.position = center;
            Emit(mist, source, puff, size, 26, 6f, 9f, 3.0f, 4.6f, 0.05f, new Color(0.44f, 0.27f, 0.56f, 0.5f));

            var sparks = new GameObject("A5_Sparks");
            sparks.transform.SetParent(root, false);
            sparks.transform.position = center + Vector3.up * 0.8f;
            Emit(sparks, source, puff, new Vector3(size.x * 0.5f, 1.5f, size.z * 0.5f), 3, 0.2f, 0.5f, 0.07f, 0.16f, 0.9f, new Color(0.72f, 0.92f, 0.46f, 1f));

            SanctumLighting.Point(root, SanctumPlan.SarcophagusCenter.X, SanctumPlan.SarcophagusCenter.Y, 1.0f, new Color(0.52f, 0.36f, 0.68f), 0.55f, 6f);
        }

        private static void Emit(GameObject host, Material source, Texture texture, Vector3 size, float rate, float minLife, float maxLife, float minSize, float maxSize, float speed, Color color)
        {
            var system = host.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, speed);
            main.startColor = color;
            main.maxParticles = 160;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true;
            main.prewarm = true;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;
            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var material = new Material(source) { mainTexture = texture };
            renderer.sharedMaterial = material;
            system.Play();
        }

        private static Texture2D SoftPuff()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            texture.Apply();
            return texture;
        }
    }
}
