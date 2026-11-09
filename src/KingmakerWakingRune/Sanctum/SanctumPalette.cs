using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// The scenery's materials: copies of a native shader, so they obey fog of war, over textures painted at load.
    /// No texture is shipped. Blue-green is reserved for the abyssium the source describes.
    /// </summary>
    internal sealed class SanctumPalette
    {
        private const int TextureSize = 256;
        private const int AshlarCourses = 4;

        public readonly Material Ashlar;
        public readonly Material Stone;
        public readonly Material Basalt;
        public readonly Material Iron;
        public readonly Material Abyssium;
        public readonly Material Crack;
        public readonly Material Emerald;
        public readonly Material Ember;
        public readonly Material Crystal;
        public readonly Material Void;
        public readonly Material Vellum;
        public readonly Material Leather;

        public SanctumPalette(Material source)
        {
            Ashlar = Textured(source, PaintAshlar());
            Stone = Textured(source, Paint(new Color(0.58f, 0.55f, 0.50f), new Color(0.44f, 0.41f, 0.37f), 11));
            Basalt = Textured(source, Paint(new Color(0.20f, 0.20f, 0.22f), new Color(0.12f, 0.12f, 0.14f), 23));
            Iron = Textured(source, Paint(new Color(0.17f, 0.16f, 0.17f), new Color(0.08f, 0.08f, 0.09f), 37));
            Void = Textured(source, Paint(new Color(0.04f, 0.04f, 0.05f), new Color(0.02f, 0.02f, 0.03f), 41));
            Vellum = Textured(source, Paint(new Color(0.66f, 0.60f, 0.47f), new Color(0.56f, 0.49f, 0.37f), 43));
            Leather = Textured(source, Paint(new Color(0.30f, 0.12f, 0.10f), new Color(0.20f, 0.08f, 0.07f), 47));
            Abyssium = Glowing(source, new Color(0.14f, 0.62f, 0.54f), new Color(0.25f, 0.95f, 0.82f), 1.2f);
            Crack = Glowing(source, new Color(0.20f, 0.10f, 0.26f), new Color(0.44f, 0.20f, 0.58f), 0.9f);
            Emerald = Glowing(source, new Color(0.08f, 0.50f, 0.22f), new Color(0.15f, 0.85f, 0.32f), 1.4f);
            Ember = Glowing(source, new Color(0.45f, 0.14f, 0.04f), new Color(1f, 0.42f, 0.12f), 1.6f);
            Crystal = Glowing(source, new Color(0.46f, 0.56f, 0.70f), new Color(0.65f, 0.78f, 0.95f), 0.9f);
        }

        private static Material Textured(Material source, Texture2D texture)
        {
            var material = new Material(source) { mainTexture = texture, mainTextureScale = Vector2.one };
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }
            return material;
        }

        private static Material Glowing(Material source, Color albedo, Color glow, float strength)
        {
            Material material = Textured(source, Paint(albedo, albedo * 0.85f, 53));
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", glow * strength);
            }
            return material;
        }

        private static Texture2D NewTexture()
        {
            return new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
        }

        private static Texture2D Paint(Color light, Color dark, int seed)
        {
            Texture2D texture = NewTexture();
            var pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    pixels[x + y * TextureSize] = Color.Lerp(dark, light, Grain(x, y, seed));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        /// <summary>Precisely cut blocks in running bond: four courses and eight blocks to a tile, with hairline joints.</summary>
        private static Texture2D PaintAshlar()
        {
            Texture2D texture = NewTexture();
            var pixels = new Color[TextureSize * TextureSize];
            Color light = new Color(0.63f, 0.59f, 0.53f), dark = new Color(0.47f, 0.44f, 0.39f), joint = new Color(0.16f, 0.15f, 0.14f);
            int course = TextureSize / AshlarCourses, block = TextureSize / 2;
            for (int y = 0; y < TextureSize; y++)
            {
                int row = y / course;
                int shifted = row % 2 == 0 ? 0 : block / 2;
                for (int x = 0; x < TextureSize; x++)
                {
                    int bx = (x + shifted) % TextureSize;
                    int column = bx / block;
                    bool isJoint = y % course < 2 || bx % block < 2;
                    float tone = Mathf.Abs(Hash(column, row, 7)) * 0.22f - 0.11f;
                    Color stone = Color.Lerp(dark, light, Grain(x, y, 5) + tone);
                    pixels[x + y * TextureSize] = isJoint ? joint : stone;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        private static float Grain(int x, int y, int seed)
        {
            float u = (float)x / TextureSize, v = (float)y / TextureSize;
            float value = 0.5f * Noise(u, v, 8, seed) + 0.3f * Noise(u, v, 21, seed + 1) + 0.2f * Noise(u, v, 64, seed + 2);
            return Mathf.Clamp01(value);
        }

        private static float Noise(float u, float v, int period, int seed)
        {
            float fx = u * period, fy = v * period;
            int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
            float tx = Mathf.SmoothStep(0f, 1f, fx - ix), ty = Mathf.SmoothStep(0f, 1f, fy - iy);
            float a = Lattice(ix, iy, period, seed), b = Lattice(ix + 1, iy, period, seed);
            float c = Lattice(ix, iy + 1, period, seed), d = Lattice(ix + 1, iy + 1, period, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Lattice(int x, int y, int period, int seed)
        {
            return Mathf.Abs(Hash(x % period, y % period, seed));
        }

        private static float Hash(int x, int y, int seed)
        {
            int h = x * 374761393 + y * 668265263 + seed * 2147483;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h ^ (h >> 16)) / 2147483648f;
        }
    }
}
