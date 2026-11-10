using System;
using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using KingmakerWakingRune.Blueprints;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>Native discovery and disarming for the three placed wards. Location triggers and their effects are driven from saved state.</summary>
    internal static class WardTraps
    {
        private const int SelectableLayer = 10;

        public static TrapObjectView Alarm { get; private set; }
        public static TrapObjectView Feeder { get; private set; }
        public static TrapObjectView Glyph { get; private set; }

        private static BlueprintTrap s_AlarmBlueprint, s_FeederBlueprint, s_GlyphBlueprint;
        private static Action<BlueprintScriptableObject> s_Register;

        public static void Build(Action<BlueprintScriptableObject> register)
        {
            s_Register = register;
            s_AlarmBlueprint = Trap("KWR_TrapAlarm", WardProfile.AlarmPerceptionDC, WardProfile.AlarmDisarmDC, keepsArmed: true, onFire: FireAlarm, onDisarm: () => WardRuntime.State.Alarm.Disarmed = true);
            s_FeederBlueprint = Trap("KWR_TrapFeeder", WardProfile.FeederPerceptionDC, WardProfile.FeederDisarmDC, keepsArmed: true, onFire: FireFeeder, onDisarm: () => WardRuntime.State.Door.Feeder.Disarmed = true);
            s_GlyphBlueprint = Trap("KWR_TrapGlyph", WardProfile.GlyphPerceptionDC, WardProfile.GlyphDisarmDC, keepsArmed: false, onFire: FireGlyphFromDisarm, onDisarm: () => WardRuntime.State.Glyph.Disarmed = true);
        }

        /// <summary>Built with the doors while the native loader matches views to the save, which is what makes their state persist.</summary>
        public static void CreateUnder(GameObject root)
        {
            Alarm = Create(root, s_AlarmBlueprint, "kwr_trap_alarm", WardGeometry.AlarmCenter.X, WardGeometry.AlarmCenter.Y, 0.9f);
            Feeder = Create(root, s_FeederBlueprint, "kwr_trap_feeder", 13.15f, 26.05f, 0.9f);
            Glyph = Create(root, s_GlyphBlueprint, "kwr_trap_glyph", WardGeometry.GlyphCenter.X, WardGeometry.GlyphCenter.Y, 0.9f);
        }

        private static TrapObjectView Create(GameObject root, BlueprintTrap blueprint, string id, float x, float y, float halfSize)
        {
            TrapObjectView view = TrapObjectView.CreateView(blueprint, id, null, null);
            view.gameObject.SetActive(false);
            view.transform.SetParent(root.transform, false);
            float height = (SanctumPlan.HeightAt(x, y) ?? 0f) + 0.02f;
            view.transform.position = new Vector3(SanctumPlan.ToWorldX(x), height, SanctumPlan.ToWorldZ(y));
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = "Marker";
            marker.layer = SelectableLayer;
            marker.transform.SetParent(view.transform, false);
            marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            marker.transform.localScale = Vector3.one * (halfSize * 2f);
            var renderer = marker.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.3f, 0.9f, 0.75f, 0.45f) };
            view.Renderers.Add(renderer);
            view.gameObject.SetActive(true);
            return view;
        }

        /// <summary>
        /// Matches each view to saved state after a load. A save with no entity for these traps (it predates them) would
        /// otherwise leave them inert, so an armed ward is re-armed here; a spent one stays spent.
        /// </summary>
        public static void AfterLoad(SanctumState state)
        {
            Sync(Alarm, state.Alarm.IsActive(state.Clock));
            Sync(Feeder, !state.Door.Feeder.Disarmed && !state.Door.Destroyed);
            Sync(Glyph, !state.Glyph.Triggered && !state.Glyph.Disarmed && !state.Glyph.Dispelled);
        }

        private static void Sync(TrapObjectView view, bool armed)
        {
            if (view == null || view.Data == null)
            {
                return;
            }
            if (armed && !view.Data.TrapActive)
            {
                view.Data.TrapActive = true;
                view.Data.IsPerceptionCheckPassed = false;
                view.Data.IsInGame = true;
            }
            else if (!armed && view.Data.TrapActive)
            {
                view.Deactivate();
            }
        }

        /// <summary>A trap's marker takes clicks only once found, so an unnoticed ward cannot be set off by clicking empty floor.</summary>
        public static void Tick()
        {
            foreach (TrapObjectView view in new[] { Alarm, Feeder, Glyph })
            {
                if (view?.Data != null)
                {
                    foreach (Collider collider in view.GetComponentsInChildren<Collider>(true))
                    {
                        collider.enabled = view.Data.IsPerceptionCheckPassed && view.Data.TrapActive;
                    }
                }
            }
        }

        public static bool Perceived(TrapObjectView view)
        {
            return view?.Data != null && view.Data.IsPerceptionCheckPassed;
        }

        private static BlueprintTrap Trap(string name, int perceptionDC, int disarmDC, bool keepsArmed, Action<UnitEntityData> onFire, Action onDisarm)
        {
            var trap = ScriptableObject.CreateInstance<BlueprintTrap>();
            trap.name = name;
            trap.AssetGuid = ScenarioIds.Derive("trap." + name);
            trap.PerceptionDC = perceptionDC;
            trap.PerceptionRadius = 8f;
            trap.DisableDC = disarmDC;
            trap.DisableTriggerMargin = 5;
            trap.IsHiddenWhenInactive = true;
            // A native trap is disabled whenever its actions run, so a resetting ward fires from its condition and answers no.
            trap.TriggerConditions = new ConditionsChecker { Conditions = new Condition[] { TriggerCondition(onFire, keepsArmed) } };
            trap.DisableConditions = new ConditionsChecker();
            trap.TrapActions = new ActionList();
            trap.DisableActions = new ActionList { Actions = new GameAction[] { Callback("Record disarm", onDisarm) } };
            trap.ComponentsArray = new BlueprintComponent[0];
            s_Register(trap);
            return trap;
        }

        private static Condition TriggerCondition(Action<UnitEntityData> onFire, bool keepsArmed)
        {
            var condition = ScriptableObject.CreateInstance<KwrCondition>();
            condition.name = "KWR_TrapFire";
            condition.Caption = "Fire ward";
            condition.Body = () =>
            {
                onFire(ElementsContext.GetData<BlueprintTrap.ElementsData>()?.TriggeringUnit);
                return !keepsArmed;
            };
            return condition;
        }

        private static GameAction Callback(string caption, Action body)
        {
            var callback = ScriptableObject.CreateInstance<KwrCallback>();
            callback.name = "KWR_TrapCallback";
            callback.Caption = caption;
            callback.Body = body;
            return callback;
        }

        private static void FireAlarm(UnitEntityData user)
        {
            WardRitual.AlarmAlert("failed disarm");
        }

        private static void FireFeeder(UnitEntityData user)
        {
            if (user != null)
            {
                WardDoor.StrikeIfArmed(user);
            }
        }

        private static void FireGlyphFromDisarm(UnitEntityData user)
        {
            WardHazards.DetonateGlyph(user?.Position);
        }
    }

}
