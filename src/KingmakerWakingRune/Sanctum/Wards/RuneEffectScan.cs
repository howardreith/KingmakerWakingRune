using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// Reads what an effect does from its own action tree: the damage it deals and whether it drains levels. Harm, the
    /// inflict spells and channelled negative energy carry no descriptor for that, so a plate's vulnerability must be found
    /// here. Cached per blueprint; the tree is static data.
    /// </summary>
    internal static class RuneEffectScan
    {
        private const int MaxDepth = 12;
        private static readonly FieldInfo DamageKind = Harmony12.AccessTools.Field(typeof(ContextActionDealDamage), "m_Type");
        private static readonly Dictionary<string, RuneEffect> s_Cache = new Dictionary<string, RuneEffect>();

        public static RuneEffect Of(MechanicsContext context)
        {
            RuneEffect effects = RuneEffect.None;
            for (MechanicsContext c = context; c != null; c = c.ParentContext)
            {
                effects |= Of(c.AssociatedBlueprint);
            }
            return effects;
        }

        private static RuneEffect Of(BlueprintScriptableObject blueprint)
        {
            if (blueprint == null)
            {
                return RuneEffect.None;
            }
            if (!s_Cache.TryGetValue(blueprint.AssetGuid, out RuneEffect effects))
            {
                effects = RuneEffect.None;
                foreach (BlueprintComponent component in blueprint.ComponentsArray ?? new BlueprintComponent[0])
                {
                    effects |= Walk(component, 0);
                }
                s_Cache[blueprint.AssetGuid] = effects;
            }
            return effects;
        }

        /// <summary>Every action list reachable through an element's fields, so conditionals and saves are followed.</summary>
        private static RuneEffect Walk(object element, int depth)
        {
            if (element == null || depth > MaxDepth)
            {
                return RuneEffect.None;
            }
            RuneEffect effects = element is ContextActionDealDamage damage ? Classify(damage) : RuneEffect.None;
            foreach (FieldInfo field in element.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object value = field.GetValue(element);
                if (value is ActionList list)
                {
                    effects = list.Actions.Aggregate(effects, (e, a) => e | Walk(a, depth + 1));
                }
                else if (value is GameAction action)
                {
                    effects |= Walk(action, depth + 1);
                }
                else if (value is GameAction[] actions)
                {
                    effects = actions.Aggregate(effects, (e, a) => e | Walk(a, depth + 1));
                }
            }
            return effects;
        }

        private static RuneEffect Classify(ContextActionDealDamage action)
        {
            string kind = DamageKind?.GetValue(action)?.ToString();
            if (kind == "EnergyDrain")
            {
                return RuneEffect.NegativeLevels;
            }
            if (kind != "Damage" || action.DamageType == null)
            {
                return RuneEffect.None;
            }
            if (action.DamageType.Type == DamageType.Force)
            {
                return RuneEffect.ForceDamage;
            }
            if (action.DamageType.Type != DamageType.Energy)
            {
                return RuneEffect.None;
            }
            switch (action.DamageType.Energy)
            {
                case DamageEnergyType.NegativeEnergy:
                    return RuneEffect.NegativeEnergyDamage;
                case DamageEnergyType.Cold:
                    return RuneEffect.ColdDamage;
                case DamageEnergyType.Electricity:
                    return RuneEffect.ElectricityDamage;
                case DamageEnergyType.Sonic:
                    return RuneEffect.SonicDamage;
                case DamageEnergyType.Acid:
                    return RuneEffect.AcidDamage;
                case DamageEnergyType.Fire:
                    return RuneEffect.FireDamage;
                default:
                    return RuneEffect.None;
            }
        }
    }
}
