using System;
using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Root;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using KingmakerWakingRune.Blueprints;
using Newtonsoft.Json;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>The feeder's stolen hit points on Krune, read from saved state when the buff applies.</summary>
    public sealed class KwrFeederTempHp : BuffLogic
    {
        [JsonProperty]
        private ModifiableValue.Modifier m_Modifier;

        public override void OnFactActivate()
        {
            m_Modifier = Owner.Stats.TemporaryHitPoints.AddModifier(WardKrune.PendingTemporaryHP, this, ModifierDescriptor.UntypedStackable);
        }

        public override void OnFactDeactivate()
        {
            m_Modifier?.Remove();
            m_Modifier = null;
        }
    }

    /// <summary>
    /// Every WR-04 blueprint: stable GUIDs derived from names, built once with the other mod blueprints. Donors are
    /// cloned, never altered, and each donor is resolved by the GUID recorded in docs/WR-04-RULES.md.
    /// </summary>
    internal static class WardBlueprints
    {
        private const string Staggered = "df3950af5a783bd4d91ab73eb8fa0fd3";
        private const string Fatigued = "e6f2fc5d73d88064583cb828801212f4";
        private const string Sleeping = "5e0cd801bac0e95429bb7e4d1bc61a23";
        private const string AberrationType = "3bec99efd9a363242a6c8d9957b75e91";
        /// <summary>CR2_BanditPositiveClericLevel3: the nearest native body to the GameMastery Guide's cleric 3 cultist.</summary>
        private const string CultistDonor = "9ffceca100e02644ba67cf492c17d351";
        private const string RestorationLesserAbility = "e84fc922ccf952943b5240293669b171";
        private const int FixtureHitPoints = 1000000;
        private const int FixtureAbilityScore = 200;

        public static BlueprintFaction FixtureFaction { get; private set; }
        public static BlueprintFaction StatueFaction { get; private set; }
        public static BlueprintUnit StatueFixture { get; private set; }
        public static BlueprintUnit DoorFixture { get; private set; }
        public static BlueprintUnit GemFixture { get; private set; }
        public static BlueprintUnit KruneSupport { get; private set; }
        public static BlueprintUnit VandianaSupport { get; private set; }
        public static readonly Dictionary<string, BlueprintUnit> RuneFixtures = new Dictionary<string, BlueprintUnit>();
        public static BlueprintBuff DefenseWard { get; private set; }
        public static BlueprintBuff CurseBuff { get; private set; }
        public static BlueprintBuff CurseFatigue { get; private set; }
        public static BlueprintBuff MistStagger { get; private set; }
        public static BlueprintBuff MistSleep { get; private set; }
        public static BlueprintBuff FeederTempHp { get; private set; }
        public static BlueprintFeature SihedronBrand { get; private set; }
        public static BlueprintFeature LissalaWorshipper { get; private set; }
        public static BlueprintAbility WakeAlly { get; private set; }
        public static BlueprintBuff WardMarker { get; private set; }
        public static BlueprintUnit AlarmFixture { get; private set; }
        public static BlueprintUnit GlyphFixture { get; private set; }
        public static BlueprintUnit HoundFixture { get; private set; }
        public static BlueprintUnit CultistAttacker { get; private set; }
        public static BlueprintUnit CultistReporter { get; private set; }
        public static readonly Dictionary<string, BlueprintUnit> InspectFixtures = new Dictionary<string, BlueprintUnit>();

        private static Action<BlueprintScriptableObject> s_Register;

        public static void Build(BlueprintRoot root, IEnumerable<BlueprintScriptableObject> all, Action<BlueprintScriptableObject> register)
        {
            s_Register = register;
            Dictionary<string, BlueprintBuff> buffs = all.OfType<BlueprintBuff>().GroupBy(b => b.AssetGuid).ToDictionary(g => g.Key, g => g.First());
            BlueprintUnitFact aberration = all.OfType<BlueprintFeature>().First(f => f.AssetGuid == AberrationType);

            FixtureFaction = Faction("KWR_FixtureTarget", true, true);
            StatueFaction = Faction("KWR_StatueFixture", false, true);

            BlueprintFeature objectFeature = Create<BlueprintFeature>("KWR_FixtureObject");
            objectFeature.HideInUI = true;
            objectFeature.ComponentsArray = new BlueprintComponent[]
            {
                Component<AddImmunityToCriticalHits>("KWR_NoCrits"),
                Component<AddImmunityToPrecisionDamage>("KWR_NoPrecision"),
            };
            objectFeature.Groups = new FeatureGroup[0];
            register(objectFeature);

            BlueprintUnitFact[] fixtureFacts = { aberration, objectFeature };
            BlueprintUnit donor = root.InvisibleKittenUnit;
            foreach (RuneFocus focus in KruneProjection.Foci)
            {
                RuneFixtures[focus.Id] = Fixture(root, donor, "KWR_Rune_" + focus.Id, "Thassilonian rune plate", FixtureFaction, fixtureFacts);
            }
            StatueFixture = Fixture(root, donor, "KWR_StatueFixture", "Stone figure", StatueFaction, new BlueprintUnitFact[] { objectFeature });
            DoorFixture = Fixture(root, donor, "KWR_DoorFixture", "Conduit Door", FixtureFaction, fixtureFacts);
            GemFixture = Fixture(root, donor, "KWR_GemFixture", "Flawless emerald", FixtureFaction, fixtureFacts);
            KruneSupport = Fixture(root, donor, "KWR_KruneSupport", "Runelord Krune", StatueFaction, new BlueprintUnitFact[] { objectFeature });
            VandianaSupport = Fixture(root, donor, "KWR_VandianaSupport", "Vandiana the Lashmistress", StatueFaction, new BlueprintUnitFact[] { objectFeature });

            DefenseWard = Buff(buffs[Staggered], "KWR_DefenseRuneWard", "Rune of Defense", "Runelord Krune's warding enchantment.", hidden: true, fromSpell: true, Permanent());
            CurseBuff = Buff(buffs[Fatigued], "KWR_SlothCurse", "Cursed Rune of Sloth", "A Thassilonian brand of sloth, marking an enemy of Xin-Haruka. Will DC 20. Each day, this affliction consumes 1d6 Dexterity. Each further brand raises the difficulty of removal but does no further harm.", hidden: false, fromSpell: true, Descriptor(SpellDescriptor.Curse));
            CurseFatigue = Buff(buffs[Fatigued], "KWR_SlothCurseFatigue", "Cursed Fatigue", "Fatigued until every point of Dexterity damage is healed.", hidden: false, fromSpell: false, Condition(UnitCondition.Fatigued));
            MistStagger = Buff(buffs[Staggered], "KWR_MistStagger", "Staggered by the Mist", "The mist's preserving force slows the body: staggered for one round.", hidden: false, fromSpell: false, Condition(UnitCondition.Staggered));
            MistSleep = Buff(buffs[Sleeping], "KWR_MistSleep", "Sleeping in the Mist", "Asleep in the preservation vapor. Damage or an adjacent ally's standard action wakes this creature.", hidden: false, fromSpell: false, Condition(UnitCondition.Sleeping), Condition(UnitCondition.Prone), Descriptor(SpellDescriptor.Sleep), Logic<KwrMistSleepWake>("KWR_MistWakeOnDamage"));
            FeederTempHp = Buff(buffs[Fatigued], "KWR_FeederTempHp", "Stolen Vitality", "Life stolen by the conduit door's feeder.", hidden: true, fromSpell: false, Logic<KwrFeederTempHp>("KWR_FeederTempHpLogic"));

            WardMarker = Buff(buffs[Staggered], "KWR_WardMarker", "Ward", "A standing enchantment of the sanctum.", hidden: true, fromSpell: true, Permanent());

            SihedronBrand = Create<BlueprintFeature>("KWR_SihedronBrand");
            SihedronBrand.HideInUI = true;
            SihedronBrand.ComponentsArray = new BlueprintComponent[0];
            SihedronBrand.Groups = new FeatureGroup[0];
            register(SihedronBrand);

            LissalaWorshipper = Create<BlueprintFeature>("KWR_LissalaWorshipper");
            LissalaWorshipper.HideInUI = true;
            LissalaWorshipper.ComponentsArray = new BlueprintComponent[0];
            LissalaWorshipper.Groups = new FeatureGroup[0];
            register(LissalaWorshipper);

            WakeAlly = BuildWake(all);
            register(WakeAlly);

            AlarmFixture = Fixture(root, donor, "KWR_AlarmFixture", "Alarm ward", StatueFaction, new BlueprintUnitFact[] { objectFeature });
            GlyphFixture = Fixture(root, donor, "KWR_GlyphFixture", "Glyph ward", StatueFaction, new BlueprintUnitFact[] { objectFeature });
            HoundFixture = Fixture(root, donor, "KWR_HoundFixture", "Faithful Hound", StatueFaction, new BlueprintUnitFact[] { objectFeature });
            foreach (string id in new[] { WardText.ApexId, WardText.ToolsId, WardText.LightId, WardText.PedestalId, WardText.SarcophagusId, WardText.MistId, WardText.FlailId })
            {
                InspectFixtures[id] = Fixture(root, donor, "KWR_Inspect_" + id.Replace("-", string.Empty), id == WardText.FlailId ? "Metal chain" : id, StatueFaction, new BlueprintUnitFact[] { objectFeature });
            }

            BlueprintUnit cultistDonor = all.OfType<BlueprintUnit>().First(u => u.AssetGuid == CultistDonor);
            CultistAttacker = Cultist(cultistDonor, root, "KWR_CultistAttacker", "Restored Lissalan Cultist", root.SystemMechanics.FactionMobs);
            CultistReporter = Cultist(cultistDonor, root, "KWR_CultistReporter", "Fleeing Lissalan Cultist", root.SystemMechanics.FactionNeutrals);

            foreach (var pair in AllClickable())
            {
                AddInteraction(pair);
            }
        }

        /// <summary>Every fixture a click can reach: rune, statue, door, gem and inspect proxies. Hound and hidden wards take spells only.</summary>
        private static IEnumerable<BlueprintUnit> AllClickable()
        {
            return RuneFixtures.Values.Concat(new[] { StatueFixture, DoorFixture, GemFixture }).Concat(InspectFixtures.Values);
        }

        private static void AddInteraction(BlueprintUnit unit)
        {
            var interaction = Component<KwrFixtureInteraction>("KWR_FixtureInteraction_" + unit.name);
            interaction.Conditions = new ConditionsChecker();
            unit.ComponentsArray = unit.ComponentsArray.Concat(new BlueprintComponent[] { interaction }).ToArray();
        }

        /// <summary>A standard-action touch from an adjacent ally: the printed way out of the mist's sleep other than damage.</summary>
        private static BlueprintAbility BuildWake(IEnumerable<BlueprintScriptableObject> all)
        {
            BlueprintAbility donor = all.OfType<BlueprintAbility>().First(a => a.AssetGuid == RestorationLesserAbility);
            var ability = UnityEngine.Object.Instantiate(donor);
            ability.name = "KWR_WakeAlly";
            ability.AssetGuid = ScenarioIds.Derive("ability.KWR_WakeAlly");
            string nameKey = "kwr.ability.wake", descriptionKey = nameKey + ".description";
            LocalizedText.Define(nameKey, "Wake Ally");
            LocalizedText.Define(descriptionKey, "Shake an adjacent ally awake from the mist's sleep. A standard action.");
            AccessTools.Field(typeof(BlueprintUnitFact), "m_DisplayName").SetValue(ability, LocalizedText.Create(nameKey));
            AccessTools.Field(typeof(BlueprintUnitFact), "m_Description").SetValue(ability, LocalizedText.Create(descriptionKey));
            ability.Type = AbilityType.Extraordinary;
            ability.Range = AbilityRange.Touch;
            ability.CanTargetSelf = false;
            ability.CanTargetFriends = true;
            ability.CanTargetEnemies = false;
            ability.CanTargetPoint = false;
            ability.ActionType = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard;
            var run = Component<AbilityEffectRunAction>("KWR_WakeRun");
            run.Actions = new ActionList { Actions = new GameAction[] { ScriptableObject.CreateInstance<KwrWakeAction>() } };
            var target = Component<AbilityTargetHasFact>("KWR_WakeTarget");
            target.CheckedFacts = new BlueprintUnitFact[] { MistSleep };
            ability.ComponentsArray = new BlueprintComponent[] { run, target };
            return ability;
        }

        private static BlueprintUnit Cultist(BlueprintUnit donor, BlueprintRoot root, string name, string display, BlueprintFaction faction)
        {
            string key = "kwr.unit." + name;
            LocalizedText.Define(key, display);
            var unit = UnityEngine.Object.Instantiate(donor);
            unit.name = name;
            unit.AssetGuid = ScenarioIds.Derive("unit." + name);
            unit.Faction = faction;
            unit.AddFacts = (unit.AddFacts ?? new BlueprintUnitFact[0]).Concat(new BlueprintUnitFact[] { LissalaWorshipper }).ToArray();
            var localizedName = ScriptableObject.CreateInstance<SharedStringAsset>();
            localizedName.name = name + "_Name";
            localizedName.String = LocalizedText.Create(key);
            unit.LocalizedName = localizedName;
            s_Register(unit);
            return unit;
        }

        private static BlueprintUnit Fixture(BlueprintRoot root, BlueprintUnit donor, string name, string display, BlueprintFaction faction, BlueprintUnitFact[] facts)
        {
            string key = "kwr.fixture." + name;
            LocalizedText.Define(key, display);
            BlueprintUnit unit = LodgeCast.CloneUnit(donor, root, name, ScenarioIds.Derive("unit." + name), key, null);
            unit.Faction = faction;
            unit.AddFacts = facts;
            unit.MaxHP = FixtureHitPoints;
            // Dexterity 1 gives the armor class of an object; high other scores keep any ability damage from disabling it.
            unit.Strength = FixtureAbilityScore;
            unit.Dexterity = 1;
            unit.Constitution = FixtureAbilityScore;
            unit.Intelligence = FixtureAbilityScore;
            unit.Wisdom = FixtureAbilityScore;
            unit.Charisma = FixtureAbilityScore;
            var filter = Component<UnitAggroFilter>("KWR_NoAggro_" + name);
            filter.FilterCondition = new ConditionsChecker();
            filter.ActionsOnAggro = new ActionList();
            unit.ComponentsArray = new BlueprintComponent[] { filter };
            s_Register(unit);
            return unit;
        }

        private static BlueprintFaction Faction(string name, bool neutral, bool peaceful)
        {
            BlueprintFaction faction = Create<BlueprintFaction>(name);
            faction.Neutral = neutral;
            faction.Peaceful = peaceful;
            faction.AttackFactions = new BlueprintFaction[0];
            s_Register(faction);
            return faction;
        }

        private static BlueprintBuff Buff(BlueprintBuff donor, string name, string title, string description, bool hidden, bool fromSpell, params BlueprintComponent[] components)
        {
            var buff = UnityEngine.Object.Instantiate(donor);
            buff.name = name;
            buff.AssetGuid = ScenarioIds.Derive("buff." + name);
            string titleKey = "kwr.buff." + name, descriptionKey = titleKey + ".description";
            LocalizedText.Define(titleKey, title);
            LocalizedText.Define(descriptionKey, description);
            AccessTools.Field(typeof(BlueprintUnitFact), "m_DisplayName").SetValue(buff, LocalizedText.Create(titleKey));
            AccessTools.Field(typeof(BlueprintUnitFact), "m_Description").SetValue(buff, LocalizedText.Create(descriptionKey));
            buff.Stacking = StackingType.Replace;
            SetFlags(buff, hidden, fromSpell);
            buff.ComponentsArray = components;
            s_Register(buff);
            return buff;
        }

        private static void SetFlags(BlueprintBuff buff, bool hidden, bool fromSpell)
        {
            var field = AccessTools.Field(typeof(BlueprintBuff), "m_Flags");
            // IsFromSpell is 1 (needed for a spell-only dispel), HiddenInUi 2, Harmful 0x40.
            int flags = 0x40 | (hidden ? 2 : 0) | (fromSpell ? 1 : 0);
            field.SetValue(buff, Enum.ToObject(field.FieldType, flags));
        }

        private static T Create<T>(string name) where T : BlueprintScriptableObject
        {
            var blueprint = ScriptableObject.CreateInstance<T>();
            blueprint.name = name;
            blueprint.AssetGuid = ScenarioIds.Derive("ward." + name);
            return blueprint;
        }

        private static T Component<T>(string name) where T : BlueprintComponent
        {
            var component = ScriptableObject.CreateInstance<T>();
            component.name = name;
            return component;
        }

        private static AddCondition Condition(UnitCondition condition)
        {
            var component = Component<AddCondition>("KWR_Condition_" + condition);
            component.Condition = condition;
            return component;
        }

        private static SpellDescriptorComponent Descriptor(SpellDescriptor descriptor)
        {
            var component = Component<SpellDescriptorComponent>("KWR_Descriptor");
            component.Descriptor = new SpellDescriptorWrapper(descriptor);
            return component;
        }

        private static T Logic<T>(string name) where T : BlueprintComponent
        {
            return Component<T>(name);
        }

        private static BlueprintComponent Permanent()
        {
            return Descriptor(SpellDescriptor.None);
        }
    }
}
