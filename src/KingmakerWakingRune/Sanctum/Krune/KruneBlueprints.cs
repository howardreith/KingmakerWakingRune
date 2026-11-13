using System;
using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Brain.Blueprints;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using KingmakerWakingRune.Blueprints;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Runelord Krune's native actor (page 16-17): a human conjurer 17 built on the game's own wizard class, with the
    /// printed gear, inherent and runic ability bonuses as typed native facts so nothing is counted twice. Donors are
    /// cloned and never altered. His spells, artifacts' powers and tactics belong to the combat increment.
    /// </summary>
    internal static class KruneBlueprints
    {
        /// <summary>Bartholomew Delgado's body: a robed human male caster with his own model and no random customization.</summary>
        private const string Donor = "d3061fa21b8c19847bc58b21831a68ae";
        private const string WizardClassGuid = "ba34257984f4c41408ce1dc2004e342e";
        private const string StaggeredGuid = "df3950af5a783bd4d91ab73eb8fa0fd3";
        private const string MageArmorBuffGuid = "a92acdf18049d784eaa8f2004f5d2304";
        private const string LongspearPlus3 = "10c01ea0cd99a6f40aab675c9cc1f51a";
        private const string BeltOfPerfection4 = "7cda6252ed289b74fb667b4e9f69fcbd";
        private const string AmuletDonor = "209960f8d60e23a4c8e75d14e2261263";
        private const string RingDonor = "f333bf86cd122974792162cbcd27c9ed";
        private const string WandOfAcidArrowDonor = "81cf03ed91bb40d448bdd339495622a8";
        private const string RodDonor = "81d504243708f504dbfe3f8f72efdeda";
        public const string DiamondGuid = "6a7cdeb14fc6ef44580cf639c5cdc113";
        public const string DiamondDustGuid = "92752bbbf04dfa1439af186f48aee0e9";
        public const int Diamonds = 15;
        public const int DiamondDust = 20;
        public const int AcidArrowCharges = 23;
        public const int SummonMonsterIIICharges = 12;
        public const int RodChargesPerDay = 10;

        /// <summary>Base scores before the belt (+4 Str/Dex/Con), inherent (+3) and runic (+6) bonuses; Int before the human +2.</summary>
        public static readonly int[] BaseScores = { 14, 14, 16, 20, 11, 15 };

        public static BlueprintUnit Unit { get; private set; }
        public static BlueprintCharacterClass WizardClass { get; private set; }
        public static BlueprintBuff RunesOfPower { get; private set; }
        public static readonly BlueprintBuff[] LifeFastHealing = new BlueprintBuff[4];
        public static BlueprintBuff Calibration { get; private set; }
        public static BlueprintBuff MageArmor { get; private set; }
        public static BlueprintAbility MageArmorSpell { get; private set; }
        public static BlueprintBuff PermanentDarkvision { get; private set; }
        public static BlueprintBuff PermanentSeeInvisibility { get; private set; }
        public static BlueprintBuff OpeningStagger { get; private set; }
        public static BlueprintFeature Nature { get; private set; }
        public static BlueprintAbilityResource DimensionalSteps { get; private set; }
        public static BlueprintAbilityResource RodCharges { get; private set; }
        public static BlueprintItemWeapon Spear { get; private set; }
        public static BlueprintItem Rod { get; private set; }
        public static BlueprintItemEquipmentUsable WandOfAcidArrow { get; private set; }
        public static BlueprintItemEquipmentUsable WandOfSummonMonsterIII { get; private set; }
        public static BlueprintItemEquipmentNeck Periapt { get; private set; }
        public static BlueprintItemEquipmentRing RingOfFreedom { get; private set; }
        public static BlueprintAbility DimensionDoorFx { get; private set; }
        public static string Problem { get; private set; }
        public static readonly List<string> Resolved = new List<string>();
        public static readonly List<string> Missing = new List<string>();

        private static Action<BlueprintScriptableObject> s_Register;
        private static List<BlueprintScriptableObject> s_All;

        public static bool Ready => Unit != null;

        public static void Build(BlueprintRoot root, IEnumerable<BlueprintScriptableObject> all, Action<BlueprintScriptableObject> register)
        {
            s_Register = register;
            try
            {
                s_All = all.ToList();
                BuildAll(root);
                Log.Info($"Krune built: {Unit.name}; resolved [{string.Join(", ", Resolved.ToArray())}]; not in this game [{string.Join(", ", Missing.ToArray())}].");
            }
            catch (Exception ex)
            {
                Problem = ex.Message;
                Unit = null;
                Log.Error("Krune could not be built; an awakening stays pending with this diagnostic.", ex);
            }
            finally
            {
                s_All = null;
            }
        }

        private static void BuildAll(BlueprintRoot root)
        {
            BlueprintBuff staggered = Get<BlueprintBuff>(StaggeredGuid);
            WizardClass = Get<BlueprintCharacterClass>(WizardClassGuid);
            MageArmor = Get<BlueprintBuff>(MageArmorBuffGuid);
            MageArmorSpell = Named<BlueprintAbility>("MageArmor");
            DimensionDoorFx = Optional<BlueprintAbility>("DimensionDoorCasterOnly");

            RunesOfPower = Buff(staggered, "KWR_KruneRunesOfPower", "Runes of Power", "The rune foci of Krune's sanctum infuse his body (Su): three Runes of Life (40 hit points and fast healing 1 each), Alacrity (+4 dodge to AC, +4 insight to initiative), Armor (+6 natural armor), Defense (+5 deflection to AC, +5 resistance on saves) and Control (+3 competence with his spear; protection from his own conjured creatures). A disabled focus keeps half its benefit, rounded down; Control's protection is lost. Supernatural: dispel magic does not end it.", false, false,
                Component<KwrKruneRunesOfPower>("KWR_KruneRunesOfPowerLogic"), Component<KwrKruneSpearCompetence>("KWR_KruneSpearCompetence"), Component<KwrKruneConjuredWard>("KWR_KruneConjuredWard"));
            for (int heal = 1; heal <= 3; heal++)
            {
                var fast = Component<AddEffectFastHealing>("KWR_KruneFastHealing" + heal + "Logic");
                fast.Heal = heal;
                LifeFastHealing[heal] = Buff(staggered, "KWR_KruneLifeFastHealing" + heal, "Runes of Life: Fast Healing " + heal, $"Each lit Rune of Life gives Krune fast healing 1: {heal} lit. Supernatural: dispel magic does not end it.", false, false, fast);
            }
            Calibration = Buff(staggered, "KWR_KruneCalibration", "Printed statistics", "The difference between this game's wizard hit points and Krune's printed hit points without his Runes of Life, computed once when he woke, without the difficulty preset's own bonuses.", true, false, Component<Lashmistress.KwrA4Calibration>("KWR_KruneCalibrationLogic"));
            PermanentDarkvision = Buff(staggered, "KWR_KrunePermanentDarkvision", "Darkvision (permanent)", "Krune made darkvision permanent on himself (60 feet). A spell effect: dispel magic can end it, and it does not return.", false, true, Component<Lashmistress.KwrDarkvisionSense>("KWR_KruneDarkvisionSense"));
            BlueprintBuff seeInvisibility = s_All.OfType<BlueprintBuff>().Where(b => b.name.StartsWith("SeeInvisibility", StringComparison.Ordinal) && b.name.IndexOf("Communal", StringComparison.Ordinal) < 0).OrderBy(b => b.name.Length).FirstOrDefault();
            if (seeInvisibility != null)
            {
                PermanentSeeInvisibility = Buff(seeInvisibility, "KWR_KrunePermanentSeeInvisibility", "See Invisibility (permanent)", "Krune made see invisibility permanent on himself. A spell effect: dispel magic can end it, and it does not return.", false, true, seeInvisibility.ComponentsArray);
                Resolved.Add("see invisibility from " + seeInvisibility.name);
            }
            else
            {
                Missing.Add("see invisibility buff");
            }
            OpeningStagger = Buff(staggered, "KWR_KruneOpeningStagger", "Staggered (early release)", "Torn from stasis before his ritual was complete, Krune is staggered for one round.", false, false, Condition(UnitCondition.Staggered));

            // The game's own conjuration school gives dimensional steps as uses of up to 30 feet, one a wizard level: 17 a day,
            // the printed 510 feet. That native resource is the one the opening spends.
            DimensionalSteps = Named<BlueprintAbilityResource>("ConjurationDimensionalStepsResource");
            RodCharges = Resource("KWR_KruneRodCharges", RodChargesPerDay);
            BuildItems();
            KruneDancing.Build(root, s_All);
            KruneGear.Build(s_All);

            var immunePoison = Component<BuffDescriptorImmunity>("KWR_KruneNoPoison");
            immunePoison.Descriptor = SpellDescriptor.Poison;
            Nature = Feature("KWR_KruneNature", "Runelord of Sloth", "Krune's own rules: his inherent bonuses (+3 Intelligence, Wisdom and Charisma, from wish) and runic ability bonuses (+6 enhancement to the same, Su), the revival's caster-level loss for 24 hours after he wakes, and his rod's 10 daily charges.",
                Stat(StatType.Intelligence, 3, ModifierDescriptor.Inherent), Stat(StatType.Wisdom, 3, ModifierDescriptor.Inherent), Stat(StatType.Charisma, 3, ModifierDescriptor.Inherent),
                Stat(StatType.Intelligence, 6, ModifierDescriptor.Enhancement), Stat(StatType.Wisdom, 6, ModifierDescriptor.Enhancement), Stat(StatType.Charisma, 6, ModifierDescriptor.Enhancement),
                Component<KwrKruneRevivalCasterLevel>("KWR_KruneRevivalCasterLevel"), GrantResource(RodCharges));

            var facts = new List<BlueprintUnitFact> { Nature };
            foreach (string[] feat in new[]
            {
                new[] { "AugmentSummoning" }, new[] { "CombatCasting" }, new[] { "EmpowerSpellFeat" }, new[] { "GreatFortitude" },
                new[] { "GreaterSpellFocusConjuration" }, new[] { "IronWill" }, new[] { "LightningReflexes" }, new[] { "QuickenSpellFeat" },
                new[] { "ScribingScrolls", "ScribeScroll" }, new[] { "SimpleWeaponProficiency" }, new[] { "SpellFocusConjuration" }, new[] { "Toughness" },
            })
            {
                BlueprintFeature found = feat.Select(n => s_All.OfType<BlueprintFeature>().FirstOrDefault(f => f.name == n)).FirstOrDefault(f => f != null);
                if (found != null)
                {
                    facts.Add(found);
                    Resolved.Add(found.name);
                }
                else
                {
                    Missing.Add(feat[0]);
                }
            }
            Missing.Add("Eschew Materials (no native feat)");
            Missing.Add("Inscribe Rune (no native feat)");

            var wizard = Component<AddClassLevels>("KWR_KruneWizard");
            wizard.CharacterClass = WizardClass;
            wizard.Levels = 17;
            wizard.RaceStat = StatType.Intelligence;
            wizard.LevelsStat = StatType.Unknown;
            wizard.Skills = new[] { StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld, StatType.SkillLoreReligion, StatType.SkillPerception, StatType.SkillPersuasion, StatType.SkillUseMagicDevice };
            wizard.Selections = new[] { Select("SpecialistSchoolSelection", "SpecialisationSchoolConjurationProgression"), Select("OppositionSchoolSelection", "OppositionSchoolEvocation", "OppositionSchoolIllusion") }.Where(s => s != null).ToArray();
            wizard.MemorizeSpells = new BlueprintAbility[0];
            wizard.SelectSpells = new BlueprintAbility[0];
            wizard.Archetypes = new BlueprintArchetype[0];

            BlueprintUnit donor = Get<BlueprintUnit>(Donor);
            const string name = "KWR_RunelordKrune";
            LocalizedText.Define("kwr.unit." + name, "Runelord Krune");
            BlueprintUnit unit = LodgeCast.CloneUnit(donor, root, name, ScenarioIds.Derive("unit." + name), "kwr.unit." + name, PortraitAssets.Create("RunelordKrune"));
            var portraitField = AccessTools.Field(typeof(BlueprintUnit), "m_Portrait");
            if (portraitField.GetValue(unit) == portraitField.GetValue(donor))
            {
                // Never another character's face: the native placeholder is better than a wrong portrait.
                portraitField.SetValue(unit, null);
            }
            // He wakes as an onlooker; his hostility is decided by the opening, then his faction becomes the hostile one.
            unit.Faction = root.SystemMechanics.FactionNeutrals;
            unit.Size = Size.Medium;
            unit.Alignment = Alignment.LawfulEvil;
            unit.Gender = Gender.Male;
            unit.Race = Named<BlueprintRace>("HumanRace");
            unit.Strength = BaseScores[0];
            unit.Dexterity = BaseScores[1];
            unit.Constitution = BaseScores[2];
            unit.Intelligence = BaseScores[3];
            unit.Wisdom = BaseScores[4];
            unit.Charisma = BaseScores[5];
            unit.Speed = 30.Feet();
            unit.MaxHP = 0;
            unit.Brain = InterimBrain();
            unit.StartingInventory = new BlueprintItem[0];
            unit.Body = new BlueprintUnit.UnitBody
            {
                PrimaryHand = Spear,
                Belt = Get<BlueprintItemEquipmentBelt>(BeltOfPerfection4),
                Neck = Periapt,
                Ring1 = RingOfFreedom,
                QuickSlots = new BlueprintItemEquipmentUsable[] { WandOfAcidArrow, WandOfSummonMonsterIII, null, null, null },
            };
            // The player's preset scales him as it scales the donor: the donor's own native difficulty components, unaltered.
            BlueprintComponent[] difficulty = donor.ComponentsArray.OfType<DifficultyStatAdvancement>()
                .Concat((donor.AddFacts ?? new BlueprintUnitFact[0]).SelectMany(f => f?.ComponentsArray ?? new BlueprintComponent[0]).OfType<DifficultyStatAdvancement>())
                .Cast<BlueprintComponent>().ToArray();
            unit.ComponentsArray = difficulty.Concat(new BlueprintComponent[] { wizard }).ToArray();
            unit.AddFacts = facts.ToArray();
            s_Register(unit);
            Unit = unit;
            Log.Info($"Krune unit {name}: donor {donor.name} prefab {unit.Prefab?.AssetId}, difficulty components {difficulty.Length}, facts [{string.Join(",", unit.AddFacts.Select(f => f.name).ToArray())}].");
        }

        private static void BuildItems()
        {
            Spear = UnityEngine.Object.Instantiate(Get<BlueprintItemWeapon>(LongspearPlus3));
            Spear.name = "KWR_KruneDragonToothSpear";
            Spear.AssetGuid = ScenarioIds.Derive("krune." + Spear.name);
            ItemName(Spear, "Krune's Dragon-Tooth Spear", "One of the Alara'quin, the seven icons Emperor Xin crafted for the runelords: a +3 longspear of dragon tooth, and Krune's arcane bond (major artifact). Its dancing, ego, telekinesis and other powers are not part of this build.");
            s_Register(Spear);

            // Carried, not usable: a plain item with the metamagic rod's icon. Its metamagic belongs to the combat increment;
            // its 10 daily charges are Krune's own native resource, not a counter on the item.
            BlueprintItem rodDonor = Get<BlueprintItem>(RodDonor);
            var rod = UnityEngine.Object.Instantiate(Get<BlueprintItem>(DiamondGuid));
            rod.name = "KWR_KruneRuneCarvedRod";
            rod.AssetGuid = ScenarioIds.Derive("krune." + rod.name);
            AccessTools.Field(typeof(BlueprintItem), "m_Icon").SetValue(rod, rodDonor.Icon);
            ItemName(rod, "Krune's Rune-Carved Rod", "A two-foot shaft of weathered dark grey stone crawling with runes (minor artifact): a greater metamagic rod of seven feats, up to three on one spell, with 10 charges renewed every 24 hours. Its powers are not part of this build.");
            s_Register(rod);
            Rod = rod;

            WandOfAcidArrow = UnityEngine.Object.Instantiate(Get<BlueprintItemEquipmentUsable>(WandOfAcidArrowDonor));
            WandOfAcidArrow.name = "KWR_KruneWandOfAcidArrow";
            WandOfAcidArrow.AssetGuid = ScenarioIds.Derive("krune." + WandOfAcidArrow.name);
            WandOfAcidArrow.CasterLevel = 9;
            WandOfAcidArrow.Charges = AcidArrowCharges;
            ItemName(WandOfAcidArrow, "Wand of Acid Arrow (CL 9)", null);
            s_Register(WandOfAcidArrow);

            BlueprintItemEquipmentUsable summonWand = s_All.OfType<BlueprintItemEquipmentUsable>().FirstOrDefault(w => w.name == "WandOfSummonMonsterIII");
            if (summonWand == null)
            {
                BlueprintAbility summon = s_All.OfType<BlueprintAbility>().FirstOrDefault(a => a.name == "SummonMonsterIIIBase") ?? s_All.OfType<BlueprintAbility>().FirstOrDefault(a => a.name.StartsWith("SummonMonsterIII", StringComparison.Ordinal));
                BlueprintItemEquipmentUsable wandDonor = s_All.OfType<BlueprintItemEquipmentUsable>().FirstOrDefault(w => w.name == "WandOfSummonMonsterI");
                if (summon != null && wandDonor != null)
                {
                    summonWand = UnityEngine.Object.Instantiate(wandDonor);
                    summonWand.Ability = summon;
                    summonWand.SpellLevel = 3;
                    summonWand.CasterLevel = 5;
                    Resolved.Add("summon monster III wand built on " + summon.name);
                }
            }
            else
            {
                summonWand = UnityEngine.Object.Instantiate(summonWand);
            }
            if (summonWand != null)
            {
                summonWand.name = "KWR_KruneWandOfSummonMonsterIII";
                summonWand.AssetGuid = ScenarioIds.Derive("krune." + summonWand.name);
                summonWand.Charges = SummonMonsterIIICharges;
                ItemName(summonWand, "Wand of Summon Monster III", null);
                s_Register(summonWand);
                WandOfSummonMonsterIII = summonWand;
            }
            else
            {
                Missing.Add("wand of summon monster III");
            }

            var poisonFeature = Feature("KWR_KrunePeriaptPoisonImmunity", "Proof against Poison", "Immune to poison.", ImmuneBuffs(SpellDescriptor.Poison, "KWR_KrunePeriaptNoPoisonBuffs"), ImmuneSpells(SpellDescriptor.Poison, "KWR_KrunePeriaptNoPoisonSpells"));
            Periapt = Equipment(Get<BlueprintItemEquipmentNeck>(AmuletDonor), "KWR_KrunePeriaptOfProofAgainstPoison", "Periapt of Proof against Poison", "This black-stone periapt grants its wearer immunity to poison.", Enchantment("KWR_KrunePeriaptEnchantment", poisonFeature));
            // The native permanent freedom of movement (its condition, maneuver and descriptor immunities), worn through the ring:
            // the native ring enchantment only stops holds and paralysis, which would leave him to his own black tentacles.
            BlueprintBuff freedom = s_All.OfType<BlueprintBuff>().FirstOrDefault(b => b.name == "FreedomOfMovementBuffPermanent");
            if (freedom == null)
            {
                throw new InvalidOperationException("native buff FreedomOfMovementBuffPermanent not found");
            }
            BlueprintComponent[] freedomParts = freedom.ComponentsArray.Select(c => { BlueprintComponent copy = UnityEngine.Object.Instantiate(c); copy.name = "KWR_KruneFreedom_" + c.GetType().Name; return copy; }).ToArray();
            var freedomFeature = Feature("KWR_KruneFreedomOfMovement", "Freedom of Movement", "Moves and acts normally despite magic that impedes movement.", freedomParts);
            RingOfFreedom = Equipment(Get<BlueprintItemEquipmentRing>(RingDonor), "KWR_KruneRingOfFreedomOfMovement", "Ring of Freedom of Movement", "This ring lets its wearer move and act normally, as freedom of movement, at all times.", Enchantment("KWR_KruneRingOfFreedomEnchantment", freedomFeature));
            Resolved.Add("freedom of movement from " + freedom.name);
        }

        internal static T Equipment<T>(T donor, string name, string display, string description, BlueprintEquipmentEnchantment enchantment) where T : BlueprintItemEquipmentSimple
        {
            T item = UnityEngine.Object.Instantiate(donor);
            item.name = name;
            item.AssetGuid = ScenarioIds.Derive("krune." + name);
            AccessTools.Field(typeof(BlueprintItemEquipmentSimple), "m_Enchantments").SetValue(item, new[] { enchantment });
            ForgetEnchantmentCache(item);
            ItemName(item, display, description);
            s_Register(item);
            return item;
        }

        /// <summary>A clone keeps its donor's collected enchantment list unless told to collect its own.</summary>
        private static void ForgetEnchantmentCache(BlueprintItem item)
        {
            AccessTools.Field(typeof(BlueprintItem), "m_EnchantmentsCollected").SetValue(item, false);
            AccessTools.Field(typeof(BlueprintItem), "m_CachedEnchantments").SetValue(item, null);
        }

        internal static BlueprintEquipmentEnchantment Enchantment(string name, BlueprintFeature feature)
        {
            var enchantment = Create<BlueprintEquipmentEnchantment>(name);
            var add = Component<AddUnitFeatureEquipment>(name + "Logic");
            add.Feature = feature;
            enchantment.ComponentsArray = new BlueprintComponent[] { add };
            foreach (string field in new[] { "m_EnchantName", "m_Description", "m_Prefix", "m_Suffix" })
            {
                string key = "kwr.krune.enchantment." + name + "." + field;
                LocalizedText.Define(key, field == "m_EnchantName" ? feature.Name : string.Empty);
                AccessTools.Field(typeof(BlueprintItemEnchantment), field).SetValue(enchantment, LocalizedText.Create(key));
            }
            s_Register(enchantment);
            return enchantment;
        }

        private static BuffDescriptorImmunity ImmuneBuffs(SpellDescriptor descriptor, string name)
        {
            var immune = Component<BuffDescriptorImmunity>(name);
            immune.Descriptor = descriptor;
            return immune;
        }

        private static SpellImmunityToSpellDescriptor ImmuneSpells(SpellDescriptor descriptor, string name)
        {
            var immune = Component<SpellImmunityToSpellDescriptor>(name);
            immune.Descriptor = descriptor;
            return immune;
        }

        internal static void ItemName(BlueprintItem item, string display, string description)
        {
            string key = "kwr.krune.item." + item.name;
            LocalizedText.Define(key, display);
            AccessTools.Field(typeof(BlueprintItem), "m_DisplayNameText").SetValue(item, LocalizedText.Create(key));
            if (description != null)
            {
                LocalizedText.Define(key + ".description", description);
                AccessTools.Field(typeof(BlueprintItem), "m_DescriptionText").SetValue(item, LocalizedText.Create(key + ".description"));
            }
        }

        /// <summary>
        /// The interim brain: a native melee attack only. His tactics are the combat increment's; this keeps a released
        /// diagnostic run native rather than inert, and is never what a player meets (the checkpoint holds before it).
        /// </summary>
        private static BlueprintBrain InterimBrain()
        {
            Type type = AccessTools.TypeByName("Kingmaker.Controllers.Brain.Blueprints.BlueprintAiAttack");
            var attack = (BlueprintAiAction)ScriptableObject.CreateInstance(type);
            attack.name = "KWR_KruneAi_Melee";
            attack.AssetGuid = ScenarioIds.Derive("krune." + attack.name);
            attack.BaseScore = 1f;
            attack.ActorConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[0];
            attack.TargetConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[0];
            s_Register(attack);
            // The director (WR-FINAL) chooses his spells, items and movement; the native attack is what is left when it has
            // nothing to cast (out of slots, or a foe at his side and nothing worth a spell).
            var director = Create<KwrKruneDirectorAction>("KWR_KruneAi_Director");
            director.BaseScore = 20f;
            director.ActorConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[0];
            director.TargetConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[0];
            var brain = Create<BlueprintBrain>("KWR_KruneBrain");
            brain.Actions = new BlueprintAiAction[] { director, attack };
            s_Register(brain);
            return brain;
        }

        internal static BlueprintAbilityResource Resource(string name, int perDay)
        {
            var resource = Create<BlueprintAbilityResource>(name);
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

        internal static AddAbilityResources GrantResource(BlueprintAbilityResource resource)
        {
            var grant = Component<AddAbilityResources>("KWR_KruneGrant_" + resource.name);
            grant.Resource = resource;
            grant.RestoreAmount = true;
            return grant;
        }

        private static SelectionEntry Select(string selection, params string[] features)
        {
            BlueprintFeatureSelection found = s_All.OfType<BlueprintFeatureSelection>().FirstOrDefault(b => b.name == selection);
            if (found == null)
            {
                Missing.Add(selection);
                return null;
            }
            return new SelectionEntry
            {
                Selection = found,
                Features = features.Select(f => s_All.OfType<BlueprintFeature>().FirstOrDefault(b => b.name == f)).Where(f => f != null).ToArray(),
            };
        }

        internal static BlueprintFeature Feature(string name, string display, string description, params BlueprintComponent[] components)
        {
            var feature = Create<BlueprintFeature>(name);
            Name(feature, display, description);
            feature.HideInUI = false;
            feature.Groups = new FeatureGroup[0];
            feature.ComponentsArray = components;
            s_Register(feature);
            return feature;
        }

        /// <summary>Flags as the game reads them: IsFromSpell 1 (what native dispel requires), HiddenInUi 2.</summary>
        internal static BlueprintBuff Buff(BlueprintBuff donor, string name, string title, string description, bool hidden, bool fromSpell, params BlueprintComponent[] components)
        {
            var buff = UnityEngine.Object.Instantiate(donor);
            buff.name = name;
            buff.AssetGuid = ScenarioIds.Derive("krune." + name);
            Name(buff, title, description);
            buff.Stacking = StackingType.Replace;
            var field = AccessTools.Field(typeof(BlueprintBuff), "m_Flags");
            field.SetValue(buff, Enum.ToObject(field.FieldType, (hidden ? 2 : 0) | (fromSpell ? 1 : 0)));
            buff.ComponentsArray = components;
            s_Register(buff);
            return buff;
        }

        internal static void Name(BlueprintUnitFact fact, string display, string description)
        {
            string key = "kwr.krune." + fact.name;
            LocalizedText.Define(key, display);
            LocalizedText.Define(key + ".description", description);
            AccessTools.Field(typeof(BlueprintUnitFact), "m_DisplayName").SetValue(fact, LocalizedText.Create(key));
            AccessTools.Field(typeof(BlueprintUnitFact), "m_Description").SetValue(fact, LocalizedText.Create(key + ".description"));
        }

        private static AddStatBonus Stat(StatType stat, int value, ModifierDescriptor descriptor)
        {
            var bonus = Component<AddStatBonus>("KWR_Krune" + stat + descriptor);
            bonus.Stat = stat;
            bonus.Value = value;
            bonus.Descriptor = descriptor;
            return bonus;
        }

        private static AddCondition Condition(UnitCondition condition)
        {
            var add = Component<AddCondition>("KWR_KruneCondition" + condition);
            add.Condition = condition;
            return add;
        }

        internal static T Get<T>(string guid) where T : BlueprintScriptableObject
        {
            return s_All.OfType<T>().FirstOrDefault(b => b.AssetGuid == guid) ?? throw new InvalidOperationException($"native {typeof(T).Name} {guid} not found");
        }

        internal static T Named<T>(string name) where T : BlueprintScriptableObject
        {
            return s_All.OfType<T>().FirstOrDefault(b => b.name == name) ?? throw new InvalidOperationException($"native {typeof(T).Name} {name} not found");
        }

        internal static T Optional<T>(string name) where T : BlueprintScriptableObject
        {
            T found = s_All.OfType<T>().FirstOrDefault(b => b.name == name);
            (found != null ? Resolved : Missing).Add(name);
            return found;
        }

        internal static void Register(BlueprintScriptableObject blueprint)
        {
            s_Register(blueprint);
        }

        internal static T Create<T>(string name) where T : BlueprintScriptableObject
        {
            var blueprint = ScriptableObject.CreateInstance<T>();
            blueprint.name = name;
            blueprint.AssetGuid = ScenarioIds.Derive("krune." + name);
            return blueprint;
        }

        internal static T Component<T>(string name) where T : BlueprintComponent
        {
            var component = ScriptableObject.CreateInstance<T>();
            component.name = name;
            return component;
        }
    }
}
