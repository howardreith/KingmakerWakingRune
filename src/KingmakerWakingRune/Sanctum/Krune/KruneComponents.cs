using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Parts;
using Newtonsoft.Json;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>The real Krune's identity in the save. The dormant receiver never carries it.</summary>
    public sealed class KrunePart : UnitPart
    {
        [JsonProperty]
        public string Key = "Krune";
    }

    /// <summary>
    /// The still-linked benefits of the rune foci (Runes of Power, Su): typed modifiers computed from the current disabled
    /// mask alone. Applying the same mask again changes nothing, so a reload or a repeated projection has no effect; a new
    /// mask replaces the modifiers without touching damage taken. Not a spell: dispel magic does not reach it.
    /// </summary>
    public sealed class KwrKruneRunesOfPower : BuffLogic
    {
        /// <summary>The mask the first activation adopts, set by the adapter just before the buff is added.</summary>
        public static int PendingMask = -1;

        [JsonProperty]
        private int m_AppliedMask = -1;

        [JsonProperty]
        private List<ModifiableValue.Modifier> m_Modifiers;

        public int AppliedMask => m_AppliedMask;

        public override void OnFactActivate()
        {
            if (m_Modifiers != null)
            {
                return;
            }
            int mask = m_AppliedMask >= 0 ? m_AppliedMask : PendingMask;
            if (mask >= 0)
            {
                Add(mask);
            }
        }

        public override void OnFactDeactivate()
        {
            Remove();
        }

        /// <summary>Replaces the modifiers for a new mask. Returns false when the mask is already the applied one.</summary>
        public bool Apply(int mask)
        {
            if (mask == m_AppliedMask && m_Modifiers != null)
            {
                return false;
            }
            Remove();
            Add(mask);
            return true;
        }

        private void Add(int mask)
        {
            Wards.KruneProjectionResult r = Wards.KruneProjection.Project(mask);
            m_AppliedMask = mask;
            m_Modifiers = new List<ModifiableValue.Modifier>();
            UnitDescriptor owner = Owner;
            Modify(owner.Stats.HitPoints, KruneRules.LifeHitPoints(mask), ModifierDescriptor.UntypedStackable);
            Modify(owner.Stats.AC, r.NaturalArmor, ModifierDescriptor.NaturalArmor);
            Modify(owner.Stats.AC, r.DodgeAC, ModifierDescriptor.Dodge);
            Modify(owner.Stats.AC, r.DeflectionAC, ModifierDescriptor.Deflection);
            Modify(owner.Stats.Initiative, r.InitiativeInsight, ModifierDescriptor.Insight);
            Modify(owner.Stats.SaveFortitude, r.ResistanceSaves, ModifierDescriptor.Resistance);
            Modify(owner.Stats.SaveReflex, r.ResistanceSaves, ModifierDescriptor.Resistance);
            Modify(owner.Stats.SaveWill, r.ResistanceSaves, ModifierDescriptor.Resistance);
        }

        private void Modify(ModifiableValue stat, int value, ModifierDescriptor descriptor)
        {
            if (value != 0)
            {
                m_Modifiers.Add(stat.AddModifier(value, this, descriptor));
            }
        }

        private void Remove()
        {
            if (m_Modifiers != null)
            {
                foreach (ModifiableValue.Modifier modifier in m_Modifiers)
                {
                    modifier?.Remove();
                }
            }
            m_Modifiers = null;
        }
    }

    /// <summary>
    /// The revival's expenditure of personal magic: his caster level for level-dependent effects is lowered by the commit
    /// snapshot's loss for 24 hours from the awakening. Class level, spell access and Hit Dice are untouched, and an item's
    /// own caster level (a wand) is never lowered.
    /// </summary>
    public sealed class KwrKruneRevivalCasterLevel : RuleInitiatorLogicComponent<RuleCalculateAbilityParams>
    {
        public override void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (evt.AbilityData?.SourceItem != null)
            {
                return;
            }
            int loss = KruneAwakening.CasterLevelLoss();
            if (loss > 0)
            {
                evt.AddBonusCasterLevel(-loss);
            }
        }

        public override void OnEventDidTrigger(RuleCalculateAbilityParams evt)
        {
        }
    }

    /// <summary>The Rune of Control's competence bonus, on attacks with Krune's dragon-tooth spear only: +3 lit, +1 dark.</summary>
    public sealed class KwrKruneSpearCompetence : RuleInitiatorLogicComponent<RuleCalculateAttackBonusWithoutTarget>
    {
        public override void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            if (evt.Weapon == null || evt.Weapon.Blueprint != KruneBlueprints.Spear)
            {
                return;
            }
            int bonus = KruneAwakening.LinkedSheet().Linked.SpearCompetence;
            if (bonus != 0)
            {
                evt.AddBonus(bonus, Fact);
            }
        }

        public override void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
        }
    }

    /// <summary>
    /// The Rune of Control's protection from his own conjured creatures, by this project's interpretation: while the rune
    /// is lit, a creature Krune summoned that attacks him meets the game's protection-from-evil numbers (+2 deflection to
    /// AC, +2 resistance on his saves against its effects), whatever its alignment. Dark, the protection is gone (a
    /// nonnumerical benefit is not halved). The bodily-contact ward is not modelled.
    /// </summary>
    public sealed class KwrKruneConjuredWard : RuleTargetLogicComponent<RuleAttackRoll>
    {
        public override void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if (IsOwnConjuration(evt.Initiator))
            {
                evt.AddTemporaryModifier(Owner.Stats.AC.AddModifier(2, this, ModifierDescriptor.Deflection));
            }
        }

        public override void OnEventDidTrigger(RuleAttackRoll evt)
        {
        }

        private bool IsOwnConjuration(UnitEntityData attacker)
        {
            UnitPartSummonedMonster summoned = attacker?.Get<UnitPartSummonedMonster>();
            return summoned != null && summoned.Summoner == Owner.Unit && KruneAwakening.LinkedSheet().Linked.ConjuredCreatureProtection;
        }
    }
}
