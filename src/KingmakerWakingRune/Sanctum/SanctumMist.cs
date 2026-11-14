using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// Cosmetic floor mist and brief energy flashes in A5. Particles only; no area effect, buff or rule exists.
    /// Transparent shaders do not honour fog of war, so the effect runs only once the party has reached A4 or A5.
    /// </summary>
    internal sealed class SanctumMist : MonoBehaviour
    {
        private const float PollSeconds = 1f;
        /// <summary>The mist's particles; switched off for good when the mist dissipates (Krune's death).</summary>
        public static GameObject Effects;

        /// <summary>Set by the aftermath (and on every load after it): the particles stay off.</summary>
        public static bool Dissipated;

        private static readonly string[] ShaderCandidates = { "Sprites/Default", "Legacy Shaders/Particles/Alpha Blended", "UI/Default" };

        private GameObject m_Effects;
        private float m_NextPoll;

        public static void Create(Transform root, Vector3 center, Vector3 size, Vector3 sarcophagus, Vector3 sarcophagusSize, Vector3[] braziers)
        {
            Shader shader = null;
            foreach (string candidate in ShaderCandidates)
            {
                shader = Shader.Find(candidate);
                if (shader != null)
                {
                    Log.Info($"A5 mist shader: {candidate}.");
                    break;
                }
            }
            if (shader == null)
            {
                Log.Error("No particle shader is available; the A5 mist is omitted.");
                return;
            }

            var effects = new GameObject("A5_Effects");
            Effects = effects;
            effects.transform.SetParent(root, false);
            effects.SetActive(false);
            Texture2D puff = SoftPuff();

            var mist = new GameObject("A5_Mist");
            mist.transform.SetParent(effects.transform, false);
            mist.transform.position = center;
            Emit(mist, shader, puff, size, 26, 7f, 11f, 2.6f, 4.2f, 0.04f, new Color(0.34f, 0.22f, 0.40f, 0.46f), new Color(0.38f, 0.36f, 0.25f, 0.34f));

            // Vapour leaving the cracks and sinking to join the pool below.
            var seep = new GameObject("A5_Seep");
            seep.transform.SetParent(effects.transform, false);
            seep.transform.position = sarcophagus;
            ParticleSystem falling = Emit(seep, shader, puff, sarcophagusSize, 11, 2.2f, 3.4f, 0.5f, 1.1f, 0.08f, new Color(0.36f, 0.24f, 0.42f, 0.42f), new Color(0.34f, 0.30f, 0.24f, 0.34f));
            ParticleSystem.MainModule fall = falling.main;
            fall.gravityModifier = 0.03f;

            var sparks = new GameObject("A5_Sparks");
            sparks.transform.SetParent(effects.transform, false);
            sparks.transform.position = sarcophagus + Vector3.up * 0.1f;
            Emit(sparks, shader, puff, new Vector3(size.x * 0.5f, 1.8f, size.z * 0.5f), 7, 0.2f, 0.5f, 0.2f, 0.42f, 0.9f, new Color(0.72f, 0.92f, 0.46f, 1f), new Color(0.72f, 0.46f, 0.92f, 1f));

            // The sickly greenish-purple motes the source sees playing across A4's walls.
            var motes = new GameObject("A4_Motes");
            motes.transform.SetParent(effects.transform, false);
            motes.transform.position = sarcophagus + new Vector3(8f, 0.8f, 0f);
            Emit(motes, shader, puff, new Vector3(8f, 2.4f, 7f), 5, 2.5f, 5f, 0.1f, 0.2f, 0.25f, new Color(0.58f, 0.82f, 0.42f, 0.8f), new Color(0.66f, 0.42f, 0.86f, 0.8f));

            foreach (Vector3 brazier in braziers)
            {
                var flame = new GameObject("A4_BrazierFlame");
                flame.transform.SetParent(effects.transform, false);
                flame.transform.position = brazier;
                Emit(flame, shader, puff, new Vector3(0.26f, 0.05f, 0.26f), 22, 0.35f, 0.65f, 0.22f, 0.42f, 0.8f, new Color(1f, 0.62f, 0.2f, 0.7f), new Color(1f, 0.36f, 0.08f, 0.6f));
            }

            var watcher = new GameObject("A5_MistWatcher").AddComponent<SanctumMist>();
            watcher.transform.SetParent(root, false);
            watcher.m_Effects = effects;
        }

        private void Update()
        {
            // A save load disposes the player's state before this scene is unloaded, so the party cannot be asked for then.
            if (Dissipated || Time.unscaledTime < m_NextPoll || m_Effects.activeSelf || LoadingProcess.Instance.IsLoadingInProcess)
            {
                return;
            }
            m_NextPoll = Time.unscaledTime + PollSeconds;
            foreach (UnitEntityData unit in Game.Instance.Player.Party)
            {
                float gridX = unit.Position.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX;
                float gridY = SanctumPlan.OriginY - unit.Position.z / SanctumPlan.MetersPerSquare;
                if (SanctumPlan.Inside(SanctumPlan.Atrium, gridX, gridY))
                {
                    m_Effects.SetActive(true);
                    return;
                }
            }
        }

        private static ParticleSystem Emit(GameObject host, Shader shader, Texture texture, Vector3 size, float rate, float minLife, float maxLife, float minSize, float maxSize, float speed, Color color, Color alternate)
        {
            var system = host.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, speed);
            main.startColor = new ParticleSystem.MinMaxGradient(color, alternate);
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
            renderer.sharedMaterial = new Material(shader) { mainTexture = texture };
            return system;
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
