using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.View;
using Kingmaker.Visual.Animation;
using Kingmaker.Visual.CharacterSystem;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// Casts installed unit models as still stone figures. A prefab is cloned under an inactive parent, so none of its
    /// components ever wake, posed by aiming its limbs, baked to a plain mesh, and the clone destroyed. What remains is a
    /// mesh and a stone copy of a material: no collider, animator, unit or light, and nothing that updates.
    /// </summary>
    internal sealed class StatueBuilder
    {
        public const string ObjectPrefix = "KWR_Statue_";
        private const int MaxTextureSize = 512;
        private const float NoiseStrength = 0.1f;

        private static readonly string[] IdleClips = { "Idle", "Idle00", "Idle02" };

        private static readonly string[][] SpineLower = { new[] { "Spine_1" }, new[] { "Spine_2", "BackB_M" } };
        private static readonly string[][] SpineUpper = { new[] { "Spine_2", "BackB_M" }, new[] { "Spine_3", "Chest_M" } };
        private static readonly string[][] NeckToHead = { new[] { "Neck", "Neck_M", "UpperTorso", "Neck0" }, new[] { "Head", "Head_M", "Neck1" } };
        private static readonly string[][] LeftUpperArm = { new[] { "L_Up_arm", "L_Arm_Upper", "Shoulder_L" }, new[] { "L_ForeArm", "L_Arm_Lower", "Elbow_L" } };
        private static readonly string[][] LeftForearm = { new[] { "L_ForeArm", "L_Arm_Lower", "Elbow_L" }, new[] { "L_Hand", "L_Palm", "Wrist_L" } };
        private static readonly string[][] RightUpperArm = { new[] { "R_Up_arm", "R_Arm_Upper", "Shoulder_R" }, new[] { "R_ForeArm", "R_Arm_Lower", "Elbow_R" } };
        private static readonly string[][] RightForearm = { new[] { "R_ForeArm", "R_Arm_Lower", "Elbow_R" }, new[] { "R_Hand", "R_Palm", "Wrist_R" } };
        private static readonly string[][] LeftThigh = { new[] { "L_Up_leg", "L_Leg0_Upper" }, new[] { "L_leg", "L_Leg0_Lower" } };
        private static readonly string[][] LeftShin = { new[] { "L_leg", "L_Leg0_Lower" }, new[] { "L_foot", "L_Foot0" } };
        private static readonly string[][] RightThigh = { new[] { "R_Up_leg", "R_Leg0_Upper" }, new[] { "R_leg", "R_Leg0_Lower" } };
        private static readonly string[][] RightShin = { new[] { "R_leg", "R_Leg0_Lower" }, new[] { "R_foot", "R_Foot0" } };

        private readonly Transform m_Root;
        private readonly Material m_StoneSource;
        private readonly GameObject m_Holder;
        private readonly Dictionary<string, Material> m_Stone = new Dictionary<string, Material>();

        public StatueBuilder(Transform root, Material stoneSource)
        {
            m_Root = root;
            m_StoneSource = stoneSource;
            m_Holder = new GameObject("KWR_StatueWorkbench");
            m_Holder.SetActive(false);
            m_Holder.transform.SetParent(root, false);
        }

        /// <summary>Removes the workbench and releases the donor prefabs once every figure has been cast.</summary>
        public void Finish()
        {
            UnityEngine.Object.DestroyImmediate(m_Holder);
            foreach (StatueModel model in StatueManifest.Models)
            {
                ResourcesLibrary.TryUnloadResource(model.PrefabId);
            }
        }

        /// <summary>Casts one authored slot at a floor position, facing along the yaw. A figure that cannot be cast is reported and skipped.</summary>
        public void Place(string slotId, Vector3 foot, float yaw)
        {
            StatueSlot slot = StatueManifest.Slot(slotId);
            try
            {
                Cast(slot, foot, yaw);
            }
            catch (Exception ex)
            {
                Log.Error($"Statue {slotId} could not be cast from {slot.Model}.", ex);
            }
        }

        private void Cast(StatueSlot slot, Vector3 foot, float yaw)
        {
            StatueModel model = StatueManifest.Model(slot.Model);
            var prefab = ResourcesLibrary.TryGetResource<UnitEntityView>(model.PrefabId, true);
            if (prefab == null)
            {
                Log.Error($"Statue {slot.Id}: prefab {model.Name} is not in the installed game.");
                return;
            }
            GameObject clone = UnityEngine.Object.Instantiate(prefab.gameObject, m_Holder.transform);
            try
            {
                clone.transform.localPosition = Vector3.zero;
                clone.transform.localRotation = Quaternion.identity;
                clone.transform.localScale = Vector3.one;
                Dictionary<string, Transform> bones = BonesOf(clone);
                float standing = Stature(clone);
                SampleIdle(clone);
                ApplyPose(bones, StatueManifest.Pose(slot.Pose));
                Bake(clone, slot, standing, foot, yaw);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static Dictionary<string, Transform> BonesOf(GameObject clone)
        {
            var bones = new Dictionary<string, Transform>();
            foreach (Transform t in clone.GetComponentsInChildren<Transform>(true))
            {
                string name = t.name.Trim();
                if (!bones.ContainsKey(name))
                {
                    bones[name] = t;
                }
            }
            return bones;
        }

        /// <summary>The figure's height as modelled, before any pose: its bind pose or first idle frame.</summary>
        private static float Stature(GameObject clone)
        {
            float low = float.MaxValue, high = float.MinValue;
            foreach (SkinnedMeshRenderer renderer in Castable(clone))
            {
                var baked = new Mesh();
                renderer.BakeMesh(baked);
                Matrix4x4 local = BakeSpace(renderer);
                foreach (Vector3 v in baked.vertices)
                {
                    float y = local.MultiplyPoint3x4(v).y;
                    low = Mathf.Min(low, y);
                    high = Mathf.Max(high, y);
                }
                UnityEngine.Object.DestroyImmediate(baked);
            }
            return high > low ? high - low : 1.8f;
        }

        private static void SampleIdle(GameObject clone)
        {
            if (clone.GetComponentInChildren<Character>(true) != null)
            {
                return;
            }
            var manager = clone.GetComponentInChildren<AnimationManager>(true);
            Animator animator = clone.GetComponentInChildren<Animator>(true);
            if (manager?.AnimationSet == null || animator == null)
            {
                return;
            }
            AnimationClip idle = manager.AnimationSet.Actions.Where(a => a != null && a.Clips != null).SelectMany(a => a.Clips).FirstOrDefault(c => c != null && IdleClips.Contains(c.name));
            if (idle != null)
            {
                idle.SampleAnimation(animator.gameObject, 0f);
            }
        }

        private static void ApplyPose(Dictionary<string, Transform> bones, StatuePose pose)
        {
            Aim(bones, SpineLower, pose.Spine);
            Aim(bones, SpineUpper, pose.Chest);
            Aim(bones, NeckToHead, pose.Neck);
            Aim(bones, LeftUpperArm, pose.LeftUpperArm);
            Aim(bones, LeftForearm, pose.LeftForearm);
            Aim(bones, RightUpperArm, pose.RightUpperArm);
            Aim(bones, RightForearm, pose.RightForearm);
            Aim(bones, LeftThigh, pose.LeftThigh);
            Aim(bones, LeftShin, pose.LeftShin);
            Aim(bones, RightThigh, pose.RightThigh);
            Aim(bones, RightShin, pose.RightShin);
        }

        /// <summary>Turns a bone so the line to its child runs along a direction. Rigs without this bone are left as they are.</summary>
        private static void Aim(Dictionary<string, Transform> bones, string[][] segment, float[] direction)
        {
            Transform from = Find(bones, segment[0]), to = Find(bones, segment[1]);
            if (from == null || to == null)
            {
                return;
            }
            Vector3 current = to.position - from.position;
            if (current.sqrMagnitude < 0.000001f)
            {
                return;
            }
            from.rotation = Quaternion.FromToRotation(current, new Vector3(direction[0], direction[1], direction[2])) * from.rotation;
        }

        private static Transform Find(Dictionary<string, Transform> bones, string[] names)
        {
            foreach (string name in names)
            {
                if (bones.TryGetValue(name, out Transform bone))
                {
                    return bone;
                }
            }
            return null;
        }

        /// <summary>Renderers that are the creature itself: not effects, shells, weapons or skinless props.</summary>
        private static IEnumerable<SkinnedMeshRenderer> Castable(GameObject clone)
        {
            foreach (SkinnedMeshRenderer renderer in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (renderer.sharedMesh != null && renderer.bones.Length > 0 && material != null && !material.shader.name.Contains("Particles")
                    && material.mainTexture != null && material.mainTexture.width > 4 && renderer.name.IndexOf("weapon", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    yield return renderer;
                }
            }
        }

        private static Matrix4x4 BakeSpace(SkinnedMeshRenderer renderer)
        {
            // BakeMesh leaves the renderer's own scale out of the vertices.
            return Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one);
        }

        private void Bake(GameObject clone, StatueSlot slot, float standing, Vector3 foot, float yaw)
        {
            var parts = new List<(Vector3[] Vertices, Vector3[] Normals, Vector2[] Uv, int[] Triangles, Material Source)>();
            float low = float.MaxValue, high = float.MinValue;
            foreach (SkinnedMeshRenderer renderer in Castable(clone))
            {
                var baked = new Mesh();
                renderer.BakeMesh(baked);
                Matrix4x4 local = BakeSpace(renderer);
                Vector3[] vertices = baked.vertices, normals = baked.normals;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = local.MultiplyPoint3x4(vertices[i]);
                    normals[i] = local.MultiplyVector(normals[i]).normalized;
                    low = Mathf.Min(low, vertices[i].y);
                    high = Mathf.Max(high, vertices[i].y);
                }
                parts.Add((vertices, normals, baked.uv, baked.triangles, renderer.sharedMaterial));
                UnityEngine.Object.DestroyImmediate(baked);
            }
            if (parts.Count == 0)
            {
                Log.Error($"Statue {slot.Id}: {slot.Model} has nothing to cast.");
                return;
            }
            float scale = slot.Height / (slot.FitToHeight ? high - low : standing);
            Matrix4x4 place = Matrix4x4.TRS(foot, Quaternion.Euler(0f, yaw, 0f), Vector3.one * scale) * Matrix4x4.Translate(Vector3.down * low);
            foreach (var group in parts.GroupBy(p => p.Source))
            {
                var mesh = new Mesh { name = ObjectPrefix + slot.Id };
                var vertices = new List<Vector3>();
                var normals = new List<Vector3>();
                var uvs = new List<Vector2>();
                var triangles = new List<int>();
                foreach (var part in group)
                {
                    int offset = vertices.Count;
                    vertices.AddRange(part.Vertices.Select(v => place.MultiplyPoint3x4(v)));
                    normals.AddRange(part.Normals.Select(n => place.MultiplyVector(n).normalized));
                    uvs.AddRange(part.Uv);
                    triangles.AddRange(part.Triangles.Select(t => t + offset));
                }
                mesh.indexFormat = vertices.Count > 60000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                var figure = new GameObject(ObjectPrefix + slot.Id);
                figure.transform.SetParent(m_Root, false);
                figure.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = figure.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = StoneOf(group.Key, slot.Finish);
                renderer.receiveShadows = true;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>A stone copy of the sanctum's own material, using the model's painted detail as carved relief.</summary>
        private Material StoneOf(Material source, StatueFinish finish)
        {
            string key = source.GetInstanceID() + "/" + finish;
            if (m_Stone.TryGetValue(key, out Material stone))
            {
                return stone;
            }
            stone = new Material(m_StoneSource) { mainTexture = Carve(source.mainTexture, finish), mainTextureScale = Vector2.one, name = "KWR_Statue_" + finish };
            if (stone.HasProperty("_Color"))
            {
                stone.SetColor("_Color", Color.white);
            }
            m_Stone[key] = stone;
            return stone;
        }

        private static Texture2D Carve(Texture source, StatueFinish finish)
        {
            int size = Mathf.Min(MaxTextureSize, Mathf.Max(source.width, source.height));
            RenderTexture target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = SanctumPalette.OwnedTexturePrefix + "Statue" };
            texture.ReadPixels(new Rect(0f, 0f, size, size), 0, 0, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            Color light = Tone(finish);
            Color[] pixels = texture.GetPixels();
            var grain = new System.Random(4026 + (int)finish);
            float contrastLow = 0.34f, contrastHigh = 1.05f;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float value = Mathf.Lerp(contrastLow, contrastHigh, Mathf.Clamp01(0.25f + c.r * 0.3f + c.g * 0.5f + c.b * 0.2f));
                float speck = 1f + ((float)grain.NextDouble() - 0.5f) * NoiseStrength;
                pixels[i] = new Color(light.r * value * speck, light.g * value * speck, light.b * value * speck, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        private static Color Tone(StatueFinish finish)
        {
            switch (finish)
            {
                case StatueFinish.Sandstone:
                    return new Color(0.72f, 0.64f, 0.52f);
                case StatueFinish.Greywacke:
                    return new Color(0.56f, 0.56f, 0.56f);
                case StatueFinish.Basalt:
                    return new Color(0.36f, 0.36f, 0.4f);
                default:
                    return new Color(0.70f, 0.68f, 0.62f);
            }
        }
    }
}
