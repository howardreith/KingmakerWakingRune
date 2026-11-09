using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// Runtime mesh assembly, split into spatial chunks so the forward renderer can light each piece with
    /// the lights actually near it. Vertices are world-space; UVs are supplied by the caller.
    /// </summary>
    internal sealed class ChunkedMesh
    {
        private const float ChunkSize = 7f;

        private sealed class Part
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();
        }

        private readonly Dictionary<long, Part> m_Parts = new Dictionary<long, Part>();

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            Add(Key((a + b + c) / 3f), a, b, c, ua, ub, uc, normal);
        }

        /// <summary>A triangle that keeps the supplied vertex normals, for smooth curved parts.</summary>
        public void SmoothTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Vector3 na, Vector3 nb, Vector3 nc)
        {
            Add(Key((a + b + c) / 3f), a, b, c, ua, ub, uc, na, nb, nc);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            long key = Key((a + b + c + d) / 4f);
            Add(key, a, b, c, ua, ub, uc, normal);
            Add(key, a, c, d, ua, uc, ud, normal);
        }

        /// <summary>A flat upward polygon of triangles, with world-planar UVs of the given tile size.</summary>
        public void PlanarTriangle(Vector3 a, Vector3 b, Vector3 c, float tile)
        {
            if (Vector3.Cross(b - a, c - a).y < 0f)
            {
                Vector3 t = b;
                b = c;
                c = t;
            }
            Triangle(a, b, c, Planar(a, tile), Planar(b, tile), Planar(c, tile));
        }

        public static Vector2 Planar(Vector3 p, float tile)
        {
            return new Vector2(p.x / tile, p.z / tile);
        }

        public void Instantiate(Transform parent, string name, Material material)
        {
            int index = 0;
            foreach (Part part in m_Parts.Values)
            {
                var go = new GameObject($"{name}_{index++}");
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = ToMesh(part, go.name);
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.receiveShadows = true;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>All chunks as one mesh; used for the walkable collider.</summary>
        public Mesh ToSingleMesh(string name)
        {
            var all = new Part();
            foreach (Part part in m_Parts.Values)
            {
                int offset = all.Vertices.Count;
                all.Vertices.AddRange(part.Vertices);
                all.Normals.AddRange(part.Normals);
                all.Uvs.AddRange(part.Uvs);
                foreach (int t in part.Triangles)
                {
                    all.Triangles.Add(t + offset);
                }
            }
            return ToMesh(all, name);
        }

        private static Mesh ToMesh(Part part, string name)
        {
            var mesh = new Mesh { name = name, indexFormat = part.Vertices.Count > 60000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(part.Vertices);
            mesh.SetNormals(part.Normals);
            mesh.SetUVs(0, part.Uvs);
            mesh.SetTriangles(part.Triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private void Add(long key, Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Vector3 normal)
        {
            Add(key, a, b, c, ua, ub, uc, normal, normal, normal);
        }

        private void Add(long key, Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Vector3 na, Vector3 nb, Vector3 nc)
        {
            if (!m_Parts.TryGetValue(key, out Part part))
            {
                part = new Part();
                m_Parts[key] = part;
            }
            int i = part.Vertices.Count;
            part.Vertices.Add(a);
            part.Vertices.Add(b);
            part.Vertices.Add(c);
            part.Normals.Add(na);
            part.Normals.Add(nb);
            part.Normals.Add(nc);
            part.Uvs.Add(ua);
            part.Uvs.Add(ub);
            part.Uvs.Add(uc);
            part.Triangles.Add(i);
            part.Triangles.Add(i + 1);
            part.Triangles.Add(i + 2);
        }

        private static long Key(Vector3 p)
        {
            long x = (long)Math.Floor(p.x / ChunkSize) + 1000;
            long z = (long)Math.Floor(p.z / ChunkSize) + 1000;
            return x * 100000 + z;
        }
    }
}
