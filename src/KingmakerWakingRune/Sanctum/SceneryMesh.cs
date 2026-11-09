using System.Collections.Generic;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// Inert scenery merged into a few meshes per material. Parts are blocks, tapered prisms, turned profiles and
    /// transformed Unity primitives; nothing here has a collider, so scenery cannot affect navigation or be clicked.
    /// </summary>
    internal sealed class SceneryMesh
    {
        private const float BlockTile = 1.2f;
        private static readonly Dictionary<PrimitiveType, Mesh> s_Primitives = new Dictionary<PrimitiveType, Mesh>();

        private readonly Dictionary<Material, ChunkedMesh> m_Meshes = new Dictionary<Material, ChunkedMesh>();

        public void Instantiate(Transform parent, string name)
        {
            foreach (KeyValuePair<Material, ChunkedMesh> entry in m_Meshes)
            {
                entry.Value.Instantiate(parent, name, entry.Key);
            }
        }

        private ChunkedMesh Mesh(Material material)
        {
            if (!m_Meshes.TryGetValue(material, out ChunkedMesh mesh))
            {
                mesh = new ChunkedMesh();
                m_Meshes[material] = mesh;
            }
            return mesh;
        }

        public void Box(Material material, Vector3 center, Vector3 size, Quaternion rotation)
        {
            Vector3 x = rotation * Vector3.right * size.x / 2f, y = rotation * Vector3.up * size.y / 2f, z = rotation * Vector3.forward * size.z / 2f;
            Prism(material, new[]
            {
                center - x - y - z, center + x - y - z, center + x - y + z, center - x - y + z,
                center - x + y - z, center + x + y - z, center + x + y + z, center - x + y + z,
            });
        }

        /// <summary>A six-faced solid: four bottom corners in ring order, then the four top corners above them.</summary>
        public void Prism(Material material, Vector3[] c)
        {
            Vector3 middle = Vector3.zero;
            foreach (Vector3 corner in c)
            {
                middle += corner;
            }
            middle /= 8f;
            ChunkedMesh mesh = Mesh(material);
            Face(mesh, middle, c[0], c[1], c[2], c[3]);
            Face(mesh, middle, c[7], c[6], c[5], c[4]);
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                Face(mesh, middle, c[i], c[j], c[j + 4], c[i + 4]);
            }
        }

        private static void Face(ChunkedMesh mesh, Vector3 middle, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            float u = (b - a).magnitude / BlockTile, v = (d - a).magnitude / BlockTile;
            Vector2 ua = Vector2.zero, ub = new Vector2(u, 0f), uc = new Vector2(u, v), ud = new Vector2(0f, v);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), (a + c) / 2f - middle) >= 0f)
            {
                mesh.Quad(a, b, c, d, ua, ub, uc, ud);
            }
            else
            {
                mesh.Quad(a, d, c, b, ua, ud, uc, ub);
            }
        }

        /// <summary>A vertical prism from an outline in ring order, with a capped top scaled about the outline's centre.</summary>
        public void Extrude(Material material, Vector3[] outline, float height, float topScale)
        {
            ChunkedMesh mesh = Mesh(material);
            Vector3 middle = Vector3.zero;
            foreach (Vector3 point in outline)
            {
                middle += point;
            }
            middle /= outline.Length;
            Vector3 apex = middle + Vector3.up * height;
            Vector3 solid = middle + Vector3.up * height / 2f;
            for (int i = 0; i < outline.Length; i++)
            {
                Vector3 a = outline[i], b = outline[(i + 1) % outline.Length];
                Vector3 ta = apex + (a - middle) * topScale, tb = apex + (b - middle) * topScale;
                Face(mesh, solid, a, b, tb, ta);
                mesh.PlanarTriangle(apex, ta, tb, BlockTile);
                mesh.PlanarTriangle(middle, b, a, BlockTile);
            }
        }

        /// <summary>A surface turned about an axis. The profile is radius, height pairs from the axis origin; squash narrows it along one side.</summary>
        public void Lathe(Material material, Vector3 origin, Vector3 axis, float[] profile, int sides = 16, float squash = 1f)
        {
            ChunkedMesh mesh = Mesh(material);
            Vector3 up = axis.normalized;
            Vector3 reference = Mathf.Abs(up.y) > 0.98f ? Vector3.forward : Vector3.up;
            Vector3 right = Vector3.Cross(reference, up).normalized, forward = Vector3.Cross(up, right).normalized;
            Vector3 Ring(float radius, float height, int side, out Vector3 normal)
            {
                float angle = side * Mathf.PI * 2f / sides;
                normal = (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle) * squash).normalized;
                return origin + up * height + (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle) * squash) * radius;
            }
            for (int p = 0; p + 3 < profile.Length; p += 2)
            {
                for (int s = 0; s < sides; s++)
                {
                    Vector3 a = Ring(profile[p], profile[p + 1], s, out Vector3 na), b = Ring(profile[p], profile[p + 1], s + 1, out Vector3 nb);
                    Vector3 c = Ring(profile[p + 2], profile[p + 3], s + 1, out Vector3 nc), d = Ring(profile[p + 2], profile[p + 3], s, out Vector3 nd);
                    Vector2 ua = new Vector2((float)s / sides * 3f, profile[p + 1]), ub = new Vector2((float)(s + 1) / sides * 3f, profile[p + 1]);
                    Vector2 uc = new Vector2(ub.x, profile[p + 3]), ud = new Vector2(ua.x, profile[p + 3]);
                    bool flat = Mathf.Abs(profile[p + 1] - profile[p + 3]) < 0.0001f;
                    if (flat)
                    {
                        na = nb = nc = nd = up;
                    }
                    bool facesOut = Vector3.Dot(Vector3.Cross(b - a, c - a), flat ? up : (na + nb)) >= 0f;
                    if (facesOut)
                    {
                        mesh.SmoothTriangle(a, b, c, ua, ub, uc, na, nb, nc);
                        mesh.SmoothTriangle(a, c, d, ua, uc, ud, na, nc, nd);
                    }
                    else
                    {
                        mesh.SmoothTriangle(a, c, b, ua, uc, ub, na, nc, nb);
                        mesh.SmoothTriangle(a, d, c, ua, ud, uc, na, nd, nc);
                    }
                }
            }
        }

        /// <summary>A flat strip between two points, lying on a surface with the given normal.</summary>
        public void Strip(Material material, Vector3 a, Vector3 b, Vector3 normal, float width, float thickness = 0.014f)
        {
            Vector3 along = b - a;
            Box(material, (a + b) / 2f, new Vector3(width, thickness, along.magnitude + width * 0.5f), Quaternion.LookRotation(along, normal));
        }

        /// <summary>A Unity primitive's surface, placed and baked into the merged mesh.</summary>
        public void Solid(PrimitiveType type, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            AddPrimitive(type, Matrix4x4.TRS(position, rotation, scale), material);
        }

        public void AddPrimitive(PrimitiveType type, Matrix4x4 matrix, Material material)
        {
            Mesh source = Primitive(type);
            ChunkedMesh mesh = Mesh(material);
            Vector3[] vertices = source.vertices, normals = source.normals;
            Vector2[] uvs = source.uv;
            int[] triangles = source.triangles;
            Matrix4x4 normalMatrix = matrix.inverse.transpose;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                mesh.SmoothTriangle(matrix.MultiplyPoint3x4(vertices[a]), matrix.MultiplyPoint3x4(vertices[b]), matrix.MultiplyPoint3x4(vertices[c]),
                    uvs[a], uvs[b], uvs[c],
                    normalMatrix.MultiplyVector(normals[a]).normalized, normalMatrix.MultiplyVector(normals[b]).normalized, normalMatrix.MultiplyVector(normals[c]).normalized);
            }
        }

        private static Mesh Primitive(PrimitiveType type)
        {
            if (!s_Primitives.TryGetValue(type, out Mesh mesh) || mesh == null)
            {
                GameObject go = GameObject.CreatePrimitive(type);
                mesh = go.GetComponent<MeshFilter>().sharedMesh;
                s_Primitives[type] = mesh;
                Object.DestroyImmediate(go);
            }
            return mesh;
        }
    }
}
