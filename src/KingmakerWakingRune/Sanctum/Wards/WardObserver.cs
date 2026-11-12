using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Enums.Damage;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Parts;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// Turns resolved native rule results at the fixtures into rune outcomes. Runs after each rule has finished, so what it
    /// reads is what actually happened, never an ability's name. Only events aimed at a fixture proxy matter here.
    /// </summary>
    internal sealed class WardObserver :
        IGlobalRulebookHandler<RuleDealDamage>,
        IGlobalRulebookHandler<RuleDrainEnergy>,
        IGlobalRulebookHandler<RuleDealStatDamage>,
        IGlobalRulebookHandler<RuleDispelMagic>,
        IGlobalRulebookHandler<RuleCastSpell>
    {
        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            Guard(() => HandleDamage(evt));
        }

        private static void HandleDamage(RuleDealDamage evt)
        {
            string fixtureId = WardFixtures.IdOf(evt.Target);
            if (fixtureId == WardText.DoorId || fixtureId == WardText.GemId)
            {
                HandleBreakable(evt, fixtureId);
                return;
            }
            RuneFocus focus = Focus(evt.Target);
            if (focus == null || evt.ResultDamage == null)
            {
                return;
            }
            var components = new List<RuneDamageComponent>();
            foreach (DamageValue value in evt.ResultDamage.Where(v => !v.Source.Immune && v.FinalValue > 0))
            {
                components.Add(new RuneDamageComponent(TypeOf(value.Source), value.FinalValue));
            }
            if (components.Count == 0)
            {
                return;
            }
            UnitEntityData actor = Attacker(evt);
            bool bypassHardness = evt.ResultDamage.Any(v => v.Source is PhysicalDamage physical && HasAdamantine(physical));
            RuneOutcome outcome = RuneRules.ApplyDamage(focus, WardRuntime.State.Focus(focus.Id), WardRuntime.Now, Id(actor), IsSummoned(actor), bypassHardness, components);
            WardRuntime.ReportRune(focus, outcome, $"damage [{string.Join(", ", components.Select(c => $"{c.Type} {c.Amount}").ToArray())}] by {actor?.CharacterName}");
            // Damage never accumulates on a proxy: thresholds are the only measure of a rune.
            WardFixtures.Find(focus.Id).Damage = 0;
        }

        public void OnEventAboutToTrigger(RuleDrainEnergy evt)
        {
        }

        public void OnEventDidTrigger(RuleDrainEnergy evt)
        {
            Guard(() => HandleDrain(evt));
        }

        private static void HandleDrain(RuleDrainEnergy evt)
        {
            RuneFocus focus = Focus(evt.Target);
            if (focus == null || evt.TargetIsImmune || evt.Count <= 0)
            {
                return;
            }
            TimeSpan? duration = (TimeSpan?)Harmony12.AccessTools.Field(typeof(RuleDrainEnergy), "m_Duration").GetValue(evt);
            double expiresAt = duration.HasValue ? WardRuntime.Now + duration.Value.TotalSeconds : -1d;
            UnitEntityData actor = evt.Initiator;
            RuneOutcome outcome = RuneRules.ApplyNegativeLevels(focus, WardRuntime.State.Focus(focus.Id), WardRuntime.Now, Id(actor), evt.Count, expiresAt);
            WardRuntime.ReportRune(focus, outcome, $"{evt.Count} negative level(s) by {actor?.CharacterName}");
        }

        public void OnEventAboutToTrigger(RuleDealStatDamage evt)
        {
        }

        public void OnEventDidTrigger(RuleDealStatDamage evt)
        {
            Guard(() => HandleStat(evt));
        }

        private static void HandleStat(RuleDealStatDamage evt)
        {
            RuneFocus focus = Focus(evt.Target);
            if (focus == null || evt.IsDrain || evt.Damage <= 0)
            {
                return;
            }
            // BuffPoisonStatDamage reports the victim as initiator; the poisoner is the context's caster.
            var descriptor = evt.Reason?.Context?.SpellDescriptor ?? SpellDescriptor.None;
            if ((descriptor & (SpellDescriptor.Poison | SpellDescriptor.Disease)) == SpellDescriptor.None)
            {
                return;
            }
            UnitEntityData actor = evt.Reason?.Context?.MaybeCaster ?? evt.Initiator;
            RuneOutcome outcome = RuneRules.ApplyAbilityDamage(focus, WardRuntime.State.Focus(focus.Id), WardRuntime.Now, Id(actor), evt.Stat.Type.ToString(), evt.Damage);
            WardRuntime.ReportRune(focus, outcome, $"{evt.Damage} {evt.Stat.Type} from {descriptor} by {actor?.CharacterName}");
        }

        public void OnEventAboutToTrigger(RuleDispelMagic evt)
        {
        }

        public void OnEventDidTrigger(RuleDispelMagic evt)
        {
            Guard(() => HandleDispel(evt));
        }

        private static void HandleDispel(RuleDispelMagic evt)
        {
            if (evt.Buff != null && evt.Buff.Blueprint == WardBlueprints.WardMarker)
            {
                WardRuntime.OnWardDispel(WardFixtures.IdOf(evt.Target), evt.Success, evt.CasterLevelCheck, evt.DC);
                return;
            }
            RuneFocus focus = Focus(evt.Target);
            if (focus == null || evt.Buff == null || evt.Buff.Blueprint != WardBlueprints.DefenseWard)
            {
                return;
            }
            UnitEntityData actor = evt.Initiator;
            RuneOutcome outcome = RuneRules.ApplyDispel(focus, WardRuntime.State.Focus(focus.Id), WardRuntime.Now, Id(actor), evt.Success, evt.CasterLevelCheck, evt.DC);
            WardRuntime.ReportRune(focus, outcome, $"dispel by {actor?.CharacterName}: {evt.CasterLevelCheck} vs DC {evt.DC}");
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            Guard(() => HandleCast(evt));
        }

        private static void HandleCast(RuleCastSpell evt)
        {
            string fixtureId = WardFixtures.IdOf(evt.SpellTarget.Unit);
            if (evt.Success && fixtureId != null && WardStatues.IsStoneToFlesh(evt.Spell.Blueprint) && StatueManifest.Slots.Any(s => s.Id == fixtureId))
            {
                WardStatues.OnRestored(fixtureId, evt.Initiator);
                return;
            }
            RuneFocus focus = Focus(evt.SpellTarget.Unit);
            if (focus == null || !evt.Success || !evt.Spell.Blueprint.IsSpell)
            {
                return;
            }
            SpellDescriptor descriptor = evt.Context.SpellDescriptor;
            // Heighten and similar change the context; abilities granted outside a spellbook report zero there.
            int level = Math.Max(evt.Context.Params.SpellLevel, evt.Spell.SpellLevel);
            UnitEntityData actor = evt.Initiator;
            RuneOutcome outcome = RuneRules.ApplySpell(focus, WardRuntime.State.Focus(focus.Id), WardRuntime.Now, Id(actor), (descriptor & SpellDescriptor.Death) != 0, (descriptor & SpellDescriptor.Compulsion) != 0, level);
            WardRuntime.ReportRune(focus, outcome, $"{evt.Spell.Blueprint.name} (effective level {level}, {descriptor}) by {actor?.CharacterName}");
        }

        /// <summary>The door and emerald keep their own hit points; a proxy's HP is only a place for native damage to land.</summary>
        private static void HandleBreakable(RuleDealDamage evt, string fixtureId)
        {
            if (evt.ResultDamage == null)
            {
                return;
            }
            // One resolved hit: every component together, then hardness once. Energy is halved before hardness, as the core
            // object rules say for most objects; physical and force damage apply in full.
            int total = evt.ResultDamage.Where(v => !v.Source.Immune && v.FinalValue > 0).Sum(v => ObjectShare(v.Source, v.FinalValue));
            evt.Target.Damage = 0;
            bool bypass = evt.ResultDamage.Any(v => v.Source is PhysicalDamage physical && HasAdamantine(physical));
            if (fixtureId == WardText.DoorId)
            {
                var weapon = evt.DamageBundle.Weapon;
                WardDoor.TakeHit(Attacker(evt), total, weapon != null && weapon.Blueprint.IsMelee, bypass);
            }
            else
            {
                // Striking the soul gem is an attack on the rite: its guardians answer (WR-06). Destruction stays immediate.
                Lashmistress.LashmistressEncounter.EmeraldStruck(Attacker(evt));
                WardGem.TakeHit(total, bypass);
            }
        }

        /// <summary>A failed rule read must never disturb the game; the rulebook would otherwise swallow it unseen.</summary>
        private static void Guard(Action handler)
        {
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                Log.Error("Ward observer failed.", ex);
            }
        }

        private static RuneFocus Focus(UnitEntityData target)
        {
            string id = WardFixtures.IdOf(target);
            return id != null && KruneProjection.Foci.Any(f => f.Id == id) ? KruneProjection.Focus(id) : null;
        }

        private static UnitEntityData Attacker(RuleDealDamage evt)
        {
            return evt.Reason?.Context?.MaybeCaster ?? evt.Initiator;
        }

        private static string Id(UnitEntityData unit)
        {
            return unit?.UniqueId ?? string.Empty;
        }

        /// <summary>A real Summon Monster style creature: a pet is never this.</summary>
        private static bool IsSummoned(UnitEntityData unit)
        {
            UnitPartSummonedMonster summon = unit?.Get<UnitPartSummonedMonster>();
            return summon != null && summon.IsLinkedToSummoner && !unit.Descriptor.IsPet;
        }

        private static int ObjectShare(BaseDamage damage, int amount)
        {
            if (damage is EnergyDamage energy)
            {
                // Life energy does not harm stone at all.
                if (energy.EnergyType == DamageEnergyType.NegativeEnergy || energy.EnergyType == DamageEnergyType.PositiveEnergy)
                {
                    return 0;
                }
                return amount / 2;
            }
            return amount;
        }

        private static bool HasAdamantine(PhysicalDamage physical)
        {
            return (physical.MaterialsMask & PhysicalDamageMaterial.Adamantite) != 0;
        }

        private static RuneDamageType TypeOf(BaseDamage damage)
        {
            switch (damage)
            {
                case PhysicalDamage physical:
                    return Physical(physical);
                case ForceDamage _:
                    return RuneDamageType.Force;
                case EnergyDamage energy:
                    return Energy(energy);
                default:
                    return RuneDamageType.Other;
            }
        }

        private static RuneDamageType Physical(PhysicalDamage physical)
        {
            PhysicalDamageForm form = physical.Form.GetValueOrDefault();
            if ((form & PhysicalDamageForm.Bludgeoning) != 0)
            {
                return RuneDamageType.Bludgeoning;
            }
            return (form & PhysicalDamageForm.Piercing) != 0 ? RuneDamageType.Piercing : RuneDamageType.Slashing;
        }

        private static RuneDamageType Energy(EnergyDamage energy)
        {
            switch (energy.EnergyType)
            {
                case DamageEnergyType.Fire:
                    return RuneDamageType.Fire;
                case DamageEnergyType.Cold:
                    return RuneDamageType.Cold;
                case DamageEnergyType.Acid:
                    return RuneDamageType.Acid;
                case DamageEnergyType.Electricity:
                    return RuneDamageType.Electricity;
                case DamageEnergyType.Sonic:
                    return RuneDamageType.Sonic;
                case DamageEnergyType.NegativeEnergy:
                    return RuneDamageType.NegativeEnergy;
                default:
                    return RuneDamageType.Other;
            }
        }
    }

}
