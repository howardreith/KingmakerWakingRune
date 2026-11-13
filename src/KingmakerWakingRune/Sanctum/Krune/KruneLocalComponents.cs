using System;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using Newtonsoft.Json;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>The saved state of Krune's local spells on whoever bears them (the ring of spell turning can pass to a party member).</summary>
    public sealed class KwrConjuredMagicPart : UnitPart
    {
        [JsonProperty]
        public int SpellTurningLevels;
        [JsonProperty]
        public int PhaseDoorPassages;
        [JsonProperty]
        public string JuxtaposedId = string.Empty;
        [JsonProperty]
        public int JuxtaposeDC;
        [JsonProperty]
        public double LastJuxtaposeAt = -100d;
        /// <summary>Black tentacles holding this creature: the area's identity, so its end releases it.</summary>
        [JsonProperty]
        public string HeldByArea = string.Empty;
    }

    /// <summary>Phase door's passages: CL/2 when cast, one spent each time he steps through; gone when none are left.</summary>
    public sealed class KwrPhaseDoorPassages : BuffLogic, IInitiatorRulebookHandler<RuleCastSpell>
    {
        public override void OnTurnOn()
        {
            KwrConjuredMagicPart part = Owner.Ensure<KwrConjuredMagicPart>();
            if (part.PhaseDoorPassages <= 0)
            {
                part.PhaseDoorPassages = Math.Max(1, Context.Params.CasterLevel / 2);
            }
            if (!Owner.HasFact(KruneLocalSpells.PhaseDoorStep))
            {
                Owner.AddFact(KruneLocalSpells.PhaseDoorStep);
            }
        }

        public override void OnTurnOff()
        {
            Owner.Ensure<KwrConjuredMagicPart>().PhaseDoorPassages = 0;
            var step = Owner.Abilities.GetFact(KruneLocalSpells.PhaseDoorStep);
            if (step != null)
            {
                Owner.RemoveFact(step);
            }
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            if (evt.Spell.Blueprint != KruneLocalSpells.PhaseDoorStep)
            {
                return;
            }
            KwrConjuredMagicPart part = Owner.Ensure<KwrConjuredMagicPart>();
            part.PhaseDoorPassages--;
            KruneAwakening.Note($"phase door: a passage used, {part.PhaseDoorPassages} left.");
            if (part.PhaseDoorPassages <= 0)
            {
                Buff.Remove();
            }
        }
    }

    /// <summary>
    /// Black tentacles: every creature in the area, at its first entry and each round, is the target of a grapple check
    /// (d20 + caster level + 5, the tentacles' own CMB) against its native CMD; a held creature is crushed for 1d6+4 each
    /// round. Freedom of movement (immunity to being held in place) makes the grapple fail. Spell resistance does not apply.
    /// </summary>
    public sealed class KwrTentacleArea : AbilityAreaEffectLogic
    {
        protected override void OnUnitEnter(MechanicsContext context, AreaEffectEntityData areaEffect, UnitEntityData unit)
        {
            Grapple(context, areaEffect, unit);
        }

        protected override void OnUnitExit(MechanicsContext context, AreaEffectEntityData areaEffect, UnitEntityData unit)
        {
            Release(unit, areaEffect.UniqueId);
        }

        protected override void OnRound(MechanicsContext context, AreaEffectEntityData areaEffect)
        {
            foreach (UnitEntityData unit in areaEffect.UnitsInside.Where(u => u.IsInGame).ToList())
            {
                if (unit.Descriptor.Get<KwrConjuredMagicPart>()?.HeldByArea == areaEffect.UniqueId && unit.Buffs.HasFact(KruneLocalSpells.TentaclesGrappled))
                {
                    Crush(context, unit);
                }
                else
                {
                    Grapple(context, areaEffect, unit);
                }
            }
        }

        public static int Cmb(MechanicsContext context)
        {
            return Math.Max(1, context.Params.CasterLevel) + 5;
        }

        private static void Grapple(MechanicsContext context, AreaEffectEntityData area, UnitEntityData unit)
        {
            if (unit.Descriptor.State.IsDead || unit.Buffs.HasFact(KruneLocalSpells.TentaclesGrappled))
            {
                return;
            }
            if (unit.Descriptor.State.HasConditionImmunity(UnitCondition.CantMove))
            {
                KruneAwakening.Note($"black tentacles cannot hold {unit.CharacterName} (freedom of movement).");
                return;
            }
            UnitEntityData caster = context.MaybeCaster ?? unit;
            int d20 = Rulebook.Trigger(new RuleRollD20(caster)).Result;
            int cmd = Rulebook.Trigger(new RuleCalculateCMD(caster, unit, CombatManeuver.Grapple)).Result;
            bool held = d20 != 1 && (d20 == 20 || d20 + Cmb(context) >= cmd);
            KruneAwakening.Note($"black tentacles grapple {unit.CharacterName}: d20 {d20} + {Cmb(context)} vs CMD {cmd}: {(held ? "held" : "missed")}.");
            if (!held)
            {
                return;
            }
            unit.Descriptor.Ensure<KwrConjuredMagicPart>().HeldByArea = area.UniqueId;
            unit.Descriptor.AddBuff(KruneLocalSpells.TentaclesGrappled, context, null);
            Crush(context, unit);
        }

        private static void Crush(MechanicsContext context, UnitEntityData unit)
        {
            var damage = new PhysicalDamage(new DiceFormula(1, DiceType.D6), PhysicalDamageForm.Bludgeoning);
            damage.AddBonus(4);
            UnitEntityData source = context.MaybeCaster ?? unit;
            Rulebook.Trigger(new RuleDealDamage(source, unit, new DamageBundle(damage)) { Reason = context });
        }

        public static void Release(UnitEntityData unit, string areaId)
        {
            KwrConjuredMagicPart part = unit.Descriptor.Get<KwrConjuredMagicPart>();
            if (part == null || part.HeldByArea != areaId)
            {
                return;
            }
            part.HeldByArea = string.Empty;
            unit.Descriptor.Buffs.RemoveFact(KruneLocalSpells.TentaclesGrappled);
        }
    }

    /// <summary>A creature held by the tentacles tries to break free each round (CMB, Athletics or Mobility, the game's own rule) against their CMD.</summary>
    public sealed class KwrTentacleHold : BuffLogic, ITickEachRound
    {
        public void OnNewRound()
        {
            UnitEntityData unit = Owner.Unit;
            KwrConjuredMagicPart part = Owner.Get<KwrConjuredMagicPart>();
            AreaEffectEntityData area = Game.Instance.State.AreaEffects.FirstOrDefault(a => a.UniqueId == part?.HeldByArea);
            if (area == null)
            {
                Buff.Remove();
                return;
            }
            int dc = 10 + KwrTentacleArea.Cmb(Context);
            UnitEntityData grappler = Context.MaybeCaster ?? unit;
            bool free = unit.TryBreakFree(grappler, UnitHelper.BreakFreeFlags.CanUseCMB, Context, dc);
            KruneAwakening.Note($"{unit.CharacterName} tries to break free of the tentacles (DC {dc}): {(free ? "free" : "still held")}.");
            if (free)
            {
                part.HeldByArea = string.Empty;
                Buff.Remove();
            }
        }

        public override void OnTurnOff()
        {
            KwrConjuredMagicPart part = Owner.Get<KwrConjuredMagicPart>();
            if (part != null)
            {
                part.HeldByArea = string.Empty;
            }
        }
    }

    /// <summary>Spell turning's levels: 1d4+6 when it starts, kept on the bearer, cleared when the effect ends (dispel, duration).</summary>
    public sealed class KwrSpellTurningLogic : BuffLogic
    {
        public override void OnTurnOn()
        {
            KwrConjuredMagicPart part = Owner.Ensure<KwrConjuredMagicPart>();
            if (part.SpellTurningLevels <= 0)
            {
                part.SpellTurningLevels = RulebookEvent.Dice.D(new DiceFormula(1, DiceType.D4)) + 6;
                KruneAwakening.Note($"spell turning on {Owner.CharacterName}: {part.SpellTurningLevels} spell levels.");
            }
        }

        public override void OnTurnOff()
        {
            Owner.Ensure<KwrConjuredMagicPart>().SpellTurningLevels = 0;
        }
    }

    /// <summary>
    /// Spell turning: a single-target spell aimed at the bearer (not an area, cone, touch, chain or weapon delivery) is aimed at
    /// its own caster instead, at the moment it is cast, and its levels are spent. A spell of more levels than remain rebounds
    /// with a chance of the levels left over its own level, and the turning is then exhausted (core rule). One redirect point:
    /// the cast rule's own target, so the spell then resolves natively against its caster.
    /// </summary>
    [HarmonyPatch]
    internal static class SpellTurningPatch
    {
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Constructor(typeof(RuleCastSpell), new[] { typeof(AbilityData), typeof(TargetWrapper) });
        }

        private static void Prefix(AbilityData spell, ref TargetWrapper target)
        {
            UnitEntityData bearer = target?.Unit;
            UnitEntityData caster = spell?.Caster?.Unit;
            if (bearer == null || caster == null || bearer == caster || KruneLocalSpells.SpellTurning == null || !bearer.Buffs.HasFact(KruneLocalSpells.SpellTurning) || !Turnable(spell))
            {
                return;
            }
            KwrConjuredMagicPart part = bearer.Descriptor.Ensure<KwrConjuredMagicPart>();
            int level = Math.Max(1, spell.SpellLevel);
            bool turned;
            if (level <= part.SpellTurningLevels)
            {
                part.SpellTurningLevels -= level;
                turned = true;
            }
            else
            {
                int chance = part.SpellTurningLevels * 100 / level;
                int roll = RulebookEvent.Dice.D(new DiceFormula(1, DiceType.D100));
                turned = roll <= chance;
                part.SpellTurningLevels = 0;
                KruneAwakening.Note($"spell turning partly overwhelmed by {spell.Blueprint.name} (level {level}): d100 {roll} vs {chance}.");
            }
            KruneAwakening.Note($"spell turning: {caster.CharacterName}'s {spell.Blueprint.name} (level {level}) {(turned ? "rebounds on its caster" : "gets through")}; {part.SpellTurningLevels} levels left.");
            if (turned)
            {
                target = new TargetWrapper(caster);
            }
            if (part.SpellTurningLevels <= 0)
            {
                bearer.Descriptor.Buffs.RemoveFact(KruneLocalSpells.SpellTurning);
            }
        }

        public static bool Turnable(AbilityData spell)
        {
            BlueprintAbility b = spell.Blueprint;
            if (b.Type != AbilityType.Spell || b.CanTargetPoint || b.Range == AbilityRange.Personal || b.Range == AbilityRange.Touch || b.AoERadius.Meters > 0f)
            {
                return false;
            }
            return b.GetComponent<AbilityTargetsAround>() == null && b.GetComponent<AbilityDeliverChain>() == null && b.GetComponent<AbilityDeliverTouch>() == null && b.GetComponent<AbilityDeliverAttackWithWeapon>() == null;
        }
    }

    /// <summary>
    /// Globe of invulnerability: a creature inside cannot be affected by a spell of 4th level or lower whose caster stands
    /// outside a globe. Through the game's own spell-resistance rule, which also gates spells that allow no resistance.
    /// </summary>
    [HarmonyPatch(typeof(RuleSpellResistanceCheck), nameof(RuleSpellResistanceCheck.OnTrigger))]
    internal static class GlobePatch
    {
        private static void Postfix(RuleSpellResistanceCheck __instance)
        {
            if (KruneLocalSpells.InsideGlobe == null || __instance.TargetIsImmune || !__instance.Target.Buffs.HasFact(KruneLocalSpells.InsideGlobe))
            {
                return;
            }
            MechanicsContext context = __instance.Context;
            if (context?.SourceAbility == null || context.SourceAbility.Type != AbilityType.Spell || context.SpellLevel > 4)
            {
                return;
            }
            UnitEntityData caster = context.MaybeCaster;
            if (caster != null && caster.Buffs.HasFact(KruneLocalSpells.InsideGlobe))
            {
                return;
            }
            Traverse.Create(__instance).Property("TargetIsImmune").SetValue(true);
            KruneAwakening.Note($"globe of invulnerability: {context.SourceAbility.name} (level {context.SpellLevel}) from outside cannot affect {__instance.Target.CharacterName}.");
        }
    }

    /// <summary>Hostile juxtaposition's link, made when the target fails its Will save (spell resistance already passed natively).</summary>
    public sealed class KwrJuxtaposeLink : ContextAction
    {
        public override string GetCaption()
        {
            return "Juxtapose";
        }

        public override void RunAction()
        {
            UnitEntityData caster = Context.MaybeCaster;
            UnitEntityData target = Target.Unit;
            if (caster == null || target == null)
            {
                return;
            }
            RuleSavingThrow save = Context.TriggerRule(new RuleSavingThrow(target, SavingThrowType.Will, Context.Params.DC));
            KruneAwakening.Note($"hostile juxtaposition on {target.CharacterName}: Will {save.RollResult} vs DC {Context.Params.DC}: {(save.IsPassed ? "negated" : "linked")}.");
            if (save.IsPassed)
            {
                return;
            }
            KwrConjuredMagicPart part = caster.Descriptor.Ensure<KwrConjuredMagicPart>();
            part.JuxtaposedId = target.UniqueId;
            part.JuxtaposeDC = Context.Params.DC;
            Kingmaker.UnitLogic.Buffs.Buff buff = caster.Descriptor.AddBuff(KruneLocalSpells.Juxtaposition, Context, TimeSpan.FromSeconds(6d * Math.Max(1, Context.Params.CasterLevel)));
            if (buff != null)
            {
                buff.IsFromSpell = true;
            }
        }
    }

    /// <summary>
    /// Once a round, an attack on the linked caster lets the linked creature be swapped into his place (Will negates). The
    /// attack is spent on the empty air where he stood: a bounded adaptation (the source has it strike the swapped creature).
    /// </summary>
    [HarmonyPatch(typeof(RuleAttackRoll), nameof(RuleAttackRoll.OnTrigger))]
    internal static class JuxtapositionPatch
    {
        private static void Prefix(RuleAttackRoll __instance)
        {
            UnitEntityData bearer = __instance.Target;
            if (KruneLocalSpells.Juxtaposition == null || bearer == null || !bearer.Buffs.HasFact(KruneLocalSpells.Juxtaposition))
            {
                return;
            }
            KwrConjuredMagicPart part = bearer.Descriptor.Get<KwrConjuredMagicPart>();
            double now = Game.Instance.TimeController.GameTime.TotalSeconds;
            if (part == null || now - part.LastJuxtaposeAt < 6d)
            {
                return;
            }
            UnitEntityData linked = Game.Instance.State.Units.FirstOrDefault(u => u.UniqueId == part.JuxtaposedId);
            if (linked == null || !linked.IsInGame || linked.Descriptor.State.IsDead || linked == __instance.Initiator || bearer.DistanceTo(linked) > 65.Feet().Meters)
            {
                return;
            }
            part.LastJuxtaposeAt = now;
            RuleSavingThrow save = Rulebook.Trigger(new RuleSavingThrow(linked, SavingThrowType.Will, part.JuxtaposeDC));
            KruneAwakening.Note($"hostile juxtaposition: {__instance.Initiator?.CharacterName} attacks; {linked.CharacterName} Will {save.RollResult} vs DC {part.JuxtaposeDC}: {(save.IsPassed ? "resists" : "is swapped in")}.");
            if (save.IsPassed)
            {
                return;
            }
            UnityEngine.Vector3 here = bearer.Position;
            bearer.Position = linked.Position;
            linked.Position = here;
            bearer.View?.StopMoving();
            linked.View?.StopMoving();
            __instance.AutoMiss = true;
        }
    }

    /// <summary>Wall of stone: the director (or any caster) raises a real wall at the target point (KruneWalls).</summary>
    public sealed class KwrRaiseWall : ContextAction
    {
        public override string GetCaption()
        {
            return "Raise wall of stone";
        }

        public override void RunAction()
        {
            KruneWalls.Raise(Context, Target.Point);
        }
    }
}
