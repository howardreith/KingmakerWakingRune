using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using KingmakerWakingRune.Sanctum;
using KingmakerWakingRune.Sanctum.Wards;

namespace KingmakerWakingRune.Patches
{
    /// <summary>
    /// A rune plate is an inanimate thing in a peaceful fixture faction, so a spell that selects or spares targets by
    /// hostility never counted it: Controlled Fireball (AbilityTargetsAround m_TargetType=Enemy, then ContextConditionIsEnemy
    /// choosing full or minimum damage) delivered nothing at all to the plate it was aimed at, and the attempt still counted
    /// as a failure (WR-PLAYTEST-01). Scoped adaptation: in the sanctum, for a damaging ability cast by the party's side,
    /// the seven authored rune plates count as enemies in exactly those two places. Real allies, other creatures, other
    /// spells, other areas and other campaigns are untouched; the damage itself, the save, hardness and the rune's own
    /// rules stay native and are counted once, by the existing damage observer.
    /// </summary>
    internal static class RuneTargeting
    {
        private static readonly Dictionary<BlueprintAbility, bool> s_Damaging = new Dictionary<BlueprintAbility, bool>();

        public static bool IsRunePlate(UnitEntityData unit)
        {
            string id = unit == null ? null : WardFixtures.IdOf(unit);
            return id != null && KruneProjection.Foci.Any(f => f.Id == id);
        }

        public static bool Applies(UnitEntityData caster, BlueprintAbility ability)
        {
            return caster != null && ability != null && SanctumScope.IsSanctumLoaded && PartySide(caster) && Damaging(ability);
        }

        private static bool PartySide(UnitEntityData caster)
        {
            return caster.IsPlayerFaction || (caster.Get<UnitPartSummonedMonster>()?.Summoner?.IsPlayerFaction ?? false);
        }

        /// <summary>An ability whose own run actions deal damage somewhere in their tree (cached per blueprint).</summary>
        private static bool Damaging(BlueprintAbility ability)
        {
            if (!s_Damaging.TryGetValue(ability, out bool damaging))
            {
                damaging = ability.GetComponents<AbilityEffectRunAction>().Any(r => Deals(r.Actions, 0));
                s_Damaging[ability] = damaging;
            }
            return damaging;
        }

        private static bool Deals(ActionList list, int depth)
        {
            if (list?.Actions == null || depth > 6)
            {
                return false;
            }
            foreach (GameAction action in list.Actions)
            {
                if (action is ContextActionDealDamage)
                {
                    return true;
                }
                if (action is Conditional c && (Deals(c.IfTrue, depth + 1) || Deals(c.IfFalse, depth + 1)))
                {
                    return true;
                }
                if (action is ContextActionConditionalSaved s && (Deals(s.Succeed, depth + 1) || Deals(s.Failed, depth + 1)))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>Enemy-only area selection: the plates inside the area (same radius, same line of effect) join the targets.</summary>
    [HarmonyPatch(typeof(AbilityTargetsAround), nameof(AbilityTargetsAround.Select))]
    internal static class RuneTargetsAroundPatch
    {
        private static void Postfix(AbilityTargetsAround __instance, AbilityExecutionContext context, TargetWrapper anchor, ref IEnumerable<TargetWrapper> __result)
        {
            if (__instance.TargetType != TargetType.Enemy || !RuneTargeting.Applies(context?.MaybeCaster, context?.AbilityBlueprint))
            {
                return;
            }
            List<TargetWrapper> targets = __result.ToList();
            var included = new HashSet<UnitEntityData>(targets.Where(t => t.IsUnit).Select(t => t.Unit));
            foreach (UnitEntityData unit in GameHelper.GetTargetsAround(anchor.IsUnit ? anchor.Unit.EyePosition : anchor.Point, __instance.AoERadius, checkLOS: true, includeDead: false))
            {
                if (!included.Contains(unit) && RuneTargeting.IsRunePlate(unit))
                {
                    targets.Add(new TargetWrapper(unit));
                    Log.Info($"Rune targeting: {unit.CharacterName} ({WardFixtures.IdOf(unit)}) is inside {context.AbilityBlueprint.name}'s area; it counts as an enemy of the party's spell.");
                }
            }
            __result = targets;
        }
    }

    /// <summary>The same spell's "is the target an enemy" choice (full or minimum damage) counts the plate as an enemy.</summary>
    [HarmonyPatch(typeof(ContextConditionIsEnemy), "CheckCondition")]
    internal static class RuneIsEnemyPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (__result)
            {
                return;
            }
            var data = ElementsContext.GetData<Kingmaker.UnitLogic.Mechanics.MechanicsContext.Data>();
            var context = data?.Context;
            UnitEntityData target = data?.CurrentTarget?.Unit;
            if (RuneTargeting.IsRunePlate(target) && RuneTargeting.Applies(context?.MaybeCaster, context?.SourceAbility))
            {
                __result = true;
            }
        }
    }
}
