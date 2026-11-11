using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Units;
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
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using Newtonsoft.Json;

namespace KingmakerWakingRune.Sanctum.Encounter
{
    /// <summary>Marks the encounter's one combat entity, so a reload re-binds to it by identity.</summary>
    public sealed class KurshuPart : UnitPart
    {
        [JsonProperty]
        public string EncounterId;
    }

    /// <summary>
    /// Her spell-like abilities' caster level: 15, the printed 12 with its three negative levels added back. Divine Separation
    /// then lowers it the way a native negative level does. A limited wish duplicates its spell at a 7th-level spell's DC.
    /// </summary>
    public sealed class KwrKurshuCasting : RuleInitiatorLogicComponent<RuleCalculateAbilityParams>
    {
        public override void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            var ability = evt.Blueprint as BlueprintAbility;
            if (!KurshuBlueprints.IsSpellLike(ability))
            {
                return;
            }
            evt.ReplaceCasterLevel = KurshuProfile.UndrainedCasterLevel;
            if (KurshuBlueprints.IsLimitedWish(ability))
            {
                evt.ReplaceSpellLevel = KurshuProfile.LimitedWishSpellLevel;
            }
        }

        public override void OnEventDidTrigger(RuleCalculateAbilityParams evt)
        {
        }
    }

    /// <summary>
    /// Divine Separation's negative levels, applied exactly as the native negative-level buff applies its own (skills, saves,
    /// attacks, CMD, 5 hit points each, ability checks and caster level), but held here: no restoration finds them, and the
    /// native count that kills a creature never includes them. Only Feed lowers <see cref="Count"/>.
    /// </summary>
    public sealed class KwrDivineSeparation : BuffLogic, IInitiatorRulebookHandler<RuleSkillCheck>, IRulebookHandler<RuleSkillCheck>, IInitiatorRulebookSubscriber, IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IRulebookHandler<RuleCalculateAbilityParams>
    {
        /// <summary>Read by the buff as it applies, so its count always comes from the saved encounter state.</summary>
        public static int PendingCount { get; set; }

        [JsonProperty]
        private List<ModifiableValue.Modifier> m_Modifiers;

        [JsonProperty]
        public int Count { get; private set; }

        public override void OnFactActivate()
        {
            if (Count == 0)
            {
                Count = PendingCount;
            }
            Apply();
        }

        public override void OnFactDeactivate()
        {
            m_Modifiers?.ForEach(m => m.Remove());
            m_Modifiers = null;
        }

        private void Apply()
        {
            m_Modifiers?.ForEach(m => m.Remove());
            m_Modifiers = new List<ModifiableValue.Modifier>();
            if (Count <= 0)
            {
                return;
            }
            foreach (StatType stat in StatTypeHelper.Skills.Concat(StatTypeHelper.Saves))
            {
                m_Modifiers.Add(Owner.Stats.GetStat(stat).AddModifier(-Count, this, ModifierDescriptor.NegativeEnergyPenalty));
            }
            m_Modifiers.Add(Owner.Stats.AdditionalAttackBonus.AddModifier(-Count, this, ModifierDescriptor.NegativeEnergyPenalty));
            m_Modifiers.Add(Owner.Stats.AdditionalCMD.AddModifier(-Count, this, ModifierDescriptor.NegativeEnergyPenalty));
            m_Modifiers.Add(Owner.Stats.HitPoints.AddModifier(-5 * Count, this, ModifierDescriptor.NegativeEnergyPenalty));
        }

        public void OnEventAboutToTrigger(RuleSkillCheck evt)
        {
            if (StatTypeHelper.Attributes.Contains(evt.StatType))
            {
                evt.Bonus.AddModifier(-Count, null, ModifierDescriptor.NegativeEnergyPenalty);
            }
        }

        public void OnEventDidTrigger(RuleSkillCheck evt)
        {
        }

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            evt.AddBonusCasterLevel(-Count);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt)
        {
        }
    }

    /// <summary>
    /// Power Surge's one round: +2 to the DC of her next spell-like ability, used up by that ability's cast. The buff's own
    /// one-round duration ends an unused surge; nothing makes it permanent.
    /// </summary>
    public sealed class KwrPowerSurge : BuffLogic, IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookSubscriber, IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>
    {
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (KurshuBlueprints.IsSpellLike(evt.Blueprint as BlueprintAbility))
            {
                evt.AddBonusDC(KurshuProfile.PowerSurgeDcBonus);
            }
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt)
        {
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            if (evt.Success && KurshuBlueprints.IsSpellLike(evt.Spell?.Blueprint))
            {
                KurshuEncounter.OnSurgeUsed(Owner.Unit, evt.Spell);
                Buff.Remove();
            }
        }

        public override void OnRemoved()
        {
            KurshuEncounter.OnSurgeEnded(Owner.Unit);
        }
    }

    /// <summary>The surge ability's effect: one use recorded and the one-round buff applied.</summary>
    public sealed class KwrPowerSurgeAction : ContextAction
    {
        public override string GetCaption()
        {
            return "Power Surge";
        }

        public override void RunAction()
        {
            KurshuEncounter.ArmSurge(Context?.MaybeCaster);
        }
    }

    /// <summary>A slam drains 1d4 Intelligence: real drain, which rest does not heal and restoration does.</summary>
    public sealed class KwrIntelligenceDrain : RuleInitiatorLogicComponent<RuleAttackWithWeapon>
    {
        public override void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
        }

        public override void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.AttackRoll == null || !evt.AttackRoll.IsHit || evt.Weapon?.Blueprint != KurshuBlueprints.Slam || evt.Target == null || evt.Target.Descriptor.State.IsDead)
            {
                return;
            }
            var drain = new RuleDealStatDamage(Owner.Unit, evt.Target, StatType.Intelligence, new DiceFormula(1, DiceType.D4), 0) { IsDrain = true };
            Rulebook.Trigger(drain);
            KurshuEncounter.Note($"Slam hit {evt.Target.CharacterName}: Intelligence drain {drain.Damage} (immune {drain.Immune}); Intelligence now {evt.Target.Stats.Intelligence.ModifiedValue}, drain {evt.Target.Stats.Intelligence.Drain}.");
        }
    }

    /// <summary>
    /// The tail's grab: a free grapple check, made with the native maneuver roll, against a creature of her size or smaller.
    /// Success holds the target and constricts it once for 1d8+5. Holding with the tail leaves her free to act; see the rules.
    /// </summary>
    public sealed class KwrTailGrab : RuleInitiatorLogicComponent<RuleAttackWithWeapon>
    {
        public override void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
        }

        public override void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            UnitEntityData target = evt.Target;
            if (evt.AttackRoll == null || !evt.AttackRoll.IsHit || evt.Weapon?.Blueprint != KurshuBlueprints.Tail || target == null || target.Descriptor.State.IsDead)
            {
                return;
            }
            if (target.Descriptor.State.Size > Owner.State.Size || target.Buffs.GetBuff(KurshuBlueprints.Held) != null)
            {
                return;
            }
            var grab = Rulebook.Trigger(new RuleCombatManeuver(Owner.Unit, target, CombatManeuver.Grapple));
            KurshuEncounter.Note($"Tail grab on {target.CharacterName}: {grab.InitiatorCMValue} vs CMD {grab.TargetCMD}: {(grab.Success ? "held" : "slips free")}.");
            if (!grab.Success)
            {
                return;
            }
            target.Descriptor.AddBuff(KurshuBlueprints.Held, Owner.Unit, null);
            var damage = new PhysicalDamage(new DiceFormula(1, DiceType.D8), PhysicalDamageForm.Bludgeoning);
            damage.AddBonus(5);
            var constrict = Rulebook.Trigger(new RuleDealDamage(Owner.Unit, target, new DamageBundle(damage)));
            KurshuEncounter.Note($"Constrict on {target.CharacterName}: {constrict.Damage} bludgeoning.");
        }
    }

    /// <summary>
    /// Held in her coils: cannot move, -4 Dexterity, -2 on attacks. Each new round the target tries to break free with its
    /// own maneuver roll against her CMD; it is released at once if she falls, is held herself, or is out of reach.
    /// </summary>
    public sealed class KwrKurshuHold : BuffLogic, ITickEachRound
    {
        [JsonProperty]
        private List<ModifiableValue.Modifier> m_Modifiers;

        public override void OnFactActivate()
        {
            m_Modifiers = new List<ModifiableValue.Modifier>
            {
                Owner.Stats.Dexterity.AddModifier(-4, this, ModifierDescriptor.UntypedStackable),
                Owner.Stats.AdditionalAttackBonus.AddModifier(-2, this, ModifierDescriptor.UntypedStackable),
            };
        }

        public override void OnFactDeactivate()
        {
            m_Modifiers?.ForEach(m => m.Remove());
            m_Modifiers = null;
        }

        public void OnNewRound()
        {
            UnitEntityData holder = Context.MaybeCaster;
            if (holder == null || !holder.Descriptor.State.IsConscious || !holder.Descriptor.State.CanAct || holder.DistanceTo(Owner.Unit) > KurshuTactics.ReachMeters + holder.Corpulence + Owner.Unit.Corpulence + 0.5f)
            {
                KurshuEncounter.Note($"{Owner.CharacterName} is released from the coils.");
                Buff.Remove();
                return;
            }
            var escape = Rulebook.Trigger(new RuleCombatManeuver(Owner.Unit, holder, CombatManeuver.Grapple));
            KurshuEncounter.Note($"{Owner.CharacterName} struggles against the coils: {escape.InitiatorCMValue} vs CMD {escape.TargetCMD}: {(escape.Success ? "breaks free" : "still held")}.");
            if (escape.Success)
            {
                Buff.Remove();
            }
        }
    }

    /// <summary>Feed's target: the point must lie on an eligible corpse within her reach.</summary>
    public sealed class KwrFeedTarget : BlueprintComponent, IAbilityTargetChecker
    {
        public bool CanTarget(UnitEntityData caster, TargetWrapper target)
        {
            return KurshuEncounter.FeedCorpseAt(caster, target.Point, out _) != null;
        }
    }

    /// <summary>Feed's effect, once the full-round action has gone off.</summary>
    public sealed class KwrFeedAction : ContextAction
    {
        public override string GetCaption()
        {
            return "Feed";
        }

        public override void RunAction()
        {
            KurshuEncounter.Feed(Context?.MaybeCaster, Target?.Point ?? UnityEngine.Vector3.zero);
        }
    }

    /// <summary>Read only: every spell resistance check against her, every hit she takes and every cast she makes, for the record.</summary>
    public sealed class KwrKurshuObserver : OwnedGameLogicComponent<UnitDescriptor>, ITargetRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>, ITargetRulebookSubscriber, ITargetRulebookHandler<RuleSpellResistanceCheck>, IRulebookHandler<RuleSpellResistanceCheck>, IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            KurshuEncounter.OnDamaged(Owner.Unit, evt);
        }

        public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt)
        {
        }

        public void OnEventDidTrigger(RuleSpellResistanceCheck evt)
        {
            KurshuEncounter.Note($"Spell resistance {evt.SpellResistance} against {evt.Initiator?.CharacterName}'s {evt.Context?.SourceAbility?.name}: roll {evt.Roll}, {(evt.IsSpellResisted ? "resisted" : "overcome")}.");
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            KurshuEncounter.OnCast(Owner.Unit, evt);
        }
    }
}
