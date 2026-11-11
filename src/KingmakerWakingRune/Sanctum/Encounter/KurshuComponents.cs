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

    /// <summary>
    /// A slam drains 1d4 Intelligence: real drain, which rest does not heal and restoration does. Drain that would take the
    /// score below 1, where the engine would kill, is held back instead and leaves the victim comatose (see <see cref="MindDrainRules"/>).
    /// </summary>
    public sealed class KwrIntelligenceDrain : RuleInitiatorLogicComponent<RuleAttackWithWeapon>
    {
        public override void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
        }

        public override void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            UnitEntityData target = evt.Target;
            if (evt.AttackRoll == null || !evt.AttackRoll.IsHit || evt.Weapon?.Blueprint != KurshuBlueprints.Slam || target == null || target.Descriptor.State.IsDead)
            {
                return;
            }
            KurshuEncounter.DrainIntelligence(Owner.Unit, target, new DiceFormula(1, DiceType.D4), "Slam hit");
        }
    }

    /// <summary>
    /// Intelligence 0 from her drain: comatose (the native unconscious condition, so helpless and unable to act) while the
    /// held-back drain stands. Each round it checks whether restoration has removed at least that much drain; then the
    /// victim wakes, and the held-back points become ordinary drain.
    /// </summary>
    public sealed class KwrMindDrained : BuffLogic, ITickEachRound
    {
        /// <summary>Read by the buff as it applies.</summary>
        public static int Pending0 { get; set; }

        [JsonProperty]
        public int Pending { get; set; }

        [JsonProperty]
        public int DrainWhenHeldBack { get; private set; }

        public override void OnFactActivate()
        {
            if (Pending == 0)
            {
                Pending = Pending0;
                DrainWhenHeldBack = Owner.Stats.Intelligence.Drain;
            }
        }

        public void OnNewRound()
        {
            var stat = Owner.Stats.Intelligence;
            if (!MindDrainRules.Recovered(DrainWhenHeldBack, stat.Drain, Pending))
            {
                return;
            }
            stat.Drain = MindDrainRules.DrainOnWaking(stat.Drain, Pending);
            KurshuEncounter.Note($"{Owner.CharacterName} wakes: restoration removed enough drain; Intelligence {stat.ModifiedValue}, drain {stat.Drain}.");
            Buff.Remove();
        }
    }

    /// <summary>
    /// The tail's grab: a free grapple check, with the native maneuver roll, against a creature of her size or smaller while
    /// her tail holds no one else. She grapples normally, so she too is grappled. A success constricts once for 1d8+5.
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
            KurshuEncounter.TryGrab(Owner.Unit, target);
        }
    }

    /// <summary>
    /// The grappled condition's penalties: -4 Dexterity and -2 on attack rolls. The attack penalty also lowers combat
    /// maneuver checks natively, so a check to grapple or escape adds the 2 back, as the grappled condition excepts them.
    /// </summary>
    internal static class Grappled
    {
        public const int AttackPenalty = 2;

        public static List<ModifiableValue.Modifier> Apply(BuffLogic logic, UnitDescriptor owner)
        {
            return new List<ModifiableValue.Modifier>
            {
                owner.Stats.Dexterity.AddModifier(-4, logic, ModifierDescriptor.UntypedStackable),
                owner.Stats.AdditionalAttackBonus.AddModifier(-AttackPenalty, logic, ModifierDescriptor.UntypedStackable),
            };
        }
    }

    /// <summary>
    /// Held in her coils, grappled: cannot move, -4 Dexterity, -2 on attacks, no attacks of opportunity, and a spell needs a
    /// concentration check. Each new round the creature tries to break free with its own maneuver roll against her CMD, and
    /// freedom of movement frees it outright.
    /// </summary>
    public sealed class KwrKurshuHold : BuffLogic, ITickEachRound
    {
        [JsonProperty]
        private List<ModifiableValue.Modifier> m_Modifiers;

        public override void OnFactActivate()
        {
            m_Modifiers = Grappled.Apply(this, Owner);
        }

        public override void OnFactDeactivate()
        {
            m_Modifiers?.ForEach(m => m.Remove());
            m_Modifiers = null;
        }

        public void OnNewRound()
        {
            KurshuEncounter.OnHeldRound(Owner.Unit, Context.MaybeCaster);
        }
    }

    /// <summary>
    /// Kurshu grappling the creature she holds: the same grappled condition on her. Whenever her standard action is ready
    /// again (real time) or her turn begins (turn-based), she maintains the hold with it or lets go; see the encounter.
    /// </summary>
    public sealed class KwrKurshuGrappling : BuffLogic
    {
        [JsonProperty]
        private List<ModifiableValue.Modifier> m_Modifiers;

        public override void OnFactActivate()
        {
            m_Modifiers = Grappled.Apply(this, Owner);
        }

        public override void OnFactDeactivate()
        {
            m_Modifiers?.ForEach(m => m.Remove());
            m_Modifiers = null;
        }

    }

    /// <summary>Maintaining the hold, a standard action: the grapple check again at +5, and on a success the tail and constrict.</summary>
    public sealed class KwrMaintainGrappleAction : ContextAction
    {
        public override string GetCaption()
        {
            return "Maintain grapple";
        }

        public override void RunAction()
        {
            KurshuEncounter.Maintain(Context?.MaybeCaster);
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
    public sealed class KwrKurshuObserver : OwnedGameLogicComponent<UnitDescriptor>, ITargetRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>, ITargetRulebookSubscriber, ITargetRulebookHandler<RuleSpellResistanceCheck>, IRulebookHandler<RuleSpellResistanceCheck>, IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>, IInitiatorRulebookSubscriber, IInitiatorRulebookHandler<RuleAttackWithWeapon>, IRulebookHandler<RuleAttackWithWeapon>
    {
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            KurshuEncounter.Note($"attacks {evt.Target?.CharacterName} with {evt.Weapon?.Blueprint?.name}: {evt.AttackRoll?.Roll}+{evt.AttackRoll?.AttackBonus} vs AC {evt.AttackRoll?.TargetAC}: {(evt.AttackRoll != null && evt.AttackRoll.IsHit ? "hit" : "miss")}; combat {Owner.Unit.IsInCombat}, turn-based {TurnBased.Controllers.CombatController.IsInTurnBasedCombat()}.");
            KurshuEncounter.OnAttack(Owner.Unit, evt);
        }

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
