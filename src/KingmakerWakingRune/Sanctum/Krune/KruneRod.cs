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
using Kingmaker.Utility;
using KingmakerWakingRune.Sanctum.Wards;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Krune's rune-carved rod (page 16; RodRules): a greater metamagic rod of seven feats, up to three applied to one spell as
    /// it is cast, paid from 10 charges renewed every 24 hours (one charge per spell level added). The game's own metamagic is
    /// added while the cast's parameters are calculated (the slot and its level are untouched) and the charges are spent only
    /// when the spell is actually cast. Charges are promised only within the cast itself: the game takes a cast's parameters
    /// as it builds the cast rule and resolves that rule at once, so the promise and the payment are one step (a
    /// concentration check reads what the rod would add, promising nothing; an effect recalculating parameters later
    /// without the spell's own data gets nothing from the rod). Empower, Enlarge (the game's Reach), Extend and Maximize are applied; Silent, Still and
    /// Widen are not implemented in this draft: listed as unavailable, never applied or charged.
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
            public AbilityData Ability;
            public string Reservation;
            public List<string> Requested;
            public List<RodOption> Applied = new List<RodOption>();
            public double At;
        }

        private static Order s_Order;
        private sealed class Pending
        {
            public List<RodOption> Applied;
            public double At;
        }

        /// <summary>A safety net only: a promise is made and settled within one cast rule; one left by a failed rule lapses after this.</summary>
        private const double PendingSeconds = 6d;

        // Each caster's metamagic from the rod (Krune's or a party caster's), by unit and spell, between the cast rule's
        // parameters and its resolution. While it is pending its charges are promised: another caster (a spell cast within
        // that cast) cannot draw them.
        private static readonly Dictionary<string, Pending> s_Pending = new Dictionary<string, Pending>();

        // The spells whose cast rules are being built now (innermost last): only their parameters promise charges.
        private static readonly List<AbilityData> s_Casting = new List<AbilityData>();

        /// <summary>A cast rule is being built for this spell: its parameters are the cast's own.</summary>
        public static void BeginCastRule(AbilityData spell)
        {
            s_Casting.Add(spell);
        }

        public static void EndCastRule(AbilityData spell)
        {
            int at = s_Casting.LastIndexOf(spell);
            if (at >= 0)
            {
                s_Casting.RemoveAt(at);
            }
        }

        private static bool Casting(AbilityData spell)
        {
            return spell != null && s_Casting.Contains(spell);
        }

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
            Wielder = KruneBlueprints.Feature("KWR_RuneRodWielder", "Krune's Rune-Carved Rod", "The party carries Krune's rune-carved rod: Empower, Enlarge (adapted: as Reach Spell, the next range band), Extend and Maximize Spell, up to three on one spell, from its 10 daily charges. Silent, Still and Widen Spell are not implemented in this draft (never applied or charged).",
                add, KruneBlueprints.Component<KwrRodWielder>("KWR_RuneRodWielderLogic"));
        }

        public static void ResetSession()
        {
            s_Order = null;
            s_Pending.Clear();
            s_Casting.Clear();
        }

        /// <summary>Read only, for the harness: the pending order, if any.</summary>
        public static string DebugState()
        {
            Order o = s_Order;
            KruneState k = WardStore.Existing?.Krune;
            string pending = string.Join(", ", s_Pending.Select(p => p.Key.Substring(0, 8) + ":" + string.Join("+", p.Value.Applied.Select(a => a.Feat).ToArray()) + "@" + p.Value.At.ToString("0.0")).ToArray());
            return (o == null ? "no order" : $"order {o.Spell.name} [{string.Join("+", o.Requested.ToArray())}] at {o.At:0.0} applied [{string.Join("+", o.Applied.Select(a => a.Feat).ToArray())}]")
                + $"; pending [{pending}]; stored {k?.RodCharges} renewedAt {k?.RodRenewedAt:0}";
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

        /// <summary>
        /// The charges left now: taken over once from Krune's old resource, then renewed on the rod's fixed 24-hour schedule
        /// (RodRules.Renew: a renewal that falls due while the rod is full still moves the schedule on, so an expenditure
        /// right after it is never refunded by the next read).
        /// </summary>
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
            double renewedAt = krune.RodRenewedAt;
            int renewed = RodRules.Renew(krune.RodCharges, ref renewedAt, now, WardProfile.DaySeconds);
            if (renewedAt != krune.RodRenewedAt)
            {
                KruneAwakening.Note($"rune-carved rod: renewal due ({krune.RodCharges} -> {renewed}); next renewal at {renewedAt + WardProfile.DaySeconds:0}.");
            }
            krune.RodCharges = renewed;
            krune.RodRenewedAt = renewedAt;
            return krune.RodCharges;
        }

        /// <summary>Spends what a cast promised; returns false (and spends what is left) if the rod cannot pay in full.</summary>
        private static bool Spend(int cost)
        {
            KruneState krune = WardStore.Existing?.Krune;
            if (krune == null || cost <= 0)
            {
                return cost <= 0;
            }
            int have = Charges();
            krune.RodCharges = Math.Max(0, have - cost);
            return have >= cost;
        }

        /// <summary>The charges other pending casts have promised (a round's grace), excluding this caster's own.</summary>
        private static int ReservedByOthers(string key)
        {
            double now = WardRuntime.Now;
            foreach (string stale in s_Pending.Where(p => now < p.Value.At || now - p.Value.At > PendingSeconds).Select(p => p.Key).ToList())
            {
                s_Pending.Remove(stale);
            }
            // A caster casts one spell at a time: his own previews of other spells promise nothing against him.
            string caster = key.Substring(0, key.IndexOf('/') + 1);
            return s_Pending.Where(p => !p.Key.StartsWith(caster, StringComparison.Ordinal)).Sum(p => p.Value.Applied.Sum(o => o.Cost));
        }

        private static string KeyOf(UnitEntityData caster, BlueprintAbility spell)
        {
            return caster.UniqueId + "/" + (spell.Parent ?? spell).AssetGuid;
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
            List<RodOption> preview = RodRules.Apply(requested, Legal(spell.Blueprint).ToList(), already, RodRules.Available(Charges(), ReservedByOthers(KeyOf(krune, spell.Blueprint))));
            if (preview.Count == 0)
            {
                return null;
            }
            s_Order = new Order { CasterId = krune.UniqueId, Spell = spell.Blueprint, Command = command, Ability = spell, Requested = requested, At = WardRuntime.Now };
            return string.Join(" + ", preview.Select(o => o.Feat).ToArray());
        }

        /// <summary>His command ended (cast, interrupted or replaced): an order still unspent ends with it.</summary>
        public static void CommandEnded(UnitCommand command)
        {
            if (s_Order != null && s_Order.Command == command)
            {
                if (s_Order.Reservation != null) s_Pending.Remove(s_Order.Reservation);
                KruneAwakening.Note($"rune-carved rod: the order for {s_Order.Spell.name} ends with its command (not cast).");
                s_Order = null;
            }
        }

        public static void KruneParams(UnitEntityData krune, RuleCalculateAbilityParams evt)
        {
            Order order = s_Order;
            if (order == null || evt.AbilityData == null || krune.UniqueId != order.CasterId || !ReferenceEquals(evt.AbilityData, order.Ability))
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
            List<RodOption> applied = ApplyTo(evt, order.Requested, KeyOf(krune, evt.Spell));
            if (Casting(evt.AbilityData))
            {
                order.Reservation = KeyOf(krune, evt.Spell);
                order.Applied = applied;
            }
        }

        public static void KruneCast(UnitEntityData krune, RuleCastSpell evt)
        {
            Order order = s_Order;
            if (order == null || krune.UniqueId != order.CasterId || !ReferenceEquals(evt.Spell, order.Ability))
            {
                return;
            }
            s_Order = null;
            s_Pending.Remove(KeyOf(krune, evt.Spell.Blueprint));
            Pay(krune, evt, order.Applied);
        }

        public static void PartyParams(UnitEntityData caster, RuleCalculateAbilityParams evt)
        {
            if (evt.Spell == null || evt.AbilityData == null || evt.Spell.Type != AbilityType.Spell || !Possesses(caster))
            {
                return;
            }
            List<string> requested = RodRules.Options.Where(o => o.Supported && s_Toggles.TryGetValue(o.Native, out BlueprintActivatableAbility t)
                && caster.Descriptor.ActivatableAbilities.Enumerable.Any(a => a.Blueprint == t && a.IsOn)).Select(o => o.Native).ToList();
            if (requested.Count > 0)
            {
                ApplyTo(evt, requested, KeyOf(caster, evt.Spell));
            }
            else if (Casting(evt.AbilityData))
            {
                s_Pending.Remove(KeyOf(caster, evt.Spell));
            }
        }

        public static void PartyCast(UnitEntityData caster, RuleCastSpell evt)
        {
            string key = KeyOf(caster, evt.Spell.Blueprint);
            if (s_Pending.TryGetValue(key, out Pending pending))
            {
                s_Pending.Remove(key);
                if (Possesses(caster))
                {
                    Pay(caster, evt, pending.Applied);
                }
            }
        }

        /// <summary>
        /// The metamagic one cast may have: what it asks for, legal for the spell, and affordable from the charges not already
        /// promised to another pending cast. With too few charges the spell is simply cast without it (never unpaid). Only
        /// the cast rule's own parameters promise the charges; any other calculation (a concentration check) only reads.
        /// </summary>
        private static List<RodOption> ApplyTo(RuleCalculateAbilityParams evt, List<string> requested, string key)
        {
            bool cast = Casting(evt.AbilityData);
            List<string> already = RodRules.Options.Where(o => o.Supported && evt.HasMetamagic(Parse(o.Native))).Select(o => o.Native).ToList();
            int available = RodRules.Available(Charges(), ReservedByOthers(key));
            List<RodOption> applied = RodRules.Apply(requested, Legal(evt.Spell).ToList(), already, available);
            foreach (RodOption option in applied)
            {
                evt.AddMetamagic(Parse(option.Native));
            }
            if (!cast)
            {
                if (applied.Count > 0)
                {
                    Log.Info($"Rune-carved rod: {evt.Initiator?.CharacterName}'s {evt.Spell.name} read outside its cast (a concentration check): [{string.Join("+", applied.Select(o => o.Feat).ToArray())}], nothing promised ({available} charges free).");
                }
                return applied;
            }
            if (applied.Count > 0)
            {
                s_Pending[key] = new Pending { Applied = applied, At = WardRuntime.Now };
            }
            else
            {
                s_Pending.Remove(key);
                if (requested.Count > 0)
                {
                    Log.Info($"Rune-carved rod: {evt.Initiator?.CharacterName}'s {evt.Spell.name}: [{string.Join("+", requested.ToArray())}] not applied ({available} charges free).");
                }
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
            bool paid = Spend(cost);
            KruneState krune = WardStore.Existing?.Krune;
            string text = $"the rune-carved rod adds {string.Join(" + ", applied.Select(o => o.Feat).ToArray())} to {caster.CharacterName}'s {evt.Spell.Blueprint.name}: {cost} charges spent, {krune?.RodCharges} left{(paid ? string.Empty : " (UNPAID: the rod ran short)")}.";
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

    /// <summary>
    /// Every cast rule takes its spell's parameters while it is built (RuleCastSpell's constructor) and is resolved at once;
    /// marking that moment binds the rod's promise to the cast itself rather than to any other calculation.
    /// </summary>
    [HarmonyPatch(typeof(RuleCastSpell), MethodType.Constructor, new Type[] { typeof(AbilityData), typeof(TargetWrapper) })]
    internal static class RodCastRulePatch
    {
        private static void Prefix(AbilityData spell)
        {
            RuneRod.BeginCastRule(spell);
        }

        private static void Postfix(AbilityData spell)
        {
            RuneRod.EndCastRule(spell);
        }
    }
}
