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
                case "load":
                    return Load(args[1]);
                case "save":
                    return Save(args[1]);
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
            Ability fact = caster.Descriptor.AddFact<Ability>(ability);
            AbilityData data = fact.Data;
            UnitEntityData victim = WardFixtures.Find(target);
            if (victim == null)
            {
                return $"no fixture {target}";
            }
            TargetWrapper wrapper = victim;
            bool can = data.CanTarget(wrapper);
            UnitCommand command = UnitUseAbility.CreateCastCommand(data, wrapper);
            caster.Commands.Run(command);
            return $"SETUP granted {ability.name}; caster={caster.CharacterName} target={target} canTarget={can}; command issued, native rules resolve it.";
        }

        private static string Attack(UnitEntityData attacker, string target)
        {
            UnitEntityData victim = WardFixtures.Find(target);
            if (victim == null)
            {
                return $"no fixture {target}";
            }
            attacker.Commands.Run(UnitAttack.CreateAttackCommand(attacker, victim));
            return $"SETUP attack command issued: {attacker.CharacterName} -> {target}.";
        }
    }
}
