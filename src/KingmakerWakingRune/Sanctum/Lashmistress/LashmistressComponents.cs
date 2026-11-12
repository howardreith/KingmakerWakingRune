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
using Kingmaker.UnitLogic.Abilities;
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
    /// A defender drinking one of its own potions as its brain's choice. The native cast-spell action treats any ability that
    /// comes from an item as already spent, so it never drinks a potion (0.7.10's potion actions could never fire); this
    /// action finds the potion in the creature's own quick slots, is available while one is left, and drinks it on itself.
    /// </summary>
    public sealed class KwrAiDrinkPotion : Kingmaker.Controllers.Brain.Blueprints.BlueprintAiAction
    {
        public string PotionGuid;

        private AbilityData Potion(UnitEntityData unit)
        {
            Kingmaker.Items.ItemEntity potion = unit.Body.QuickSlots.Select(s => s.MaybeItem).FirstOrDefault(i => i != null && i.Blueprint.AssetGuid == PotionGuid && i.Count > 0);
            return potion?.Ability?.Data;
        }

        public override void InitContext(DecisionContext context)
        {
            base.InitContext(context);
            context.Ability = Potion(context.Unit);
        }

        public override bool IsAvailable(DecisionContext context)
        {
            AbilityData potion = Potion(context.Unit);
            return potion != null && potion.IsAvailableForCast;
        }

        public override bool IsExpended(DecisionContext context)
        {
            return Potion(context.Unit) == null;
        }

        public override Kingmaker.UnitLogic.Commands.Base.UnitCommand CreateCommand(DecisionContext context, UnitEntityData target)
        {
            AbilityData potion = Potion(context.Unit);
            LashmistressEncounter.Note($"{context.Unit.CharacterName}'s own brain drinks {potion?.Blueprint.name} (its potion).");
            return new KwrPotionDrink(potion, context.Unit);
        }

        public override Kingmaker.Controllers.Brain.Blueprints.TargetType GetTargetType(DecisionContext context)
        {
            return Kingmaker.Controllers.Brain.Blueprints.TargetType.Self;
        }

        public override float GetRange(DecisionContext context)
        {
            return 0f;
        }

        public override float GetRadius(DecisionContext context)
        {
            return 0f;
        }
    }

    /// <summary>
    /// The drink its brain chose. A potion is a move action, and the native brain replaces any of its commands that has not yet
    /// started every few tenths of a second; a drink waiting for its move action was replaced before it could ever start. This
    /// one may be replaced only after waiting a full round without starting, as the native rule then applies.
    /// </summary>
    public sealed class KwrPotionDrink : Kingmaker.UnitLogic.Commands.UnitUseAbility
    {
        private readonly TimeSpan m_Created = Kingmaker.Game.Instance.TimeController.GameTime;

        public KwrPotionDrink(AbilityData spell, Kingmaker.Utility.TargetWrapper target)
            : base(spell, target)
        {
        }

        public override bool AiCanInterrupt
        {
            get
            {
                if (!IsStarted && Kingmaker.Game.Instance.TimeController.GameTime - m_Created < TimeSpan.FromSeconds(6d))
                {
                    return false;
                }
                return base.AiCanInterrupt;
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
    /// Channel energy is a cleric feature: its DC is 10 + half the cleric level + Charisma (page 13: DC 13). The game reads the
    /// whole character level for an ability outside a spellbook, so the caster level of her channel is her cleric level.
    /// </summary>
    public sealed class KwrClericChannel : RuleInitiatorLogicComponent<RuleCalculateAbilityParams>
    {
        public override void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            var ability = evt.Blueprint as BlueprintAbility;
            if (ability != null && ability.name.StartsWith("ChannelNegative", StringComparison.Ordinal) && evt.Spellbook == null)
            {
                evt.ReplaceCasterLevel = Owner.Progression.GetClassLevel(LashmistressBlueprints.ClericClassBlueprint);
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
            bool outsider = LashmistressBlueprints.OutsiderType != null && target.Descriptor.HasFact(LashmistressBlueprints.OutsiderType);
            AlignedBurst.Roll(Context.Params.CasterLevel, share, outsider, out int dice, out int sides);
            int rolled = Rulebook.Trigger(new RuleRollDice(caster, new DiceFormula(dice, sides == 6 ? DiceType.D6 : DiceType.D8))).Result;
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
            LashmistressEncounter.Note($"{GetCaption()}: {target.CharacterName} ({alignment}{(outsider ? ", outsider" : string.Empty)}) share {share}, Will {save.RollResult} vs {Context.Params.DC} {(save.IsPassed ? "saved" : "failed")}, CL {Context.Params.CasterLevel} {dice}d{sides} = {rolled} -> {amount}{(rider ? (Unholy ? ", sickened" : ", dazed") : string.Empty)}.");
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
    /// The whip's own enchantment, so it follows the weapon to whoever wields it: its damage is nonlethal, computed by the
    /// native damage rule (damage reduction, the preset, and so on) and added to the game's own nonlethal total instead of
    /// hit points; a creature with an armor bonus of +1 or a natural armor bonus of +3 takes none (Core Rulebook).
    /// </summary>
    public sealed class KwrWhipLash : Kingmaker.Blueprints.Items.Ecnchantments.WeaponEnchantmentLogic, IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>, IInitiatorRulebookSubscriber
    {
        private static readonly HashSet<RuleDealDamage> s_Lashes = new HashSet<RuleDealDamage>();

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (evt.DamageBundle?.Weapon != Owner || evt.Target == null)
            {
                return;
            }
            int armor = WhipNonlethal.ArmorBonus(evt.Target);
            int natural = WhipNonlethal.NaturalArmorBonus(evt.Target);
            if (Nonlethal.WhipBlocked(armor, natural))
            {
                evt.Modifier = 0f;
                LashmistressEncounter.Note($"Whip ({evt.Initiator.CharacterName}) on {evt.Target.CharacterName}: no damage, armor bonus {armor}, natural armor {natural}.");
                return;
            }
            evt.IsFake = true;
            s_Lashes.Add(evt);
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (!s_Lashes.Remove(evt))
            {
                return;
            }
            WhipNonlethal.Deal(evt.Initiator, evt.Target, evt.Damage, "whip");
        }
    }

    /// <summary>The game's own nonlethal total, written for the one source in this mod that deals it (her whip).</summary>
    internal static class WhipNonlethal
    {
        public static int ArmorBonus(UnitEntityData unit)
        {
            return unit.Stats.AC.Modifiers.Where(m => m.ModDescriptor == ModifierDescriptor.Armor || m.ModDescriptor == ModifierDescriptor.ArmorEnhancement).Sum(m => m.ModValue);
        }

        public static int NaturalArmorBonus(UnitEntityData unit)
        {
            return unit.Stats.AC.Modifiers.Where(m => m.ModDescriptor == ModifierDescriptor.NaturalArmor || m.ModDescriptor == ModifierDescriptor.NaturalArmorEnhancement).Sum(m => m.ModValue);
        }

        public static void Deal(UnitEntityData source, UnitEntityData target, int amount, string what)
        {
            if (target == null || target.Descriptor.State.IsDead || amount <= 0)
            {
                return;
            }
            int before = target.Stats.DamageNonLethal.BaseValue;
            bool canFall = target.Descriptor.State.AllowDyingCondition;
            Nonlethal.Apply(before, amount, target.Stats.HitPoints.ModifiedValue, target.Damage, canFall, out int after, out int overflow);
            target.Stats.DamageNonLethal.BaseValue = after;
            if (overflow > 0)
            {
                var direct = new DirectDamage(DiceFormula.Zero, overflow);
                Rulebook.Trigger(new RuleDealDamage(source, target, new DamageBundle(direct)));
            }
            LashmistressEncounter.Note($"{what} ({source?.CharacterName}) on {target.CharacterName}: {amount} nonlethal, total {before} -> {after} (hp {target.HPLeft}/{target.Stats.HitPoints.ModifiedValue}, lethal damage {target.Damage}{(overflow > 0 ? $", {overflow} past maximum hit points dealt as lethal" : string.Empty)}{(canFall ? string.Empty : "; cannot fall unconscious in this game, stops one short")}).");
        }
    }

    /// <summary>
    /// Healing magic heals the same amount of nonlethal damage (Core Rulebook). The native heal is capped at lethal damage, so
    /// for a creature with nonlethal damage only, the cap is raised for that one rule to the larger of the two totals and each
    /// total is then healed by what the roll covers. Untouched for every creature without nonlethal damage.
    /// </summary>
    internal sealed class NonlethalHealing : IGlobalRulebookHandler<RuleHealDamage>
    {
        private readonly Dictionary<RuleHealDamage, int> m_Before = new Dictionary<RuleHealDamage, int>();

        public void OnEventAboutToTrigger(RuleHealDamage evt)
        {
            UnitEntityData target = evt.Target;
            int nonlethal = target?.Stats.DamageNonLethal.BaseValue ?? 0;
            if (nonlethal <= 0)
            {
                return;
            }
            m_Before[evt] = target.Damage;
            if (nonlethal > target.Damage)
            {
                target.Damage = nonlethal;
            }
        }

        public void OnEventDidTrigger(RuleHealDamage evt)
        {
            if (!m_Before.TryGetValue(evt, out int lethalBefore))
            {
                return;
            }
            m_Before.Remove(evt);
            UnitEntityData target = evt.Target;
            int nonlethal = target.Stats.DamageNonLethal.BaseValue;
            target.Damage = Math.Max(0, lethalBefore - Math.Min(evt.Value, lethalBefore));
            target.Stats.DamageNonLethal.BaseValue = Nonlethal.Heal(nonlethal, evt.Value);
            LashmistressEncounter.Note($"healing on {target.CharacterName}: {evt.Value} heals lethal {lethalBefore} -> {target.Damage} and nonlethal {nonlethal} -> {target.Stats.DamageNonLethal.BaseValue}.");
        }
    }

    /// <summary>
    /// Sanctuary (cleric 1): an opponent that tries to attack her directly makes a Will save, once for this casting. On a
    /// failure that attack is lost and it cannot target her while the ward lasts; area effects are not affected. The verdicts
    /// are saved with the buff. See <see cref="Patches.SanctuaryTargetPatch"/> for commands that target her.
    /// </summary>
    public sealed class KwrSanctuaryWard : BuffLogic, ITargetRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>, ITargetRulebookSubscriber
    {
        [JsonProperty]
        public Dictionary<string, bool> Verdicts = new Dictionary<string, bool>();

        /// <summary>Rolls the attacker's save if it has none yet; true when that attacker is barred.</summary>
        public bool Attempt(UnitEntityData attacker, string how)
        {
            if (attacker == null || attacker == Owner.Unit || !attacker.IsEnemy(Owner.Unit))
            {
                return false;
            }
            if (Verdicts == null)
            {
                Verdicts = new Dictionary<string, bool>();
            }
            bool barred = SanctuaryRule.Barred(Verdicts, attacker.UniqueId, () => Context.TriggerRule(new RuleSavingThrow(attacker, SavingThrowType.Will, Context.Params.DC)).IsPassed, out bool rolled);
            if (rolled)
            {
                LashmistressEncounter.Note($"Sanctuary on {Owner.Unit.CharacterName}: {attacker.CharacterName} tries to {how}: Will vs {Context.Params.DC} {(barred ? "failed: it cannot attack her while the ward lasts" : "passed: unaffected by this casting")}.");
            }
            return barred;
        }

        public bool KnownBarred(UnitEntityData attacker)
        {
            return attacker != null && Verdicts != null && SanctuaryRule.KnownBarred(Verdicts, attacker.UniqueId);
        }

        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if (Attempt(evt.Initiator, "attack her"))
            {
                evt.AutoMiss = true;
            }
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
        }
    }

    /// <summary>Her sanctuary ends when she attacks or casts a harmful spell at a foe.</summary>
    public sealed class KwrSanctuaryEnds : BuffLogic, IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>, IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            End("she attacked " + evt.Target?.CharacterName);
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            UnitEntityData target = evt.SpellTarget?.Unit;
            bool harmful = evt.Spell?.Blueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful && (target == null || target.IsEnemy(Owner.Unit));
            if (harmful)
            {
                End("she cast " + evt.Spell.Blueprint.name);
            }
        }

        private void End(string why)
        {
            LashmistressEncounter.Note($"Sanctuary on {Owner.Unit.CharacterName} ends: {why}.");
            Buff.Remove();
        }
    }

    /// <summary>Chill Touch: the charges a casting holds, one touch each; another spell dissipates the rest (Core Rulebook).</summary>
    public sealed class KwrChillTouchCharges : BuffLogic, IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>, IInitiatorRulebookSubscriber
    {
        [JsonProperty]
        public int Charges = -1;

        public override void OnFactActivate()
        {
            if (Charges < 0)
            {
                Charges = ConjurerRules.ChillTouchCharges(Context.Params.CasterLevel);
                LashmistressEncounter.Note($"Chill Touch on {Owner.Unit.CharacterName}: {Charges} touches held (CL {Context.Params.CasterLevel}).");
            }
        }

        public void Spend()
        {
            Charges--;
            LashmistressEncounter.Note($"Chill Touch on {Owner.Unit.CharacterName}: {Math.Max(0, Charges)} touches left.");
            if (Charges <= 0)
            {
                Buff.Remove();
            }
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            BlueprintAbility spell = evt.Spell?.Blueprint;
            if (spell != null && spell.Type == AbilityType.Spell && spell != LashmistressBlueprints.ChillTouchStrike && spell != LashmistressBlueprints.PreparedAbilities["ChillTouch"])
            {
                LashmistressEncounter.Note($"Chill Touch on {Owner.Unit.CharacterName}: {Charges} held touches dissipate ({spell.name} cast).");
                Buff.Remove();
            }
        }
    }

    /// <summary>One touch of Chill Touch: 1d6 negative energy, and 1 Strength damage unless a Fortitude save succeeds.</summary>
    public sealed class KwrChillTouchStrikeAction : ContextAction
    {
        public override string GetCaption()
        {
            return "Chill Touch: one touch";
        }

        public override void RunAction()
        {
            UnitEntityData target = Target.Unit;
            UnitEntityData caster = Context.MaybeCaster;
            if (target == null || caster == null || target.Descriptor.State.IsDead)
            {
                return;
            }
            var energy = new EnergyDamage(new DiceFormula(1, DiceType.D6), DamageEnergyType.NegativeEnergy);
            Context.TriggerRule(new RuleDealDamage(caster, target, new DamageBundle(energy)));
            RuleSavingThrow save = Context.TriggerRule(new RuleSavingThrow(target, SavingThrowType.Fortitude, Context.Params.DC));
            if (!save.IsPassed)
            {
                Context.TriggerRule(new RuleDealStatDamage(caster, target, StatType.Strength, new DiceFormula(1, DiceType.One), 0));
            }
            LashmistressEncounter.Note($"Chill Touch on {target.CharacterName}: Fortitude {save.RollResult} vs {Context.Params.DC} {(save.IsPassed ? "saved" : "failed: 1 Strength damage")}.");
            caster.Buffs.Enumerable.SelectMany(b => b.SelectComponents<KwrChillTouchCharges>()).FirstOrDefault()?.Spend();
        }
    }

    /// <summary>
    /// Arcane bond (her ring), once a day: she recovers one spent wizard slot as a free action and casts it with its own action,
    /// as casting a spell from her book through the bond would. The slot is chosen by <see cref="ConjurerRules.ArcaneBondChoice"/>.
    /// </summary>
    public sealed class KwrArcaneBondAction : ContextAction
    {
        public override string GetCaption()
        {
            return "Arcane bond: recover a spent wizard spell";
        }

        public override void RunAction()
        {
            UnitEntityData caster = Context.MaybeCaster;
            Spellbook wizard = caster?.Descriptor.GetSpellbook(LashmistressBlueprints.WizardClassBlueprint);
            if (wizard == null)
            {
                return;
            }
            string source = LashmistressEncounter.ArcaneBondChoice(wizard, out SpellSlot slot);
            if (slot == null)
            {
                LashmistressEncounter.Note($"Arcane bond: {caster.CharacterName} has no spent wizard spell worth recovering.");
                return;
            }
            slot.Available = true;
            caster.Brain.RestoreAvailableActions();
            LashmistressEncounter.Note($"Arcane bond: {caster.CharacterName} recovers {source} (level {slot.SpellLevel}) through her bonded ring.");
        }
    }

    /// <summary>Summoner's charm (conjurer 1): her summoned creatures last half her wizard level longer, at least 1 round.</summary>
    public sealed class KwrSummonersCharm : RuleInitiatorLogicComponent<RuleSummonUnit>
    {
        public override void OnEventAboutToTrigger(RuleSummonUnit evt)
        {
            if (evt.Context?.SourceAbility == null || evt.Context.SourceAbility.Type != AbilityType.Spell)
            {
                return;
            }
            int rounds = ConjurerRules.SummonersCharmRounds(Owner.Progression.GetClassLevel(LashmistressBlueprints.WizardClassBlueprint));
            evt.BonusDuration = new Kingmaker.Utility.Rounds(evt.BonusDuration.Value + rounds);
            LashmistressEncounter.Note($"Summoner's charm: {evt.Blueprint.name} lasts {evt.Duration.Value} + {evt.BonusDuration.Value} rounds.");
        }

        public override void OnEventDidTrigger(RuleSummonUnit evt)
        {
        }
    }

    /// <summary>A Blackstrike's Disruptive feat; the defensive-casting DC is raised in <see cref="Patches.DisruptivePatch"/>.</summary>
    public sealed class KwrDisruptive : OwnedGameLogicComponent<UnitDescriptor>
    {
    }

    /// <summary>
    /// Her local abilities outside a spellbook act at the book they come from: a Chill Touch touch is a 1st-level wizard spell
    /// at her arcane caster level, with Intelligence for its DC.
    /// </summary>
    public sealed class KwrA4SpellParams : RuleInitiatorLogicComponent<RuleCalculateAbilityParams>
    {
        public override void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (evt.Blueprint != null && evt.Blueprint == LashmistressBlueprints.ChillTouchStrike)
            {
                Spellbook wizard = Owner.GetSpellbook(LashmistressBlueprints.WizardClassBlueprint);
                evt.ReplaceCasterLevel = wizard?.CasterLevel ?? LashmistressProfile.VandianaArcaneCasterLevel;
                evt.ReplaceSpellLevel = 1;
                evt.ReplaceStat = StatType.Intelligence;
            }
        }

        public override void OnEventDidTrigger(RuleCalculateAbilityParams evt)
        {
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
