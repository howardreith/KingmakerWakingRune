using System;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;

namespace KingmakerWakingRune.Sanctum.Lashmistress
{
    /// <summary>
    /// Hostile rules between the party's side and an A4 defender, wherever they stand: an attack roll, damage, a saving throw
    /// against a hostile effect, or a harmful spell aimed at one. A cancelled cursor raises no rule, so it engages nothing.
    /// </summary>
    internal sealed class LashmistressObserver :
        IGlobalRulebookHandler<RuleAttackRoll>,
        IGlobalRulebookHandler<RuleDealDamage>,
        IGlobalRulebookHandler<RuleSavingThrow>,
        IGlobalRulebookHandler<RuleCastSpell>
    {
        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            Guard(() => LashmistressEncounter.OnHostileRule(evt.Initiator, evt.Target, "attack roll"));
        }

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            Guard(() => LashmistressEncounter.OnHostileRule(evt.Initiator, evt.Target, "damage"));
        }

        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
        }

        public void OnEventDidTrigger(RuleSavingThrow evt)
        {
            Guard(() => LashmistressEncounter.OnHostileRule(evt.Reason?.Caster, evt.Initiator, "saving throw"));
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            Guard(() =>
            {
                if (evt.Spell?.Blueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful && evt.SpellTarget?.Unit != null)
                {
                    LashmistressEncounter.OnHostileRule(evt.Initiator, evt.SpellTarget.Unit, "harmful spell " + evt.Spell.Blueprint.name);
                }
            });
        }

        private static void Guard(Action handler)
        {
            if (!SanctumScope.IsSanctumLoaded)
            {
                return;
            }
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                Log.Error("A4 observer failed.", ex);
            }
        }
    }
}
