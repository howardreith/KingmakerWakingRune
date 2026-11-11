using System;
using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Brain.Blueprints;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using KingmakerWakingRune.Blueprints;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Encounter
{
    /// <summary>
    /// Kurshu the Undying, built once with the other mod blueprints. The Erinyes donor supplies the model, its native class
    /// (for real Hit Dice, base attack and saves) and its difficulty scaling, and nothing else: its weapons, facts, brain and
    /// spells are dropped. Every statistic is set from <see cref="KurshuProfile"/>; see docs/WR-05-KURSHU.md.
    /// </summary>
    internal static class KurshuBlueprints
    {
        /// <summary>CR8_ErinyesDevilStandard: the game's winged female outsider.</summary>
        private const string Donor = "6ea3a75279bab234aa723989e30cb15a";
        private const string SlamLarge1d6 = "7fe0fa95a5c21ee439e6849b7e018a82";
        private const string TailLarge1d8 = "ae822725634c6f0418b8c48bd29df255";
        private const string OutsiderType = "9054d3988d491d944ac144e27b6bc318";
        private const string BuffDonor = "df3950af5a783bd4d91ab73eb8fa0fd3";
        private const int HitDice = 17;

        public static BlueprintUnit Unit { get; private set; }
        public static BlueprintItemWeapon Slam { get; private set; }
        public static BlueprintItemWeapon Tail { get; private set; }
        public static BlueprintBuff Regeneration { get; private set; }
        public static BlueprintBuff Separation { get; private set; }
        public static BlueprintBuff Surge { get; private set; }
        public static BlueprintBuff Held { get; private set; }
        public static BlueprintBuff Grappling { get; private set; }
        public static BlueprintBuff MindDrained { get; private set; }
        /// <summary>Her once-a-day Will reroll; removed by its own use, and given only when she first appears.</summary>
        public static BlueprintBuff ImprovedIronWill { get; private set; }
        public static BlueprintAbility MaintainGrapple { get; private set; }
        /// <summary>The native feats she carries from her printed list (the others are calibrated, crafting, or flight).</summary>
        public static readonly List<BlueprintFeature> NativeFeats = new List<BlueprintFeature>();
        public static BlueprintAbility PowerSurge { get; private set; }
        public static BlueprintAbility FeedAbility { get; private set; }
        public static BlueprintAbility Displacement { get; private set; }
        public static BlueprintAbility GreaterTeleport { get; private set; }
        public static BlueprintFeature OutsiderFeature { get; private set; }
        /// <summary>Why the encounter could not be built, or null.</summary>
        public static string Problem { get; private set; }

        /// <summary>Every ability of hers that spends a daily use, with its <see cref="KurshuUses"/> key.</summary>
        public static readonly Dictionary<BlueprintAbility, string> UseKeys = new Dictionary<BlueprintAbility, string>();
        public static readonly Dictionary<string, BlueprintAbilityResource> Resources = new Dictionary<string, BlueprintAbilityResource>();
        private static readonly HashSet<BlueprintAbility> s_SpellLike = new HashSet<BlueprintAbility>();
        private static readonly HashSet<BlueprintAbility> s_LimitedWish = new HashSet<BlueprintAbility>();

        private static Action<BlueprintScriptableObject> s_Register;

        public static bool IsSpellLike(BlueprintAbility ability)
        {
            return ability != null && s_SpellLike.Contains(ability);
        }

        public static bool IsLimitedWish(BlueprintAbility ability)
        {
            return ability != null && s_LimitedWish.Contains(ability);
        }

        /// <summary>One spell-like ability: the native spell it is cloned from, how she uses it, and how the brain weighs it.</summary>
        private sealed class SlaSpec
        {
            public string Use;
            public string Spell;
            public string Display;
            public KurshuAim? Aim;
            public float Score;
            public bool LimitedWish;
        }

        private static readonly SlaSpec[] Slas =
        {
            new SlaSpec { Use = KurshuUses.CureSeriousWounds, Spell = "CureSeriousWoundsCast", Aim = KurshuAim.CureSelf, Score = 10f },
            new SlaSpec { Use = KurshuUses.Displacement, Spell = "Displacement", Aim = KurshuAim.Displacement, Score = 9f },
            new SlaSpec { Use = KurshuUses.HoldPerson, Spell = "HoldPerson", Aim = KurshuAim.HoldPerson, Score = 7.5f },
            new SlaSpec { Use = KurshuUses.Fireball, Spell = "Fireball", Aim = KurshuAim.Fireball, Score = 7f },
            new SlaSpec { Use = KurshuUses.StinkingCloud, Spell = "StinkingCloud", Aim = KurshuAim.StinkingCloud, Score = 6.5f },
            new SlaSpec { Use = KurshuUses.Slow, Spell = "Slow", Aim = KurshuAim.Slow, Score = 6.5f },
            new SlaSpec { Use = KurshuUses.LightningBolt, Spell = "LightningBolt", Aim = KurshuAim.LightningBolt, Score = 6f },
            new SlaSpec { Use = KurshuUses.DispelMagic, Spell = "DispelMagicTarget", Display = "Dispel Magic", Aim = KurshuAim.DispelMagic, Score = 5.5f },
            new SlaSpec { Use = KurshuUses.VampiricTouch, Spell = "VampiricTouchCast", Aim = KurshuAim.VampiricTouch, Score = 5f },
            new SlaSpec { Use = KurshuUses.MagicMissile, Spell = "MagicMissile", Aim = KurshuAim.MagicMissile, Score = 4f },
            new SlaSpec { Use = KurshuUses.GreaterTeleport, Spell = "DimensionDoorCasterOnly", Display = "Greater Teleport (self only)" },
            new SlaSpec { Use = KurshuUses.LimitedWish, Spell = "BreakEnchantment", Display = "Limited Wish (Break Enchantment)", Aim = KurshuAim.BreakEnchantmentSelf, Score = 11f, LimitedWish = true },
            new SlaSpec { Use = KurshuUses.LimitedWish, Spell = "HoldMonster", Display = "Limited Wish (Hold Monster)", Aim = KurshuAim.HoldMonster, Score = 7.4f, LimitedWish = true },
            new SlaSpec { Use = KurshuUses.LimitedWish, Spell = "ConeOfCold", Display = "Limited Wish (Cone of Cold)", Aim = KurshuAim.ConeOfCold, Score = 7.2f, LimitedWish = true },
            new SlaSpec { Use = KurshuUses.LimitedWish, Spell = "DispelMagicGreaterTarget", Display = "Limited Wish (Greater Dispel Magic)", Aim = KurshuAim.GreaterDispelMagic, Score = 5f, LimitedWish = true },
        };

        public static void Build(BlueprintRoot root, IEnumerable<BlueprintScriptableObject> all, Action<BlueprintScriptableObject> register)
        {
            s_Register = register;
            try
            {
                BuildAll(root, all.ToList());
                Log.Info($"Kurshu built: {Unit.name} ({Unit.AssetGuid}), {s_SpellLike.Count} spell-like abilities, {Resources.Count} daily resources.");
            }
            catch (Exception ex)
            {
                Problem = ex.Message;
                Unit = null;
                Log.Error("Kurshu could not be built; A1 stays without its encounter.", ex);
            }
        }

        private static void BuildAll(BlueprintRoot root, List<BlueprintScriptableObject> all)
        {
            BlueprintUnit donor = all.OfType<BlueprintUnit>().First(u => u.AssetGuid == Donor);
            Dictionary<string, BlueprintAbility> abilities = all.OfType<BlueprintAbility>().GroupBy(a => a.name).ToDictionary(g => g.Key, g => g.First());
            Dictionary<string, BlueprintFeature> features = all.OfType<BlueprintFeature>().GroupBy(f => f.name).ToDictionary(g => g.Key, g => g.First());
            Dictionary<string, BlueprintItemWeapon> weapons = all.OfType<BlueprintItemWeapon>().GroupBy(w => w.AssetGuid).ToDictionary(g => g.Key, g => g.First());
            BlueprintBuff buffDonor = all.OfType<BlueprintBuff>().First(b => b.AssetGuid == BuffDonor);
            Slam = weapons[SlamLarge1d6];
            Tail = weapons[TailLarge1d8];
            OutsiderFeature = all.OfType<BlueprintFeature>().First(f => f.AssetGuid == OutsiderType);

            foreach (string key in KurshuUses.PerDay.Keys)
            {
                Resources[key] = Resource(key, KurshuUses.PerDay[key]);
            }

            var granted = new List<BlueprintUnitFact>();
            var actions = new List<BlueprintAiAction>();
            foreach (SlaSpec spec in Slas)
            {
                if (!abilities.TryGetValue(spec.Spell, out BlueprintAbility spell))
                {
                    throw new InvalidOperationException($"native spell {spec.Spell} not found");
                }
                BlueprintAbility sla = SpellLike(spell, spec);
                granted.Add(sla);
                if (spec.Use == KurshuUses.Displacement)
                {
                    Displacement = sla;
                }
                if (spec.Use == KurshuUses.GreaterTeleport)
                {
                    GreaterTeleport = sla;
                }
                if (spec.Aim.HasValue)
                {
                    actions.Add(CastAction(sla, spec.Aim.Value, spec.Score));
                }
                Log.Info($"Kurshu SLA {sla.name} from {spell.name} ({spell.AssetGuid}): use {spec.Use}, AI {(spec.Aim.HasValue ? spec.Aim + " " + spec.Score : "director only")}.");
            }

            Surge = Buff(buffDonor, "KWR_KurshuPowerSurgeBuff", "Power Surge", "The DC of Kurshu's next spell-like ability this round is increased by 2.", false, Component<KwrPowerSurge>("KWR_KurshuPowerSurgeLogic"));
            PowerSurge = OwnAbility("KWR_KurshuPowerSurge", "Power Surge", "Three times per day as a swift action, the herald can increase the DC of her next spell-like ability cast that round by 2.", KurshuUses.PowerSurge, Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift, AbilityRange.Personal, Run<KwrPowerSurgeAction>());
            granted.Add(PowerSurge);
            actions.Add(CastAction(PowerSurge, KurshuAim.PowerSurge, 8.5f));

            FeedAbility = OwnAbility("KWR_KurshuFeed", "Feed", "Once per day, the herald can devour an outsider's corpse of at least 8 Hit Dice as a full-round action, removing 1 negative level gained from her divine separation for each Hit Die of the devoured outsider.", KurshuUses.Feed, Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard, AbilityRange.Touch, Run<KwrFeedAction>(), Component<KwrFeedTarget>("KWR_KurshuFeedTarget"));
            FeedAbility.SetIsFullRoundAction(true);
            FeedAbility.CanTargetPoint = true;
            FeedAbility.CanTargetSelf = false;
            FeedAbility.CanTargetEnemies = false;
            FeedAbility.CanTargetFriends = false;
            granted.Add(FeedAbility);

            actions.Add(AttackAction());

            Regeneration = Buff(buffDonor, "KWR_KurshuRegeneration", "Regeneration 5 (acid)", "Kurshu heals 5 hit points each round and cannot die from hit point damage; acid stops her regeneration for a round, and only then can she die.", false,
                Regenerate());
            Separation = Buff(buffDonor, "KWR_KurshuDivineSeparation", "Divine Separation", "Separated from Lissala for thousands of years, Kurshu carries negative levels that no restoration can remove. Only feeding on an outsider's corpse removes them.", false,
                Component<KwrDivineSeparation>("KWR_KurshuDivineSeparationLogic"));
            // Both sides of her grapple carry the grappled condition: no movement, -4 Dexterity, -2 on attacks, no attacks of
            // opportunity, and a spell needs the native concentration check for difficult casting.
            Held = Buff(buffDonor, "KWR_KurshuHeld", "Held in Kurshu's Coils", "Grappled by the herald's tail: cannot move, -4 Dexterity, -2 on attack rolls, no attacks of opportunity, and casting a spell needs a concentration check. Each round the creature tries to break free; freedom of movement frees it.", true,
                Condition(UnitCondition.CantMove), Condition(UnitCondition.DisableAttacksOfOpportunity), Condition(UnitCondition.SpellCastingIsDifficult), Descriptor(SpellDescriptor.MovementImpairing), Component<KwrKurshuHold>("KWR_KurshuHoldLogic"));
            Grappling = Buff(buffDonor, "KWR_KurshuGrappling", "Grappling", "Kurshu holds a creature in her tail and is grappled herself: she cannot move, takes -4 Dexterity and -2 on attack rolls, makes no attacks of opportunity, and needs a concentration check to cast. Each round she must spend a standard action to keep the hold, or let go.", false,
                Condition(UnitCondition.CantMove), Condition(UnitCondition.DisableAttacksOfOpportunity), Condition(UnitCondition.SpellCastingIsDifficult), Component<KwrKurshuGrappling>("KWR_KurshuGrapplingLogic"));
            MaintainGrapple = OwnAbility("KWR_KurshuMaintainGrapple", "Maintain Grapple", "As a standard action, the herald squeezes the creature held in her tail: a grapple check at +5, and on a success tail and constrict damage.", null, Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard, AbilityRange.Custom, Run<KwrMaintainGrappleAction>());
            MaintainGrapple.Type = AbilityType.Extraordinary;
            MaintainGrapple.CustomRange = 15.Feet();
            MaintainGrapple.CanTargetEnemies = true;
            MaintainGrapple.CanTargetSelf = false;
            granted.Add(MaintainGrapple);
            var reroll = Component<ModifyD20>("KWR_KurshuImprovedIronWillReroll");
            reroll.Rule = RuleType.SavingThrow;
            reroll.RollsAmount = 1;
            reroll.RerollOnlyIfFailed = true;
            reroll.DispellOnRerollFinished = true;
            reroll.Chance = Simple(0);
            reroll.Value = Simple(0);
            reroll.Skill = new StatType[0];
            var saves = AccessTools.Field(typeof(ModifyD20), "m_SavingThrowType");
            saves.SetValue(reroll, Enum.ToObject(saves.FieldType, 4));
            ImprovedIronWill = Buff(buffDonor, "KWR_KurshuImprovedIronWill", "Improved Iron Will", "Once per day, Kurshu may reroll a Will save. She uses it on the first Will save she would fail, and the reroll stands.", false, reroll);
            MindDrained = Buff(buffDonor, "KWR_KurshuMindDrained", "Comatose (Intelligence 0)", "Kurshu's slams have drained this creature's Intelligence to 0: it lies comatose and helpless. Restoration that removes the drain wakes it.", true,
                Condition(UnitCondition.Unconscious), Component<KwrMindDrained>("KWR_KurshuMindDrainedLogic"));

            BlueprintFeature nature = Feature("KWR_KurshuNature", NatureComponents(features));
            BlueprintFeature calibration = Feature("KWR_KurshuPrintedStatistics", Calibration(donor, all, out BlueprintCharacterClass outsiderClass));
            var spellLike = Feature("KWR_KurshuSpellLikeAbilities", new BlueprintComponent[] { AddFactsOf(granted) }.Concat(Resources.Values.Select(GrantResource)).ToArray());

            var brain = Create<BlueprintBrain>("KWR_KurshuBrain");
            brain.Actions = actions.ToArray();
            s_Register(brain);

            string nameKey = "kwr.unit.kurshu";
            LocalizedText.Define(nameKey, "Kurshu the Undying");
            BlueprintUnit unit = LodgeCast.CloneUnit(donor, root, "KWR_KurshuTheUndying", ScenarioIds.Derive("unit.KWR_KurshuTheUndying"), nameKey, null);
            unit.Faction = root.SystemMechanics.FactionMobs;
            unit.Size = Size.Large;
            unit.Alignment = Alignment.LawfulEvil;
            unit.Gender = Gender.Female;
            unit.Strength = 20;
            unit.Dexterity = 23;
            unit.Constitution = 26;
            unit.Intelligence = 31;
            unit.Wisdom = 20;
            unit.Charisma = 25;
            unit.Speed = 30.Feet();
            unit.Brain = brain;
            unit.Race = null;
            unit.StartingInventory = new Kingmaker.Blueprints.Items.BlueprintItem[0];
            unit.Body = new BlueprintUnit.UnitBody
            {
                AdditionalLimbs = new[] { Slam, Slam },
                AdditionalSecondaryLimbs = new[] { Tail },
                QuickSlots = new Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentUsable[5],
            };
            var classLevels = Component<AddClassLevels>("KWR_KurshuHitDice");
            classLevels.CharacterClass = outsiderClass;
            classLevels.Levels = HitDice;
            classLevels.Skills = new StatType[0];
            classLevels.Selections = new SelectionEntry[0];
            classLevels.MemorizeSpells = new BlueprintAbility[0];
            classLevels.SelectSpells = new BlueprintAbility[0];
            classLevels.Archetypes = new BlueprintArchetype[0];
            // The player's preset scales her as it scales the donor: the donor's own native difficulty components, unaltered.
            BlueprintComponent[] difficulty = DifficultyComponents(donor);
            unit.ComponentsArray = difficulty.Concat(new BlueprintComponent[] { classLevels }).ToArray();
            unit.MaxHP = s_MaxHP;
            var facts = new List<BlueprintUnitFact> { OutsiderFeature };
            foreach (string subtype in new[] { "SubtypeEvil", "SubtypeLawful", "SubtypeExtraplanar" })
            {
                if (features.TryGetValue(subtype, out BlueprintFeature feature))
                {
                    facts.Add(feature);
                }
                else
                {
                    Log.Info($"Kurshu: native {subtype} not found; her subtype is recorded in the rules document only.");
                }
            }
            // Printed feats: Great Fortitude, Iron Will and Improved Initiative are already in the calibrated saves and
            // initiative, the crafting feats have nothing to act on here, and Hover has no flight to modify. Combat Casting
            // and Combat Reflexes are the game's own feats, acting as they do for anyone. The game's own Improved Iron Will
            // (IronWillImproved) is a flat +2 Will, not the printed once-a-day reroll, and would lift her Will above the
            // printed value; her reroll is built from the native d20 component instead (ImprovedIronWill, below).
            foreach (string feat in new[] { "CombatCasting", "CombatReflexes" })
            {
                if (features.TryGetValue(feat, out BlueprintFeature feature))
                {
                    NativeFeats.Add(feature);
                    facts.Add(feature);
                }
                else
                {
                    throw new InvalidOperationException($"native feat {feat} not found");
                }
            }
            facts.Add(nature);
            facts.Add(calibration);
            facts.Add(spellLike);
            unit.AddFacts = facts.ToArray();
            s_Register(unit);
            Unit = unit;
            Log.Info($"Kurshu unit: donor {donor.name} prefab {unit.Prefab?.AssetId}, class {outsiderClass.name} x{HitDice}, MaxHP {unit.MaxHP}, difficulty components {difficulty.Length}, facts [{string.Join(",", unit.AddFacts.Select(f => f.name).ToArray())}].");
        }

        private static int s_MaxHP;

        /// <summary>
        /// The printed numbers from the donor's own class at 17 Hit Dice: what its native progression gives is read from the
        /// class tables, and only the difference to the printed value is added, so nothing is counted twice.
        /// </summary>
        private static BlueprintComponent[] Calibration(BlueprintUnit donor, List<BlueprintScriptableObject> all, out BlueprintCharacterClass outsiderClass)
        {
            AddClassLevels donorLevels = donor.ComponentsArray.OfType<AddClassLevels>().FirstOrDefault()
                ?? (donor.AddFacts ?? new BlueprintUnitFact[0]).SelectMany(f => f.ComponentsArray ?? new BlueprintComponent[0]).OfType<AddClassLevels>().FirstOrDefault();
            outsiderClass = donorLevels?.CharacterClass ?? all.OfType<BlueprintCharacterClass>().First(c => c.name == "OutsiderClass");
            int bab = outsiderClass.BaseAttackBonus.GetBonus(HitDice);
            int fort = outsiderClass.FortitudeSave.GetBonus(HitDice);
            int reflex = outsiderClass.ReflexSave.GetBonus(HitDice);
            int will = outsiderClass.WillSave.GetBonus(HitDice);
            int hitDie = (int)outsiderClass.HitDie;
            // A non-player class adds half its die plus one on odd levels (ApplyClassMechanics); Constitution adds +8 a level.
            int dice = Enumerable.Range(1, HitDice).Sum(level => hitDie / 2 + level % 2);
            int hitPoints = KurshuProfile.Undrained("Hit points") - dice - 8 * HitDice;
            s_MaxHP = Math.Max(0, hitPoints);
            Log.Info($"Kurshu calibration: class {outsiderClass.name} d{hitDie} at {HitDice}: BAB {bab}, saves {fort}/{reflex}/{will}, dice {dice}; MaxHP {s_MaxHP} plus {hitPoints - s_MaxHP}.");
            var list = new List<BlueprintComponent>
            {
                Stat(StatType.BaseAttackBonus, 17 - bab, ModifierDescriptor.UntypedStackable),
                // Printed Fortitude, Reflex and Will before negative levels, less Con +8, Dex +6 and Wis +5.
                Stat(StatType.SaveFortitude, KurshuProfile.Undrained("Fortitude") - 8 - fort, ModifierDescriptor.UntypedStackable),
                Stat(StatType.SaveReflex, KurshuProfile.Undrained("Reflex") - 6 - reflex, ModifierDescriptor.UntypedStackable),
                Stat(StatType.SaveWill, KurshuProfile.Undrained("Will") - 5 - will, ModifierDescriptor.UntypedStackable),
                Stat(StatType.AC, KurshuProfile.Authored("Natural armor"), ModifierDescriptor.NaturalArmor),
                Stat(StatType.AC, 2, ModifierDescriptor.Deflection),
                // Improved Initiative's +4 on Dex +6: printed +10.
                Stat(StatType.Initiative, 4, ModifierDescriptor.UntypedStackable),
                // Ranks and class bonus on Wisdom +5 make printed Perception; Stealth's printed total already carries the Large
                // -4, which the engine applies itself, so it is added back here once.
                Stat(StatType.SkillPerception, KurshuProfile.Undrained("Perception") - 5, ModifierDescriptor.UntypedStackable),
                Stat(StatType.SkillStealth, KurshuProfile.Undrained("Stealth") - 6 + 4, ModifierDescriptor.UntypedStackable),
                // CMB and CMD need nothing: the engine's Large maneuver bonus is +2 and it also applies the -1 size modifier
                // (to attack, so CMB, and to touch AC, so CMD), which nets the printed +1. See docs/WR-05-KURSHU.md.
            };
            if (hitPoints - s_MaxHP != 0)
            {
                list.Add(Stat(StatType.HitPoints, hitPoints - s_MaxHP, ModifierDescriptor.UntypedStackable));
            }
            return list.Where(c => !(c is AddStatBonus s) || s.Value != 0).ToArray();
        }

        private static BlueprintComponent[] NatureComponents(Dictionary<string, BlueprintFeature> features)
        {
            var dr = Component<AddDamageResistancePhysical>("KWR_KurshuDR");
            dr.Value = Simple(KurshuProfile.Authored("Damage reduction (cold iron and magic)"));
            // Both together: only a weapon that is cold iron and magic gets through (Or false requires every criterion).
            dr.Or = false;
            dr.BypassedByMaterial = true;
            dr.Material = PhysicalDamageMaterial.ColdIron;
            dr.BypassedByMagic = true;
            dr.MinEnhancementBonus = 1;
            var list = new List<BlueprintComponent> { dr };
            foreach (DamageEnergyType energy in new[] { DamageEnergyType.Cold, DamageEnergyType.Electricity, DamageEnergyType.Fire, DamageEnergyType.Sonic })
            {
                var resist = Component<AddDamageResistanceEnergy>("KWR_KurshuResist" + energy);
                resist.Type = energy;
                resist.Value = Simple(KurshuProfile.Authored("Resist cold, electricity, fire, sonic"));
                list.Add(resist);
            }
            var sr = Component<AddSpellResistance>("KWR_KurshuSR");
            sr.Value = Simple(KurshuProfile.Authored("Spell resistance"));
            list.Add(sr);
            var trip = Component<ManeuverImmunity>("KWR_KurshuNoTrip");
            trip.Type = CombatManeuver.Trip;
            list.Add(trip);
            // Flight in a game without flight: nothing on the ground trips or slows her. She still walks the navigation mesh.
            list.Add(Immunity(UnitCondition.Prone));
            list.Add(Immunity(UnitCondition.DifficultTerrain));
            var grab = Component<ManeuverBonus>("KWR_KurshuGrab");
            grab.Type = CombatManeuver.Grapple;
            grab.Bonus = 4;
            list.Add(grab);
            list.Add(Component<KwrKurshuCasting>("KWR_KurshuCasting"));
            list.Add(Component<KwrIntelligenceDrain>("KWR_KurshuIntelligenceDrain"));
            list.Add(Component<KwrTailGrab>("KWR_KurshuTailGrab"));
            list.Add(Component<KwrKurshuObserver>("KWR_KurshuObserver"));
            return list.ToArray();
        }

        private static BlueprintComponent[] DifficultyComponents(BlueprintUnit donor)
        {
            return donor.ComponentsArray.OfType<DifficultyStatAdvancement>()
                .Concat((donor.AddFacts ?? new BlueprintUnitFact[0]).SelectMany(f => f?.ComponentsArray ?? new BlueprintComponent[0]).OfType<DifficultyStatAdvancement>())
                .Cast<BlueprintComponent>().ToArray();
        }

        /// <summary>Native regeneration: while it runs she falls unconscious instead of dying; acid stops it for a round.</summary>
        private static Kingmaker.UnitLogic.Buffs.Components.AddEffectRegeneration Regenerate()
        {
            var regeneration = Component<Kingmaker.UnitLogic.Buffs.Components.AddEffectRegeneration>("KWR_KurshuRegenerationLogic");
            regeneration.Heal = KurshuProfile.Regeneration;
            regeneration.CancelDamageEnergyTypes = new[] { DamageEnergyType.Acid };
            return regeneration;
        }

        private static BlueprintAbility SpellLike(BlueprintAbility spell, SlaSpec spec)
        {
            var ability = UnityEngine.Object.Instantiate(spell);
            ability.name = "KWR_Kurshu" + spec.Spell + (spec.LimitedWish ? "Wish" : string.Empty);
            ability.AssetGuid = ScenarioIds.Derive("ability." + ability.name);
            ability.Type = AbilityType.SpellLike;
            ability.MaterialComponent = new BlueprintAbility.MaterialComponentData();
            if (ability.CanTargetPoint && !ability.CanTargetEnemies && ability.Range != AbilityRange.Personal && spec.Aim.HasValue)
            {
                // A point-only area has no enemy list for the native brain, which then aims it at herself: it is centred on
                // a foe's position instead, which the point targeting would also allow.
                ability.CanTargetEnemies = true;
            }
            if (spec.Display != null)
            {
                string key = "kwr.ability." + ability.name;
                LocalizedText.Define(key, spec.Display);
                AccessTools.Field(typeof(BlueprintUnitFact), "m_DisplayName").SetValue(ability, LocalizedText.Create(key));
            }
            var logic = Component<AbilityResourceLogic>("KWR_KurshuUse_" + ability.name);
            logic.RequiredResource = Resources[spec.Use];
            logic.IsSpendResource = true;
            logic.Amount = 1;
            ability.ComponentsArray = (ability.ComponentsArray ?? new BlueprintComponent[0]).Where(c => !(c is AbilityResourceLogic)).Concat(new BlueprintComponent[] { logic }).ToArray();
            s_Register(ability);
            s_SpellLike.Add(ability);
            UseKeys[ability] = spec.Use;
            if (spec.LimitedWish)
            {
                s_LimitedWish.Add(ability);
            }
            return ability;
        }

        private static BlueprintAbility OwnAbility(string name, string display, string description, string use, Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType action, AbilityRange range, params BlueprintComponent[] components)
        {
            var ability = Create<BlueprintAbility>(name);
            string key = "kwr.ability." + name;
            LocalizedText.Define(key, display);
            LocalizedText.Define(key + ".description", description);
            AccessTools.Field(typeof(BlueprintUnitFact), "m_DisplayName").SetValue(ability, LocalizedText.Create(key));
            AccessTools.Field(typeof(BlueprintUnitFact), "m_Description").SetValue(ability, LocalizedText.Create(key + ".description"));
            ability.Type = AbilityType.Supernatural;
            ability.ActionType = action;
            ability.Range = range;
            ability.CanTargetSelf = range == AbilityRange.Personal;
            ability.LocalizedDuration = new Kingmaker.Localization.LocalizedString();
            ability.LocalizedSavingThrow = new Kingmaker.Localization.LocalizedString();
            ability.ComponentsArray = components;
            if (use != null)
            {
                var logic = Component<AbilityResourceLogic>("KWR_KurshuUse_" + name);
                logic.RequiredResource = Resources[use];
                logic.IsSpendResource = true;
                logic.Amount = 1;
                ability.ComponentsArray = components.Concat(new BlueprintComponent[] { logic }).ToArray();
                UseKeys[ability] = use;
            }
            s_Register(ability);
            return ability;
        }

        private static AbilityEffectRunAction Run<T>() where T : GameAction
        {
            var run = Component<AbilityEffectRunAction>("KWR_KurshuRun_" + typeof(T).Name);
            run.Actions = new ActionList { Actions = new GameAction[] { ScriptableObject.CreateInstance<T>() } };
            return run;
        }

        private static BlueprintAbilityResource Resource(string key, int perDay)
        {
            var resource = Create<BlueprintAbilityResource>("KWR_KurshuResource_" + key);
            var field = AccessTools.Field(typeof(BlueprintAbilityResource), "m_MaxAmount");
            object amount = field.GetValue(resource);
            AccessTools.Field(amount.GetType(), "BaseValue").SetValue(amount, perDay);
            // A blueprint made in code has null class lists, which the native maximum walks: every list starts empty.
            foreach (var list in amount.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Where(f => f.FieldType.IsArray))
            {
                list.SetValue(amount, Array.CreateInstance(list.FieldType.GetElementType(), 0));
            }
            field.SetValue(resource, amount);
            s_Register(resource);
            return resource;
        }

        private static AddAbilityResources GrantResource(BlueprintAbilityResource resource)
        {
            var grant = Component<AddAbilityResources>("KWR_KurshuGrant_" + resource.name);
            grant.Resource = resource;
            grant.RestoreAmount = true;
            return grant;
        }

        private static AddFacts AddFactsOf(IEnumerable<BlueprintUnitFact> facts)
        {
            var add = Component<AddFacts>("KWR_KurshuSpellLikeFacts");
            add.Facts = facts.ToArray();
            return add;
        }

        private static BlueprintAiCastSpell CastAction(BlueprintAbility ability, KurshuAim aim, float score)
        {
            var action = Create<BlueprintAiCastSpell>("KWR_KurshuAi_" + ability.name);
            action.Ability = ability;
            action.Locators = new EntityReference[0];
            action.BaseScore = score;
            action.ActorConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[] { Consider(aim, false) };
            action.TargetConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[] { Consider(aim, true) };
            s_Register(action);
            return action;
        }

        /// <summary>Her slams and tail, through the game's own (internal) attack action.</summary>
        private static BlueprintAiAction AttackAction()
        {
            Type type = AccessTools.TypeByName("Kingmaker.Controllers.Brain.Blueprints.BlueprintAiAttack");
            var action = (BlueprintAiAction)ScriptableObject.CreateInstance(type);
            action.name = "KWR_KurshuAi_Melee";
            action.AssetGuid = ScenarioIds.Derive("kurshu.KWR_KurshuAi_Melee");
            action.BaseScore = 5.5f;
            action.ActorConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[] { Consider(KurshuAim.Melee, false) };
            action.TargetConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[] { Consider(KurshuAim.Melee, true) };
            s_Register(action);
            return action;
        }

        private static KwrKurshuConsideration Consider(KurshuAim aim, bool target)
        {
            var consideration = Create<KwrKurshuConsideration>("KWR_KurshuConsider_" + aim + (target ? "_Target" : "_Actor"));
            consideration.Aim = aim;
            consideration.OnTarget = target;
            s_Register(consideration);
            return consideration;
        }

        private static BlueprintFeature Feature(string name, BlueprintComponent[] components)
        {
            var feature = Create<BlueprintFeature>(name);
            feature.HideInUI = true;
            feature.Groups = new FeatureGroup[0];
            feature.ComponentsArray = components;
            s_Register(feature);
            return feature;
        }

        private static BlueprintBuff Buff(BlueprintBuff donor, string name, string title, string description, bool harmful, params BlueprintComponent[] components)
        {
            var buff = UnityEngine.Object.Instantiate(donor);
            buff.name = name;
            buff.AssetGuid = ScenarioIds.Derive("kurshu." + name);
            string key = "kwr.buff." + name;
            LocalizedText.Define(key, title);
            LocalizedText.Define(key + ".description", description);
            AccessTools.Field(typeof(BlueprintUnitFact), "m_DisplayName").SetValue(buff, LocalizedText.Create(key));
            AccessTools.Field(typeof(BlueprintUnitFact), "m_Description").SetValue(buff, LocalizedText.Create(key + ".description"));
            buff.Stacking = StackingType.Replace;
            var field = AccessTools.Field(typeof(BlueprintBuff), "m_Flags");
            // Not from a spell, so no dispel can strip her nature; Harmful (0x40) only for the hold on a party member.
            field.SetValue(buff, Enum.ToObject(field.FieldType, harmful ? 0x40 : 0));
            buff.ComponentsArray = components;
            s_Register(buff);
            return buff;
        }

        private static AddStatBonus Stat(StatType stat, int value, ModifierDescriptor descriptor)
        {
            var bonus = Component<AddStatBonus>("KWR_Kurshu" + stat + descriptor);
            bonus.Stat = stat;
            bonus.Value = value;
            bonus.Descriptor = descriptor;
            return bonus;
        }

        private static AddConditionImmunity Immunity(UnitCondition condition)
        {
            var immunity = Component<AddConditionImmunity>("KWR_KurshuImmune" + condition);
            immunity.Condition = condition;
            return immunity;
        }

        private static AddCondition Condition(UnitCondition condition)
        {
            var add = Component<AddCondition>("KWR_KurshuCondition" + condition);
            add.Condition = condition;
            return add;
        }

        /// <summary>The buff's descriptor, which native immunities (freedom of movement's among them) check as it applies.</summary>
        private static SpellDescriptorComponent Descriptor(SpellDescriptor descriptor)
        {
            var component = Component<SpellDescriptorComponent>("KWR_KurshuDescriptor" + descriptor);
            component.Descriptor = descriptor;
            return component;
        }

        private static ContextValue Simple(int value)
        {
            return new ContextValue { ValueType = ContextValueType.Simple, Value = value };
        }

        private static T Create<T>(string name) where T : BlueprintScriptableObject
        {
            var blueprint = ScriptableObject.CreateInstance<T>();
            blueprint.name = name;
            blueprint.AssetGuid = ScenarioIds.Derive("kurshu." + name);
            return blueprint;
        }

        private static T Component<T>(string name) where T : BlueprintComponent
        {
            var component = ScriptableObject.CreateInstance<T>();
            component.name = name;
            return component;
        }
    }
}
