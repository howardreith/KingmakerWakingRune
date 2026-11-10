using System;
using System.Linq;
using System.Text;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using KingmakerWakingRune.Sanctum;
using KingmakerWakingRune.Sanctum.Wards;
using Newtonsoft.Json;
using UnityEngine;

namespace KingmakerWakingRune.Diagnostics
{
    /// <summary>
    /// Labelled test setup for the running game, reachable only through <see cref="HarnessProbe"/> and absent from a normal
    /// install. A command can position, grant or advance; every effect it provokes still resolves through native rules.
    /// </summary>
    internal static class WardHarness
    {
        public static string Run(string commandLine)
        {
            string[] args = commandLine.Split(' ').Where(a => a.Length > 0).ToArray();
            if (args.Length == 0)
            {
                return "empty command";
            }
            switch (args[0])
            {
                case "state":
                    return WardStore.Dump();
                case "fixtures":
                    return Fixtures();
                case "party":
                    return Party();
                case "teleport":
                    return Teleport(Member(args[1]), float.Parse(args[2]), float.Parse(args[3]));
                case "cast":
                    return Cast(Member(args[1]), args[2], args[3]);
                case "attack":
                    return Attack(Member(args[1]), args[2]);
                case "castpoint":
                    return CastPoint(Member(args[1]), args[2], float.Parse(args[3]), float.Parse(args[4]));
                case "perform":
                    return Perform(args[1], args[2]);
                case "study":
                    return Study(args[1], args[2], args.Length > 3 ? args[3] : null);
                case "load":
                    return Load(args[1]);
                case "save":
                    return Save(args[1]);
                case "brand":
                    Member(args[1]).Descriptor.AddFact(WardBlueprints.SihedronBrand);
                    return $"INJECTED door credential on {Member(args[1]).CharacterName}: no native Sihedron exists to recognize.";
                case "sound":
                    return Sound();
                case "kurshu":
                    return "INJECTED Kurshu defeat fixture: " + RitualRules.DefeatKurshu(WardRuntime.State.Ritual, WardRuntime.Now);
                case "advance":
                    Game.Instance.AdvanceGameTime(TimeSpan.FromSeconds(double.Parse(args[1])));
                    return $"INJECTED time advance: {args[1]} seconds, now {WardRuntime.Now:0}.";
                default:
                    return $"unknown command {args[0]}";
            }
        }

        private static UnitEntityData Member(string index)
        {
            return Game.Instance.Player.Party[int.Parse(index)];
        }

        private static string Fixtures()
        {
            var sb = new StringBuilder();
            foreach (var pair in WardFixtures.Live.OrderBy(p => p.Key))
            {
                UnitEntityData unit = pair.Value;
                Vector3 p = unit.Position;
                sb.AppendLine($"{pair.Key} name='{unit.CharacterName}' grid=({p.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX:0.00},{SanctumPlan.OriginY - p.z / SanctumPlan.MetersPerSquare:0.00}) h={p.y:0.00} corpulence={unit.Corpulence:0.00} visible={unit.IsVisibleForPlayer} inFog={unit.IsInFogOfWar} hp={unit.Descriptor.HPLeft}/{unit.Stats.HitPoints.ModifiedValue} inCombat={unit.IsInCombat} tempHp={unit.Stats.TemporaryHitPoints.ModifiedValue} buffs=[{string.Join(",", unit.Buffs.Enumerable.Select(b => b.Blueprint.name).ToArray())}]");
            }
            return sb.ToString();
        }

        private static string Party()
        {
            var sb = new StringBuilder();
            int index = 0;
            foreach (UnitEntityData unit in Game.Instance.Player.Party)
            {
                Vector3 p = unit.Position;
                sb.AppendLine($"{index++} '{unit.CharacterName}' hp={unit.Descriptor.HPLeft}/{unit.Stats.HitPoints.ModifiedValue} dex={unit.Stats.Dexterity.ModifiedValue} dexDamage={unit.Stats.Dexterity.Damage} grid=({p.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX:0.00},{SanctumPlan.OriginY - p.z / SanctumPlan.MetersPerSquare:0.00}) inCombat={unit.IsInCombat} fatigued={unit.Descriptor.State.HasCondition(UnitCondition.Fatigued)} buffs=[{string.Join(",", unit.Buffs.Enumerable.Select(b => b.Blueprint.name).ToArray())}]");
            }
            return sb.ToString();
        }

        private static string Load(string fileFragment)
        {
            Game.Instance.SaveManager.UpdateSaveListIfNeeded();
            Kingmaker.EntitySystem.Persistence.SaveInfo save = Game.Instance.SaveManager.FirstOrDefault(s => s.FileName != null && s.FileName.Contains(fileFragment));
            if (save == null)
            {
                return $"no save matching {fileFragment}";
            }
            Kingmaker.MainMenu menu = UnityEngine.Object.FindObjectOfType<Kingmaker.MainMenu>();
            if (menu != null)
            {
                menu.LoadGame(save);
            }
            else
            {
                Game.Instance.LoadGame(save);
            }
            return $"SETUP load requested: {save.FileName}.";
        }

        private static string Save(string name)
        {
            var save = Game.Instance.SaveManager.CreateNewSave("KWR_AUTOMATION_" + name);
            Game.Instance.SaveGame(save);
            return $"SETUP save requested: {save.FileName} ('{save.Name}').";
        }

        private static string Teleport(UnitEntityData unit, float x, float y)
        {
            float height = SanctumPlan.HeightAt(x, y) ?? 0f;
            unit.Position = new Vector3(SanctumPlan.ToWorldX(x), height, SanctumPlan.ToWorldZ(y));
            return $"INJECTED position: {unit.CharacterName} to ({x},{y}).";
        }

        private static string Cast(UnitEntityData caster, string blueprintName, string target)
        {
            BlueprintAbility ability = ResourcesLibrary.GetBlueprints<BlueprintAbility>().FirstOrDefault(a => a.name == blueprintName);
            if (ability == null)
            {
                return $"no ability named {blueprintName}";
            }
            AbilityData data = Granted(caster, ability);
            UnitEntityData victim;
            string missing = Resolve(target, out victim);
            if (missing != null)
            {
                return missing;
            }
            TargetWrapper wrapper = victim;
            bool can = data.CanTarget(wrapper);
            UnitCommand command = UnitUseAbility.CreateCastCommand(data, wrapper);
            caster.Commands.Run(command);
            return $"SETUP granted {ability.name}; caster={caster.CharacterName} target={target} canTarget={can}; command issued, native rules resolve it.";
        }

        /// <summary>An ability outside any spellbook reports level zero; this restores its real spell level for the route checks.</summary>
        private static AbilityData Granted(UnitEntityData caster, BlueprintAbility ability)
        {
            AbilityData data = caster.Descriptor.AddFact<Ability>(ability).Data;
            var spellLists = ability.GetComponents<Kingmaker.Blueprints.Classes.Spells.SpellListComponent>().ToList();
            data.OverrideSpellLevel = spellLists.Count > 0 ? spellLists.Max(l => l.SpellLevel) : 1;
            return data;
        }

        /// <summary>Posts both real door events and reports the playing IDs; zero means the event or its bank is not available.</summary>
        private static string Sound()
        {
            var door = UnityEngine.Object.FindObjectOfType<SanctumDoor>();
            uint open = AkSoundEngine.PostEvent("DungeonStoneDoorOpen", door.gameObject);
            uint close = AkSoundEngine.PostEvent("DungeonDoorStone_Close", door.gameObject);
            return $"door sound playing ids: open={open} close={close}";
        }

        private static string Resolve(string target, out UnitEntityData unit)
        {
            unit = target.StartsWith("party:") ? Member(target.Substring(6)) : WardFixtures.Find(target);
            return unit == null ? $"no target {target}" : null;
        }

        private static string CastPoint(UnitEntityData caster, string blueprintName, float x, float y)
        {
            BlueprintAbility ability = ResourcesLibrary.GetBlueprints<BlueprintAbility>().FirstOrDefault(a => a.name == blueprintName);
            if (ability == null)
            {
                return $"no ability named {blueprintName}";
            }
            AbilityData data = Granted(caster, ability);
            var point = new Vector3(SanctumPlan.ToWorldX(x), SanctumPlan.HeightAt(x, y) ?? 0f, SanctumPlan.ToWorldZ(y));
            caster.Commands.Run(UnitUseAbility.CreateCastCommand(data, point));
            return $"SETUP granted {ability.name}; caster={caster.CharacterName} point=({x},{y}); native rules resolve it.";
        }

        /// <summary>Runs one fixture's dialogue action as if its answer were clicked; the rolls and effects behind it stay native.</summary>
        private static string Perform(string fixtureId, string key)
        {
            WardAction action = WardActions.For(fixtureId).FirstOrDefault(a => a.Key == key);
            if (action == null)
            {
                return $"no action {key} on {fixtureId}";
            }
            action.Run(Game.Instance.Player.MainCharacter.Value);
            WardInteract.RunPending();
            return $"SETUP performed {key} on {fixtureId}.";
        }

        private static string Study(string fixtureId, string key, string skillName)
        {
            InspectDef def = WardText.Find(fixtureId);
            InspectCheck check = def?.Checks.FirstOrDefault(c => c.Key == key);
            if (check == null)
            {
                return $"no check {key} on {fixtureId}";
            }
            WardSkill skill = skillName != null ? (WardSkill)Enum.Parse(typeof(WardSkill), skillName) : check.Skills[0];
            WardInteract.Study(fixtureId, check, skill);
            return $"SETUP studied {key} on {fixtureId} with {skill}.";
        }

        private static string Attack(UnitEntityData attacker, string target)
        {
            UnitEntityData victim;
            string missing = Resolve(target, out victim);
            if (missing != null)
            {
                return missing;
            }
            attacker.Commands.Run(UnitAttack.CreateAttackCommand(attacker, victim));
            return $"SETUP attack command issued: {attacker.CharacterName} -> {target}.";
        }
    }
}
