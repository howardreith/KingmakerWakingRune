using System;
using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Controllers;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ResourceLinks;
using KingmakerWakingRune.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Krune's spells that this game does not have, each built on native parts (a native spell's look, native areas, buffs,
    /// saves, combat maneuvers and teleport logic) acting by the printed rule. Every one is a real spell in his book, spent
    /// from its slot by a native cast. Limits are stated on each.
    /// </summary>
    internal static class KruneLocalSpells
    {
        public static BlueprintBuff TentaclesGrappled { get; private set; }
        public static BlueprintBuff InsideGlobe { get; private set; }
        public static BlueprintBuff SolidFogSlow { get; private set; }
        public static BlueprintBuff SpellTurning { get; private set; }
        public static BlueprintBuff Juxtaposition { get; private set; }
        public static BlueprintBuff PhaseDoorPassages { get; private set; }
        public static BlueprintAbility PhaseDoorStep { get; private set; }

        private static Action<BlueprintScriptableObject> s_Register;
        private static Dictionary<string, BlueprintAbility> s_Native;

        public static Dictionary<string, BlueprintAbility> Build(Dictionary<string, BlueprintAbility> native, Action<BlueprintScriptableObject> register)
        {
            s_Register = register;
            s_Native = native;
            var built = new Dictionary<string, BlueprintAbility>();
            BlueprintBuff buffDonor = KruneBlueprints.OpeningStagger;

            // Teleportation within the sanctum: the game's own dimension door logic and effects, to a legal sanctum point.
            BlueprintAbility door = native["DimensionDoorCasterOnly"];
            built["KWR_KruneGreaterTeleport"] = Teleport(door, "KWR_KruneGreaterTeleport", "Greater Teleport", "Krune teleports himself to any place in his sanctum he chooses. In this scenario he never leaves the sanctum: he fights to the death (page 17).", 0f, SpellSchool.Conjuration);
            PhaseDoorStep = Teleport(door, "KWR_KrunePhaseDoorStep", "Phase Door (passage)", "Krune steps through his ethereal passage, up to 40 feet through solid stone, to the other side.", 40f, SpellSchool.Conjuration);
            PhaseDoorStep.Type = AbilityType.SpellLike;
            var passages = Component<KwrPhaseDoorPassages>("KWR_KrunePhaseDoorPassagesLogic");
            PhaseDoorPassages = Buff(buffDonor, "KWR_KrunePhaseDoorPassages", "Phase Door", "An ethereal passage only Krune can use: one passage per two caster levels, until used up.", false, true, passages);
            built["KWR_KrunePhaseDoor"] = SelfSpell(native["MageShield"], "KWR_KrunePhaseDoor", "Phase Door", "Krune creates an ethereal passage through stone that only he can use, one passage per two caster levels (CL 17: 8). He may step through it at once.", SpellSchool.Conjuration,
                Apply<KwrApplySelfBuff>(a => { a.Buff = PhaseDoorPassages; a.SecondsPerLevel = 0d; }));

            // Fog cloud and solid fog: the game's obscuring mist area, at a point, with fog cloud's duration; solid fog also slows.
            BlueprintAbilityAreaEffect mist = AreaOf(native["ObscuringMist"]);
            built["KWR_KruneFogCloud"] = AreaSpell(native["ObscuringMist"], "KWR_KruneFogCloud", "Fog Cloud", "A bank of fog billows out from a point within medium range, 20-foot radius: it obscures all sight beyond 5 feet (concealment, total beyond 5 feet). 10 minutes per level.", mist, 600d, SpellSchool.Conjuration, SpellDescriptor.None);
            SolidFogSlow = Buff(buffDonor, "KWR_KruneSolidFogSlow", "Solid Fog", "Thick fog: speed reduced to 5 feet, -2 on melee attack and damage rolls.", false, true,
                Stat(StatType.Speed, -25), Stat(StatType.AdditionalAttackBonus, -2), Stat(StatType.AdditionalDamage, -2));
            BlueprintAbilityAreaEffect solid = CloneArea(mist, "KWR_KruneSolidFogArea", extra: AreaBuff(SolidFogSlow));
            built["KWR_KruneSolidFog"] = AreaSpell(native["ObscuringMist"], "KWR_KruneSolidFog", "Solid Fog", "Thick fog at a point within medium range, 20-foot radius: it obscures sight as fog cloud, and a creature in it moves at 5 feet and takes -2 on melee attack and damage rolls. 1 minute per level.", solid, 60d, SpellSchool.Conjuration, SpellDescriptor.None);

            // Black tentacles: a 20-foot spread of tentacles that grapple (CMB = CL + 5) each round and crush the grappled.
            TentaclesGrappled = Buff(buffDonor, "KWR_KruneTentaclesGrappled", "Grappled by black tentacles", "Grappled: the creature cannot move and is entangled (-2 attack, -4 Dexterity), and takes 1d6+4 bludgeoning each round it stays held. It may try to break free each round (CMB, Athletics or Mobility against the tentacles' CMD).", false, true,
                Condition(UnitCondition.CantMove), Condition(UnitCondition.Entangled), Component<KwrTentacleHold>("KWR_KruneTentacleHoldLogic"));
            BlueprintAbilityAreaEffect tentacles = CloneArea(AreaOf(native["Web"]), "KWR_KruneBlackTentaclesArea", replace: true, extra: new BlueprintComponent[] { Component<KwrTentacleArea>("KWR_KruneTentacleAreaLogic") });
            built["KWR_KruneBlackTentacles"] = AreaSpell(native["Web"], "KWR_KruneBlackTentacles", "Black Tentacles", "Rubbery black tentacles fill a 20-foot spread within medium range for 1 round per level. Each round, at the start, they try to grapple every creature in the area (CMB = caster level + 5) and crush any they hold for 1d6+4. Spell resistance does not apply.", tentacles, 6d, SpellSchool.Conjuration, SpellDescriptor.None);

            // Globe of invulnerability: an immobile 10-foot globe where spells of 4th level or lower cast from outside fail.
            InsideGlobe = Buff(buffDonor, "KWR_KruneInsideGlobe", "Within a globe of invulnerability", "Spells of 4th level or lower cast from outside the globe cannot affect this creature.", false, true);
            BlueprintAbilityAreaEffect globe = CloneArea(mist, "KWR_KruneGlobeArea", replace: true, extra: AreaBuff(InsideGlobe), size: 10.Feet(), fx: false);
            built["KWR_KruneGlobeOfInvulnerability"] = SelfArea(native["MageShield"], "KWR_KruneGlobeOfInvulnerability", "Globe of Invulnerability", "An immobile, faintly shimmering globe 10 feet across surrounds Krune for 1 round per level: spells of 4th level or lower cast from outside it cannot affect anything within. It does not move with him.", globe, 6d, SpellSchool.Abjuration);

            // Spell turning: 1d4+6 spell levels of single-target spells aimed at him are turned on their casters (KruneSpellTurning).
            SpellTurning = Buff(buffDonor, "KWR_KruneSpellTurningBuff", "Spell Turning", "Single-target spells aimed at Krune rebound on their casters, up to the levels left (1d4+6 when cast). A spell of more levels than remain may still partly rebound. Areas and touches are not turned.", false, true, Component<KwrSpellTurningLogic>("KWR_KruneSpellTurningLogic"));
            built["KWR_KruneSpellTurning"] = SelfSpell(native["MageShield"], "KWR_KruneSpellTurning", "Spell Turning", "Single-target spells aimed at Krune are turned back on their casters, 1d4+6 spell levels in all, for 10 minutes per level or until used up.", SpellSchool.Abjuration,
                Apply<KwrApplySelfBuff>(a => { a.Buff = SpellTurning; a.SecondsPerLevel = 600d; }));

            // Hostile juxtaposition: a linked foe may be swapped in front of an attack meant for him (KruneJuxtaposition).
            Juxtaposition = Buff(buffDonor, "KWR_KruneJuxtapositionBuff", "Hostile Juxtaposition", "Krune is linked to a nearby foe: once a round, when he is attacked, that foe must succeed on a Will save or trade places with him, and the attack is spent on empty air where he stood.", false, true);
            BlueprintAbility link = Shell(native["HoldPerson"], "KWR_KruneHostileJuxtaposition", "Hostile Juxtaposition", "Krune links himself to one creature within close range for 1 round per level (Will negates). Once a round, when he is the target of a melee or ranged attack, the linked creature must succeed on another Will save or trade places with him; the attack is lost. Spell resistance applies.");
            link.Range = AbilityRange.Close;
            link.CanTargetEnemies = true;
            link.CanTargetFriends = false;
            link.CanTargetSelf = false;
            link.SpellResistance = true;
            link.ComponentsArray = Keep(link).Concat(new BlueprintComponent[] { RunActions(Apply<KwrJuxtaposeLink>(a => { })), School(SpellSchool.Conjuration) }).ToArray();
            built["KWR_KruneHostileJuxtaposition"] = link;

            // Wall of stone: a real obstacle of breakable 5-foot sections (KruneWalls).
            BlueprintAbility wall = Shell(native["Web"], "KWR_KruneWallOfStone", "Wall of Stone", "Krune conjures a wall of rock 10 feet high and up to 40 feet long, 4 inches thick, merged into the floor, across the point he chooses within medium range. It blocks passage and sight. Each 5-foot section has hardness 8 and 60 hit points and can be broken or disintegrated; the wall is not dispellable. It cannot form where a creature stands.");
            wall.Range = AbilityRange.Medium;
            wall.CanTargetPoint = true;
            wall.CanTargetEnemies = false;
            wall.CanTargetSelf = false;
            wall.SpellResistance = false;
            wall.ComponentsArray = new BlueprintComponent[] { RunActions(Apply<KwrRaiseWall>(a => { })), School(SpellSchool.Conjuration) };
            built["KWR_KruneWallOfStone"] = wall;
            return built;
        }

        public static BlueprintAbilityAreaEffect AreaOf(BlueprintAbility ability)
        {
            foreach (AbilityEffectRunAction run in ability.ComponentsArray.OfType<AbilityEffectRunAction>())
            {
                ContextActionSpawnAreaEffect spawn = Flatten(run.Actions).OfType<ContextActionSpawnAreaEffect>().FirstOrDefault();
                if (spawn != null)
                {
                    return spawn.AreaEffect;
                }
            }
            throw new InvalidOperationException("no area effect in " + ability.name);
        }

        private static IEnumerable<GameAction> Flatten(ActionList list)
        {
            foreach (GameAction action in list?.Actions ?? new GameAction[0])
            {
                yield return action;
                if (action is Conditional conditional)
                {
                    foreach (GameAction inner in Flatten(conditional.IfTrue).Concat(Flatten(conditional.IfFalse)))
                    {
                        yield return inner;
                    }
                }
            }
        }

        private static BlueprintAbility Teleport(BlueprintAbility door, string name, string display, string description, float maxFeet, SpellSchool school)
        {
            var ability = UnityEngine.Object.Instantiate(door);
            ability.name = name;
            ability.AssetGuid = ScenarioIds.Derive("krune." + name);
            ability.Type = AbilityType.Spell;
            ability.Range = AbilityRange.Unlimited;
            ability.CanTargetPoint = true;
            Name(ability, display, description);
            var legal = Component<KwrSanctumDestination>(name + "_Destination");
            legal.MaxFeet = maxFeet;
            ability.ComponentsArray = ability.ComponentsArray.Where(c => !(c is SpellListComponent)).Concat(new BlueprintComponent[] { legal }).ToArray();
            AccessTools.Field(typeof(BlueprintAbility), "m_CachedTargetCheckers").SetValue(ability, null);
            s_Register(ability);
            return ability;
        }

        private static BlueprintAbility AreaSpell(BlueprintAbility look, string name, string display, string description, BlueprintAbilityAreaEffect area, double secondsPerLevel, SpellSchool school, SpellDescriptor descriptor)
        {
            BlueprintAbility ability = Shell(look, name, display, description);
            ability.Range = AbilityRange.Medium;
            ability.CanTargetPoint = true;
            ability.CanTargetEnemies = true;
            ability.CanTargetSelf = false;
            ability.SpellResistance = false;
            var radius = Component<AbilityAoERadius>(name + "_Radius");
            AccessTools.Field(typeof(AbilityAoERadius), "m_Radius").SetValue(radius, area.Size);
            ability.ComponentsArray = Keep(ability).Concat(new BlueprintComponent[] { radius, RunActions(Apply<KwrSpawnArea>(a => { a.Area = area; a.SecondsPerLevel = secondsPerLevel; })), School(school) }).ToArray();
            return ability;
        }

        private static BlueprintAbility SelfArea(BlueprintAbility look, string name, string display, string description, BlueprintAbilityAreaEffect area, double secondsPerLevel, SpellSchool school)
        {
            BlueprintAbility ability = Shell(look, name, display, description);
            ability.Range = AbilityRange.Personal;
            ability.CanTargetSelf = true;
            ability.CanTargetPoint = false;
            ability.ComponentsArray = Keep(ability).Concat(new BlueprintComponent[] { RunActions(Apply<KwrSpawnArea>(a => { a.Area = area; a.SecondsPerLevel = secondsPerLevel; })), School(school) }).ToArray();
            return ability;
        }

        private static BlueprintAbility SelfSpell(BlueprintAbility look, string name, string display, string description, SpellSchool school, GameAction action)
        {
            BlueprintAbility ability = Shell(look, name, display, description);
            ability.Range = AbilityRange.Personal;
            ability.CanTargetSelf = true;
            ability.ComponentsArray = Keep(ability).Concat(new BlueprintComponent[] { RunActions(action), School(school) }).ToArray();
            return ability;
        }

        private static BlueprintAbilityAreaEffect CloneArea(BlueprintAbilityAreaEffect donor, string name, bool replace = false, BlueprintComponent[] extra = null, Feet? size = null, bool fx = true)
        {
            var area = UnityEngine.Object.Instantiate(donor);
            area.name = name;
            area.AssetGuid = ScenarioIds.Derive("krune." + name);
            area.SpellResistance = false;
            if (size.HasValue)
            {
                area.Size = size.Value;
            }
            if (!fx)
            {
                area.Fx = new PrefabLink();
            }
            area.ComponentsArray = (replace ? new BlueprintComponent[0] : area.ComponentsArray).Concat(extra ?? new BlueprintComponent[0]).ToArray();
            s_Register(area);
            return area;
        }

        private static BlueprintComponent[] AreaBuff(BlueprintBuff buff)
        {
            var component = Component<AbilityAreaEffectBuff>(buff.name + "_Area");
            component.Buff = buff;
            component.Condition = new ConditionsChecker { Conditions = new Condition[0] };
            return new BlueprintComponent[] { component };
        }

        /// <summary>A copy of a native spell for its look (icon, animation); its rule components are replaced.</summary>
        private static BlueprintAbility Shell(BlueprintAbility donor, string name, string display, string description)
        {
            var ability = UnityEngine.Object.Instantiate(donor);
            ability.name = name;
            ability.AssetGuid = ScenarioIds.Derive("krune." + name);
            ability.Type = AbilityType.Spell;
            Name(ability, display, description);
            AccessTools.Field(typeof(BlueprintAbility), "m_CachedTargetCheckers").SetValue(ability, null);
            s_Register(ability);
            return ability;
        }

        private static IEnumerable<BlueprintComponent> Keep(BlueprintAbility shell)
        {
            return (shell.ComponentsArray ?? new BlueprintComponent[0]).Where(c => c is AbilitySpawnFx || c is AbilityAoERadius);
        }

        private static void Name(BlueprintUnitFact fact, string display, string description)
        {
            string key = "kwr.ability." + fact.name;
            LocalizedText.Define(key, display);
            LocalizedText.Define(key + ".desc", description);
            AccessTools.Field(typeof(BlueprintUnitFact), "m_DisplayName").SetValue(fact, LocalizedText.Create(key));
            AccessTools.Field(typeof(BlueprintUnitFact), "m_Description").SetValue(fact, LocalizedText.Create(key + ".desc"));
        }

        private static BlueprintBuff Buff(BlueprintBuff donor, string name, string title, string description, bool hidden, bool fromSpell, params BlueprintComponent[] components)
        {
            var buff = UnityEngine.Object.Instantiate(donor);
            buff.name = name;
            buff.AssetGuid = ScenarioIds.Derive("krune." + name);
            Name(buff, title, description);
            buff.ComponentsArray = components;
            buff.Stacking = StackingType.Replace;
            var flags = AccessTools.Field(typeof(BlueprintBuff), "m_Flags");
            flags.SetValue(buff, Enum.ToObject(flags.FieldType, (hidden ? 2 : 0) | (fromSpell ? 1 : 0)));
            s_Register(buff);
            return buff;
        }

        private static AddStatBonus Stat(StatType stat, int value)
        {
            var bonus = Component<AddStatBonus>("KWR_KruneStat_" + stat + value);
            bonus.Stat = stat;
            bonus.Value = value;
            bonus.Descriptor = ModifierDescriptor.UntypedStackable;
            return bonus;
        }

        private static AddCondition Condition(UnitCondition condition)
        {
            var add = Component<AddCondition>("KWR_KruneCondition_" + condition);
            add.Condition = condition;
            return add;
        }

        private static SpellComponent School(SpellSchool school)
        {
            var component = Component<SpellComponent>("KWR_KruneSchool_" + school);
            component.School = school;
            return component;
        }

        private static AbilityEffectRunAction RunActions(params GameAction[] actions)
        {
            var run = Component<AbilityEffectRunAction>("KWR_KruneRun");
            run.Actions = new ActionList { Actions = actions };
            return run;
        }

        private static T Apply<T>(Action<T> set) where T : GameAction
        {
            var action = ScriptableObject.CreateInstance<T>();
            set(action);
            return action;
        }

        private static T Component<T>(string name) where T : BlueprintComponent
        {
            var component = ScriptableObject.CreateInstance<T>();
            component.name = name;
            return component;
        }
    }

    /// <summary>A destination inside the sanctum on real floor, outside the coffin, on the navmesh and clear of creatures.</summary>
    public sealed class KwrSanctumDestination : BlueprintComponent, IAbilityTargetChecker
    {
        public float MaxFeet;

        public bool CanTarget(UnitEntityData caster, TargetWrapper target)
        {
            if (target == null || target.IsUnit)
            {
                return false;
            }
            if (MaxFeet > 0f && Vector3.Distance(caster.Position, target.Point) > MaxFeet.Feet().Meters + 0.5f)
            {
                return false;
            }
            return KruneMoves.LegalDestination(target.Point, caster);
        }
    }

    /// <summary>A spell's area at the target point (or on the caster) for the spell's duration at the context's caster level.</summary>
    public sealed class KwrSpawnArea : ContextAction
    {
        public BlueprintAbilityAreaEffect Area;
        public double SecondsPerLevel;

        public override string GetCaption()
        {
            return "Spawn " + Area?.name;
        }

        public override void RunAction()
        {
            double seconds = Math.Max(1, Context.Params.CasterLevel) * SecondsPerLevel;
            AreaEffectsController.Spawn(Context, Area, Target, TimeSpan.FromSeconds(seconds));
        }
    }

    /// <summary>A personal spell's buff on its caster for its duration (0: until used up, with no time limit).</summary>
    public sealed class KwrApplySelfBuff : ContextAction
    {
        public BlueprintBuff Buff;
        public double SecondsPerLevel;

        public override string GetCaption()
        {
            return "Apply " + Buff?.name;
        }

        public override void RunAction()
        {
            UnitEntityData caster = Context.MaybeCaster;
            if (caster == null)
            {
                return;
            }
            TimeSpan? duration = SecondsPerLevel > 0d ? TimeSpan.FromSeconds(Math.Max(1, Context.Params.CasterLevel) * SecondsPerLevel) : (TimeSpan?)null;
            Kingmaker.UnitLogic.Buffs.Buff buff = caster.Descriptor.AddBuff(Buff, Context, duration);
            if (buff != null)
            {
                buff.IsFromSpell = true;
            }
        }
    }
}
