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
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Brain.Blueprints;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using KingmakerWakingRune.Blueprints;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Lashmistress
{
    /// <summary>A placeholder entry that holds a printed slot and can never be cast here (deferred, or cast before arrival).</summary>
    public sealed class KwrNeverCastable : BlueprintComponent, IAbilityCasterChecker
    {
        public string Reason;

        public bool CorrectCaster(UnitEntityData caster)
        {
            return false;
        }

        public string GetReason()
        {
            return Reason;
        }
    }

    /// <summary>A spell's buff on its target, for the spell's own duration at the caster level the context carries.</summary>
    public sealed class KwrApplySpellBuff : ContextAction
    {
        public BlueprintBuff Buff;
        public double SecondsPerLevel;

        public override string GetCaption()
        {
            return "Apply " + Buff?.name;
        }

        public override void RunAction()
        {
            UnitEntityData target = Target.Unit;
            if (target == null || Buff == null)
            {
                return;
            }
            target.Descriptor.AddBuff(Buff, Context, TimeSpan.FromSeconds(SecondsPerLevel * Math.Max(1, Context.Params.CasterLevel)));
        }
    }

    /// <summary>The Blackstrike's hide in plain sight: the native forced stealth, as a move action, wherever the light is dim.</summary>
    public sealed class KwrHideInPlainSightAction : ContextAction
    {
        public override string GetCaption()
        {
            return "Hide in plain sight";
        }

        public override void RunAction()
        {
            UnitEntityData unit = Target.Unit;
            if (unit == null)
            {
                return;
            }
            unit.Stealth.ForceEnterStealth = true;
            unit.Stealth.WantEnterStealth = true;
            Kingmaker.Game.Instance.StealthController.TickUnit(unit);
            LashmistressEncounter.Note($"{unit.CharacterName} melts into the shadows (hide in plain sight): in stealth {unit.Descriptor.State.IsInStealth}.");
        }
    }

    /// <summary>
    /// The four A4 defenders and what they need, built once with the other mod blueprints. Donors give a model and their own
    /// native difficulty scaling only; every number comes from <see cref="LashmistressProfile"/> through native classes, gear and
    /// feats. See docs/WR-06-LASHMISTRESS.md.
    /// </summary>
    internal static class LashmistressBlueprints
    {
        /// <summary>SecludedLodge_HighPriestessOfLamashtu02: a human woman in a priestess's robes.</summary>
        private const string VandianaDonor = "cf68a7bc6251d754d8ccd27f4dc59be8";
        /// <summary>MivonGuest 1 and MivonGuest: two different armed human men.</summary>
        private const string AgarikDonor = "8c5aa0d56f611ea46a340456d56a315d";
        private const string JorgasDonor = "3239726cf2095af4db55ca7a68f3213f";
        /// <summary>TempleOfTheElkHydra: the game's nearest serpentine body, drawn at Large. The game has no naga.</summary>
        private const string NagaDonor = "2c8d261997f3229419e7185592fa59fa";
        private const string BuffDonor = "df3950af5a783bd4d91ab73eb8fa0fd3";

        private const string ClericClass = "67819271767a9dd4fbfd4ae700befea0";
        private const string WizardClass = "ba34257984f4c41408ce1dc2004e342e";
        private const string TheurgeClass = "0920ea7e4fd7a404282e3d8b0ac41838";
        private const string FighterClass = "48ac8db94d5de7645906c7d0ad3bcfbd";
        private const string RogueClass = "299aa766dee3cbf4790da4efb8c72484";
        private const string AberrationClass = "e40e01860956b8b4d80059d4437996f5";

        private const string GreatswordPlus1 = "e4925df9c7ab2714793cd5834e0a3039";
        private const string MasterworkShortbow = "7e691f466745f9942b4fc4f9627018ae";
        private const string ChainshirtPlus1 = "91bf657f26eb80f4ba05b0b8440b1e8c";
        private const string LightShieldPlus2 = "2a298726445c2184bbeab9f34ece55eb";
        private const string AmuletNaturalArmor1 = "209960f8d60e23a4c8e75d14e2261263";
        private const string CloakResistance1 = "4aea6773c1da01c42bd382c1b7c384bc";
        private const string CloakResistance2 = "04dff7841c5f499478c91487d9bbdcef";
        private const string RingProtection1 = "f333bf86cd122974792162cbcd27c9ed";
        private const string RingProtection2 = "254755ca73eb52a4d8ab1a663106659b";
        private const string HeadbandIntelligence2 = "22184af355826444087506d7f2f8f0b1";
        private const string FlailPlus1 = "481f2d4c035c6b244a5732037db6c7cf";
        private const string Bite1d4 = "35dfad6517f401145af54111be04d6cf";
        private const string Sting1d4 = "df44800dbe7b4ba43ac6e0e435041ed8";

        public const string PotionCatsGrace = "94b24d55f514cdd45a79d34904fea944";
        public const string PotionBullsStrength = "2983c97b71ba60443b1c749854ef53a1";
        public const string PotionBlur = "5bb722fed5172ed48baa2139246bc355";
        public const string PotionCureModerate = "f991f3051c3b9e64fabc87891077b613";

        public const string MageArmorBuffGuid = "a92acdf18049d784eaa8f2004f5d2304";
        private const string OutsiderTypeGuid = "9054d3988d491d944ac144e27b6bc318";
        private const string ProtectionFromGoodBuffGuid = "b19e788487556aa4397080ef3dbb3619";
        private const string SickenedBuff = "4e42460798665fd4cb9173ffa7ada323";
        private const string DazeBuffGuid = "9934fedff1b14994ea90205d189c8759";

        /// <summary>
        /// The game's own "from a spell" buff flag: native Dispel Magic considers only buffs that carry it. Every spell effect
        /// this encounter builds carries it (before 0.7.22 none did, so none could be dispelled).
        /// </summary>
        private const int SpellEffect = 1;

        /// <summary>The spell-effect buffs, so a buff saved before 0.7.22 is marked at load as a new one is at creation.</summary>
        public static readonly List<BlueprintBuff> SpellEffectBuffs = new List<BlueprintBuff>();

        public static readonly Dictionary<string, BlueprintUnit> Units = new Dictionary<string, BlueprintUnit>();
        public static BlueprintFeature AdvancedCreature { get; private set; }
        public static BlueprintBuff Calibration { get; private set; }
        public static BlueprintBuff PreparedFalseLife { get; private set; }
        public static BlueprintBuff MageArmor { get; private set; }
        public static BlueprintBuff ImprovedGreatFortitude { get; private set; }
        public static BlueprintBuff Sickened { get; private set; }
        public static BlueprintBuff Dazed { get; private set; }
        public static BlueprintBuff CommandHalt { get; private set; }
        public static BlueprintBuff DarknessAura { get; private set; }
        public static BlueprintBuff InDarkness { get; private set; }
        public static BlueprintBuff DarkvisionBuff { get; private set; }
        public static BlueprintBuff NagaSleep { get; private set; }
        public static BlueprintFeature DarkvisionSense { get; private set; }
        public static BlueprintItemWeapon Whip { get; private set; }
        public static BlueprintItemWeapon NagaBite { get; private set; }
        public static BlueprintItemWeapon NagaSting { get; private set; }
        public static BlueprintAbility HideInPlainSight { get; private set; }
        /// <summary>The self-only variant of the game's own Dimension Door, which she casts from its prepared base.</summary>
        public static BlueprintAbility DimensionDoorSelf { get; private set; }
        /// <summary>The game's own outsider type, which Order's Wrath and Unholy Blight read.</summary>
        public static BlueprintFeature OutsiderType { get; private set; }
        public static BlueprintBuff ChillTouchCharge { get; private set; }
        public static BlueprintAbility ChillTouchStrike { get; private set; }
        public static BlueprintBuff SanctuaryBuff { get; private set; }
        public static BlueprintBuff MagicCircleAura { get; private set; }
        public static BlueprintBuff ProtectionFromGoodBuff { get; private set; }
        public static BlueprintAbility ArcaneBond { get; private set; }
        public static BlueprintAbilityResource ArcaneBondResource { get; private set; }
        public static BlueprintFeature Disruptive { get; private set; }
        public static BlueprintWeaponEnchantment WhipLash { get; private set; }
        public static BlueprintCharacterClass ClericClassBlueprint { get; private set; }
        public static BlueprintCharacterClass WizardClassBlueprint { get; private set; }
        public static BlueprintSpellList VandianaClericSpecial { get; private set; }
        public static BlueprintSpellList VandianaWizardSpecial { get; private set; }

        /// <summary>Every printed prepared entry's ability, by its <see cref="PreparedEntry.Source"/> key.</summary>
        public static readonly Dictionary<string, BlueprintAbility> PreparedAbilities = new Dictionary<string, BlueprintAbility>();
        /// <summary>The placeholder each formerly deferred entry had (0.7.0-0.7.10), by source key; see <see cref="LegacyDeferred"/>.</summary>
        public static readonly Dictionary<string, BlueprintAbility> LegacyPlaceholders = new Dictionary<string, BlueprintAbility>();
        private static readonly string[] LegacyDeferred = { "MagicCircleAgainstGood", "Sanctuary", "ChillTouch" };
        public static readonly Dictionary<BlueprintAbility, int> NagaSpellLevel = new Dictionary<BlueprintAbility, int>();
        public static readonly Dictionary<int, BlueprintAbilityResource> NagaSlots = new Dictionary<int, BlueprintAbilityResource>();
        public static readonly Dictionary<string, BlueprintAbility> NagaAbilities = new Dictionary<string, BlueprintAbility>();
        public static string Problem { get; private set; }

        private static Action<BlueprintScriptableObject> s_Register;
        private static List<BlueprintScriptableObject> s_All;

        public static void Build(BlueprintRoot root, IEnumerable<BlueprintScriptableObject> all, Action<BlueprintScriptableObject> register)
        {
            s_Register = register;
            try
            {
                s_All = all.ToList();
                BuildAll(root);
                Log.Info($"A4 built: {string.Join(", ", Units.Select(u => u.Key + "=" + u.Value.name).ToArray())}; {PreparedAbilities.Count} prepared abilities, {NagaAbilities.Count} naga spells.");
            }
            catch (Exception ex)
            {
                Problem = ex.Message;
                Units.Clear();
                Log.Error("The A4 defenders could not be built; A4 stays as it was.", ex);
            }
            finally
            {
                s_All = null;
            }
        }

        private static T Get<T>(string guid) where T : BlueprintScriptableObject
        {
            return s_All.OfType<T>().FirstOrDefault(b => b.AssetGuid == guid) ?? throw new InvalidOperationException($"native {typeof(T).Name} {guid} not found");
        }

        private static T Named<T>(string name) where T : BlueprintScriptableObject
        {
            return s_All.OfType<T>().FirstOrDefault(b => b.name == name) ?? throw new InvalidOperationException($"native {typeof(T).Name} {name} not found");
        }

        private static void BuildAll(BlueprintRoot root)
        {
            BlueprintBuff buffDonor = Get<BlueprintBuff>(BuffDonor);
            ClericClassBlueprint = Get<BlueprintCharacterClass>(ClericClass);
            WizardClassBlueprint = Get<BlueprintCharacterClass>(WizardClass);

            AdvancedCreature = Feature("KWR_A4AdvancedCreature", "Advanced Creature", "The advanced simple template, rebuilt: +4 to every ability score and +2 natural armor; everything that follows from them is recomputed by the game.",
                Stat(StatType.Strength, 4, ModifierDescriptor.UntypedStackable), Stat(StatType.Dexterity, 4, ModifierDescriptor.UntypedStackable), Stat(StatType.Constitution, 4, ModifierDescriptor.UntypedStackable),
                Stat(StatType.Intelligence, 4, ModifierDescriptor.UntypedStackable), Stat(StatType.Wisdom, 4, ModifierDescriptor.UntypedStackable), Stat(StatType.Charisma, 4, ModifierDescriptor.UntypedStackable));
            Calibration = Buff(buffDonor, "KWR_A4Calibration", "Printed statistics", "The differences between this game's class tables and the printed statistics, computed once when the creature appeared, without the difficulty preset's own bonuses.", 0, Component<KwrA4Calibration>("KWR_A4CalibrationLogic"));
            PreparedFalseLife = Buff(buffDonor, "KWR_VandianaFalseLife", "False Life", "Vandiana cast false life before beginning the ritual: 14 temporary hit points (as printed), lost to damage and never refilled.", SpellEffect, Component<KwrPreparedFalseLife>("KWR_VandianaFalseLifeLogic"));
            MageArmor = Get<BlueprintBuff>(MageArmorBuffGuid);
            Sickened = Get<BlueprintBuff>(SickenedBuff);
            Dazed = Get<BlueprintBuff>(DazeBuffGuid);
            ImprovedGreatFortitude = Reroll(buffDonor);

            DarkvisionSense = Feature("KWR_A4Darkvision", "Darkvision", "Sees through Vandiana's magical darkness within 60 feet.", Component<KwrDarkvisionSense>("KWR_A4DarkvisionSense"));
            InDarkness = Buff(buffDonor, "KWR_A4InDarkness", "In Magical Darkness", "Within Vandiana's darkness: attacks against this creature by anyone without darkvision within 60 feet have a 20% miss chance (concealment).", 0x40, Component<KwrInDarkness>("KWR_A4InDarknessLogic"));
            var darknessArea = Create<BlueprintAbilityAreaEffect>("KWR_A4DarknessArea");
            darknessArea.Shape = AreaEffectShape.Cylinder;
            darknessArea.Size = 20.Feet();
            // An area made in code has no effect link; the game loads one when the area appears, and a missing link threw
            // there, so before 0.7.16 the darkness never appeared at all. An empty link has no visual and loads nothing.
            darknessArea.Fx = new PrefabLink();
            darknessArea.AffectEnemies = true;
            darknessArea.AggroEnemies = false;
            var inside = Component<AbilityAreaEffectBuff>("KWR_A4DarknessInside");
            inside.Buff = InDarkness;
            inside.Condition = new ConditionsChecker { Conditions = new Condition[0] };
            darknessArea.ComponentsArray = new BlueprintComponent[] { inside };
            s_Register(darknessArea);
            var aura = Component<AddAreaEffect>("KWR_A4DarknessAura");
            aura.AreaEffect = darknessArea;
            DarknessAura = Buff(buffDonor, "KWR_VandianaDarkness", "Darkness", "Magical darkness radiates 20 feet from an object Vandiana carries: creatures in it have concealment (20% miss chance) from anyone without darkvision. A spell effect: dispel magic ends it.", SpellEffect, aura);
            DarkvisionBuff = Buff(buffDonor, "KWR_VandianaDarkvisionBuff", "Darkvision", "Vandiana sees in darkness to 60 feet: her own darkness does not hide her foes from her.", SpellEffect, Component<KwrDarkvisionSense>("KWR_VandianaDarkvisionSense"));
            CommandHalt = Buff(buffDonor, "KWR_A4CommandHalt", "Commanded: Halt", "Obeying Vandiana's command, this creature stands in place and takes no actions this round.", SpellEffect | 0x40,
                Condition(UnitCondition.CantAct), Condition(UnitCondition.CantMove), Descriptor(SpellDescriptor.MindAffecting | SpellDescriptor.Compulsion));
            NagaSleep = Buff(buffDonor, "KWR_DarkNagaSleep", "Dark Naga Poison: Asleep", "Asleep from the dark naga's sting for 2d4 minutes: helpless. A poison: neutralize poison ends it, and creatures immune to poison or sleep are unaffected. Damage or an ally's wake action rouses the sleeper.", 0x40,
                Condition(UnitCondition.Sleeping), Descriptor(SpellDescriptor.Poison | SpellDescriptor.Sleep), Component<Wards.KwrMistSleepWake>("KWR_DarkNagaSleepWake"));
            OutsiderType = Get<BlueprintFeature>(OutsiderTypeGuid);
            ProtectionFromGoodBuff = Get<BlueprintBuff>(ProtectionFromGoodBuffGuid);
            BuildWardSpells(buffDonor);
            Disruptive = Feature("KWR_BlackstrikeDisruptive", "Disruptive", "Foes this creature threatens find it harder to cast defensively: +4 to the DC of their defensive casting checks.", Component<KwrDisruptive>("KWR_BlackstrikeDisruptiveLogic"));

            BuildWeapons();
            BuildVandianaSpells(buffDonor);
            DimensionDoorSelf = Named<BlueprintAbility>("DimensionDoorCasterOnly");
            BuildNagaSpells();
            HideInPlainSight = OwnAbility("KWR_A4HideInPlainSight", "Hide in Plain Sight", "Within 10 feet of dim light, which fills this sanctum, the shadowdancer can use Stealth even while being observed.", Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move, AbilityRange.Personal, Run<KwrHideInPlainSightAction>());
            HideInPlainSight.Type = AbilityType.Supernatural;

            BuildVandiana(root);
            BuildBlackstrike(root, DefenderKeys.Agarik, "Agarik", AgarikDonor);
            BuildBlackstrike(root, DefenderKeys.Jorgas, "Jorgas", JorgasDonor);
            BuildNaga(root);
        }

        private static void BuildWeapons()
        {
            // A whip: the game has none. A one-handed reach weapon with the whip's printed reach and 1d3, built on the flail
            // +1's item (enhancement +1, so magic) with its own weapon type. Its nonlethal damage and armor rule are the
            // weapon's own enchantment, so they follow the whip to any wielder.
            WhipLash = Create<BlueprintWeaponEnchantment>("KWR_WhipNonlethal");
            foreach (var text in new[] { new { Field = "m_EnchantName", Text = "Whip" }, new { Field = "m_Description", Text = "Deals nonlethal damage, and none to a creature with an armor bonus of +1 or more or a natural armor bonus of +3 or more." }, new { Field = "m_Prefix", Text = string.Empty }, new { Field = "m_Suffix", Text = string.Empty } })
            {
                string key = "kwr.enchantment.whip." + text.Field;
                LocalizedText.Define(key, text.Text);
                AccessTools.Field(typeof(BlueprintItemEnchantment), text.Field).SetValue(WhipLash, LocalizedText.Create(key));
            }
            WhipLash.ComponentsArray = new BlueprintComponent[] { Component<KwrWhipLash>("KWR_WhipNonlethalLogic") };
            s_Register(WhipLash);
            BlueprintItemWeapon flail = Get<BlueprintItemWeapon>(FlailPlus1);
            var type = UnityEngine.Object.Instantiate(flail.Type);
            type.name = "KWR_WhipType";
            type.AssetGuid = ScenarioIds.Derive("a4." + type.name);
            AccessTools.Field(typeof(BlueprintWeaponType), "m_AttackRange").SetValue(type, 15.Feet());
            AccessTools.Field(typeof(BlueprintWeaponType), "m_BaseDamage").SetValue(type, new DiceFormula(1, DiceType.D3));
            Name(type, "kwr.weapontype.whip", "Whip");
            s_Register(type);
            Whip = UnityEngine.Object.Instantiate(flail);
            Whip.name = "KWR_VandianaWhipPlus1";
            Whip.AssetGuid = ScenarioIds.Derive("a4." + Whip.name);
            AccessTools.Field(typeof(BlueprintItemWeapon), "m_Type").SetValue(Whip, type);
            var enchantments = AccessTools.Field(typeof(BlueprintItemWeapon), "m_Enchantments");
            enchantments.SetValue(Whip, ((BlueprintWeaponEnchantment[])enchantments.GetValue(Whip) ?? new BlueprintWeaponEnchantment[0]).Concat(new[] { WhipLash }).ToArray());
            Name(Whip, "kwr.item.whip", "Whip +1");
            s_Register(Whip);

            NagaBite = NaturalWeapon(Get<BlueprintItemWeapon>(Bite1d4), "KWR_DarkNagaBite", new DiceFormula(1, DiceType.D4));
            NagaSting = NaturalWeapon(Get<BlueprintItemWeapon>(Sting1d4), "KWR_DarkNagaSting", new DiceFormula(2, DiceType.D4));
        }

        /// <summary>
        /// The effects behind three of her printed spells: Sanctuary's ward, Magic Circle against Good (the game's own
        /// Protection from Good on her allies within 10 feet of the warded creature) and Chill Touch's held touches.
        /// </summary>
        private static void BuildWardSpells(BlueprintBuff buffDonor)
        {
            SanctuaryBuff = Buff(buffDonor, "KWR_VandianaSanctuary", "Sanctuary", "Any opponent that tries to attack Vandiana directly must succeed on a Will save or lose that attack and be unable to attack her while the ward lasts; each saves once against this casting. Area effects are unaffected. It ends if she attacks or casts a harmful spell at a foe. A spell effect: dispel magic ends it.", SpellEffect,
                Component<KwrSanctuaryWard>("KWR_VandianaSanctuaryWard"), Component<KwrSanctuaryEnds>("KWR_VandianaSanctuaryEnds"));

            var circleArea = Create<BlueprintAbilityAreaEffect>("KWR_A4MagicCircleArea");
            circleArea.Shape = AreaEffectShape.Cylinder;
            circleArea.Size = 10.Feet();
            circleArea.Fx = new PrefabLink();
            circleArea.AffectEnemies = false;
            circleArea.AggroEnemies = false;
            var warded = Component<AbilityAreaEffectBuff>("KWR_A4MagicCircleInside");
            warded.Buff = ProtectionFromGoodBuff;
            warded.Condition = new ConditionsChecker { Conditions = new Condition[0] };
            circleArea.ComponentsArray = new BlueprintComponent[] { warded };
            s_Register(circleArea);
            var emanation = Component<AddAreaEffect>("KWR_A4MagicCircleAura");
            emanation.AreaEffect = circleArea;
            MagicCircleAura = Buff(buffDonor, "KWR_VandianaMagicCircle", "Magic Circle against Good", "A 10-foot emanation from the warded creature: it and its allies inside have the game's own Protection from Good (deflection and resistance bonuses against good creatures) for 10 minutes per level. A spell effect: dispel magic ends it.", SpellEffect, emanation);

            BlueprintAbility touchDonor = Named<BlueprintAbility>("TouchOfFatigueEffect");
            var strike = UnityEngine.Object.Instantiate(touchDonor);
            strike.name = "KWR_VandianaChillTouchStrike";
            strike.AssetGuid = ScenarioIds.Derive("a4." + strike.name);
            strike.Type = AbilityType.Spell;
            strike.SpellResistance = true;
            Name(strike, "kwr.ability." + strike.name, "Chill Touch (touch)", "A melee touch: 1d6 negative energy damage, and 1 Strength damage unless the creature succeeds on a Fortitude save. Spell resistance applies. One touch of those Chill Touch holds.");
            strike.ComponentsArray = (touchDonor.ComponentsArray ?? new BlueprintComponent[0]).Where(c => c is AbilitySpawnFx || c is AbilityDeliverTouch)
                .Concat(new BlueprintComponent[] { RunActions(ScriptableObject.CreateInstance<KwrChillTouchStrikeAction>()), School(SpellSchool.Necromancy) }).ToArray();
            s_Register(strike);
            ChillTouchStrike = strike;
            ChillTouchCharge = Buff(buffDonor, "KWR_VandianaChillTouch", "Chill Touch (held)", "Vandiana holds one touch of Chill Touch per caster level. Casting another spell dissipates the rest.", SpellEffect,
                Component<KwrChillTouchCharges>("KWR_VandianaChillTouchCharges"), AddFactsOf("KWR_VandianaChillTouchStrikeFact", new BlueprintUnitFact[] { strike }));

            ArcaneBondResource = Resource("KWR_VandianaArcaneBondUses", 1);
            var spend = Component<AbilityResourceLogic>("KWR_VandianaArcaneBondSpend");
            spend.RequiredResource = ArcaneBondResource;
            spend.IsSpendResource = true;
            spend.Amount = 1;
            ArcaneBond = OwnAbility("KWR_VandianaArcaneBond", "Arcane Bond (Ring)", "Once per day, through her bonded ring, Vandiana recovers one spent wizard spell (a free action) and casts it with its own action, as a wizard casts a spell from her book through her bond.", Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free, AbilityRange.Personal,
                Run<KwrArcaneBondAction>(), spend);
        }

        /// <summary>A Large creature's printed natural weapon: the dice are fixed at Large, so the engine does not scale them again.</summary>
        private static BlueprintItemWeapon NaturalWeapon(BlueprintItemWeapon donor, string name, DiceFormula dice)
        {
            var weapon = UnityEngine.Object.Instantiate(donor);
            weapon.name = name;
            weapon.AssetGuid = ScenarioIds.Derive("a4." + name);
            AccessTools.Field(typeof(BlueprintItemWeapon), "m_OverrideDamageDice").SetValue(weapon, true);
            AccessTools.Field(typeof(BlueprintItemWeapon), "m_DamageDice").SetValue(weapon, dice);
            AccessTools.Field(typeof(BlueprintItemWeapon), "m_Size").SetValue(weapon, Size.Large);
            s_Register(weapon);
            return weapon;
        }

        private static void BuildVandianaSpells(BlueprintBuff buffDonor)
        {
            var abilities = s_All.OfType<BlueprintAbility>().GroupBy(a => a.name).ToDictionary(g => g.Key, g => g.First());
            foreach (PreparedEntry entry in VandianaSpells.Cleric.Concat(VandianaSpells.Wizard))
            {
                if (PreparedAbilities.ContainsKey(entry.Source))
                {
                    continue;
                }
                BlueprintAbility ability;
                switch (entry.Carry)
                {
                    case SpellCarry.Native:
                        ability = abilities.TryGetValue(entry.Source, out BlueprintAbility native) ? native : throw new InvalidOperationException($"native spell {entry.Source} not found");
                        break;
                    case SpellCarry.Local:
                        ability = LocalSpell(entry, abilities, buffDonor);
                        break;
                    default:
                        // Mage Armor, False Life and Alarm are the game's own spells, prepared and then spent before arrival.
                        ability = entry.Carry == SpellCarry.PreCast && abilities.TryGetValue(entry.Source, out BlueprintAbility own) ? own : Placeholder(entry);
                        break;
                }
                PreparedAbilities[entry.Source] = ability;
            }
            // Entries held as placeholders before 0.7.11 and real spells since: the old placeholders still exist, so a save
            // that prepared them loads, and the load swaps each prepared slot for the spell (see LashmistressEncounter).
            foreach (PreparedEntry entry in VandianaSpells.Cleric.Concat(VandianaSpells.Wizard).Where(e => LegacyDeferred.Contains(e.Source)).GroupBy(e => e.Source).Select(g => g.First()))
            {
                LegacyPlaceholders[entry.Source] = Placeholder(new PreparedEntry(entry.Name, entry.Level, SpellCarry.Deferred, entry.Source, entry.Domain));
            }
            VandianaClericSpecial = SpellList("KWR_VandianaDomainEntries", VandianaSpells.Cleric.Where(e => e.Domain));
            VandianaWizardSpecial = SpellList("KWR_VandianaSchoolEntries", VandianaSpells.Wizard.Where(e => e.Source == "MagesFaithfulHound"));
        }

        private static BlueprintSpellList SpellList(string name, IEnumerable<PreparedEntry> entries)
        {
            var list = Create<BlueprintSpellList>(name);
            list.SpellsByLevel = Enumerable.Range(0, 10).Select(level => new SpellLevelList(level) { Spells = entries.Where(e => e.Level == level).Select(e => PreparedAbilities[e.Source]).Distinct().ToList() }).ToArray();
            s_Register(list);
            return list;
        }

        /// <summary>A printed spell this game lacks with no combat treatment: it holds its slot and is listed as deferred.</summary>
        private static BlueprintAbility Placeholder(PreparedEntry entry)
        {
            bool before = entry.Carry == SpellCarry.PreCast;
            string reason = before ? "Cast before the party arrived." : "Not available in this game (deferred; see docs/WR-06-LASHMISTRESS.md).";
            var check = Component<KwrNeverCastable>("KWR_A4Never_" + entry.Source);
            check.Reason = reason;
            var ability = OwnAbility("KWR_A4Prepared_" + entry.Source, entry.Name, entry.Name + " (prepared). " + reason, Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard, AbilityRange.Personal, check);
            ability.Type = AbilityType.Spell;
            return ability;
        }

        /// <summary>An encounter-local spell, built on a native shell for its look and targeting, acting by its printed rule.</summary>
        private static BlueprintAbility LocalSpell(PreparedEntry entry, Dictionary<string, BlueprintAbility> abilities, BlueprintBuff buffDonor)
        {
            switch (entry.Source)
            {
                case "OrdersWrath":
                    return Burst("KWR_VandianaOrdersWrath", entry.Name, "Divine power smites chaotic creatures in a 30-foot cube (1d8 per two caster levels, at most 5d8; chaotic outsiders 1d6 per level, at most 10d6) and dazes them for a round; neither lawful nor chaotic creatures take half and are not dazed; lawful creatures are unharmed. Will half (and no daze; a quarter for the neutral). Spell resistance applies. Lawful.",
                        abilities["SoundBurst"], AbilityRange.Medium, 15.Feet(), false);
                case "UnholyBlight":
                    return Burst("KWR_VandianaUnholyBlight", entry.Name, "Unholy power harms good creatures in a 20-foot spread (1d8 per two caster levels, at most 5d8; good outsiders 1d6 per level, at most 10d6) and sickens them for 1d4 rounds; neither good nor evil creatures take half and are not sickened; evil creatures are unharmed. Will half (and no sickening; a quarter for the neutral). Spell resistance applies. Evil.",
                        abilities["SoundBurst"], AbilityRange.Medium, 20.Feet(), true);
                case "Command":
                    {
                        var shell = Shell(abilities["HoldPerson"], "KWR_VandianaCommand", entry.Name, "One living creature within close range obeys a one-word command: halt. It stands in place and takes no action for one round. Will negates; mind-affecting, compulsion, language-dependent. Spell resistance applies.");
                        shell.Range = AbilityRange.Close;
                        shell.ComponentsArray = Keep(shell).Concat(new BlueprintComponent[] { Run<KwrCommandAction>(), School(SpellSchool.Enchantment), Descriptor(SpellDescriptor.MindAffecting | SpellDescriptor.Compulsion) }).ToArray();
                        return shell;
                    }
                case "Darkness":
                    {
                        var shell = Shell(abilities["DivineFavor"], "KWR_VandianaDarknessSpell", entry.Name, "An object Vandiana carries radiates magical darkness in a 20-foot radius for 1 minute per level: creatures in it have concealment from anyone without darkvision. In this game the object is on her person.");
                        var apply = ScriptableObject.CreateInstance<KwrApplySpellBuff>();
                        apply.Buff = DarknessAura;
                        apply.SecondsPerLevel = 60d;
                        shell.Range = AbilityRange.Personal;
                        shell.ComponentsArray = Keep(shell).Concat(new BlueprintComponent[] { RunActions(apply), School(SpellSchool.Evocation), Descriptor(SpellDescriptor.None) }).ToArray();
                        return shell;
                    }
                case "MagicCircleAgainstGood":
                    {
                        var shell = Shell(abilities["ProtectionFromGood"], "KWR_VandianaMagicCircleSpell", entry.Name, "The touched creature emanates a 10-foot circle for 10 minutes per level: it and its allies inside have the game's own Protection from Good. Vandiana casts it on herself, among her defenders.");
                        var apply = ScriptableObject.CreateInstance<KwrApplySpellBuff>();
                        apply.Buff = MagicCircleAura;
                        apply.SecondsPerLevel = 600d;
                        shell.Range = AbilityRange.Touch;
                        shell.CanTargetSelf = true;
                        shell.CanTargetFriends = true;
                        shell.CanTargetEnemies = false;
                        shell.ComponentsArray = Keep(shell).Concat(new BlueprintComponent[] { RunActions(apply), School(SpellSchool.Abjuration), Descriptor(SpellDescriptor.Evil) }).ToArray();
                        return shell;
                    }
                case "Sanctuary":
                    {
                        var shell = Shell(abilities["DivineFavor"], "KWR_VandianaSanctuarySpell", entry.Name, "For 1 round per level, any opponent that tries to attack Vandiana directly must succeed on a Will save or be unable to attack her. Area effects are unaffected. It ends if she attacks.");
                        var apply = ScriptableObject.CreateInstance<KwrApplySpellBuff>();
                        apply.Buff = SanctuaryBuff;
                        apply.SecondsPerLevel = 6d;
                        shell.Range = AbilityRange.Personal;
                        shell.ComponentsArray = Keep(shell).Concat(new BlueprintComponent[] { RunActions(apply), School(SpellSchool.Abjuration) }).ToArray();
                        return shell;
                    }
                case "ChillTouch":
                    {
                        var shell = Shell(abilities["DivineFavor"], "KWR_VandianaChillTouchSpell", entry.Name, "Vandiana's hand holds one touch of negative energy per caster level: each deals 1d6 damage and 1 Strength damage unless the creature succeeds on a Fortitude save. Casting another spell dissipates the rest.");
                        var apply = ScriptableObject.CreateInstance<KwrApplySpellBuff>();
                        apply.Buff = ChillTouchCharge;
                        apply.SecondsPerLevel = 600d;
                        shell.Range = AbilityRange.Personal;
                        shell.ComponentsArray = Keep(shell).Concat(new BlueprintComponent[] { RunActions(apply), School(SpellSchool.Necromancy) }).ToArray();
                        return shell;
                    }
                case "Darkvision":
                    {
                        var shell = Shell(abilities["DivineFavor"], "KWR_VandianaDarkvisionSpell", entry.Name, "Vandiana can see in darkness to 60 feet for 1 hour per level: her own magical darkness does not hide her foes.");
                        var apply = ScriptableObject.CreateInstance<KwrApplySpellBuff>();
                        apply.Buff = DarkvisionBuff;
                        apply.SecondsPerLevel = 3600d;
                        shell.Range = AbilityRange.Personal;
                        shell.ComponentsArray = Keep(shell).Concat(new BlueprintComponent[] { RunActions(apply), School(SpellSchool.Transmutation) }).ToArray();
                        return shell;
                    }
                default:
                    throw new InvalidOperationException("no local spell " + entry.Source);
            }
        }

        private static BlueprintAbility Burst(string name, string display, string description, BlueprintAbility donor, AbilityRange range, Feet radius, bool unholy)
        {
            var shell = Shell(donor, name, display, description);
            shell.Range = range;
            shell.SpellResistance = true;
            shell.CanTargetPoint = true;
            shell.CanTargetEnemies = true;
            var around = Component<AbilityTargetsAround>(name + "_Targets");
            AccessTools.Field(typeof(AbilityTargetsAround), "m_Radius").SetValue(around, radius);
            AccessTools.Field(typeof(AbilityTargetsAround), "m_TargetType").SetValue(around, Kingmaker.UnitLogic.Abilities.Components.TargetType.Any);
            AccessTools.Field(typeof(AbilityTargetsAround), "m_Condition").SetValue(around, new ConditionsChecker { Conditions = new Condition[0] });
            var action = ScriptableObject.CreateInstance<KwrAlignedBurstAction>();
            action.Unholy = unholy;
            // Order's Wrath is [lawful], Unholy Blight [evil]: native protections against those descriptors apply.
            shell.ComponentsArray = Keep(shell).Concat(new BlueprintComponent[] { around, RunActions(action), School(SpellSchool.Evocation), Descriptor(unholy ? SpellDescriptor.Evil : SpellDescriptor.Law) }).ToArray();
            return shell;
        }

        /// <summary>A copy of a native spell for its look (icon, animation, effects); its rule components are replaced.</summary>
        private static BlueprintAbility Shell(BlueprintAbility donor, string name, string display, string description)
        {
            var ability = UnityEngine.Object.Instantiate(donor);
            ability.name = name;
            ability.AssetGuid = ScenarioIds.Derive("a4." + name);
            ability.Type = AbilityType.Spell;
            Name(ability, "kwr.ability." + name, display, description);
            s_Register(ability);
            return ability;
        }

        /// <summary>Only presentation survives from a shell: its spawn effects and visual components.</summary>
        private static IEnumerable<BlueprintComponent> Keep(BlueprintAbility shell)
        {
            return (shell.ComponentsArray ?? new BlueprintComponent[0]).Where(c => c is AbilitySpawnFx);
        }

        private static void BuildNagaSpells()
        {
            var abilities = s_All.OfType<BlueprintAbility>().GroupBy(a => a.name).ToDictionary(g => g.Key, g => g.First());
            for (int level = 1; level <= 3; level++)
            {
                NagaSlots[level] = Resource("KWR_DarkNagaSlots" + level, LashmistressProfile.NagaSlots(level, advanced: true));
            }
            foreach (PreparedEntry entry in NagaSpells.Known.Where(e => e.Carry == SpellCarry.Native))
            {
                BlueprintAbility spell = abilities.TryGetValue(entry.Source, out BlueprintAbility native) ? native : throw new InvalidOperationException($"native spell {entry.Source} not found");
                var ability = UnityEngine.Object.Instantiate(spell);
                ability.name = "KWR_DarkNaga" + entry.Source;
                ability.AssetGuid = ScenarioIds.Derive("a4." + ability.name);
                ability.Type = AbilityType.Spell;
                ability.MaterialComponent = new BlueprintAbility.MaterialComponentData();
                var logic = Component<AbilityResourceLogic>("KWR_DarkNagaSlot_" + entry.Source);
                logic.RequiredResource = NagaSlots[entry.Level];
                logic.IsSpendResource = true;
                logic.Amount = 1;
                ability.ComponentsArray = (ability.ComponentsArray ?? new BlueprintComponent[0]).Where(c => !(c is AbilityResourceLogic)).Concat(new BlueprintComponent[] { logic }).ToArray();
                s_Register(ability);
                NagaSpellLevel[ability] = entry.Level;
                NagaAbilities[entry.Source] = ability;
            }
        }

        private static void BuildVandiana(BlueprintRoot root)
        {
            BlueprintUnit donor = Get<BlueprintUnit>(VandianaDonor);
            var human = Named<BlueprintRace>("HumanRace");
            Abilities scores = LashmistressProfile.VandianaBase;
            var cleric = Levels("KWR_VandianaCleric", ClericClassBlueprint, LashmistressProfile.VandianaClericLevels, new[] { StatType.SkillLoreReligion, StatType.SkillPerception, StatType.SkillKnowledgeArcana },
                Select("ChannelEnergySelection", "ChannelNegativeFeature"), Select("DomainsSelection", "KnowledgeDomainProgression"), Select("SecondDomainsSelection", "RuneDomainProgressionSecondary"));
            var wizard = Levels("KWR_VandianaWizard", WizardClassBlueprint, LashmistressProfile.VandianaWizardLevels, new[] { StatType.SkillKnowledgeArcana, StatType.SkillPerception, StatType.SkillUseMagicDevice },
                Select("SpecialistSchoolSelection", "SpecialisationSchoolConjurationProgression"), Select("OppositionSchoolSelection", "OppositionSchoolEvocation", "OppositionSchoolIllusion"));
            var theurge = Levels("KWR_VandianaTheurge", Get<BlueprintCharacterClass>(TheurgeClass), LashmistressProfile.VandianaTheurgeLevels, new[] { StatType.SkillKnowledgeArcana, StatType.SkillPerception },
                Select("MysticTheurgeDivineSpellbookSelection", "MysticTheurgeClericProgression"), Select("MysticTheurgeArcaneSpellbookSelection", "MysticTheurgeWizard"));
            var facts = new List<BlueprintUnitFact>();
            foreach (string feat in new[] { "CombatCasting", "GreatFortitude", "LightningReflexes", "SpellFocusConjuration", "Toughness" })
            {
                facts.Add(Named<BlueprintFeature>(feat));
            }
            facts.Add(Feature("KWR_VandianaNature", "Lashmistress", "Vandiana's own rules: channel energy at her cleric level, summoner's charm (her summoned creatures last half her wizard level longer), her arcane bond's daily use, and her Chill Touch at her arcane caster level.",
                Component<KwrClericChannel>("KWR_VandianaChannelLevel"), Component<KwrSummonersCharm>("KWR_VandianaSummonersCharm"), Component<KwrA4SpellParams>("KWR_VandianaSpellParams"), GrantResource(ArcaneBondResource)));
            facts.Add(ArcaneBond);
            var body = new BlueprintUnit.UnitBody
            {
                PrimaryHand = Whip,
                SecondaryHand = Get<BlueprintItemShield>(LightShieldPlus2),
                Neck = Get<BlueprintItemEquipmentNeck>(AmuletNaturalArmor1),
                Shoulders = Get<BlueprintItemEquipmentShoulders>(CloakResistance2),
                Ring1 = Get<BlueprintItemEquipmentRing>(RingProtection2),
                QuickSlots = new BlueprintItemEquipmentUsable[] { Get<BlueprintItemEquipmentUsable>(PotionCatsGrace), null, null, null, null },
            };
            // This game gives a non-party caster bonus spells from the casting score's base value only, so the printed Int 17
            // (which includes her headband of vast intelligence +2) is her base score, and the headband is carried, not worn:
            // counted once, and still recoverable. The human's flexible +2 is spent on Charisma, built 2 lower for the printed 14.
            BlueprintUnit unit = Unit(root, donor, DefenderKeys.Vandiana, "Vandiana the Lashmistress", new Abilities(scores.Str, scores.Dex, scores.Con, scores.Int + LashmistressProfile.VandianaHeadband, scores.Wis, scores.Cha - 2), human, Size.Medium, Alignment.LawfulEvil, Gender.Female, 30,
                body, new BlueprintComponent[] { cleric, wizard, theurge }, facts, VandianaBrain());
            unit.StartingInventory = new BlueprintItem[] { Get<BlueprintItemEquipmentHead>(HeadbandIntelligence2) };
            Units[DefenderKeys.Vandiana] = unit;
        }

        private static void BuildBlackstrike(BlueprintRoot root, string key, string display, string donorGuid)
        {
            BlueprintUnit donor = Get<BlueprintUnit>(donorGuid);
            var human = Named<BlueprintRace>("HumanRace");
            Abilities scores = LashmistressProfile.BlackstrikeBase;
            var weaponFocus = Named<BlueprintParametrizedFeature>("WeaponFocus");
            var weaponSpecialization = Named<BlueprintParametrizedFeature>("WeaponSpecialization");
            var fighter = Levels("KWR_" + display + "Fighter", Get<BlueprintCharacterClass>(FighterClass), LashmistressProfile.BlackstrikeFighterLevels, new[] { StatType.SkillPerception, StatType.SkillStealth, StatType.SkillMobility, StatType.SkillAthletics },
                Select("FighterFeatSelection", "WeaponFocus", "PowerAttackFeature", "WeaponSpecialization", "Dodge"),
                Parametrized(weaponFocus, WeaponCategory.Greatsword), Parametrized(weaponSpecialization, WeaponCategory.Greatsword),
                Select("BasicFeatSelection", "CombatReflexes", "Toughness", "LightningReflexes", "Mobility"),
                Select("WeaponTrainingSelection", "WeaponTrainingHeavyBlades"));
            var shadow = Levels("KWR_" + display + "Shadowdancer", ShadowdancerHitDice(), LashmistressProfile.BlackstrikeShadowdancerLevels, new[] { StatType.SkillStealth, StatType.SkillPerception, StatType.SkillMobility },
                Select("BasicFeatSelection", "VitalStrikeFeature"));
            var facts = new List<BlueprintUnitFact>
            {
                AdvancedCreature,
                AdvancedNaturalArmor("KWR_" + display + "AdvancedArmor", LashmistressProfile.AdvancedNaturalArmor),
                Named<BlueprintFeature>("Evasion"),
                Named<BlueprintFeature>("UncannyDodge"),
                Named<BlueprintFeature>("HideInPlainSight"),
                DarkvisionSense,
                HideInPlainSight,
                Disruptive,
            };
            var body = new BlueprintUnit.UnitBody
            {
                PrimaryHand = Get<BlueprintItemWeapon>(GreatswordPlus1),
                PrimaryHandAlternative1 = Get<BlueprintItemWeapon>(MasterworkShortbow),
                Armor = Get<BlueprintItemArmor>(ChainshirtPlus1),
                Neck = Get<BlueprintItemEquipmentNeck>(AmuletNaturalArmor1),
                Shoulders = Get<BlueprintItemEquipmentShoulders>(CloakResistance1),
                Ring1 = Get<BlueprintItemEquipmentRing>(RingProtection1),
                QuickSlots = new[] { Get<BlueprintItemEquipmentUsable>(PotionBullsStrength), Get<BlueprintItemEquipmentUsable>(PotionBlur), Get<BlueprintItemEquipmentUsable>(PotionCureModerate), null, null },
            };
            // Printed CN (NPC Codex); the human +2 goes to Charisma, built 2 lower.
            BlueprintUnit unit = Unit(root, donor, key, display, new Abilities(scores.Str, scores.Dex, scores.Con, scores.Int, scores.Wis, scores.Cha - 2), human, Size.Medium, Alignment.ChaoticNeutral, Gender.Male, 30,
                body, new BlueprintComponent[] { fighter, shadow, AmbushBehaviour() }, facts, MeleeBrain(key));
            Units[key] = unit;
        }

        private static BlueprintCharacterClass s_Shadowdancer;

        /// <summary>
        /// The game has no shadowdancer. Its two levels are built as a prestige class with the shadowdancer's own progression
        /// (d8, medium base attack, good Reflex, poor Fortitude and Will) and no features; its level 1-2 features are added as
        /// facts (evasion, uncanny dodge, hide in plain sight). It is never offered to the player.
        /// </summary>
        private static BlueprintCharacterClass ShadowdancerHitDice()
        {
            if (s_Shadowdancer != null)
            {
                return s_Shadowdancer;
            }
            BlueprintCharacterClass rogue = Get<BlueprintCharacterClass>(RogueClass);
            var cls = UnityEngine.Object.Instantiate(rogue);
            cls.name = "KWR_ShadowdancerHitDice";
            cls.AssetGuid = ScenarioIds.Derive("a4." + cls.name);
            cls.PrestigeClass = true;
            cls.FortitudeSave = Named<BlueprintStatProgression>("SavesPrestigeLow");
            cls.ReflexSave = Named<BlueprintStatProgression>("SavesPrestigeHigh");
            cls.WillSave = Named<BlueprintStatProgression>("SavesPrestigeLow");
            cls.Spellbook = null;
            cls.Archetypes = new BlueprintArchetype[0];
            cls.EquipmentEntities = new Kingmaker.Visual.CharacterSystem.KingmakerEquipmentEntity[0];
            cls.MaleEquipmentEntities = new EquipmentEntityLink[0];
            cls.FemaleEquipmentEntities = new EquipmentEntityLink[0];
            cls.StartingItems = new BlueprintItem[0];
            cls.ComponentsArray = new BlueprintComponent[0];
            var progression = Create<BlueprintProgression>("KWR_ShadowdancerHitDiceProgression");
            progression.LevelEntries = new LevelEntry[0];
            progression.Classes = new[] { cls };
            progression.Groups = new FeatureGroup[0];
            s_Register(progression);
            cls.Progression = progression;
            Name(cls, "kwr.class.shadowdancer", "Shadowdancer");
            s_Register(cls);
            s_Shadowdancer = cls;
            return cls;
        }

        private static void BuildNaga(BlueprintRoot root)
        {
            BlueprintUnit donor = Get<BlueprintUnit>(NagaDonor);
            var levels = Levels("KWR_DarkNagaHitDice", Get<BlueprintCharacterClass>(AberrationClass), LashmistressProfile.NagaHitDice, new[] { StatType.SkillPerception, StatType.SkillStealth, StatType.SkillKnowledgeArcana, StatType.SkillPersuasion },
                Select("BasicFeatSelection", "Alertness", "CombatCasting", "Dodge", "LightningReflexes", "Stealthy"));
            var immunePoison = Component<BuffDescriptorImmunity>("KWR_DarkNagaPoisonImmunity");
            immunePoison.Descriptor = SpellDescriptor.Poison;
            var immunePoisonSpells = Component<SpellImmunityToSpellDescriptor>("KWR_DarkNagaPoisonSpellImmunity");
            immunePoisonSpells.Descriptor = SpellDescriptor.Poison;
            var guarded = Component<SavingThrowBonusAgainstDescriptor>("KWR_DarkNagaGuardedThoughts");
            guarded.SpellDescriptor = SpellDescriptor.Charm;
            guarded.ModifierDescriptor = ModifierDescriptor.Racial;
            guarded.Value = 2;
            var trip = Component<ManeuverImmunity>("KWR_DarkNagaNoTrip");
            trip.Type = CombatManeuver.Trip;
            var nature = Feature("KWR_DarkNagaNature", "Dark Naga", "Immune to poison; guarded thoughts (+2 on saves against charm); cannot be tripped; darkvision 60 feet; casts as a 7th-level sorcerer from its own slots; its sting sends the victim to sleep.",
                immunePoison, immunePoisonSpells, guarded, trip, Stat(StatType.AC, LashmistressProfile.NagaNaturalArmor + LashmistressProfile.AdvancedNaturalArmor, ModifierDescriptor.NaturalArmor),
                Component<KwrNagaCasting>("KWR_DarkNagaCasting"), Component<KwrNagaSting>("KWR_DarkNagaStingLogic"));
            var spellFacts = Feature("KWR_DarkNagaSpells", "Dark Naga Spells", "Spells known, cast spontaneously from its own spells per day.",
                new BlueprintComponent[] { AddFactsOf("KWR_DarkNagaSpellFacts", NagaAbilities.Values) }.Concat(NagaSlots.Values.Select(GrantResource)).ToArray());
            var facts = new List<BlueprintUnitFact> { AdvancedCreature, nature, spellFacts, DarkvisionSense };
            var body = new BlueprintUnit.UnitBody
            {
                AdditionalLimbs = new[] { NagaBite, NagaSting },
                AdditionalSecondaryLimbs = new BlueprintItemWeapon[0],
                QuickSlots = new BlueprintItemEquipmentUsable[5],
                DisableHands = true,
            };
            Abilities scores = LashmistressProfile.NagaBase;
            BlueprintUnit unit = Unit(root, donor, DefenderKeys.Naga, "Dark Naga", scores, null, Size.Large, Alignment.LawfulEvil, Gender.Male, 40, body, new BlueprintComponent[] { levels }, facts, NagaBrain());
            Units[DefenderKeys.Naga] = unit;
        }

        private static BlueprintUnit Unit(BlueprintRoot root, BlueprintUnit donor, string key, string display, Abilities scores, BlueprintRace race, Size size, Alignment alignment, Gender gender, int speed, BlueprintUnit.UnitBody body, BlueprintComponent[] levels, List<BlueprintUnitFact> facts, BlueprintBrain brain)
        {
            string name = "KWR_" + key.Replace("-", string.Empty);
            string nameKey = "kwr.unit." + name;
            LocalizedText.Define(nameKey, display);
            BlueprintUnit unit = LodgeCast.CloneUnit(donor, root, name, ScenarioIds.Derive("unit." + name), nameKey, null);
            unit.Faction = root.SystemMechanics.FactionMobs;
            unit.Size = size;
            unit.Alignment = alignment;
            unit.Gender = gender;
            unit.Strength = scores.Str;
            unit.Dexterity = scores.Dex;
            unit.Constitution = scores.Con;
            unit.Intelligence = scores.Int;
            unit.Wisdom = scores.Wis;
            unit.Charisma = scores.Cha;
            unit.Speed = speed.Feet();
            unit.Brain = brain;
            unit.Race = race;
            unit.StartingInventory = new BlueprintItem[0];
            unit.Body = body;
            unit.MaxHP = 0;
            // The player's preset scales them as it scales the donor: the donor's own native difficulty components, unaltered.
            BlueprintComponent[] difficulty = donor.ComponentsArray.OfType<DifficultyStatAdvancement>()
                .Concat((donor.AddFacts ?? new BlueprintUnitFact[0]).SelectMany(f => f?.ComponentsArray ?? new BlueprintComponent[0]).OfType<DifficultyStatAdvancement>())
                .Cast<BlueprintComponent>().ToArray();
            unit.ComponentsArray = difficulty.Concat(levels).ToArray();
            unit.AddFacts = facts.ToArray();
            s_Register(unit);
            Log.Info($"A4 unit {name}: donor {donor.name} prefab {unit.Prefab?.AssetId}, difficulty components {difficulty.Length}, levels [{string.Join(",", levels.OfType<AddClassLevels>().Select(l => l.CharacterClass.name + "x" + l.Levels).ToArray())}], facts [{string.Join(",", unit.AddFacts.Select(f => f.name).ToArray())}].");
            return unit;
        }

        private static AddAmbushBehaviour AmbushBehaviour()
        {
            var ambush = Component<AddAmbushBehaviour>("KWR_BlackstrikeAmbush");
            ambush.JoinCombatDisatnce = 5f;
            return ambush;
        }

        private static BlueprintFeature AdvancedNaturalArmor(string name, int value)
        {
            return Feature(name, "Advanced Creature: Natural Armor", "The advanced template's +2 natural armor.", Stat(StatType.AC, value, ModifierDescriptor.NaturalArmor));
        }

        private static AddClassLevels Levels(string name, BlueprintCharacterClass cls, int levels, StatType[] skills, params SelectionEntry[] selections)
        {
            var add = Component<AddClassLevels>(name);
            add.CharacterClass = cls;
            add.Levels = levels;
            add.RaceStat = StatType.Charisma;
            add.LevelsStat = StatType.Unknown;
            add.Skills = skills;
            add.Selections = selections.Where(s => s != null).ToArray();
            add.MemorizeSpells = new BlueprintAbility[0];
            add.SelectSpells = new BlueprintAbility[0];
            add.Archetypes = new BlueprintArchetype[0];
            return add;
        }

        private static SelectionEntry Select(string selection, params string[] features)
        {
            return new SelectionEntry
            {
                Selection = Named<BlueprintFeatureSelection>(selection),
                Features = features.Select(f => s_All.OfType<BlueprintFeature>().FirstOrDefault(b => b.name == f)).Where(f => f != null).ToArray(),
            };
        }

        private static SelectionEntry Parametrized(BlueprintParametrizedFeature feature, WeaponCategory category)
        {
            return new SelectionEntry { IsParametrizedFeature = true, ParametrizedFeature = feature, ParamWeaponCategory = category, Features = new BlueprintFeature[0] };
        }

        /// <summary>Improved Great Fortitude, printed: once a day a failed Fortitude save is rerolled (the game's own feat is a flat +2).</summary>
        private static BlueprintBuff Reroll(BlueprintBuff buffDonor)
        {
            var reroll = Component<ModifyD20>("KWR_VandianaGreatFortitudeReroll");
            reroll.Rule = RuleType.SavingThrow;
            reroll.RollsAmount = 1;
            reroll.RerollOnlyIfFailed = true;
            reroll.DispellOnRerollFinished = true;
            reroll.Chance = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 };
            reroll.Value = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 };
            reroll.Skill = new StatType[0];
            var saves = AccessTools.Field(typeof(ModifyD20), "m_SavingThrowType");
            // The same flag set Kurshu's reroll uses: 4 is Will, 1 Fortitude.
            saves.SetValue(reroll, Enum.ToObject(saves.FieldType, 1));
            return Buff(buffDonor, "KWR_VandianaImprovedGreatFortitude", "Improved Great Fortitude", "Once per day, Vandiana may reroll a Fortitude save. She uses it on the first she would fail, and the reroll stands.", 0, reroll);
        }

        private static BlueprintBrain VandianaBrain()
        {
            var actions = new List<BlueprintAiAction>();
            void Cast(string source, A4Aim aim, float score, string variant = null)
            {
                BlueprintAbility ability = PreparedAbilities[source];
                actions.Add(CastAction("KWR_VandianaAi_" + source + (variant ?? string.Empty), ability, aim, score, variant == null ? null : s_All.OfType<BlueprintAbility>().First(a => a.name == variant)));
            }
            Cast("CureCriticalWoundsCast", A4Aim.CureSelf, 11f);
            // Her bond (a free action) recovers a spent wizard spell once a worthwhile one is gone.
            actions.Add(CastAction("KWR_VandianaAi_ArcaneBond", ArcaneBond, A4Aim.ArcaneBond, 12f, null));
            // A held Chill Touch is spent on a foe at her side before anything else (another spell would waste it).
            actions.Add(CastAction("KWR_VandianaAi_ChillTouchStrike", ChillTouchStrike, A4Aim.ChillTouchStrike, 10.5f, null));
            Cast("Sanctuary", A4Aim.Sanctuary, 10.2f);
            Cast("MagicCircleAgainstGood", A4Aim.MagicCircle, 6.9f);
            Cast("ChillTouch", A4Aim.ChillTouch, 4.4f);
            // Her potion, if the east door was never heard before the fight: drunk once, as her own action. (The 0.7.10 cast
            // action of the same name is kept registered, unused, for saves that still list it.)
            CastAction("KWR_VandianaAi_CatsGracePotion", Named<BlueprintAbility>("CatsGrace"), A4Aim.GracePotion, 9.2f, null);
            actions.Add(PotionAction("KWR_VandianaAi_CatsGraceDrink", PotionCatsGrace, A4Aim.GracePotion, 9.2f));
            Cast("Haste", A4Aim.Haste, 10f);
            Cast("ConfusionSpell", A4Aim.Confusion, 8.6f);
            Cast("BalefulPolymorph", A4Aim.BalefulPolymorph, 8.4f);
            Cast("Slow", A4Aim.Slow, 7.8f);
            Cast("HoldPerson", A4Aim.HoldPerson, 7.6f);
            Cast("OrdersWrath", A4Aim.OrdersWrath, 7.4f);
            Cast("UnholyBlight", A4Aim.UnholyBlight, 7.3f);
            Cast("Darkness", A4Aim.Darkness, 7.2f);
            Cast("Enervation", A4Aim.Enervation, 7f);
            Cast("SummonMonsterIIIBase", A4Aim.Summon, 6.8f, "SummonMonsterIIISingle");
            Cast("Glitterdust", A4Aim.Glitterdust, 6.6f);
            Cast("DispelMagicTarget", A4Aim.DispelMagic, 6.4f);
            Cast("Blindness", A4Aim.Blindness, 6.2f);
            Cast("Web", A4Aim.Web, 6f);
            Cast("RemoveFear", A4Aim.RemoveFear, 6f);
            Cast("SleetStorm", A4Aim.SleetStorm, 5.2f);
            Cast("SoundBurst", A4Aim.SoundBurst, 4.8f);
            Cast("Command", A4Aim.Command, 4.6f);
            Cast("Grease", A4Aim.Grease, 4f);
            Cast("DivineFavor", A4Aim.DivineFavor, 2f);
            AddNamedCast(actions, "ChannelNegativeEnergy", A4Aim.ChannelNegative, 5.5f);
            AddNamedCast(actions, "ConjurationAcidDartAbility", A4Aim.AcidDart, 3f);
            AddNamedCast(actions, "RuneDomainBaseAbility", A4Aim.BlastRune, 2.5f);
            actions.Add(AttackAction("KWR_VandianaAi_Whip", 1f));
            return Brain("KWR_VandianaBrain", actions);
        }

        private static void AddNamedCast(List<BlueprintAiAction> actions, string ability, A4Aim aim, float score)
        {
            BlueprintAbility native = s_All.OfType<BlueprintAbility>().FirstOrDefault(a => a.name == ability);
            if (native != null)
            {
                actions.Add(CastAction("KWR_A4Ai_" + ability, native, aim, score, null));
            }
            else
            {
                Log.Info($"A4: native ability {ability} not found; her brain does not use it.");
            }
        }

        private static BlueprintBrain NagaBrain()
        {
            var actions = new List<BlueprintAiAction>
            {
                CastAction("KWR_DarkNagaAi_Displacement", NagaAbilities["Displacement"], A4Aim.Displacement, 9f, null),
                CastAction("KWR_DarkNagaAi_Shield", NagaAbilities["MageShield"], A4Aim.Shield, 6.5f, null),
                CastAction("KWR_DarkNagaAi_LightningBolt", NagaAbilities["LightningBolt"], A4Aim.LightningBolt, 7f, null),
                CastAction("KWR_DarkNagaAi_ScorchingRay", NagaAbilities["ScorchingRay"], A4Aim.ScorchingRay, 6f, null),
                CastAction("KWR_DarkNagaAi_RayOfEnfeeblement", NagaAbilities["RayOfEnfeeblement"], A4Aim.RayOfEnfeeblement, 5.6f, null),
                CastAction("KWR_DarkNagaAi_MagicMissile", NagaAbilities["MagicMissile"], A4Aim.MagicMissile, 4f, null),
                AttackAction("KWR_DarkNagaAi_Melee", 5f),
            };
            return Brain("KWR_DarkNagaBrain", actions);
        }

        /// <summary>The Blackstrike: its greatsword, and its own potions drunk as its own actions (NPC Codex 236 tactics).</summary>
        private static BlueprintBrain MeleeBrain(string key)
        {
            string name = "KWR_" + key.Replace("-", string.Empty);
            // The 0.7.10 potion actions stay registered, unused, so a save whose brain still lists them loads; the load then
            // brings the brain up to the blueprint's actions.
            CastAction(name + "Ai_Blur", Named<BlueprintAbility>("Blur"), A4Aim.BlurPotion, 9.5f, null);
            CastAction(name + "Ai_BullsStrength", Named<BlueprintAbility>("BullsStrength"), A4Aim.StrengthPotion, 9f, null);
            CastAction(name + "Ai_CureModerate", Named<BlueprintAbility>("CureModerateWoundsCast"), A4Aim.HealingPotion, 8f, null);
            return Brain(name + "Brain", new List<BlueprintAiAction>
            {
                PotionAction(name + "Ai_BlurPotion", PotionBlur, A4Aim.BlurPotion, 9.5f),
                PotionAction(name + "Ai_BullsStrengthPotion", PotionBullsStrength, A4Aim.StrengthPotion, 9f),
                PotionAction(name + "Ai_CureModeratePotion", PotionCureModerate, A4Aim.HealingPotion, 8f),
                AttackAction(name + "Ai_Melee", 5f),
            });
        }

        private static BlueprintBrain Brain(string name, List<BlueprintAiAction> actions)
        {
            var brain = Create<BlueprintBrain>(name);
            brain.Actions = actions.ToArray();
            s_Register(brain);
            return brain;
        }

        private static BlueprintAiCastSpell CastAction(string name, BlueprintAbility ability, A4Aim aim, float score, BlueprintAbility variant)
        {
            var action = Create<BlueprintAiCastSpell>(name);
            action.Ability = ability;
            action.Variant = variant;
            action.Locators = new EntityReference[0];
            action.BaseScore = score;
            action.ActorConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[] { Consider(aim, false) };
            action.TargetConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[] { Consider(aim, true) };
            s_Register(action);
            return action;
        }

        /// <summary>The creature drinks one of its own potions when its tactics call for it (see <see cref="KwrAiDrinkPotion"/>).</summary>
        private static BlueprintAiAction PotionAction(string name, string potionGuid, A4Aim aim, float score)
        {
            var action = Create<KwrAiDrinkPotion>(name);
            action.PotionGuid = potionGuid;
            action.BaseScore = score;
            action.ActorConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[] { Consider(aim, false) };
            action.TargetConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[] { Consider(aim, true) };
            s_Register(action);
            return action;
        }

        private static BlueprintAiAction AttackAction(string name, float score)
        {
            Type type = AccessTools.TypeByName("Kingmaker.Controllers.Brain.Blueprints.BlueprintAiAttack");
            var action = (BlueprintAiAction)ScriptableObject.CreateInstance(type);
            action.name = name;
            action.AssetGuid = ScenarioIds.Derive("a4." + name);
            action.BaseScore = score;
            action.ActorConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[] { Consider(A4Aim.Melee, false) };
            action.TargetConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[] { Consider(A4Aim.Melee, true) };
            s_Register(action);
            return action;
        }

        private static readonly Dictionary<string, KwrLashmistressConsideration> s_Considerations = new Dictionary<string, KwrLashmistressConsideration>();

        private static KwrLashmistressConsideration Consider(A4Aim aim, bool target)
        {
            string name = "KWR_A4Consider_" + aim + (target ? "_Target" : "_Actor");
            if (s_Considerations.TryGetValue(name, out KwrLashmistressConsideration existing))
            {
                return existing;
            }
            var consideration = Create<KwrLashmistressConsideration>(name);
            consideration.Aim = aim;
            consideration.OnTarget = target;
            s_Register(consideration);
            s_Considerations[name] = consideration;
            return consideration;
        }

        private static BlueprintAbility OwnAbility(string name, string display, string description, Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType action, AbilityRange range, params BlueprintComponent[] components)
        {
            var ability = Create<BlueprintAbility>(name);
            Name(ability, "kwr.ability." + name, display, description);
            ability.Type = AbilityType.Supernatural;
            ability.ActionType = action;
            ability.Range = range;
            ability.CanTargetSelf = range == AbilityRange.Personal;
            ability.LocalizedDuration = new Kingmaker.Localization.LocalizedString();
            ability.LocalizedSavingThrow = new Kingmaker.Localization.LocalizedString();
            ability.ComponentsArray = components;
            s_Register(ability);
            return ability;
        }

        private static AbilityEffectRunAction Run<T>() where T : GameAction
        {
            return RunActions(ScriptableObject.CreateInstance<T>());
        }

        private static AbilityEffectRunAction RunActions(params GameAction[] actions)
        {
            var run = Component<AbilityEffectRunAction>("KWR_A4Run_" + actions[0].GetType().Name);
            run.Actions = new ActionList { Actions = actions };
            return run;
        }

        private static SpellComponent School(SpellSchool school)
        {
            var component = Component<SpellComponent>("KWR_A4School" + school);
            component.School = school;
            return component;
        }

        private static SpellDescriptorComponent Descriptor(SpellDescriptor descriptor)
        {
            var component = Component<SpellDescriptorComponent>("KWR_A4Descriptor" + descriptor);
            component.Descriptor = descriptor;
            return component;
        }

        private static BlueprintAbilityResource Resource(string name, int perDay)
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

        private static AddAbilityResources GrantResource(BlueprintAbilityResource resource)
        {
            var grant = Component<AddAbilityResources>("KWR_A4Grant_" + resource.name);
            grant.Resource = resource;
            grant.RestoreAmount = true;
            return grant;
        }

        private static AddFacts AddFactsOf(string name, IEnumerable<BlueprintUnitFact> facts)
        {
            var add = Component<AddFacts>(name);
            add.Facts = facts.ToArray();
            return add;
        }

        private static BlueprintFeature Feature(string name, string display, string description, params BlueprintComponent[] components)
        {
            var feature = Create<BlueprintFeature>(name);
            Name(feature, "kwr.feature." + name, display, description);
            feature.HideInUI = false;
            feature.Groups = new FeatureGroup[0];
            feature.ComponentsArray = components;
            s_Register(feature);
            return feature;
        }

        private static BlueprintBuff Buff(BlueprintBuff donor, string name, string title, string description, int flags, params BlueprintComponent[] components)
        {
            var buff = UnityEngine.Object.Instantiate(donor);
            buff.name = name;
            buff.AssetGuid = ScenarioIds.Derive("a4." + name);
            Name(buff, "kwr.buff." + name, title, description);
            buff.Stacking = StackingType.Replace;
            var field = AccessTools.Field(typeof(BlueprintBuff), "m_Flags");
            field.SetValue(buff, Enum.ToObject(field.FieldType, flags));
            if ((flags & SpellEffect) != 0)
            {
                SpellEffectBuffs.Add(buff);
            }
            buff.ComponentsArray = components;
            s_Register(buff);
            return buff;
        }

        private static void Name(BlueprintScriptableObject blueprint, string key, string display, string description = null)
        {
            LocalizedText.Define(key, display);
            if (blueprint is BlueprintItem || blueprint is BlueprintWeaponType)
            {
                // Display names only: a field this build of the game does not have is skipped, never fatal.
                Type owner = blueprint is BlueprintItem ? typeof(BlueprintItem) : typeof(BlueprintWeaponType);
                System.Reflection.FieldInfo field = AccessTools.Field(owner, blueprint is BlueprintItem ? "m_DisplayNameText" : "m_TypeNameText");
                if (field == null)
                {
                    Log.Info($"A4: no display-name field on {owner.Name}; {blueprint.name} keeps its donor's name.");
                    return;
                }
                field.SetValue(blueprint, LocalizedText.Create(key));
                return;
            }
            if (blueprint is BlueprintUnitFact)
            {
                AccessTools.Field(typeof(BlueprintUnitFact), "m_DisplayName").SetValue(blueprint, LocalizedText.Create(key));
                if (description != null)
                {
                    LocalizedText.Define(key + ".description", description);
                    AccessTools.Field(typeof(BlueprintUnitFact), "m_Description").SetValue(blueprint, LocalizedText.Create(key + ".description"));
                }
            }
            else if (blueprint is BlueprintItem)
            {
                AccessTools.Field(typeof(BlueprintItem), "m_DisplayNameText").SetValue(blueprint, LocalizedText.Create(key));
            }
            else if (blueprint is BlueprintCharacterClass cls)
            {
                cls.LocalizedName = LocalizedText.Create(key);
            }
            else if (blueprint is BlueprintWeaponType)
            {
                AccessTools.Field(typeof(BlueprintWeaponType), "m_TypeNameText").SetValue(blueprint, LocalizedText.Create(key));
            }
        }

        private static AddStatBonus Stat(StatType stat, int value, ModifierDescriptor descriptor)
        {
            var bonus = Component<AddStatBonus>("KWR_A4" + stat + descriptor);
            bonus.Stat = stat;
            bonus.Value = value;
            bonus.Descriptor = descriptor;
            return bonus;
        }

        private static AddCondition Condition(UnitCondition condition)
        {
            var add = Component<AddCondition>("KWR_A4Condition" + condition);
            add.Condition = condition;
            return add;
        }

        private static T Create<T>(string name) where T : BlueprintScriptableObject
        {
            var blueprint = ScriptableObject.CreateInstance<T>();
            blueprint.name = name;
            blueprint.AssetGuid = ScenarioIds.Derive("a4." + name);
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
