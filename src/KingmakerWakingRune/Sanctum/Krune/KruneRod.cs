using System;
using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerWakingRune.Sanctum.Wards;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Krune's rune-carved rod (page 16; RodRules): a greater metamagic rod of seven feats, up to three applied to one spell as
    /// it is cast, paid from 10 charges renewed every 24 hours (one charge per spell level added). The game's own metamagic is
    /// added while the cast's parameters are calculated (the slot and its level are untouched) and the charges are spent only
    /// when the spell is actually cast. Empower, Enlarge (the game's Reach), Extend and Maximize are applied; Silent, Still and
    /// Widen have no mechanic in this game and are listed only.
    /// <para>
    /// The charges belong to the rod: they are kept in the saved sanctum state (taken over once from the resource earlier
    /// builds gave Krune), so a party that recovers it has what he left. Krune's director orders metamagic for one cast at a
    /// time: the order names his command and the spell, is checked again (rod in hand, charges) when it applies, and ends
    /// when that command ends, the order expires, a save loads or he dies. A party caster, while the party carries the rod,
    /// has a toggle for each applied feat (the game's party inventory is shared, so any caster may draw it).
    /// </para>
    /// </summary>
    internal static class RuneRod
    {
        private const double OrderSeconds = 12d;

        public static BlueprintFeature Wielder { get; private set; }
        private static readonly Dictionary<string, BlueprintActivatableAbility> s_Toggles = new Dictionary<string, BlueprintActivatableAbility>();

        private sealed class Order
        {
            public string CasterId;
            public BlueprintAbility Spell;
            public UnitCommand Command;
            public List<string> Requested;
            public List<RodOption> Applied = new List<RodOption>();
            public double At;
        }

        private static Order s_Order;
        // A party caster's metamagic from the rod, by unit and spell, between the parameters and the cast.
        private static readonly Dictionary<string, List<RodOption>> s_PartyApplied = new Dictionary<string, List<RodOption>>();

        public static void Build()
        {
            var feats = new List<BlueprintUnitFact>();
            foreach (RodOption option in RodRules.Options.Where(o => o.Supported))
            {
                BlueprintBuff on = KruneBlueprints.Buff(KruneBlueprints.OpeningStagger, "KWR_RuneRodOn_" + option.Feat, "Rune-Carved Rod: " + option.Feat, $"The next spell is cast with {option.Feat} Spell from Krune's rune-carved rod ({option.Cost} charge{(option.Cost > 1 ? "s" : string.Empty)}).", true, false);
                var toggle = KruneBlueprints.Create<BlueprintActivatableAbility>("KWR_RuneRodToggle_" + option.Feat);
                KruneBlueprints.Name(toggle, "Rune-Carved Rod: " + option.Feat + " Spell", $"While on, each spell this character casts gets {option.Feat} Spell from Krune's rune-carved rod ({option.Note}), paying {option.Cost} of the rod's charges, if the spell accepts it and charges remain. Up to three of the rod's feats apply to one spell. The rod has 10 charges, renewed every 24 hours.");
                toggle.Buff = on;
                toggle.ActivationType = AbilityActivationType.Immediately;
                toggle.DeactivateIfCombatEnded = false;
                toggle.ResourceAssetIds = new string[0];
                AccessTools.Field(typeof(BlueprintUnitFact), "m_Icon").SetValue(toggle, KruneBlueprints.Rod.Icon);
                toggle.ComponentsArray = new BlueprintComponent[0];
                KruneBlueprints.Register(toggle);
                s_Toggles[option.Native] = toggle;
                feats.Add(toggle);
            }
            var add = KruneBlueprints.Component<Kingmaker.UnitLogic.FactLogic.AddFacts>("KWR_RuneRodWielderFacts");
            add.Facts = feats.ToArray();
            Wielder = KruneBlueprints.Feature("KWR_RuneRodWielder", "Krune's Rune-Carved Rod", "The party carries Krune's rune-carved rod: Empower, Enlarge, Extend and Maximize Spell, up to three on one spell, from its 10 daily charges. (Silent, Still and Widen Spell have no effect in this world.)",
                add, KruneBlueprints.Component<KwrRodWielder>("KWR_RuneRodWielderLogic"));
        }

        public static void ResetSession()
        {
            s_Order = null;
            s_PartyApplied.Clear();
        }

        public static bool Possesses(UnitEntityData unit)
        {
            if (unit == null)
            {
                return false;
            }
            if (unit.IsPlayerFaction)
            {
                return Game.Instance.Player.Inventory.Items.Any(KruneLoot.IsRod);
            }
            return unit.Inventory.Items.Any(KruneLoot.IsRod);
        }

        /// <summary>The charges left now: taken over once from Krune's old resource, renewed after 24 hours.</summary>
        public static int Charges()
        {
            KruneState krune = WardStore.Existing?.Krune;
            if (krune == null)
            {
                return 0;
            }
            double now = Game.Instance.TimeController.GameTime.TotalSeconds;
            if (krune.RodCharges < 0)
            {
                UnitEntityData actor = KruneAwakening.Actor;
                int legacy = actor != null && KruneBlueprints.RodCharges != null && actor.Descriptor.Resources.ContainsResource(KruneBlueprints.RodCharges)
                    ? actor.Descriptor.Resources.GetResourceAmount(KruneBlueprints.RodCharges) : RodRules.ChargesPerDay;
                krune.RodCharges = Math.Max(0, Math.Min(RodRules.ChargesPerDay, legacy));
                krune.RodRenewedAt = now;
                KruneAwakening.Note($"rune-carved rod: its charges are kept with the rod from now on ({krune.RodCharges}).");
            }
            if (RodRules.Renews(krune.RodRenewedAt, now, WardProfile.DaySeconds) && krune.RodCharges < RodRules.ChargesPerDay)
            {
                krune.RodCharges = RodRules.ChargesPerDay;
                krune.RodRenewedAt = now;
                KruneAwakening.Note("rune-carved rod: its charges are renewed (24 hours).");
            }
            return krune.RodCharges;
        }

        private static void Spend(int cost)
        {
            KruneState krune = WardStore.Existing?.Krune;
            if (krune != null && cost > 0)
            {
                krune.RodCharges = Math.Max(0, Charges() - cost);
            }
        }

        public static IEnumerable<string> Legal(BlueprintAbility spell)
        {
            return RodRules.Options.Where(o => o.Supported && (spell.AvailableMetamagic & Parse(o.Native)) != 0).Select(o => o.Native);
        }

        public static Metamagic Parse(string native)
        {
            return (Metamagic)Enum.Parse(typeof(Metamagic), native);
        }

        /// <summary>The director's order for one cast; returns what it asks for (applied when the cast's parameters are taken), or null.</summary>
        public static string Offer(UnitEntityData krune, AbilityData spell, List<string> requested, UnitCommand command)
        {
            s_Order = null;
            if (requested.Count == 0 || spell.Blueprint.Type != AbilityType.Spell || !Possesses(krune))
            {
                return null;
            }
            List<string> already = RodRules.Options.Where(o => o.Supported && spell.MetamagicData != null && spell.MetamagicData.Has(Parse(o.Native))).Select(o => o.Native).ToList();
            List<RodOption> preview = RodRules.Apply(requested, Legal(spell.Blueprint).ToList(), already, Charges());
            if (preview.Count == 0)
            {
                return null;
            }
            s_Order = new Order { CasterId = krune.UniqueId, Spell = spell.Blueprint, Command = command, Requested = requested, At = WardRuntime.Now };
            return string.Join(" + ", preview.Select(o => o.Feat).ToArray());
        }

        /// <summary>His command ended (cast, interrupted or replaced): an order still unspent ends with it.</summary>
        public static void CommandEnded(UnitCommand command)
        {
            if (s_Order != null && s_Order.Command == command)
            {
                KruneAwakening.Note($"rune-carved rod: the order for {s_Order.Spell.name} ends with its command (not cast).");
                s_Order = null;
            }
        }

        private static bool Same(BlueprintAbility a, BlueprintAbility b)
        {
            return a != null && b != null && (a == b || a.Parent == b || b.Parent == a);
        }

        public static void KruneParams(UnitEntityData krune, RuleCalculateAbilityParams evt)
        {
            Order order = s_Order;
            if (order == null || krune.UniqueId != order.CasterId || !Same(evt.Spell, order.Spell))
            {
                return;
            }
            double now = WardRuntime.Now;
            if (now < order.At || now - order.At > OrderSeconds || krune.Descriptor.State.IsDead)
            {
                KruneAwakening.Note($"rune-carved rod: the order for {order.Spell.name} lapsed.");
                s_Order = null;
                return;
            }
            if (!Possesses(krune))
            {
                KruneAwakening.Note("rune-carved rod: no longer in his hands; the order is void.");
                s_Order = null;
                return;
            }
            order.Applied = ApplyTo(evt, order.Requested);
        }

        public static void KruneCast(UnitEntityData krune, RuleCastSpell evt)
        {
            Order order = s_Order;
            if (order == null || krune.UniqueId != order.CasterId || !Same(evt.Spell.Blueprint, order.Spell))
            {
                return;
            }
            s_Order = null;
            Pay(krune, evt, order.Applied);
        }

        public static void PartyParams(UnitEntityData caster, RuleCalculateAbilityParams evt)
        {
            if (evt.Spell == null || evt.Spell.Type != AbilityType.Spell || !Possesses(caster))
            {
                return;
            }
            List<string> requested = RodRules.Options.Where(o => o.Supported && s_Toggles.TryGetValue(o.Native, out BlueprintActivatableAbility t)
                && caster.Descriptor.ActivatableAbilities.Enumerable.Any(a => a.Blueprint == t && a.IsOn)).Select(o => o.Native).ToList();
            s_PartyApplied[caster.UniqueId + "/" + evt.Spell.AssetGuid] = requested.Count == 0 ? new List<RodOption>() : ApplyTo(evt, requested);
        }

        public static void PartyCast(UnitEntityData caster, RuleCastSpell evt)
        {
            string key = caster.UniqueId + "/" + evt.Spell.Blueprint.AssetGuid;
            if (s_PartyApplied.TryGetValue(key, out List<RodOption> applied))
            {
                s_PartyApplied.Remove(key);
                if (Possesses(caster))
                {
                    Pay(caster, evt, applied);
                }
            }
        }

        private static List<RodOption> ApplyTo(RuleCalculateAbilityParams evt, List<string> requested)
        {
            List<string> already = RodRules.Options.Where(o => o.Supported && evt.HasMetamagic(Parse(o.Native))).Select(o => o.Native).ToList();
            List<RodOption> applied = RodRules.Apply(requested, Legal(evt.Spell).ToList(), already, Charges());
            foreach (RodOption option in applied)
            {
                evt.AddMetamagic(Parse(option.Native));
            }
            return applied;
        }

        private static void Pay(UnitEntityData caster, RuleCastSpell evt, List<RodOption> applied)
        {
            int cost = applied.Sum(o => o.Cost);
            if (cost <= 0 || evt.IsUMDFailed)
            {
                return;
            }
            Spend(cost);
            string text = $"the rune-carved rod adds {string.Join(" + ", applied.Select(o => o.Feat).ToArray())} to {caster.CharacterName}'s {evt.Spell.Blueprint.name}: {cost} charges spent, {Charges()} left.";
            KruneAwakening.Note(text);
            Log.Info("Rune-carved rod: " + text);
        }

        private static double s_NextSync;

        /// <summary>A party caster has the rod's toggles while the party carries it, and loses them when it does not.</summary>
        public static void SyncWielders()
        {
            if (Wielder == null || Game.Instance?.Player == null || Time() < s_NextSync)
            {
                return;
            }
            s_NextSync = Time() + 2d;
            bool carried = Game.Instance.Player.Inventory.Items.Any(KruneLoot.IsRod);
            foreach (UnitEntityData unit in Game.Instance.Player.Party)
            {
                bool caster = unit.Descriptor.Spellbooks.Any(b => b.CasterLevel > 0);
                bool has = unit.Descriptor.Progression.Features.HasFact(Wielder);
                if (carried && caster && !has)
                {
                    unit.Descriptor.AddFact(Wielder);
                }
                else if ((!carried || !caster) && has)
                {
                    unit.Descriptor.RemoveFact(Wielder);
                }
            }
        }

        private static double Time()
        {
            return UnityEngine.Time.unscaledTime;
        }
    }

    /// <summary>Krune's own use of the rod: his director's order for one cast (RuneRod).</summary>
    public sealed class KwrKruneRod : OwnedGameLogicComponent<UnitDescriptor>, IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookHandler<RuleCastSpell>
    {
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            RuneRod.KruneParams(Owner.Unit, evt);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt)
        {
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            RuneRod.KruneCast(Owner.Unit, evt);
        }
    }

    /// <summary>A party caster's use of the rod: the feats toggled on (RuneRod).</summary>
    public sealed class KwrRodWielder : OwnedGameLogicComponent<UnitDescriptor>, IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookHandler<RuleCastSpell>
    {
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            RuneRod.PartyParams(Owner.Unit, evt);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt)
        {
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            RuneRod.PartyCast(Owner.Unit, evt);
        }
    }
}
