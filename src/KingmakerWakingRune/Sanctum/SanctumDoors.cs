using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Visual.FogOfWar;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Sanctum.Wards;
using Pathfinding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// A native door whose leaf is mod geometry. Interaction, the open state in the save, and the navmesh cut are all
    /// <see cref="StandardDoor"/>'s; this only swings the leaf and raises the sight blocker to match. No lock or trap.
    /// </summary>
    internal sealed class SanctumDoor : StandardDoor
    {
        private const float SwingDegreesPerSecond = 110f;

        public DoorPlan Plan;
        public Transform Pivot;
        public FogOfWarBlocker Blocker;
        public BoxCollider Hit;

        private float m_Angle;
        private bool m_Shown;

        protected override void OnDataSetted()
        {
            // Restoring a door's saved state is not a transition, so the native open-by-default call must not sound.
            WithoutSound(() => base.OnDataSetted());
            ShowState(true);
        }

        protected override void OnInteract(UnitEntityData user)
        {
            if (Plan.Id == "A2" && !WardRuntime.State.Door.Destroyed)
            {
                WardInteract.Open("DoorA2", user);
                return;
            }
            Toggle(user);
        }

        /// <summary>One native open or close: guarded against closing on anyone, and it sounds exactly once.</summary>
        public void Toggle(UnitEntityData user)
        {
            UnitEntityData inTheWay = IsOpen ? SanctumDoors.FindInSwing(Plan) : null;
            if (inTheWay != null)
            {
                Log.Info($"Sanctum door {Plan.Id}: closing refused, {inTheWay.CharacterName} is in its way (corpulence {inTheWay.View.Corpulence:0.00} m at {inTheWay.Position}).");
                EventBus.RaiseEvent(delegate(IWarningNotificationUIHandler h) { h.HandleWarning("Someone is standing in the doorway."); });
                return;
            }
            base.OnInteract(user);
            Log.Info($"Sanctum door {Plan.Id}: {(IsOpen ? "opened" : "closed")} by {user.CharacterName}.");
            WardRuntime.OnDoorTransition(Plan.Id, IsOpen);
        }

        public void OpenSilently()
        {
            WithoutSound(Open);
        }

        public void HideLeaf()
        {
            foreach (Renderer renderer in Pivot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }
            Hit.enabled = false;
        }

        /// <summary>Runs one state change with the door's sounds emptied, for changes that are not real transitions.</summary>
        private void WithoutSound(System.Action change)
        {
            var open = AccessTools.Field(typeof(StandardDoor), "m_OpenSound");
            var close = AccessTools.Field(typeof(StandardDoor), "m_CloseSound");
            object openSound = open.GetValue(this), closeSound = close.GetValue(this);
            open.SetValue(this, string.Empty);
            close.SetValue(this, string.Empty);
            change();
            open.SetValue(this, openSound);
            close.SetValue(this, closeSound);
        }

        private void Update()
        {
            if (Data != null && IsOpen != m_Shown)
            {
                ShowState(false);
            }
            float target = m_Shown ? Plan.OpenDegrees : 0f;
            if (!Mathf.Approximately(m_Angle, target))
            {
                m_Angle = Mathf.MoveTowards(m_Angle, target, SwingDegreesPerSecond * Time.deltaTime);
                Pivot.localRotation = Quaternion.Euler(0f, m_Angle, 0f);
            }
        }

        private void ShowState(bool instantly)
        {
            m_Shown = IsOpen;
            Blocker.enabled = !m_Shown;
            // A closed door is an easy target; an open leaf should not catch clicks meant for the floor around it.
            Vector3 size = Hit.size;
            size.z = m_Shown ? SanctumDoors.OpenHitDepth : Plan.ClosedHitDepth;
            Hit.size = size;
            if (instantly)
            {
                m_Angle = m_Shown ? Plan.OpenDegrees : 0f;
                Pivot.localRotation = Quaternion.Euler(0f, m_Angle, 0f);
            }
        }
    }

    /// <summary>
    /// Creates the two mapped doors as scene entities while the native loader is matching views to the save, which
    /// is what lets their state persist. A fresh arrival meets them closed. A save already inside the sanctum but with
    /// no recorded door state was made before the doors existed, so they open once for it: nobody in that save can be
    /// standing where a closed door would seal them. Any state a save has recorded is left as it is.
    /// </summary>
    internal static class SanctumDoors
    {
        private const float LeafHeight = SanctumPlan.DoorLeafHeight;
        private const float LeafThickness = 0.22f;
        private const float StarRadius = 0.92f;
        private const int SelectableLayer = 10;
        /// <summary>Metres a user may stand from the door: beyond the leaf's reach, so nobody has to stand in its swing to shut it.</summary>
        private const float UseDistance = 3.2f;
        public const float OpenHitDepth = 0.24f;

        private static GameObject s_Root;

        internal static DoorPlan[] ClosedPlans()
        {
            return s_Root == null ? new DoorPlan[0] : s_Root.GetComponentsInChildren<SanctumDoor>(true)
                .Where(d => d.Data != null && !d.IsOpen).Select(d => d.Plan).ToArray();
        }

        public static bool AreReady => s_Root != null && s_Root.GetComponentsInChildren<SanctumDoor>(true).Count(d => d.Data != null) == SanctumPlan.Doors.Length;

        public static IEnumerable<EntityViewBase> Create(IEnumerable<EntityViewBase> donorViews)
        {
            Scene mechanics = SceneManager.GetSceneByName(ScenarioBlueprints.Sanctum.DynamicScene.SceneName);
            Scene statics = SceneManager.GetSceneByName(ScenarioBlueprints.Sanctum.StaticScene.SceneName);
            if (s_Root == null)
            {

                const string openSound = "DungeonStoneDoorOpen", closeSound = "DungeonDoorStone_Close";
                var palette = new SanctumPalette(SanctumScope.HarvestMaterials(statics).Rock);
                s_Root = new GameObject("KWR_SanctumDoors");
                SceneManager.MoveGameObjectToScene(s_Root, mechanics);
                bool alreadyInside = Game.Instance.Player.UnlockableFlags.IsUnlocked(ScenarioBlueprints.SanctumEnteredFlag);
                foreach (DoorPlan plan in SanctumPlan.Doors)
                {
                    CreateDoor(plan, palette, openSound, closeSound, alreadyInside);
                }
                WardTraps.CreateUnder(s_Root);
                Log.Info($"Sanctum doors created; door sounds {openSound}/{closeSound}.");
            }
            return s_Root.GetComponentsInChildren<EntityViewBase>(true);
        }


        /// <summary>Keeps a loaded party from being sealed inside a door that a pre-door save has placed them in.</summary>
        public static void ReleaseOccupied()
        {
            if (s_Root == null)
            {
                return;
            }
            foreach (SanctumDoor door in s_Root.GetComponentsInChildren<SanctumDoor>(true))
            {
                if (door.Data != null && !door.IsOpen && FindUnit((x, y, height, radius) => SanctumPlan.DoorwayOverlap(door.Plan, x, y, height, radius)) != null)
                {
                    door.OpenSilently();
                    Log.Info($"Sanctum door {door.Plan.Id} opened on load: a party member stands in the doorway.");
                }
            }
        }

        /// <summary>The native door closes with no check of its own, so its whole swing is guarded here against each unit's real footprint.</summary>
        public static UnitEntityData FindInSwing(DoorPlan plan)
        {
            return FindUnit((x, y, height, radius) => SanctumPlan.ClosureBlocked(plan, x, y, height, radius));
        }

        private static UnitEntityData FindUnit(System.Func<float, float, float, float, bool> overlaps)
        {
            foreach (UnitEntityData unit in Game.Instance.State.Units)
            {
                // Ward fixtures are invisible stand-ins, not bodies: one stands in A2's own doorway by design.
                if (unit.IsInGame && unit.View != null && WardFixtures.IdOf(unit) == null)
                {
                    float x = unit.Position.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX;
                    float y = SanctumPlan.OriginY - unit.Position.z / SanctumPlan.MetersPerSquare;
                    if (overlaps(x, y, unit.Position.y, unit.View.Corpulence / SanctumPlan.MetersPerSquare))
                    {
                        return unit;
                    }
                }
            }
            return null;
        }

        private static void CreateDoor(DoorPlan plan, SanctumPalette palette, string openSound, string closeSound, bool openIfUnrecorded)
        {
            Vector3 hinge = World(plan.Hinge), latch = World(plan.Latch), center = World(plan.Center);
            float width = (latch - hinge).magnitude;

            var go = new GameObject("KWR_Door_" + plan.Id);
            go.SetActive(false);
            go.transform.SetParent(s_Root.transform, false);
            go.transform.position = center;

            go.AddComponent<MapObjectView>().UniqueId = "kwr_sanctum_door_" + plan.Id.ToLowerInvariant();
            go.AddComponent<AkGameObj>();
            var animator = new GameObject("Animator");
            animator.transform.SetParent(go.transform, false);
            animator.AddComponent<Animator>();

            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(go.transform, false);
            pivot.position = hinge;
            float yaw = -Mathf.Atan2(latch.z - hinge.z, latch.x - hinge.x) * Mathf.Rad2Deg;
            var frame = new GameObject("Leaf").transform;
            frame.SetParent(pivot, false);
            frame.localRotation = Quaternion.Euler(0f, yaw, 0f);
            BoxCollider hit = BuildLeaf(frame, plan, palette, width);

            var cut = go.AddComponent<NavmeshCut>();
            cut.type = NavmeshCut.MeshType.Rectangle;
            float depth = 2f * SanctumPlan.DoorwayHalfDepth * SanctumPlan.MetersPerSquare;
            float span = width + 0.3f;
            cut.rectangleSize = plan.Hinge.Y == plan.Latch.Y ? new Vector2(span, depth) : new Vector2(depth, span);
            cut.height = 5f;
            cut.useRotation = false;

            var polygon = go.AddComponent<FogOfWarBlockerPolygon>();
            polygon.Height = 12f;
            AccessTools.Field(typeof(Kingmaker.Utility.PolygonComponent), "m_Points").SetValue(polygon, SanctumPlan.TracedBothWays(new[] { plan.Hinge, plan.Latch }).Select(p => World(p) - center).ToList());
            AccessTools.Field(typeof(Kingmaker.Utility.PolygonComponent), "m_Closed").SetValue(polygon, false);

            var door = go.AddComponent<SanctumDoor>();
            door.Plan = plan;
            door.Pivot = pivot;
            door.Blocker = go.GetComponent<FogOfWarBlocker>();
            door.Hit = hit;
            door.ObstacleAnimation = new AnimationClip { name = "KWR_DoorState" };
            door.DisableNavmeshCutWhenOpen = true;
            door.OpenByDefault = openIfUnrecorded;
            door.ProximityRadius = UseDistance;
            door.UseAnimationState = UnitAnimationInteractionType.UseObject;
            AccessTools.Field(typeof(StandardDoor), "m_OpenSound").SetValue(door, openSound);
            AccessTools.Field(typeof(StandardDoor), "m_CloseSound").SetValue(door, closeSound);

            go.SetActive(true);
        }

        /// <summary>The leaf runs along local +X from the hinge; local +Z faces the side the party meets it from.</summary>
        private static BoxCollider BuildLeaf(Transform leaf, DoorPlan plan, SanctumPalette palette, float width)
        {
            var mesh = new SceneryMesh();
            float clear = width - 0.04f;
            mesh.Box(palette.Stone, new Vector3(width / 2f, LeafHeight / 2f, 0f), new Vector3(clear, LeafHeight, LeafThickness), Quaternion.identity);
            // A shallow raised margin, the only ornament a plain sanctum door needs.
            Vector3 face = new Vector3(0f, 0f, LeafThickness / 2f);
            foreach (float sign in new[] { 1f, -1f })
            {
                Vector3 side = face * sign;
                mesh.Box(palette.Basalt, new Vector3(width / 2f, 0.1f, 0f) + side, new Vector3(clear, 0.2f, 0.03f), Quaternion.identity);
                mesh.Box(palette.Basalt, new Vector3(width / 2f, LeafHeight - 0.1f, 0f) + side, new Vector3(clear, 0.2f, 0.03f), Quaternion.identity);
            }
            if (plan.Sihedron)
            {
                AddSihedron(mesh, palette, new Vector3(width / 2f, LeafHeight * 0.52f, 0f) + face);
            }
            mesh.Instantiate(leaf, "LeafMesh");

            var hit = new GameObject("Hit") { layer = SelectableLayer };
            hit.transform.SetParent(leaf, false);
            var box = hit.AddComponent<BoxCollider>();
            box.center = new Vector3(width / 2f, LeafHeight / 2f, 0f);
            box.size = new Vector3(clear, LeafHeight, plan.ClosedHitDepth);
            return box;
        }

        /// <summary>The seven-pointed star, an abyssium disk at each point, and the handprint at its centre.</summary>
        private static void AddSihedron(SceneryMesh mesh, SanctumPalette palette, Vector3 center)
        {
            Vector3 normal = Vector3.forward, u = Vector3.right, v = Vector3.up;
            Vector3 surface = center + normal * 0.012f;
            mesh.Lathe(palette.Basalt, center, normal, new[] { StarRadius + 0.22f, 0f, StarRadius + 0.22f, 0.01f, 0f, 0.01f }, 40);
            Vector3 previous = Vector3.zero;
            for (int i = 0; i <= 7; i++)
            {
                // Every third vertex of a heptagon traces the star in one line.
                float angle = (90f + i * 3 * 360f / 7f) * Mathf.Deg2Rad;
                Vector3 point = surface + (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * StarRadius;
                if (i > 0)
                {
                    mesh.Strip(palette.Abyssium, previous, point, normal, 0.05f);
                }
                previous = point;
            }
            for (int i = 0; i < 7; i++)
            {
                float angle = (90f + i * 360f / 7f) * Mathf.Deg2Rad;
                Vector3 disk = surface + (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * StarRadius;
                mesh.Lathe(palette.Iron, disk, normal, new[] { 0.135f, 0f, 0.135f, 0.02f, 0f, 0.02f }, 16);
                mesh.Lathe(palette.Abyssium, disk + normal * 0.022f, normal, new[] { 0.105f, 0f, 0.105f, 0.01f, 0f, 0.01f }, 16);
            }
            // The palm and fingers press into the centre, with the altered inscription as small marks around it.
            mesh.Lathe(palette.Void, surface, normal, new[] { 0.11f, 0f, 0.11f, 0.008f, 0f, 0.008f }, 14, 1.2f);
            for (int i = 0; i < 4; i++)
            {
                float offset = (i - 1.5f) * 0.065f;
                mesh.Strip(palette.Void, surface + u * offset + v * 0.11f, surface + u * offset * 1.3f + v * 0.27f, normal, 0.042f);
            }
            mesh.Strip(palette.Void, surface + u * -0.11f + v * 0.02f, surface + u * -0.24f + v * 0.13f, normal, 0.042f);
            for (int i = 0; i < 12; i++)
            {
                float angle = (15f + i * 30f) * Mathf.Deg2Rad;
                Vector3 mark = surface + (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * 0.43f;
                Vector3 tick = (u * Mathf.Cos(angle + 0.8f) + v * Mathf.Sin(angle + 0.8f)) * 0.04f;
                mesh.Strip(palette.Abyssium, mark - tick, mark + tick, normal, 0.016f);
            }
        }

        private static Vector3 World(GridPoint p)
        {
            return new Vector3(SanctumPlan.ToWorldX(p.X), 0f, SanctumPlan.ToWorldZ(p.Y));
        }
    }
}
