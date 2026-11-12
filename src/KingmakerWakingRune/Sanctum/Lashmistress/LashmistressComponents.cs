using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Brain;
using Kingmaker.Controllers.Brain.Blueprints.Considerations;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Newtonsoft.Json;

namespace KingmakerWakingRune.Sanctum.Lashmistress
{
    /// <summary>A defender's encounter identity, saved with the unit; nothing else may resolve to it.</summary>
    public sealed class LashmistressPart : UnitPart
    {
        [JsonProperty]
        public string DefenderKey;
    }

    /// <summary>The brain's question to the A4 tactics, answered from the live field. Native commands act.</summary>
    public sealed class KwrLashmistressConsideration : Consideration
    {
        public A4Aim Aim;
        public bool OnTarget;

        public override float Score(DecisionContext context)
        {
            try
            {
                return LashmistressEncounter.Score(Aim, OnTarget, context);
            }
            catch (Exception ex)
            {
                Log.Error($"A4 consideration {Aim} failed.", ex);
                return 0f;
            }
        }
    }

    /// <summary>
    /// The dark naga casts as a 7th-level sorcerer (Bestiary 211) from its own per-level slots, so each of its spells is a
    /// real spell at caster level 7 and its own spell level; the DC stays Charisma-based (and the preset's native bonus).
    /// </summary>
    public sealed class KwrNagaCasting : RuleInitiatorLogicComponent<RuleCalculateAbilityParams>
    {
        public override void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            var ability = evt.Blueprint as BlueprintAbility;
            if (ability != null && LashmistressBlueprints.NagaSpellLevel.TryGetValue(ability, out int level))
            {
                evt.ReplaceCasterLevel = LashmistressProfile.NagaCasterLevel;
                evt.ReplaceSpellLevel = level;
            }
        }

        public override void OnEventDidTrigger(RuleCalculateAbilityParams evt)
        {
        }
    }

    /// <summary>
    /// Order's Wrath and Unholy Blight on one creature in the burst (spell resistance is checked natively first): a native
    /// Will save, then damage by the creature's alignment on the spell's axis, then the rider for a failed full share.
    /// </summary>
    public sealed class KwrAlignedBurstAction : ContextAction
    {
        public bool Unholy;

        public override string GetCaption()
        {
            return Unholy ? "Unholy Blight on one creature" : "Order's Wrath on one creature";
        }

        public override void RunAction()
        {
            UnitEntityData target = Target.Unit;
            UnitEntityData caster = Context.MaybeCaster;
            if (target == null || caster == null || target.Descriptor.State.IsDead)
            {
                return;
            }
            Alignment alignment = target.Descriptor.Alignment.Value;
            BlightShare share = Unholy
                ? AlignedBurst.UnholyBlight(alignment.HasComponent(AlignmentComponent.Good), alignment.HasComponent(AlignmentComponent.Evil))
                : AlignedBurst.OrdersWrath(alignment.HasComponent(AlignmentComponent.Lawful), alignment.HasComponent(AlignmentComponent.Chaotic));
            if (share == BlightShare.Unaffected)
            {
                LashmistressEncounter.Note($"{GetCaption()}: {target.CharacterName} ({alignment}) is unaffected.");
                return;
            }
            RuleSavingThrow save = Context.TriggerRule(new RuleSavingThrow(target, SavingThrowType.Will, Context.Params.DC));
            int dice = AlignedBurst.Dice(Context.Params.CasterLevel);
            int rolled = Rulebook.Trigger(new RuleRollDice(caster, new DiceFormula(dice, DiceType.D8))).Result;
            int amount = AlignedBurst.Damage(rolled, share, save.IsPassed);
            if (amount > 0)
            {
                var energy = new EnergyDamage(DiceFormula.Zero, Unholy ? DamageEnergyType.Unholy : DamageEnergyType.Divine);
                energy.AddBonus(amount);
                Context.TriggerRule(new RuleDealDamage(caster, target, new DamageBundle(energy)));
            }
            bool rider = AlignedBurst.Rider(share, save.IsPassed);
            if (rider)
            {
                if (Unholy)
                {
                    int rounds = Rulebook.Trigger(new RuleRollDice(caster, new DiceFormula(1, DiceType.D4))).Result;
                    target.Descriptor.AddBuff(LashmistressBlueprints.Sickened, Context, TimeSpan.FromSeconds(rounds * 6d));
                }
                else
                {
                    target.Descriptor.AddBuff(LashmistressBlueprints.Dazed, Context, TimeSpan.FromSeconds(6d));
                }
            }
            LashmistressEncounter.Note($"{GetCaption()}: {target.CharacterName} ({alignment}) share {share}, Will {save.RollResult} vs {Context.Params.DC} {(save.IsPassed ? "saved" : "failed")}, {dice}d8 = {rolled} -> {amount}{(rider ? (Unholy ? ", sickened" : ", dazed") : string.Empty)}.");
        }
    }

    /// <summary>Command (halt): a native Will save; on a failure the creature stands and takes no action for one round.</summary>
    public sealed class KwrCommandAction : ContextAction
    {
        public override string GetCaption()
        {
            return "Command: halt";
        }

        public override void RunAction()
        {
            UnitEntityData target = Target.Unit;
            if (target == null || target.Descriptor.State.IsDead)
            {
                return;
            }
            RuleSavingThrow save = Context.TriggerRule(new RuleSavingThrow(target, SavingThrowType.Will, Context.Params.DC));
            if (!save.IsPassed)
            {
                target.Descriptor.AddBuff(LashmistressBlueprints.CommandHalt, Context, TimeSpan.FromSeconds(6d));
            }
            LashmistressEncounter.Note($"Command (halt) on {target.CharacterName}: Will {save.RollResult} vs {Context.Params.DC}: {(save.IsPassed ? "resisted" : "halts for a round")}.");
        }
    }

    /// <summary>Marks a creature that sees in Vandiana's darkness (darkvision 60 feet).</summary>
    public sealed class KwrDarkvisionSense : OwnedGameLogicComponent<UnitDescriptor>
    {
    }

    /// <summary>Marks a creature standing in her darkness; the concealment it grants is read in <see cref="Patches.WardPatches"/>.</summary>
    public sealed class KwrInDarkness : BuffLogic
    {
    }

    /// <summary>
    /// The dark naga's sting (Bestiary 211): a native Fortitude save against its Con-based DC with the poison's own descriptor,
    /// so poison immunities, neutralize poison and bonuses against poison all act natively; a failure is sleep for 2d4 minutes.
    /// The poison's one save is also its cure.
    /// </summary>
    public sealed class KwrNagaSting : RuleInitiatorLogicComponent<RuleAttackWithWeapon>
    {
        public override void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
        }

        public override void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            UnitEntityData target = evt.Target;
            if (evt.AttackRoll == null || !evt.AttackRoll.IsHit || evt.Weapon?.Blueprint != LashmistressBlueprints.NagaSting || target == null || target.Descriptor.State.IsDead)
            {
                return;
            }
            LashmistressEncounter.Poison(Owner.Unit, target);
        }
    }

    /// <summary>
    /// Her +1 whip's damage is nonlethal, and this game has no nonlethal damage: the hit is a native attack roll at its real
    /// 15-foot reach, and its 1d3+1 is recorded, not dealt as lethal damage. See docs/WR-06-LASHMISTRESS.md.
    /// </summary>
    public sealed class KwrNonlethalWhip : RuleInitiatorLogicComponent<RuleDealDamage>
    {
        public override void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (evt.AttackRoll?.Weapon?.Blueprint == LashmistressBlueprints.Whip)
            {
                evt.Modifier = 0f;
            }
        }

        public override void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (evt.AttackRoll?.Weapon?.Blueprint == LashmistressBlueprints.Whip)
            {
                LashmistressEncounter.Note($"Vandiana's whip strikes {evt.Target.CharacterName}: {evt.DamageWithoutReduction} nonlethal, which this game cannot deal; recorded only.");
            }
        }
    }

    /// <summary>
    /// The once-only calibration to the printed sheet, computed at spawn with the preset's own Difficulty modifiers left out,
    /// so the player's preset still applies on top. Saved with the unit; nothing recomputes it.
    /// </summary>
    public sealed class KwrA4Calibration : BuffLogic
    {
        public static Dictionary<StatType, int> Pending = new Dictionary<StatType, int>();

        [JsonProperty]
        public Dictionary<StatType, int> Offsets;

        [JsonProperty]
        private List<ModifiableValue.Modifier> m_Modifiers;

        public override void OnFactActivate()
        {
            if (Offsets == null)
            {
                Offsets = new Dictionary<StatType, int>(Pending);
            }
            if (m_Modifiers != null)
            {
                return;
            }
            m_Modifiers = new List<ModifiableValue.Modifier>();
            foreach (var pair in Offsets.Where(p => p.Value != 0))
            {
                m_Modifiers.Add(Owner.Stats.GetStat(pair.Key).AddModifier(pair.Value, this, ModifierDescriptor.UntypedStackable));
            }
        }

        public override void OnFactDeactivate()
        {
            if (m_Modifiers != null)
            {
                foreach (var modifier in m_Modifiers)
                {
                    modifier?.Remove();
                }
            }
            m_Modifiers = null;
        }
    }

    /// <summary>
    /// The False Life she cast before the ritual: the printed 14 temporary hit points, initialized once, spent by damage
    /// natively and never refilled. It is a spell effect, so a dispel can remove it. Applied from her own spell context.
    /// </summary>
    public sealed class KwrPreparedFalseLife : BuffLogic
    {
        [JsonProperty]
        private ModifiableValue.Modifier m_Modifier;

        public int Remaining => m_Modifier != null && m_Modifier.AppliedTo != null ? Math.Max(0, m_Modifier.ModValue) : 0;

        public override void OnFactActivate()
        {
            if (m_Modifier == null)
            {
                m_Modifier = Owner.Stats.TemporaryHitPoints.AddModifier(LashmistressProfile.VandianaPrintedFalseLife, this, ModifierDescriptor.UntypedStackable);
            }
        }

        public override void OnFactDeactivate()
        {
            m_Modifier?.Remove();
            m_Modifier = null;
        }
    }
}
