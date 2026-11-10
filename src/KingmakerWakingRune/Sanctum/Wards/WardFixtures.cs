using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.View;
using Newtonsoft.Json;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>Marks a unit as a sanctum fixture's target proxy and gives it a stable identity in the save.</summary>
    public sealed class WardFixturePart : UnitPart
    {
        [JsonProperty]
        public string FixtureId;
    }

    /// <summary>
    /// The stand-ins that give scenery a place in native spell and attack targeting. Each is created once and stays in the
    /// area's own state, so it comes back with its save. Scenery itself stays baked; a proxy never adds a visible model.
    /// </summary>
    internal static class WardFixtures
    {
        private const string KruneId = "Krune";
        private const string VandianaId = "Vandiana";
        private const string GemId = "Gem";
        private const string DoorId = "DoorA2";

        public static readonly Dictionary<string, UnitEntityData> Live = new Dictionary<string, UnitEntityData>();

        public static string IdOf(UnitEntityData unit)
        {
            return unit?.Descriptor.Get<WardFixturePart>()?.FixtureId;
        }

        public static UnitEntityData Krune => Find(KruneId);

        public static UnitEntityData Vandiana => Find(VandianaId);

        public static UnitEntityData Gem => Find(GemId);

        public static UnitEntityData Door => Find(DoorId);

        public static UnitEntityData Find(string id)
        {
            return Live.TryGetValue(id, out UnitEntityData unit) && unit != null && unit.Descriptor != null ? unit : null;
        }

        /// <summary>Re-indexes fixtures the loader restored and creates any that are missing. Safe to repeat.</summary>
        public static void Ensure()
        {
            Live.Clear();
            foreach (UnitEntityData unit in Game.Instance.State.Units)
            {
                string id = IdOf(unit);
                if (id != null)
                {
                    Live[id] = unit;
                }
            }
            foreach (RuneFocus focus in KruneProjection.Foci)
            {
                EnsureOne(focus.Id, WardBlueprints.RuneFixtures[focus.Id], RunePoint(focus.Id));
            }
            foreach (StatueSlot slot in StatueManifest.Slots)
            {
                if (SanctumDressing.StatueFeet.TryGetValue(slot.Id, out Vector3 foot))
                {
                    EnsureOne(slot.Id, WardBlueprints.StatueFixture, foot);
                }
            }
            EnsureOne(DoorId, WardBlueprints.DoorFixture, GridWorld(13.15f, 26.0f));
            EnsureOne(GemId, WardBlueprints.GemFixture, GridWorld(14.8f, 9.6f));
            EnsureOne(KruneId, WardBlueprints.KruneSupport, GridWorld(SanctumPlan.SarcophagusCenter.X, SanctumPlan.SarcophagusCenter.Y));
            EnsureOne(VandianaId, WardBlueprints.VandianaSupport, GridWorld(13.9f, 9.6f));
            foreach (UnitEntityData unit in Live.Values)
            {
                Configure(unit);
            }
        }

        private static void EnsureOne(string id, BlueprintUnit blueprint, Vector3 at)
        {
            if (Live.ContainsKey(id))
            {
                return;
            }
            UnitEntityData unit = Game.Instance.EntityCreator.SpawnUnit(blueprint, at, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
            unit.Descriptor.Ensure<WardFixturePart>().FixtureId = id;
            Live[id] = unit;
            Log.Info($"Ward fixture created: {id} at {at} ({blueprint.name}).");
        }

        private static void Configure(UnitEntityData unit)
        {
            UnitState state = unit.Descriptor.State;
            // Countable flags survive saves, so these are only taken once per unit and never stacked by reloads.
            if (!state.IsIgnoredByCombat)
            {
                state.IsIgnoredByCombat.Retain();
            }
            if (!state.HasCondition(UnitCondition.Unlootable))
            {
                state.AddCondition(UnitCondition.Unlootable);
            }
            if (!state.HasCondition(UnitCondition.CantMove))
            {
                state.AddCondition(UnitCondition.CantMove);
            }
            if (!state.HasCondition(UnitCondition.CantAct))
            {
                state.AddCondition(UnitCondition.CantAct);
            }
            if (unit.View != null)
            {
                foreach (Renderer renderer in unit.View.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = false;
                }
            }
        }

        private static Vector3 RunePoint(string id)
        {
            RunePlate plate = SanctumPlan.RunePlates.First(p => p.Id == id);
            float x = plate.Position.X + plate.InwardNormal.X * 0.5f;
            float y = plate.Position.Y + plate.InwardNormal.Y * 0.5f;
            return GridWorld(x, y);
        }

        private static Vector3 GridWorld(float x, float y)
        {
            return new Vector3(SanctumPlan.ToWorldX(x), SanctumPlan.HeightAt(x, y) ?? 0f, SanctumPlan.ToWorldZ(y));
        }
    }
}
