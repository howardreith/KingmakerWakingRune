using System.Collections.Generic;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// Inert source-described scenery: ziggurat supports, door frames, the seven wall-built A3 rune fixtures, the A4
    /// altar, braziers, portal surround and pedestal, and the A5 suspended sarcophagus with its mist. The forms lean
    /// on battered faces and flared cornices; ornament stays where the source puts it.
    /// </summary>
    internal static class SanctumDressing
    {
        private const float PlateRadius = SanctumPlan.MetersPerFoot * 2.5f;
        private const float FixtureWidth = 2.6f;
        private const float FixtureFace = 2.5f;
        private const float SarcophagusHang = 1.5f;
        private const float SarcophagusHeight = 0.85f;
        private static readonly Color RuneLight = new Color(0.25f, 0.95f, 0.82f);

        private enum Pose
        {
            Bearing,
            Anger,
            Agony,
            Despair,
        }

        private static Transform s_Root;
        private static SceneryMesh s_Mesh;
        private static SanctumPalette s_Palette;

        public static void Build(Transform root, SanctumMaterials materials)
        {
            s_Root = new GameObject("Dressing").transform;
            s_Root.SetParent(root, false);
            s_Mesh = new SceneryMesh();
            s_Palette = materials.Palette;

            BuildZiggurat();
            BuildDoorFrames();
            foreach (RunePlate plate in SanctumPlan.RunePlates)
            {
                BuildRuneFixture(plate);
            }
            BuildAtrium();
            BuildInnerSanctum();
            s_Mesh.Instantiate(s_Root, "Scenery");
        }

        private static void BuildZiggurat()
        {
            float ring = SanctumPlan.RingHeight;
            // Petrified humans bearing the first tier on the north face, either side of the stairs.
            foreach (float x in new[] { 10.7f, 11.5f, 14.7f, 15.5f })
            {
                Figure(World(x, SanctumPlan.ZigguratNorth - 0.05f, 0f), 0f, ring * 0.98f, false, Pose.Bearing);
            }
            // And the second tier, on the terrace's inner face.
            foreach (float x in new[] { 11.9f, 14.1f })
            {
                Figure(World(x, SanctumPlan.RingInnerNorth - 0.04f, ring), 0f, 1.75f, false, Pose.Bearing);
            }
            foreach (float y in new[] { 17.9f, 21.2f })
            {
                Figure(World(SanctumPlan.ZigguratWest - 0.05f, y, 0f), 90f, ring * 0.98f, false, Pose.Bearing);
                Figure(World(SanctumPlan.ZigguratEast + 0.05f, y, 0f), -90f, ring * 0.98f, false, Pose.Bearing);
            }
            // The round finials drawn at the head of each flight on the map.
            foreach (GridPoint p in new[] { new GridPoint(11.95f, 16.25f), new GridPoint(14.28f, 16.25f), new GridPoint(11.9f, 22.95f), new GridPoint(14.4f, 22.95f) })
            {
                s_Mesh.Lathe(s_Palette.Stone, World(p, ring), Vector3.up, new[] { 0.3f, 0f, 0.26f, 0.08f, 0.2f, 0.55f, 0.26f, 0.6f }, 14);
                s_Mesh.Solid(PrimitiveType.Sphere, World(p, ring + 0.88f), Vector3.one * 0.6f, Quaternion.identity, s_Palette.Stone);
            }
            // The hand-sized apex rune: a stylish figure with no meaning, set flush into the stone.
            Vector3 apex = World(13f, 19.6f, SanctumPlan.ApexHeight + 0.01f);
            s_Mesh.Lathe(s_Palette.Basalt, apex, Vector3.up, new[] { 0.3f, 0f, 0.3f, 0.012f, 0f, 0.012f }, 20);
            Rune(apex + Vector3.up * 0.018f, Vector3.up, 0.12f, RuneShapes["Apex"]);
        }

        /// <summary>
        /// Stone jambs for both doors. The A2 door faces the camera and takes a lintel with a flared cornice; the
        /// east door is seen from above, where a lintel would hide the leaf, so it keeps its jambs and threshold.
        /// </summary>
        private static void BuildDoorFrames()
        {
            foreach (DoorPlan door in SanctumPlan.Doors)
            {
                Vector3 hinge = World(door.Hinge, 0f), latch = World(door.Latch, 0f);
                Vector3 along = (latch - hinge).normalized;
                const float top = 3.15f, jamb = 0.34f;
                Jamb(hinge - along * jamb / 2f, along, jamb, top);
                Jamb(latch + along * jamb / 2f, along, jamb, top);
                Vector3 across = Vector3.Cross(Vector3.up, along);
                s_Mesh.Box(s_Palette.Basalt, (hinge + latch) / 2f + Vector3.up * 0.015f, new Vector3((latch - hinge).magnitude + 2f * jamb, 0.03f, 0.62f), Quaternion.LookRotation(across));
                if (door.Sihedron)
                {
                    Vector3 middle = (hinge + latch) / 2f + Vector3.up * (top + 0.16f);
                    float span = (latch - hinge).magnitude + 2f * jamb + 0.1f;
                    s_Mesh.Box(s_Palette.Stone, middle, new Vector3(span, 0.32f, 0.58f), Quaternion.LookRotation(across));
                    Cornice(middle + Vector3.up * 0.16f, across, along, span, 0.58f, 0.2f, 0.16f);
                    SanctumLighting.Point(s_Root, door.Center.X, door.Center.Y - 0.7f, 1.6f, RuneLight, 0.55f, 3.6f);
                }
            }
        }

        private static void Jamb(Vector3 foot, Vector3 along, float width, float height)
        {
            Vector3 across = Vector3.Cross(Vector3.up, along);
            const float depth = 0.3f, taper = 0.8f;
            Vector3 top = foot + Vector3.up * height;
            float w = width / 2f, tw = w * taper, td = depth * taper;
            s_Mesh.Prism(s_Palette.Stone, new[]
            {
                foot - along * w - across * depth, foot + along * w - across * depth, foot + along * w + across * depth, foot - along * w + across * depth,
                top - along * tw - across * td, top + along * tw - across * td, top + along * tw + across * td, top - along * tw + across * td,
            });
        }

        /// <summary>A flared cap: a course that widens as it rises, the signature of the sanctum's restrained masonry.</summary>
        private static void Cornice(Vector3 bottomCenter, Vector3 forward, Vector3 side, float width, float depth, float height, float flare)
        {
            float w = width / 2f, d = depth / 2f;
            Vector3 top = bottomCenter + Vector3.up * height;
            float tw = w + flare, td = d + flare;
            s_Mesh.Prism(s_Palette.Basalt, new[]
            {
                bottomCenter - side * w - forward * d, bottomCenter + side * w - forward * d, bottomCenter + side * w + forward * d, bottomCenter - side * w + forward * d,
                top - side * tw - forward * td, top + side * tw - forward * td, top + side * tw + forward * td, top - side * tw + forward * td,
            });
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

        /// <summary>
        /// A rune plate built into its wall: a battered stone mass rising from the wall line, leaning back into the
        /// rock by the plate's tilt, with the great round plate inlaid in its face and three figures cast around it.
        /// </summary>
        private static void BuildRuneFixture(RunePlate plate)
        {
            var inward = new Vector3(plate.InwardNormal.X, 0f, -plate.InwardNormal.Y).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, inward);
            float tilt = plate.Tilt * Mathf.Deg2Rad;
            Vector3 lean = Vector3.up * Mathf.Cos(tilt) - inward * Mathf.Sin(tilt);
            Vector3 normal = inward * Mathf.Cos(tilt) + Vector3.up * Mathf.Sin(tilt);
            Vector3 foot = World(plate.Position, 0f) + inward * plate.Depth;
            Vector3 head = foot + lean * FixtureFace;
            const float body = 0.6f;
            float w = FixtureWidth / 2f, tw = w * 0.9f;
            s_Mesh.Prism(s_Palette.Stone, new[]
            {
                foot - side * w, foot + side * w, Grounded(foot - normal * body + side * w), Grounded(foot - normal * body - side * w),
                head - side * tw, head + side * tw, head - normal * body + side * tw, head - normal * body - side * tw,
            });
            Cornice(head - lean * 0.2f - normal * body / 2f, normal, side, FixtureWidth * 0.92f, body + 0.02f, 0.24f, 0.13f);
            s_Mesh.Box(s_Palette.Basalt, foot + Vector3.up * 0.06f + inward * 0.06f, new Vector3(FixtureWidth + 0.24f, 0.12f, 0.42f), Quaternion.LookRotation(inward));

            Vector3 center = foot + lean * (FixtureFace * 0.52f) + normal * 0.01f;
            s_Mesh.Lathe(s_Palette.Void, center, normal, new[] { PlateRadius + 0.13f, 0f, PlateRadius + 0.13f, 0.012f, 0f, 0.012f }, 40);
            s_Mesh.Lathe(s_Palette.Abyssium, center + normal * 0.012f, normal, new[] { PlateRadius + 0.05f, 0f, PlateRadius + 0.05f, 0.014f, 0f, 0.014f }, 40);
            s_Mesh.Lathe(s_Palette.Stone, center + normal * 0.026f, normal, new[] { PlateRadius, 0f, PlateRadius, 0.03f, 0f, 0.03f }, 40);
            s_Mesh.Lathe(s_Palette.Basalt, center + normal * 0.056f, normal, new[] { PlateRadius * 0.9f, 0f, PlateRadius * 0.9f, 0.006f, 0f, 0.006f }, 40);
            Rune(center + normal * 0.07f, normal, PlateRadius * 0.72f, RuneShapes[plate.Rune]);

            // The trio of extraplanar husks framing the rune: anger, agony and, crouched above it, despair.
            float yaw = Yaw(inward);
            Figure(foot + side * 1.12f + inward * 0.02f, yaw, 1.8f, true, Pose.Anger);
            Figure(foot - side * 1.12f + inward * 0.02f, yaw, 1.8f, true, Pose.Agony);
            Figure(head + normal * 0.04f - Vector3.up * 0.02f, yaw, 0.92f, true, Pose.Despair);

            Vector3 lit = center + normal * 1.1f;
            SanctumLighting.Point(s_Root, lit.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX, SanctumPlan.OriginY - lit.z / SanctumPlan.MetersPerSquare, Mathf.Max(lit.y, 1.4f), RuneLight, 0.8f, 4.2f);
        }

        private static Vector3 Grounded(Vector3 p)
        {
            return new Vector3(p.x, Mathf.Max(0f, p.y), p.z);
        }

        private static void BuildAtrium()
        {
            GridPoint[] altar = SanctumPlan.Altar;
            Vector3 centre = World((altar[0].X + altar[2].X) / 2f, (altar[0].Y + altar[2].Y) / 2f, 0f);
            float altarX = (altar[2].X - altar[0].X) * SanctumPlan.MetersPerSquare, altarZ = (altar[2].Y - altar[0].Y) * SanctumPlan.MetersPerSquare;
            BuildAltar(centre, altarX, altarZ);

            GridPoint north = Centroid(SanctumPlan.BrazierNorth), south = Centroid(SanctumPlan.BrazierSouth);
            foreach (GridPoint brazier in new[] { north, south })
            {
                BuildBrazier(World(brazier, 0f));
            }
            SanctumLighting.Point(s_Root, north.X + 0.3f, (north.Y + south.Y) / 2f, 1.9f, new Color(1f, 0.55f, 0.24f), 1.0f, 5.5f);

            BuildPortalSurround();
            BuildPedestal(World(Centroid(SanctumPlan.Pedestal), 0f));
        }

        private static void BuildAltar(Vector3 centre, float sizeX, float sizeZ)
        {
            const float plinth = 0.14f, body = 0.8f, cap = 0.14f;
            s_Mesh.Box(s_Palette.Basalt, centre + Vector3.up * plinth / 2f, new Vector3(sizeX + 0.16f, plinth, sizeZ + 0.16f), Quaternion.identity);
            float bx = sizeX / 2f, bz = sizeZ / 2f, tx = bx * 0.86f, tz = bz * 0.92f;
            Vector3 foot = centre + Vector3.up * plinth, head = foot + Vector3.up * body;
            s_Mesh.Prism(s_Palette.Stone, new[]
            {
                foot + new Vector3(-bx, 0f, -bz), foot + new Vector3(bx, 0f, -bz), foot + new Vector3(bx, 0f, bz), foot + new Vector3(-bx, 0f, bz),
                head + new Vector3(-tx, 0f, -tz), head + new Vector3(tx, 0f, -tz), head + new Vector3(tx, 0f, tz), head + new Vector3(-tx, 0f, tz),
            });
            Cornice(head, Vector3.forward, Vector3.right, sizeX * 0.86f, sizeZ * 0.92f, cap, 0.11f);
            Vector3 top = head + Vector3.up * cap;

            // A book of prayers lying open, an incense burner and a few reagents, as the source lists them.
            Vector3 book = top + new Vector3(0.02f, 0.03f, 0.48f);
            s_Mesh.Box(s_Palette.Leather, book, new Vector3(0.52f, 0.02f, 0.4f), Quaternion.Euler(0f, 8f, 0f));
            s_Mesh.Box(s_Palette.Vellum, book + new Vector3(-0.12f, 0.022f, 0f), new Vector3(0.23f, 0.018f, 0.36f), Quaternion.Euler(0f, 8f, 5f));
            s_Mesh.Box(s_Palette.Vellum, book + new Vector3(0.12f, 0.022f, 0f), new Vector3(0.23f, 0.018f, 0.36f), Quaternion.Euler(0f, 8f, -5f));
            s_Mesh.Lathe(s_Palette.Iron, top + new Vector3(0.2f, 0f, -0.1f), Vector3.up, new[] { 0.09f, 0f, 0.05f, 0.03f, 0.05f, 0.08f, 0.1f, 0.15f, 0.08f, 0.17f }, 12);
            s_Mesh.Lathe(s_Palette.Stone, top + new Vector3(-0.22f, 0f, -0.18f), Vector3.up, new[] { 0.05f, 0f, 0.07f, 0.06f, 0.03f, 0.13f, 0.04f, 0.17f }, 10);
            s_Mesh.Lathe(s_Palette.Basalt, top + new Vector3(-0.24f, 0f, 0.02f), Vector3.up, new[] { 0.04f, 0f, 0.06f, 0.05f, 0.025f, 0.1f, 0.03f, 0.13f }, 10);
            // The flawless emerald that held Krune's soul, set into the surface rather than resting on it.
            Vector3 emerald = top + new Vector3(0.02f, 0.004f, -0.52f);
            s_Mesh.Lathe(s_Palette.Iron, emerald, Vector3.up, new[] { 0.16f, 0f, 0.16f, 0.012f, 0f, 0.012f }, 6);
            s_Mesh.Lathe(s_Palette.Emerald, emerald + Vector3.up * 0.012f, Vector3.up, new[] { 0.115f, 0f, 0.085f, 0.04f, 0f, 0.055f }, 6);
        }

        private static void BuildBrazier(Vector3 at)
        {
            s_Mesh.Lathe(s_Palette.Iron, at, Vector3.up, new[] { 0.25f, 0f, 0.22f, 0.05f, 0.06f, 0.1f, 0.05f, 0.92f, 0.1f, 0.97f }, 12);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                s_Mesh.Strip(s_Palette.Iron, at + outward * 0.2f + Vector3.up * 0.04f, at + outward * 0.05f + Vector3.up * 0.55f, outward, 0.05f, 0.05f);
            }
            s_Mesh.Lathe(s_Palette.Iron, at + Vector3.up * 0.95f, Vector3.up, new[] { 0.08f, 0f, 0.3f, 0.16f, 0.37f, 0.27f, 0.33f, 0.27f }, 16);
            s_Mesh.Lathe(s_Palette.Ember, at + Vector3.up * 1.2f, Vector3.up, new[] { 0.32f, 0f, 0.22f, 0.05f, 0f, 0.075f }, 12);
        }

        /// <summary>
        /// The iron tile mosaic framing a door-sized space on the east wall, set in a stone surround. The space
        /// itself is dark, flat and inert: the portal has no function in this milestone.
        /// </summary>
        private static void BuildPortalSurround()
        {
            const float tilt = 30f * Mathf.Deg2Rad, width = 1.3f, height = 2.3f, band = 0.34f;
            Vector3 inward = Vector3.left;
            Vector3 lean = Vector3.up * Mathf.Cos(tilt) - inward * Mathf.Sin(tilt);
            Vector3 normal = inward * Mathf.Cos(tilt) + Vector3.up * Mathf.Sin(tilt);
            Vector3 side = Vector3.Cross(Vector3.up, inward);
            Vector3 foot = World(SanctumPlan.PortalRecess, 0f) + inward * 0.55f;
            float faceHeight = height + band + 0.42f, half = width / 2f + band + 0.22f;
            Vector3 head = foot + lean * faceHeight;
            const float body = 0.5f;
            s_Mesh.Prism(s_Palette.Stone, new[]
            {
                foot - side * half, foot + side * half, Grounded(foot - normal * body + side * half), Grounded(foot - normal * body - side * half),
                head - side * half * 0.92f, head + side * half * 0.92f, head - normal * body + side * half * 0.92f, head - normal * body - side * half * 0.92f,
            });
            Cornice(head - lean * 0.18f - normal * body / 2f, normal, side, half * 1.84f, body + 0.02f, 0.22f, 0.12f);

            Vector3 sill = foot + lean * 0.06f + normal * 0.012f;
            Vector3 opening = sill + lean * height / 2f;
            s_Mesh.Box(s_Palette.Void, opening, new Vector3(width, height, 0.012f), Quaternion.LookRotation(normal, lean));
            const int tilesWide = 7, tilesHigh = 11;
            float tileW = (width + 2f * band) / tilesWide, tileH = (height + band) / tilesHigh;
            Vector3 corner = sill - side * (width / 2f + band);
            for (int row = 0; row < tilesHigh; row++)
            {
                for (int column = 0; column < tilesWide; column++)
                {
                    Vector3 offset = side * ((column + 0.5f) * tileW) + lean * ((row + 0.5f) * tileH);
                    bool inOpening = Mathf.Abs(column + 0.5f - tilesWide / 2f) * tileW < width / 2f && row * tileH < height;
                    if (!inOpening)
                    {
                        s_Mesh.Box((row + column) % 2 == 0 ? s_Palette.Iron : s_Palette.Basalt, corner + offset + normal * 0.012f, new Vector3(tileW * 0.92f, tileH * 0.92f, 0.024f), Quaternion.LookRotation(normal, lean));
                    }
                }
            }
        }

        /// <summary>The three-foot pedestal and its shallow bowl of a dozen inscribed crystal shards.</summary>
        private static void BuildPedestal(Vector3 p)
        {
            float height = 3f * SanctumPlan.MetersPerFoot;
            s_Mesh.Lathe(s_Palette.Stone, p, Vector3.up, new[] { 0.36f, 0f, 0.36f, 0.08f, 0.24f, 0.14f, 0.2f, height - 0.14f, 0.3f, height - 0.08f }, 18);
            s_Mesh.Lathe(s_Palette.Basalt, p + Vector3.up * (height - 0.08f), Vector3.up, new[] { 0.3f, 0f, 0.41f, 0.09f, 0.36f, 0.1f, 0f, 0.04f }, 18);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad, r = 0.06f + 0.022f * i;
                s_Mesh.Box(s_Palette.Crystal, p + new Vector3(Mathf.Cos(angle) * r, height + 0.04f, Mathf.Sin(angle) * r), new Vector3(0.05f, 0.13f, 0.05f), Quaternion.Euler(20f + i * 11f, i * 47f, 15f));
            }
        }

        private static void BuildInnerSanctum()
        {
            GridPoint centre = SanctumPlan.SarcophagusCenter;
            Vector3 at = World(centre, SarcophagusHang);
            float halfLength = (SanctumPlan.SarcophagusShadow[2].X - SanctumPlan.SarcophagusShadow[0].X) * SanctumPlan.MetersPerSquare / 2f;
            float halfWidth = (SanctumPlan.SarcophagusShadow[2].Y - SanctumPlan.SarcophagusShadow[0].Y) * SanctumPlan.MetersPerSquare / 2f;
            // The map's elongated hexagon: shoulders broad, ends drawn to a point.
            Vector3[] Outline(float scale, float y)
            {
                float a = halfLength * scale, b = halfWidth * scale;
                Vector3 c = at + Vector3.up * y;
                return new[] { c + new Vector3(-a, 0f, 0f), c + new Vector3(-a * 0.55f, 0f, b), c + new Vector3(a * 0.55f, 0f, b), c + new Vector3(a, 0f, 0f), c + new Vector3(a * 0.55f, 0f, -b), c + new Vector3(-a * 0.55f, 0f, -b) };
            }
            s_Mesh.Extrude(s_Palette.Stone, Outline(0.92f, 0f), SarcophagusHeight, 1.05f);
            s_Mesh.Extrude(s_Palette.Basalt, Outline(1.0f, SarcophagusHeight), 0.12f, 0.96f);
            s_Mesh.Extrude(s_Palette.Stone, Outline(0.78f, SarcophagusHeight + 0.12f), 0.1f, 0.8f);

            // A dozen cracks, the vapour's way out: eight jagged lines over the raised lid, four down the north flank.
            var crack = new System.Random(4026);
            float lidTop = SarcophagusHeight + 0.225f;
            for (int i = 0; i < 8; i++)
            {
                float x = -halfLength * 0.46f + i * halfLength * 0.125f;
                float z = (i % 2 == 0 ? 1f : -1f) * halfWidth * (0.3f + (float)crack.NextDouble() * 0.16f);
                Vector3 previous = at + new Vector3(x, lidTop, z);
                for (int s = 0; s < 3; s++)
                {
                    Vector3 next = previous + new Vector3(0.04f + (float)crack.NextDouble() * 0.09f, 0f, (z > 0f ? -1f : 1f) * (0.1f + (float)crack.NextDouble() * 0.09f));
                    s_Mesh.Strip(s_Palette.Crack, previous, next, Vector3.up, 0.034f);
                    previous = next;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                float x = -halfLength * 0.4f + i * halfLength * 0.28f;
                Vector3 top = at + new Vector3(x, SarcophagusHeight * 0.92f, halfWidth * 0.965f + 0.012f);
                Vector3 middle = top + new Vector3(0.08f * (i % 2 == 0 ? 1f : -1f), -0.3f, 0.006f);
                Vector3 bottom = middle + new Vector3(-0.05f * (i % 2 == 0 ? 1f : -1f), -0.3f, -0.012f);
                s_Mesh.Strip(s_Palette.Crack, top, middle, Vector3.forward, 0.034f);
                s_Mesh.Strip(s_Palette.Crack, middle, bottom, Vector3.forward, 0.034f);
            }

            float[] cornerX = { -halfLength * 0.55f, halfLength * 0.55f, -halfLength * 0.55f, halfLength * 0.55f };
            float[] cornerZ = { halfWidth, halfWidth, -halfWidth, -halfWidth };
            // Piers stand against the walls. The south-east one is held below the sight line to the arrival floor.
            float[] pierHeight = { 2.8f, 2.8f, 2.4f, 1.45f };
            for (int i = 0; i < 4; i++)
            {
                Vector3 collar = at + new Vector3(cornerX[i], 0.42f, cornerZ[i]);
                s_Mesh.Solid(PrimitiveType.Cylinder, collar, new Vector3(0.2f, 0.26f, 0.2f), Quaternion.identity, s_Palette.Iron);
                Vector3 anchor = BuildAnchorPier(SanctumPlan.ChainAnchors[i], pierHeight[i], at);
                Chain(anchor, collar);
            }

            SanctumMist.Create(s_Root, World(centre, 0.12f), new Vector3(8.2f, 0.12f, 6.2f), at, new Vector3(halfLength * 1.5f, 0.1f, halfWidth * 1.5f),
                new[] { World(Centroid(SanctumPlan.BrazierNorth), 1.26f), World(Centroid(SanctumPlan.BrazierSouth), 1.26f) });
        }

        /// <summary>A battered pier engaged with the wall, holding the iron ring a chain runs from; returns the ring.</summary>
        private static Vector3 BuildAnchorPier(GridPoint wall, float height, Vector3 toward)
        {
            Vector3 foot = World(wall, 0f);
            Vector3 inward = (new Vector3(toward.x, 0f, toward.z) - foot).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, inward);
            foot -= inward * 0.14f;
            Vector3 head = foot + Vector3.up * height;
            const float w = 0.36f, d = 0.3f, tw = 0.25f, td = 0.21f;
            s_Mesh.Prism(s_Palette.Stone, new[]
            {
                foot - side * w - inward * d, foot + side * w - inward * d, foot + side * w + inward * d, foot - side * w + inward * d,
                head - side * tw - inward * td, head + side * tw - inward * td, head + side * tw + inward * td, head - side * tw + inward * td,
            });
            Cornice(head, inward, side, tw * 2f, td * 2f, 0.14f, 0.08f);
            Vector3 ring = head + inward * (td + 0.03f) - Vector3.up * 0.12f;
            s_Mesh.Solid(PrimitiveType.Sphere, ring, Vector3.one * 0.26f, Quaternion.identity, s_Palette.Iron);
            return ring;
        }

        private static void Chain(Vector3 from, Vector3 to)
        {
            const float pitch = 0.21f;
            Vector3 along = to - from;
            int links = Mathf.CeilToInt(along.magnitude / pitch);
            Quaternion look = Quaternion.LookRotation(along, Vector3.up);
            for (int i = 0; i < links; i++)
            {
                Vector3 at = from + along * ((i + 0.5f) / links);
                Quaternion twist = look * Quaternion.Euler(0f, 0f, i % 2 == 0 ? 0f : 90f);
                s_Mesh.Box(s_Palette.Iron, at, new Vector3(0.13f, 0.06f, pitch * 1.25f), twist);
            }
        }

        /// <summary>A cast figure: a petrified human bearing weight, or a horned extraplanar husk in the given extremity.</summary>
        private static void Figure(Vector3 foot, float yaw, float height, bool horned, Pose pose)
        {
            Matrix4x4 body = Matrix4x4.TRS(foot, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
            float s = height / 1.8f;
            float lean = pose == Pose.Agony ? -9f : pose == Pose.Despair ? 16f : 0f;
            Vector3 headAt = new Vector3(0f, 1.62f, pose == Pose.Despair ? 0.18f : pose == Pose.Agony ? -0.08f : 0.05f);
            Part(body, PrimitiveType.Capsule, new Vector3(-0.13f, 0.42f, 0f) * s, new Vector3(0.17f, 0.42f, 0.18f) * s, Vector3.zero);
            Part(body, PrimitiveType.Capsule, new Vector3(0.13f, 0.42f, 0f) * s, new Vector3(0.17f, 0.42f, 0.18f) * s, Vector3.zero);
            Part(body, PrimitiveType.Capsule, new Vector3(0f, 1.12f, pose == Pose.Despair ? 0.06f : 0f) * s, new Vector3(0.44f, 0.42f, 0.28f) * s, new Vector3(lean, 0f, 0f));
            Part(body, PrimitiveType.Sphere, headAt * s, new Vector3(0.24f, 0.28f, 0.24f) * s, Vector3.zero);
            switch (pose)
            {
                case Pose.Bearing:
                    // Palms braced against the load overhead.
                    Part(body, PrimitiveType.Capsule, new Vector3(-0.31f, 1.58f, 0f) * s, new Vector3(0.12f, 0.36f, 0.12f) * s, new Vector3(0f, 0f, -18f));
                    Part(body, PrimitiveType.Capsule, new Vector3(0.31f, 1.58f, 0f) * s, new Vector3(0.12f, 0.36f, 0.12f) * s, new Vector3(0f, 0f, 18f));
                    break;
                case Pose.Anger:
                    // Fists thrown up and out.
                    Part(body, PrimitiveType.Capsule, new Vector3(-0.42f, 1.5f, 0.06f) * s, new Vector3(0.12f, 0.34f, 0.12f) * s, new Vector3(-15f, 0f, -48f));
                    Part(body, PrimitiveType.Capsule, new Vector3(0.42f, 1.5f, 0.06f) * s, new Vector3(0.12f, 0.34f, 0.12f) * s, new Vector3(-15f, 0f, 48f));
                    break;
                case Pose.Agony:
                    // Hands clawing at the skull, head thrown back.
                    Part(body, PrimitiveType.Capsule, new Vector3(-0.28f, 1.56f, 0.02f) * s, new Vector3(0.11f, 0.32f, 0.11f) * s, new Vector3(0f, 0f, 30f));
                    Part(body, PrimitiveType.Capsule, new Vector3(0.28f, 1.56f, 0.02f) * s, new Vector3(0.11f, 0.32f, 0.11f) * s, new Vector3(0f, 0f, -30f));
                    break;
                default:
                    // Arms hanging, head bowed.
                    Part(body, PrimitiveType.Capsule, new Vector3(-0.3f, 0.98f, 0.14f) * s, new Vector3(0.11f, 0.38f, 0.11f) * s, new Vector3(14f, 0f, -6f));
                    Part(body, PrimitiveType.Capsule, new Vector3(0.3f, 0.98f, 0.14f) * s, new Vector3(0.11f, 0.38f, 0.11f) * s, new Vector3(14f, 0f, 6f));
                    break;
            }
            if (horned)
            {
                Part(body, PrimitiveType.Capsule, headAt * s + new Vector3(-0.15f, 0.24f, 0f) * s, new Vector3(0.05f, 0.18f, 0.05f) * s, new Vector3(0f, 0f, 28f));
                Part(body, PrimitiveType.Capsule, headAt * s + new Vector3(0.15f, 0.24f, 0f) * s, new Vector3(0.05f, 0.18f, 0.05f) * s, new Vector3(0f, 0f, -28f));
            }
        }

        private static void Part(Matrix4x4 body, PrimitiveType type, Vector3 position, Vector3 scale, Vector3 euler)
        {
            s_Mesh.AddPrimitive(type, body * Matrix4x4.TRS(position, Quaternion.Euler(euler), scale), s_Palette.Stone);
        }

        private static void Rune(Vector3 center, Vector3 normal, float radius, float[][] lines)
        {
            Basis(normal, out Vector3 u, out Vector3 v);
            foreach (float[] line in lines)
            {
                s_Mesh.Strip(s_Palette.Abyssium, center + (u * line[0] + v * line[1]) * radius, center + (u * line[2] + v * line[3]) * radius, normal, Mathf.Max(0.02f, radius * 0.1f));
            }
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
    }
}
