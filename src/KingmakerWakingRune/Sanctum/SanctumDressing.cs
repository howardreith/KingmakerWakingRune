using System.Collections.Generic;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// Inert source-described scenery: ziggurat supports, the open A2 door, the seven A3 rune plates, the A4 altar,
    /// braziers, portal mosaic and pedestal, and the A5 suspended sarcophagus with its mist. Nothing has a collider
    /// or an entity, so none of it can be targeted, interacted with or affect navigation.
    /// </summary>
    internal static class SanctumDressing
    {
        private static readonly Color RuneGreen = new Color(0.25f, 0.95f, 0.82f);

        private static Transform s_Root;
        private static Material s_Stone;
        private static Material s_Iron;
        private static Material s_Rune;
        private static Material s_Dark;
        private static Material s_ParticleSource;

        public static void Build(Transform root, SanctumMaterials materials)
        {
            s_Root = new GameObject("Dressing").transform;
            s_Root.SetParent(root, false);
            s_Stone = Tint(materials.Rock, new Color(0.60f, 0.58f, 0.55f));
            s_Iron = Tint(materials.Rock, new Color(0.13f, 0.13f, 0.15f));
            s_Dark = Tint(materials.Rock, new Color(0.30f, 0.29f, 0.28f));
            s_Rune = Glow(materials.Rock, RuneGreen);
            s_ParticleSource = materials.Particle;

            BuildZiggurat();
            BuildConduitDoor();
            foreach (RunePlate plate in SanctumPlan.RunePlates)
            {
                BuildRunePlate(plate);
            }
            BuildAtrium(materials);
            BuildInnerSanctum(materials);
        }

        private static void BuildZiggurat()
        {
            float ring = SanctumPlan.RingHeight;
            // Petrified humans bearing the first tier on the north face, either side of the stairs.
            foreach (float x in new[] { 10.7f, 11.5f, 14.7f, 15.5f })
            {
                Figure(World(x, SanctumPlan.ZigguratNorth - 0.05f, 0f), 0f, ring * 0.98f, false);
            }
            // And the second tier, on the terrace's inner face.
            foreach (float x in new[] { 11.9f, 14.1f })
            {
                Figure(World(x, SanctumPlan.RingInnerNorth - 0.04f, ring), 0f, 1.75f, false);
            }
            foreach (float y in new[] { 17.9f, 21.2f })
            {
                Figure(World(SanctumPlan.ZigguratWest - 0.05f, y, 0f), 90f, ring * 0.98f, false);
                Figure(World(SanctumPlan.ZigguratEast + 0.05f, y, 0f), -90f, ring * 0.98f, false);
            }
            // The round finials drawn at the head of each flight on the map.
            foreach (GridPoint p in new[] { new GridPoint(11.95f, 16.25f), new GridPoint(14.28f, 16.25f), new GridPoint(11.9f, 22.95f), new GridPoint(14.4f, 22.95f) })
            {
                Solid(PrimitiveType.Cylinder, World(p, ring + 0.3f), new Vector3(0.5f, 0.3f, 0.5f), Quaternion.identity, s_Stone);
                Solid(PrimitiveType.Sphere, World(p, ring + 0.85f), Vector3.one * 0.62f, Quaternion.identity, s_Stone);
            }
            // The hand-sized apex rune: a stylish figure with no meaning, set flush into the stone.
            Vector3 apex = World(13f, 19.6f, SanctumPlan.ApexHeight + 0.01f);
            Solid(PrimitiveType.Cylinder, apex, new Vector3(0.3f, 0.01f, 0.3f), Quaternion.identity, s_Dark);
            Rune(apex + Vector3.up * 0.015f, Vector3.up, 0.12f, RuneShapes["Apex"]);
        }

        private static void BuildConduitDoor()
        {
            const float top = 3.1f;
            const float y = 26.4f;
            float jamb = 0.2f;
            // Frame around the mapped opening, with the leaf held open against the stair's west wall.
            Vector3 west = World(12.4f, y, 0f), east = World(13.9f, y, 0f);
            Solid(PrimitiveType.Cube, west + Vector3.up * top / 2f - Vector3.right * jamb / 2f, new Vector3(jamb, top, 0.5f), Quaternion.identity, s_Stone);
            Solid(PrimitiveType.Cube, east + Vector3.up * top / 2f + Vector3.right * jamb / 2f, new Vector3(jamb, top, 0.5f), Quaternion.identity, s_Stone);
            Solid(PrimitiveType.Cube, (west + east) / 2f + Vector3.up * (top + 0.15f), new Vector3(east.x - west.x + 2f * jamb, 0.3f, 0.5f), Quaternion.identity, s_Stone);

            float leafX = 12.1f;
            float height = 2.9f, width = SanctumPlan.MetersPerSquare * 1.5f;
            // The stair falls toward the door, so the open leaf sits on the stair's lower end, tipped back to be readable.
            Vector3 center = World(leafX, 25.3f, 1.0f + height / 2f);
            Vector3 normal = Quaternion.AngleAxis(-28f, Vector3.forward) * Vector3.right;
            Quaternion leaf = Quaternion.LookRotation(Vector3.forward, normal);
            Solid(PrimitiveType.Cube, center, new Vector3(height, 0.12f, width), Quaternion.LookRotation(Vector3.forward, normal) , s_Stone);
            Vector3 face = center + normal * 0.08f;
            Basis(normal, out Vector3 u, out Vector3 v);
            const float radius = 0.92f;
            Vector3 previous = Vector3.zero;
            for (int i = 0; i <= 7; i++)
            {
                // Seven-pointed star: every third vertex of a heptagon traces it in one line.
                float angle = (90f + i * 3 * 360f / 7f) * Mathf.Deg2Rad;
                Vector3 point = face + (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * radius;
                if (i > 0)
                {
                    Strip(previous, point, normal, 0.055f, s_Rune);
                }
                previous = point;
            }
            for (int i = 0; i < 7; i++)
            {
                float angle = (90f + i * 360f / 7f) * Mathf.Deg2Rad;
                Solid(PrimitiveType.Cylinder, face + (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * radius + normal * 0.012f, new Vector3(0.2f, 0.012f, 0.2f), Quaternion.FromToRotation(Vector3.up, normal), s_Rune);
            }
            // The handprint at the star's centre.
            Solid(PrimitiveType.Sphere, face + normal * 0.012f, new Vector3(0.2f, 0.02f, 0.25f), Quaternion.FromToRotation(Vector3.up, normal), s_Dark);
            for (int i = 0; i < 4; i++)
            {
                float offset = (i - 1.5f) * 0.07f;
                Strip(face + u * offset + v * 0.1f + normal * 0.02f, face + u * offset * 1.3f + v * 0.27f + normal * 0.02f, normal, 0.045f, s_Dark);
            }
            SanctumLighting.Point(s_Root, 12.6f, 25.3f, 2.8f, RuneGreen, 0.8f, 4.5f);
            _ = leaf;
        }

        private static readonly Dictionary<string, float[][]> RuneShapes = new Dictionary<string, float[][]>
        {
            { "Life", new[] { L(0, -1, 0, 1), L(0, 0.3f, -0.7f, 0.9f), L(0, 0.3f, 0.7f, 0.9f), L(0, -0.4f, -0.6f, -1), L(0, -0.4f, 0.6f, -1) } },
            { "Alacrity", new[] { L(-0.5f, 1, 0.3f, 0.25f), L(0.3f, 0.25f, -0.3f, -0.05f), L(-0.3f, -0.05f, 0.5f, -1), L(-0.75f, 0.2f, 0.75f, 0.2f) } },
            { "Defense", new[] { L(-0.8f, 0.7f, 0.8f, 0.7f), L(-0.8f, 0.7f, 0, -1), L(0.8f, 0.7f, 0, -1), L(0, 0.7f, 0, -0.45f) } },
            { "Armor", new[] { L(-0.75f, 0.45f, 0, 1), L(0, 1, 0.75f, 0.45f), L(0.75f, 0.45f, 0.75f, -0.45f), L(0.75f, -0.45f, 0, -1), L(0, -1, -0.75f, -0.45f), L(-0.75f, -0.45f, -0.75f, 0.45f), L(-0.75f, 0, 0.75f, 0) } },
            { "Control", new[] { L(-0.8f, 0.8f, 0.8f, -0.8f), L(0.8f, 0.8f, -0.8f, -0.8f), L(-0.45f, 0, 0, 0.45f), L(0, 0.45f, 0.45f, 0), L(0.45f, 0, 0, -0.45f), L(0, -0.45f, -0.45f, 0) } },
            { "Apex", new[] { L(-0.8f, -0.8f, 0, 1), L(0, 1, 0.8f, -0.8f), L(-0.55f, -0.1f, 0.55f, -0.1f) } },
        };

        private static float[] L(float a, float b, float c, float d)
        {
            return new[] { a, b, c, d };
        }

        private static void BuildRunePlate(RunePlate plate)
        {
            const float radius = SanctumPlan.MetersPerFoot * 2.5f;
            var inward = new Vector3(plate.InwardNormal.X, 0f, -plate.InwardNormal.Y).normalized;
            // Printed walls are cut away for the camera, so each plate is held on a plinth and tipped back to stay legible.
            Vector3 normal = (inward * Mathf.Cos(62f * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(62f * Mathf.Deg2Rad)).normalized;
            Vector3 foot = World(plate.Position, 0f) + inward * 0.35f;
            Vector3 center = foot + Vector3.up * 1.0f;
            Solid(PrimitiveType.Cube, foot + Vector3.up * 0.42f, new Vector3(1.0f, 0.84f, 0.5f), Quaternion.LookRotation(inward), s_Stone);
            Quaternion toNormal = Quaternion.FromToRotation(Vector3.up, normal);
            Solid(PrimitiveType.Cylinder, center, new Vector3(radius * 2f, 0.07f, radius * 2f), toNormal, s_Stone);
            Solid(PrimitiveType.Cylinder, center + normal * 0.075f, new Vector3(radius * 1.82f, 0.012f, radius * 1.82f), toNormal, s_Dark);
            Rune(center + normal * 0.1f, normal, radius * 0.72f, RuneShapes[plate.Rune]);

            // The trio of extraplanar husks framing the plate: one at each side and one crouched atop it.
            Vector3 side = Vector3.Cross(Vector3.up, inward);
            Figure(foot + side * 1.15f - inward * 0.15f, Yaw(inward), 1.85f, true);
            Figure(foot - side * 1.15f - inward * 0.15f, Yaw(inward), 1.85f, true);
            Figure(foot + Vector3.up * 1.72f - normal * 0.2f, Yaw(inward), 0.95f, true);
            SanctumLighting.Point(s_Root, plate.Position.X + plate.InwardNormal.X * 0.7f, plate.Position.Y + plate.InwardNormal.Y * 0.7f, 2.0f, RuneGreen, 0.55f, 3.6f);
        }

        private static void BuildAtrium(SanctumMaterials materials)
        {
            // Altar with its scenery, in the narrowed throat.
            GridPoint[] altar = SanctumPlan.Altar;
            Vector3 centre = World((altar[0].X + altar[2].X) / 2f, (altar[0].Y + altar[2].Y) / 2f, 0f);
            float altarX = (altar[2].X - altar[0].X) * SanctumPlan.MetersPerSquare, altarZ = (altar[2].Y - altar[0].Y) * SanctumPlan.MetersPerSquare;
            Solid(PrimitiveType.Cube, centre + Vector3.up * 0.48f, new Vector3(altarX, 0.96f, altarZ), Quaternion.identity, s_Stone);
            Solid(PrimitiveType.Cube, centre + Vector3.up * 0.99f, new Vector3(altarX + 0.14f, 0.07f, altarZ + 0.14f), Quaternion.identity, s_Dark);
            Vector3 top = centre + Vector3.up * 1.03f;
            Solid(PrimitiveType.Cube, top + new Vector3(0f, 0.04f, 0.42f), new Vector3(0.42f, 0.08f, 0.56f), Quaternion.Euler(0f, 12f, 0f), Tint(materials.Rock, new Color(0.34f, 0.12f, 0.10f)));
            Solid(PrimitiveType.Cylinder, top + new Vector3(0.18f, 0.1f, -0.1f), new Vector3(0.1f, 0.1f, 0.1f), Quaternion.identity, s_Iron);
            Solid(PrimitiveType.Sphere, top + new Vector3(-0.2f, 0.06f, -0.15f), Vector3.one * 0.12f, Quaternion.identity, Tint(materials.Rock, new Color(0.46f, 0.36f, 0.22f)));
            Solid(PrimitiveType.Sphere, top + new Vector3(-0.05f, 0.06f, -0.48f), Vector3.one * 0.1f, Quaternion.identity, Tint(materials.Rock, new Color(0.3f, 0.28f, 0.42f)));
            Solid(PrimitiveType.Sphere, top + new Vector3(0.1f, 0.03f, -0.5f), new Vector3(0.2f, 0.16f, 0.2f), Quaternion.Euler(30f, 45f, 20f), Glow(materials.Rock, new Color(0.15f, 0.85f, 0.32f)));

            foreach (GridPoint[] brazier in new[] { SanctumPlan.BrazierNorth, SanctumPlan.BrazierSouth })
            {
                GridPoint c = Centroid(brazier);
                Vector3 at = World(c, 0f);
                Solid(PrimitiveType.Cylinder, at + Vector3.up * 0.5f, new Vector3(0.11f, 0.5f, 0.11f), Quaternion.identity, s_Iron);
                Solid(PrimitiveType.Cylinder, at + Vector3.up * 0.04f, new Vector3(0.48f, 0.04f, 0.48f), Quaternion.identity, s_Iron);
                Solid(PrimitiveType.Cylinder, at + Vector3.up * 1.05f, new Vector3(0.66f, 0.1f, 0.66f), Quaternion.identity, s_Iron);
                Solid(PrimitiveType.Sphere, at + Vector3.up * 1.16f, new Vector3(0.5f, 0.17f, 0.5f), Quaternion.identity, Glow(materials.Rock, new Color(1f, 0.42f, 0.12f)));
                SanctumLighting.Point(s_Root, c.X, c.Y, 1.8f, new Color(1f, 0.55f, 0.24f), 0.8f, 5f);
            }

            // Door-sized iron-tile mosaic on the east wall: the dormant portal. Scenery only.
            const float height = 2.3f, width = 1.3f;
            Vector3 mosaicNormal = (Vector3.left * Mathf.Cos(35f * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(35f * Mathf.Deg2Rad)).normalized;
            Vector3 mosaic = World(SanctumPlan.PortalRecess, height / 2f + 0.02f) + Vector3.left * 0.1f;
            Basis(mosaicNormal, out Vector3 u, out Vector3 v);
            Quaternion panel = Quaternion.LookRotation(mosaicNormal, v);
            Solid(PrimitiveType.Cube, mosaic, new Vector3(width + 0.45f, height + 0.45f, 0.1f), panel, s_Stone);
            Solid(PrimitiveType.Cube, mosaic + mosaicNormal * 0.06f, new Vector3(width, height, 0.03f), panel, s_Iron);
            Material tile = Tint(materials.Rock, new Color(0.28f, 0.26f, 0.26f));
            for (int row = 0; row < 8; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    if ((row + column) % 2 == 0)
                    {
                        Vector3 offset = u * ((column - 1.5f) * width / 4f) + v * ((row - 3.5f) * height / 8f);
                        Solid(PrimitiveType.Cube, mosaic + mosaicNormal * 0.085f + offset, new Vector3(width / 4.4f, height / 8.8f, 0.02f), panel, tile);
                    }
                }
            }

            // The three-foot pedestal with its bowl of inscribed crystal shards.
            GridPoint pedestal = Centroid(SanctumPlan.Pedestal);
            float pedestalHeight = 3f * SanctumPlan.MetersPerFoot;
            Vector3 p = World(pedestal, 0f);
            Solid(PrimitiveType.Cylinder, p + Vector3.up * pedestalHeight / 2f, new Vector3(0.62f, pedestalHeight / 2f, 0.62f), Quaternion.identity, s_Stone);
            Solid(PrimitiveType.Cylinder, p + Vector3.up * (pedestalHeight + 0.05f), new Vector3(0.82f, 0.05f, 0.82f), Quaternion.identity, s_Dark);
            Material crystal = Glow(materials.Rock, new Color(0.65f, 0.78f, 0.95f));
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad, r = 0.08f + 0.022f * i;
                Solid(PrimitiveType.Cube, p + new Vector3(Mathf.Cos(angle) * r, pedestalHeight + 0.12f, Mathf.Sin(angle) * r), new Vector3(0.05f, 0.13f, 0.05f), Quaternion.Euler(20f + i * 11f, i * 47f, 15f), crystal);
            }

            // The rim of the opaque dome. A full dome would hide the party, so this is a cutaway.
            GridPoint[] ribs = { new GridPoint(17.1f, 6.4f), new GridPoint(19.1f, 6.98f), new GridPoint(20.25f, 8.1f), new GridPoint(20.6f, 9.6f), new GridPoint(20.25f, 11.2f), new GridPoint(19.1f, 12.1f) };
            for (int i = 1; i < ribs.Length; i++)
            {
                Strip(World(ribs[i - 1], 4.75f), World(ribs[i], 4.75f), Vector3.up, 0.36f, s_Dark, 0.3f);
            }
        }

        private static void BuildInnerSanctum(SanctumMaterials materials)
        {
            GridPoint centre = SanctumPlan.SarcophagusCenter;
            const float hang = 1.65f;
            Vector3 at = World(centre, hang);
            Quaternion yaw = Quaternion.identity;
            float length = (SanctumPlan.SarcophagusShadow[2].X - SanctumPlan.SarcophagusShadow[0].X) * SanctumPlan.MetersPerSquare;
            float width = (SanctumPlan.SarcophagusShadow[2].Y - SanctumPlan.SarcophagusShadow[0].Y) * SanctumPlan.MetersPerSquare;
            Solid(PrimitiveType.Cube, at, new Vector3(length, 0.9f, width), yaw, s_Stone);
            Solid(PrimitiveType.Cube, at + Vector3.up * 0.56f, new Vector3(length * 1.04f, 0.22f, width * 1.06f), yaw, s_Dark);
            Solid(PrimitiveType.Cube, at + Vector3.up * 0.74f, new Vector3(length * 0.74f, 0.14f, width * 0.66f), yaw, s_Stone);
            // Hairline cracks along the flank, where the vapour escapes.
            for (int i = 0; i < 12; i++)
            {
                float x = -length * 0.46f + i * length * 0.083f;
                bool north = i % 2 == 0;
                Vector3 side = Vector3.forward * (north ? width / 2f + 0.01f : -width / 2f - 0.01f);
                Solid(PrimitiveType.Cube, at + side + new Vector3(x, (i % 3 - 1) * 0.12f, 0f), new Vector3(0.04f, 0.5f, 0.015f), Quaternion.Euler(0f, 0f, 12f - i * 7f), Tint(materials.Rock, new Color(0.34f, 0.18f, 0.42f)));
            }

            float[] cornerX = { -length * 0.44f, length * 0.44f, -length * 0.44f, length * 0.44f };
            float[] cornerZ = { width * 0.42f, width * 0.42f, -width * 0.42f, -width * 0.42f };
            for (int i = 0; i < 4; i++)
            {
                Vector3 corner = at + new Vector3(cornerX[i], 0.32f, cornerZ[i]);
                GridPoint a = SanctumPlan.ChainAnchors[i];
                Vector3 anchor = World(a, 3.9f);
                // Cut-away walls cannot carry an anchor at height, so each chain runs to a corbel rising from its wall.
                Vector3 foot = World(a, 0f);
                Solid(PrimitiveType.Cube, (foot + anchor) / 2f, new Vector3(0.62f, anchor.y, 0.62f), Quaternion.identity, s_Stone);
                Solid(PrimitiveType.Sphere, anchor + Vector3.up * 0.02f, Vector3.one * 0.3f, Quaternion.identity, s_Iron);
                Chain(anchor, corner);
            }

            SanctumMist.Create(s_Root, s_ParticleSource, World(centre, 0.15f), new Vector3(9f, 0.15f, 7f));
        }

        private static void Chain(Vector3 from, Vector3 to)
        {
            const float pitch = 0.17f;
            Vector3 along = to - from;
            int links = Mathf.CeilToInt(along.magnitude / pitch);
            Quaternion look = Quaternion.LookRotation(along, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < links; i++)
            {
                Vector3 at = from + along * ((i + 0.5f) / links);
                Quaternion twist = look * Quaternion.Euler(0f, i % 2 == 0 ? 0f : 90f, 0f);
                Solid(PrimitiveType.Capsule, at, new Vector3(0.075f, 0.11f, 0.03f), twist, s_Iron);
            }
        }

        /// <summary>A figure of cut stone: a petrified human bearing weight, or a horned extraplanar husk.</summary>
        private static void Figure(Vector3 foot, float yaw, float height, bool horned)
        {
            var body = new GameObject("Figure").transform;
            body.SetParent(s_Root, false);
            body.position = foot;
            body.rotation = Quaternion.Euler(0f, yaw, 0f);
            float s = height / 1.8f;
            Part(body, PrimitiveType.Capsule, new Vector3(-0.13f, 0.42f, 0f) * s, new Vector3(0.17f, 0.42f, 0.18f) * s, Vector3.zero);
            Part(body, PrimitiveType.Capsule, new Vector3(0.13f, 0.42f, 0f) * s, new Vector3(0.17f, 0.42f, 0.18f) * s, Vector3.zero);
            Part(body, PrimitiveType.Capsule, new Vector3(0f, 1.12f, 0f) * s, new Vector3(0.44f, 0.42f, 0.28f) * s, Vector3.zero);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0.05f) * s, new Vector3(0.24f, 0.28f, 0.24f) * s, Vector3.zero);
            // Raised arms: palms braced against the load overhead.
            Part(body, PrimitiveType.Capsule, new Vector3(-0.31f, 1.58f, 0f) * s, new Vector3(0.12f, 0.36f, 0.12f) * s, new Vector3(0f, 0f, -18f));
            Part(body, PrimitiveType.Capsule, new Vector3(0.31f, 1.58f, 0f) * s, new Vector3(0.12f, 0.36f, 0.12f) * s, new Vector3(0f, 0f, 18f));
            if (horned)
            {
                Part(body, PrimitiveType.Capsule, new Vector3(-0.15f, 1.86f, 0f) * s, new Vector3(0.05f, 0.18f, 0.05f) * s, new Vector3(0f, 0f, 28f));
                Part(body, PrimitiveType.Capsule, new Vector3(0.15f, 1.86f, 0f) * s, new Vector3(0.05f, 0.18f, 0.05f) * s, new Vector3(0f, 0f, -28f));
            }
        }

        private static void Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Vector3 euler)
        {
            GameObject go = Primitive(type, s_Stone);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
        }

        private static void Rune(Vector3 center, Vector3 normal, float radius, float[][] lines)
        {
            Basis(normal, out Vector3 u, out Vector3 v);
            foreach (float[] line in lines)
            {
                Strip(center + (u * line[0] + v * line[1]) * radius, center + (u * line[2] + v * line[3]) * radius, normal, Mathf.Max(0.02f, radius * 0.1f), s_Rune);
            }
        }

        private static void Strip(Vector3 a, Vector3 b, Vector3 normal, float width, Material material, float thickness = 0.014f)
        {
            Vector3 along = b - a;
            Solid(PrimitiveType.Cube, (a + b) / 2f, new Vector3(width, thickness, along.magnitude + width * 0.5f), Quaternion.LookRotation(along, normal), material);
        }

        private static void Basis(Vector3 normal, out Vector3 u, out Vector3 v)
        {
            Vector3 reference = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.98f ? Vector3.forward : Vector3.up;
            u = Vector3.Cross(reference, normal).normalized;
            v = Vector3.Cross(normal, u).normalized;
        }

        private static float Yaw(Vector3 forward)
        {
            return Quaternion.LookRotation(forward, Vector3.up).eulerAngles.y;
        }

        private static GridPoint Centroid(GridPoint[] points)
        {
            float x = 0f, y = 0f;
            foreach (GridPoint p in points)
            {
                x += p.X;
                y += p.Y;
            }
            return new GridPoint(x / points.Length, y / points.Length);
        }

        private static Vector3 World(float x, float y, float height)
        {
            return new Vector3(SanctumPlan.ToWorldX(x), height, SanctumPlan.ToWorldZ(y));
        }

        private static Vector3 World(GridPoint p, float height)
        {
            return World(p.X, p.Y, height);
        }

        private static void Solid(PrimitiveType type, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            GameObject go = Primitive(type, material);
            go.transform.SetParent(s_Root, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.transform.rotation = rotation;
        }

        private static GameObject Primitive(PrimitiveType type, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>A copy of a native shader with a flat colour, so scenery still obeys fog of war.</summary>
        private static Material Tint(Material source, Color color)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            return new Material(source) { mainTexture = texture, mainTextureScale = Vector2.one };
        }

        private static Material Glow(Material source, Color color)
        {
            Material material = Tint(source, color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.4f);
            }
            return material;
        }
    }
}
