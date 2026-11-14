using System;
using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Krune's finite combat gear and the powers tied to his artifacts (pages 15–17): boots of speed (10 rounds a day, not
    /// consecutive), a pearl of power (8th: once a day, one spent 8th-level slot back), a ring of spell turning (three times
    /// a day), Wish through his arcane bond, and the dragon-tooth spear's lesser powers and telekinesis. Every use is a native
    /// ability or item with its own charges or daily resource; nothing refills on a load, a new phase or re-entering combat.
    /// </summary>
    internal static class KruneGear
    {
        public const int DiamondsPerWish = 5;

        public static BlueprintItemEquipmentFeet Boots { get; private set; }
        public static BlueprintActivatableAbility BootsToggle { get; private set; }
        public static BlueprintItemEquipmentUsable Pearl { get; private set; }
        public static BlueprintItemEquipmentRing RingOfSpellTurning { get; private set; }
        public static BlueprintAbilityResource Bond { get; private set; }
        public static BlueprintAbility Wish { get; private set; }
        public static BlueprintAbility WishSummon { get; private set; }
        public static BlueprintAbility WishWilting { get; private set; }
        public static BlueprintAbility SpearRay { get; private set; }
        public static BlueprintAbility SpearSlow { get; private set; }
        public static BlueprintAbility SpearTelekinesis { get; private set; }
        public static BlueprintFeature Powers { get; private set; }
        public static BlueprintBuff SpearResists { get; private set; }

        public static void Build(List<BlueprintScriptableObject> all)
        {
            BlueprintAbility Native(string name) => all.OfType<BlueprintAbility>().First(a => a.name == name);

            // Boots of speed: haste as a free action, charged a round at a time, 10 rounds a day.
            BootsToggle = KruneBlueprints.Create<BlueprintActivatableAbility>("KWR_KruneBootsOfSpeedToggle");
            KruneBlueprints.Name(BootsToggle, "Boots of Speed", "As a free action the wearer clicks the heels together and is hasted. Up to 10 rounds a day, which need not be consecutive.");
            BootsToggle.Buff = all.OfType<BlueprintBuff>().First(b => b.name == "HasteBuff");
            BootsToggle.ActivationType = AbilityActivationType.Immediately;
            BootsToggle.DeactivateIfCombatEnded = true;
            BootsToggle.ResourceAssetIds = new string[0];
            AccessTools.Field(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact), "m_Icon").SetValue(BootsToggle, Native("Haste").Icon);
            var perRound = KruneBlueprints.Component<ActivatableAbilityResourceLogic>("KWR_KruneBootsPerRound");
            perRound.SpendType = ActivatableAbilityResourceLogic.ResourceSpendType.NewRound;
            BootsToggle.ComponentsArray = new BlueprintComponent[] { perRound };
            KruneBlueprints.Register(BootsToggle);
            BlueprintItemEquipmentFeet bootsDonor = all.OfType<BlueprintItemEquipmentFeet>().First();
            Boots = UnityEngine.Object.Instantiate(bootsDonor);
            Boots.name = "KWR_KruneBootsOfSpeed";
            Boots.AssetGuid = ScenarioIds.Derive("krune." + Boots.name);
            AccessTools.Field(typeof(BlueprintItemEquipmentSimple), "m_Enchantments").SetValue(Boots, new Kingmaker.Blueprints.Items.Ecnchantments.BlueprintEquipmentEnchantment[0]);
            Boots.ActivatableAbility = BootsToggle;
            Boots.Charges = 10;
            Boots.SpendCharges = true;
            Boots.RestoreChargesOnRest = true;
            KruneBlueprints.ItemName(Boots, "Boots of Speed", "As a free action, the wearer is hasted for as long as the boots are active: 10 rounds a day, not necessarily consecutive.");
            KruneBlueprints.Register(Boots);

            Log.Info("Krune gear: boots built.");
            // Pearl of power (8th level): once a day, recall one 8th-level spell already cast.
            BlueprintAbility recall = KruneBlueprints.Create<BlueprintAbility>("KWR_KrunePearlRecall");
            KruneBlueprints.Name(recall, "Pearl of Power (8th)", "Once a day, the wielder recalls one 8th-level spell he had prepared and cast: that slot is ready again.");
            recall.Type = AbilityType.Supernatural;
            recall.ActionType = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard;
            recall.Range = AbilityRange.Personal;
            recall.CanTargetSelf = true;
            recall.LocalizedDuration = new Kingmaker.Localization.LocalizedString();
            recall.LocalizedSavingThrow = new Kingmaker.Localization.LocalizedString();
            var spentEighth = KruneBlueprints.Component<KwrHasSpentSlot>("KWR_KrunePearlNeedsSpent");
            spentEighth.Level = 8;
            recall.ComponentsArray = new BlueprintComponent[] { Run(Act<KwrRecallSlot>(a => a.Level = 8)), spentEighth };
            AccessTools.Field(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact), "m_Icon").SetValue(recall, Native("MageShield").Icon);
            KruneBlueprints.Register(recall);
            Pearl = UnityEngine.Object.Instantiate(all.OfType<BlueprintItemEquipmentUsable>().First(u => u.name == "HandOfMagusDanItem"));
            Pearl.name = "KWR_KrunePearlOfPower8";
            Pearl.AssetGuid = ScenarioIds.Derive("krune." + Pearl.name);
            Pearl.Type = UsableItemType.Other;
            Pearl.Ability = recall;
            Pearl.ActivatableAbility = null;
            Pearl.Charges = 1;
            Pearl.SpendCharges = true;
            Pearl.RestoreChargesOnRest = true;
            Pearl.CasterLevel = 17;
            Pearl.SpellLevel = 8;
            KruneBlueprints.ItemName(Pearl, "Pearl of Power (8th)", "Once a day, the wielder recalls one 8th-level spell he had prepared and already cast.");
            KruneBlueprints.Register(Pearl);

            Log.Info("Krune gear: pearl built.");
            // Ring of spell turning: three times a day, spell turning on the wearer (its own charges, not his slot).
            BlueprintAbility turn = KruneBlueprints.Create<BlueprintAbility>("KWR_KruneRingSpellTurning");
            KruneBlueprints.Name(turn, "Ring of Spell Turning", "Three times a day on command, the wearer turns spells as the spell spell turning (1d4+6 spell levels).");
            turn.Type = AbilityType.Supernatural;
            turn.ActionType = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard;
            turn.Range = AbilityRange.Personal;
            turn.CanTargetSelf = true;
            turn.LocalizedDuration = new Kingmaker.Localization.LocalizedString();
            turn.LocalizedSavingThrow = new Kingmaker.Localization.LocalizedString();
            turn.ComponentsArray = new BlueprintComponent[] { Run(Act<KwrApplySelfBuff>(a => { a.SpellTurning = true; a.SecondsPerLevel = 600d; })), Params(0, 13, 7) };
            KruneBlueprints.Register(turn);
            RingOfSpellTurning = UnityEngine.Object.Instantiate(KruneBlueprints.RingOfFreedom);
            RingOfSpellTurning.name = "KWR_KruneRingOfSpellTurning";
            RingOfSpellTurning.AssetGuid = ScenarioIds.Derive("krune." + RingOfSpellTurning.name);
            AccessTools.Field(typeof(BlueprintItemEquipmentSimple), "m_Enchantments").SetValue(RingOfSpellTurning, new Kingmaker.Blueprints.Items.Ecnchantments.BlueprintEquipmentEnchantment[0]);
            RingOfSpellTurning.Ability = turn;
            RingOfSpellTurning.Charges = 3;
            RingOfSpellTurning.SpendCharges = true;
            RingOfSpellTurning.RestoreChargesOnRest = true;
            KruneBlueprints.ItemName(RingOfSpellTurning, "Ring of Spell Turning", "Three times a day on command, the wearer turns spells as spell turning.");
            KruneBlueprints.Register(RingOfSpellTurning);

            Log.Info("Krune gear: ring built.");
            // Wish through the arcane bond: the bond's single daily casting, and 25,000 gp of diamonds consumed each time.
            Bond = KruneBlueprints.Resource("KWR_KruneArcaneBond", 1);
            WishSummon = WishOf(Native("SummonMonsterVIIISingle"), "KWR_KruneWishSummon", "Wish (Summon Monster VIII)", "Krune's wish duplicates summon monster VIII.");
            WishWilting = WishOf(Native("HorridWilting"), "KWR_KruneWishWilting", "Wish (Horrid Wilting)", "Krune's wish duplicates horrid wilting.");
            Wish = KruneBlueprints.Create<BlueprintAbility>("KWR_KruneWish");
            KruneBlueprints.Name(Wish, "Wish (arcane bond)", "Krune knows wish and casts it through his bonded spear (extra challenge): once a day, by the bond's single casting, with 25,000 gp of diamonds consumed each time. Here it duplicates summon monster VIII or horrid wilting.");
            Wish.Type = AbilityType.Spell;
            Wish.Range = AbilityRange.Close;
            Wish.LocalizedDuration = new Kingmaker.Localization.LocalizedString();
            Wish.LocalizedSavingThrow = new Kingmaker.Localization.LocalizedString();
            var variants = KruneBlueprints.Component<AbilityVariants>("KWR_KruneWishVariants");
            variants.Variants = new[] { WishSummon, WishWilting };
            Wish.ComponentsArray = new BlueprintComponent[] { variants };
            KruneBlueprints.Register(Wish);
            foreach (BlueprintAbility v in variants.Variants)
            {
                v.Parent = Wish;
            }

            Log.Info("Krune gear: wish built.");
            // The spear's own lesser powers (3/day each) and its dedicated telekinesis; it serves only Krune (page 15).
            SpearRay = SpearPower(Native("RayOfEnfeeblement"), "KWR_KruneSpearRay", "Dragon-Tooth Spear: Ray of Enfeeblement", "The spear's lesser power, three times a day.", 3, 1);
            SpearSlow = SpearPower(Native("Slow"), "KWR_KruneSpearSlow", "Dragon-Tooth Spear: Slow", "The spear's lesser power, three times a day.", 3, 3);
            SpearTelekinesis = KruneBlueprints.Create<BlueprintAbility>("KWR_KruneSpearTelekinesis");
            KruneBlueprints.Name(SpearTelekinesis, "Dragon-Tooth Spear: Telekinesis", "The spear's dedicated power, used to defend its master: a telekinetic trip against one foe within medium range (CMB = caster level 17 + Charisma 3; against the foe's CMD). It uses Krune's swift action.");
            SpearTelekinesis.Type = AbilityType.Supernatural;
            SpearTelekinesis.ActionType = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift;
            SpearTelekinesis.Range = AbilityRange.Medium;
            SpearTelekinesis.CanTargetEnemies = true;
            SpearTelekinesis.LocalizedDuration = new Kingmaker.Localization.LocalizedString();
            SpearTelekinesis.LocalizedSavingThrow = new Kingmaker.Localization.LocalizedString();
            AccessTools.Field(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact), "m_Icon").SetValue(SpearTelekinesis, Native("Slow").Icon);
            SpearTelekinesis.ComponentsArray = new BlueprintComponent[] { Run(Act<KwrTelekineticTrip>(a => { })), KruneBlueprints.Component<KwrSpearPossessed>("KWR_KruneSpearHeldTk") };
            KruneBlueprints.Register(SpearTelekinesis);

            Log.Info("Krune gear: spear powers built.");
            SpearResists = KruneBlueprints.Buff(KruneBlueprints.OpeningStagger, "KWR_KruneSpearResists", "The Spear Pulls Free", "This character mastered the dragon-tooth spear's Ego, but it will not stay: each round its telekinesis tries to disarm itself (CMB 20 against this character's CMD).", false, false,
                KruneBlueprints.Component<KwrSpearResistsWielder>("KWR_KruneSpearResistsLogic"));
            Powers = KruneBlueprints.Feature("KWR_KruneArtifactPowers", "Runelord's Artifacts", "Wish through his bonded spear, and the spear's own powers.",
                KruneBlueprints.GrantResource(Bond), AddFacts(Wish, SpearRay, SpearSlow, SpearTelekinesis, KruneDancing.Loose), KruneBlueprints.GrantResource(s_SpearRays), KruneBlueprints.GrantResource(s_SpearSlows),
                KruneBlueprints.Component<KwrKruneRod>("KWR_KruneRodLogic"));
            RuneRod.Build();
        }

        private static BlueprintAbilityResource s_SpearRays;
        private static BlueprintAbilityResource s_SpearSlows;

        private static BlueprintAbility WishOf(BlueprintAbility donor, string name, string display, string description)
        {
            var ability = UnityEngine.Object.Instantiate(donor);
            ability.name = name;
            ability.AssetGuid = ScenarioIds.Derive("krune." + name);
            ability.Type = AbilityType.Spell;
            KruneBlueprints.Name(ability, display, description + " Uses the bond's daily casting and consumes 25,000 gp of diamonds.");
            var cost = KruneBlueprints.Component<AbilityResourceLogic>(name + "_Bond");
            cost.RequiredResource = Bond;
            cost.IsSpendResource = true;
            cost.Amount = 1;
            var consume = KruneBlueprints.Component<AbilityExecuteActionOnCast>(name + "_Diamonds");
            consume.Conditions = new ConditionsChecker { Conditions = new Condition[0] };
            consume.Actions = new ActionList { Actions = new GameAction[] { Act<KwrConsumeDiamonds>(a => a.Count = DiamondsPerWish) } };
            ability.ComponentsArray = ability.ComponentsArray.Where(c => !(c is SpellListComponent) && !(c is ContextSetAbilityParams))
                .Concat(new BlueprintComponent[] { cost, consume, KruneBlueprints.Component<KwrWishMaterial>(name + "_Material"), KruneBlueprints.Component<KwrSpearPossessed>(name + "_Bonded"), Params(29, 17, 9) }).ToArray();
            AccessTools.Field(typeof(BlueprintAbility), "m_CachedTargetCheckers").SetValue(ability, null);
            KruneBlueprints.Register(ability);
            return ability;
        }

        private static BlueprintAbility SpearPower(BlueprintAbility donor, string name, string display, string description, int perDay, int spellLevel)
        {
            BlueprintAbilityResource resource = KruneBlueprints.Resource(name + "Uses", perDay);
            if (name.EndsWith("Ray", StringComparison.Ordinal))
            {
                s_SpearRays = resource;
            }
            else
            {
                s_SpearSlows = resource;
            }
            var ability = UnityEngine.Object.Instantiate(donor);
            ability.name = name;
            ability.AssetGuid = ScenarioIds.Derive("krune." + name);
            ability.Type = AbilityType.SpellLike;
            KruneBlueprints.Name(ability, display, description);
            var cost = KruneBlueprints.Component<AbilityResourceLogic>(name + "_Uses");
            cost.RequiredResource = resource;
            cost.IsSpendResource = true;
            cost.Amount = 1;
            // The spear's own caster level and its Charisma 17 set its DCs: 10 + spell level + 3.
            ability.ComponentsArray = ability.ComponentsArray.Where(c => !(c is SpellListComponent) && !(c is ContextSetAbilityParams))
                .Concat(new BlueprintComponent[] { cost, KruneBlueprints.Component<KwrSpearPossessed>(name + "_Held"), Params(10 + spellLevel + 3, 17, spellLevel) }).ToArray();
            AccessTools.Field(typeof(BlueprintAbility), "m_CachedTargetCheckers").SetValue(ability, null);
            KruneBlueprints.Register(ability);
            return ability;
        }

        private static ContextSetAbilityParams Params(int dc, int casterLevel, int spellLevel)
        {
            var p = KruneBlueprints.Component<ContextSetAbilityParams>("KWR_KruneParams_" + dc + "_" + casterLevel + "_" + spellLevel);
            p.DC = dc;
            p.CasterLevel = casterLevel;
            p.SpellLevel = spellLevel;
            p.Concentration = casterLevel + 10;
            return p;
        }

        private static Kingmaker.UnitLogic.FactLogic.AddFacts AddFacts(params BlueprintUnitFact[] facts)
        {
            var add = KruneBlueprints.Component<Kingmaker.UnitLogic.FactLogic.AddFacts>("KWR_KruneArtifactFacts");
            add.Facts = facts;
            return add;
        }

        private static AbilityEffectRunAction Run(params GameAction[] actions)
        {
            var run = KruneBlueprints.Component<AbilityEffectRunAction>("KWR_KruneGearRun");
            run.Actions = new ActionList { Actions = actions };
            return run;
        }

        private static T Act<T>(Action<T> set) where T : GameAction
        {
            var action = ScriptableObject.CreateInstance<T>();
            set(action);
            return action;
        }

        /// <summary>
        /// Once per actor (KruneCombat): his powers, and the boots, ring and pearl worn and carried. A Krune made by an earlier
        /// build gets them at his first tick; nothing already spent is restored.
        /// </summary>
        public static string Equip(UnitEntityData krune)
        {
            UnitDescriptor d = krune.Descriptor;
            if (!d.HasFact(Powers))
            {
                d.AddFact(Powers);
            }
            var notes = new List<string>();
            ItemSlot.IgnoreLock.Retain();
            try
            {
                if (!krune.Body.Feet.HasItem)
                {
                    krune.Body.Feet.InsertItem(Boots.CreateEntity());
                    notes.Add("boots of speed (10 rounds)");
                }
                if (!krune.Body.Ring2.HasItem)
                {
                    krune.Body.Ring2.InsertItem(RingOfSpellTurning.CreateEntity());
                    notes.Add("ring of spell turning (3/day)");
                }
                UsableSlot free = krune.Body.QuickSlots.FirstOrDefault(s => !s.HasItem);
                if (free != null && !krune.Body.QuickSlots.Any(s => s.HasItem && s.Item.Blueprint == Pearl))
                {
                    free.InsertItem(Pearl.CreateEntity());
                    notes.Add("pearl of power (8th)");
                }
            }
            finally
            {
                ItemSlot.IgnoreLock.Release();
            }
            return string.Join(", ", notes.ToArray());
        }

        public static ItemEntity Item(UnitEntityData krune, BlueprintItem blueprint)
        {
            return krune.Body.AllSlots.Where(s => s.HasItem).Select(s => s.Item).FirstOrDefault(i => i.Blueprint == blueprint);
        }
    }

    /// <summary>The spear must be his: in his hand. (An absent or taken spear ends its powers and his bonded Wish.)</summary>
    public sealed class KwrSpearPossessed : BlueprintComponent, IAbilityCasterChecker
    {
        public bool CorrectCaster(UnitEntityData caster)
        {
            return KruneLoot.IsSpear(caster.Body.PrimaryHand.MaybeItem);
        }

        public string GetReason()
        {
            return "Requires Krune's dragon-tooth spear in hand.";
        }
    }

    /// <summary>Wish's material: 25,000 gp of diamonds actually carried.</summary>
    public sealed class KwrWishMaterial : BlueprintComponent, IAbilityCasterChecker
    {
        public bool CorrectCaster(UnitEntityData caster)
        {
            return caster.Inventory.Items.Where(i => i.Blueprint.AssetGuid == KruneBlueprints.DiamondGuid).Sum(i => i.Count) >= KruneGear.DiamondsPerWish;
        }

        public string GetReason()
        {
            return "Requires 25,000 gp of diamonds.";
        }
    }

    /// <summary>The pearl needs a spent slot of its level to recall.</summary>
    public sealed class KwrHasSpentSlot : BlueprintComponent, IAbilityCasterChecker
    {
        public int Level;

        public bool CorrectCaster(UnitEntityData caster)
        {
            Spellbook book = KruneSpellbook.Book(caster) ?? caster.Descriptor.Spellbooks.FirstOrDefault(b => !b.Blueprint.Spontaneous);
            return book != null && book.GetMemorizedSpellSlots(Level).Any(s => s.Spell != null && !s.Available);
        }

        public string GetReason()
        {
            return "No spell of that level has been cast.";
        }
    }

    public sealed class KwrConsumeDiamonds : ContextAction
    {
        public int Count;

        public override string GetCaption()
        {
            return "Consume diamonds";
        }

        public override void RunAction()
        {
            UnitEntityData caster = Context.MaybeCaster;
            ItemEntity diamonds = caster?.Inventory.Items.FirstOrDefault(i => i.Blueprint.AssetGuid == KruneBlueprints.DiamondGuid);
            if (diamonds == null)
            {
                return;
            }
            caster.Inventory.Remove(diamonds, Count);
            KruneAwakening.Note($"wish: {Count} diamonds (25,000 gp) consumed; {caster.Inventory.Items.Where(i => i.Blueprint.AssetGuid == KruneBlueprints.DiamondGuid).Sum(i => i.Count)} left.");
        }
    }

    /// <summary>Recalls one spent prepared slot of the level (the highest-value first: none is refilled beyond the one).</summary>
    public sealed class KwrRecallSlot : ContextAction
    {
        public int Level;

        public override string GetCaption()
        {
            return "Recall a spell";
        }

        public override void RunAction()
        {
            UnitEntityData caster = Context.MaybeCaster;
            Spellbook book = caster == null ? null : KruneSpellbook.Book(caster) ?? caster.Descriptor.Spellbooks.FirstOrDefault(b => !b.Blueprint.Spontaneous);
            SpellSlot slot = book?.GetMemorizedSpellSlots(Level).FirstOrDefault(s => s.Spell != null && !s.Available);
            if (slot == null)
            {
                return;
            }
            foreach (SpellSlot linked in slot.LinkedSlots ?? new[] { slot })
            {
                linked.Available = true;
            }
            KruneAwakening.Note($"pearl of power: {slot.Spell.Blueprint.name} (level {Level}) recalled.");
        }
    }

    /// <summary>The spear's telekinesis: a trip at CMB 20 (CL 17 + Cha 3) against the foe's native CMD; prone on success.</summary>
    public sealed class KwrTelekineticTrip : ContextAction
    {
        public override string GetCaption()
        {
            return "Telekinetic trip";
        }

        public override void RunAction()
        {
            UnitEntityData caster = Context.MaybeCaster;
            UnitEntityData target = Target.Unit;
            if (caster == null || target == null)
            {
                return;
            }
            int d20 = Rulebook.Trigger(new RuleRollD20(caster)).Result;
            int cmd = Rulebook.Trigger(new RuleCalculateCMD(caster, target, CombatManeuver.Trip)).Result;
            bool hit = d20 != 1 && (d20 == 20 || d20 + 20 >= cmd);
            bool tripped = hit && target.CanBeKnockedOff();
            KruneAwakening.Note($"the spear's telekinesis trips at {target.CharacterName}: d20 {d20} + 20 vs CMD {cmd}: {(tripped ? "prone" : hit ? "no (already down, or cannot be knocked down)" : "no")}.");
            if (tripped)
            {
                target.Descriptor.State.Prone.ShouldBeActive = true;
                Kingmaker.PubSubSystem.EventBus.RaiseEvent(delegate(Kingmaker.PubSubSystem.IKnockOffHandler h) { h.HandleKnockOff(caster, target); });
            }
        }
    }
}
