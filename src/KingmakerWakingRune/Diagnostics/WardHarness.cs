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
                case "spawnfoe":
                    return SpawnFoe(args[1], float.Parse(args[2]));
                case "wardpart":
                    return $"area={Game.Instance.CurrentlyLoadedArea?.name} sanctum={SanctumScope.IsSanctumLoaded} SanctumWardPart present={WardStore.HasPart} turnBased={TurnBased.Controllers.CombatController.IsInTurnBasedCombat()} inCombat={Game.Instance.Player.IsInCombat}";
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
                case "items":
                    return Items();
                case "hitdoor":
                    return HitDoor(args.Skip(1).Select(int.Parse).ToArray());
                case "damagekrune":
                    return DamageKrune(int.Parse(args[1]));
                case "grantfeeder":
                    FeederRules.Grant(WardRuntime.State.Door.Feeder, int.Parse(args[1]), WardRuntime.Now);
                    WardKrune.Refresh(WardRuntime.State, WardRuntime.Now);
                    return $"INJECTED feeder grant {args[1]}; exposed {FeederRules.ExposedTemporaryHP(WardRuntime.State.Door.Feeder, WardRuntime.Now)}.";
                case "shield":
                    return Shield(int.Parse(args[1]));
                case "condition":
                    return Condition(args[1], args[2], double.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture));
                case "uncondition":
                    return Uncondition(args[1], args[2]);
                case "units":
                    return Units();
                case "move":
                    Member(args[1]).Commands.Run(new UnitMoveTo(new Vector3(SanctumPlan.ToWorldX(float.Parse(args[2])), SanctumPlan.HeightAt(float.Parse(args[2]), float.Parse(args[3])) ?? 0f, SanctumPlan.ToWorldZ(float.Parse(args[3])))));
                    return $"SETUP move order: {Member(args[1]).CharacterName} to ({args[2]},{args[3]}).";
                case "stock":
                    return Stock();
                case "slots":
                    return Slots(Member(args[1]));
                case "cursor":
                    return Cursor();
                case "cantarget":
                    return CanTarget(Member(args[1]), args[2], args[3]);
                case "screen":
                    return ScreenPoint(args[1]);
                case "look":
                    // The camera only, as a player's own scrolling would: no unit, rule or state changes.
                    Resolve(args[1], out UnitEntityData seen);
                    if (seen == null)
                    {
                        return $"no target {args[1]}";
                    }
                    Game.Instance.UI.GetCameraRig().ScrollToImmediately(seen.Position);
                    return $"CAMERA moved to {args[1]}.";
                case "advance":
                    Game.Instance.AdvanceGameTime(TimeSpan.FromSeconds(double.Parse(args[1])));
                    return $"INJECTED time advance: {args[1]} seconds, now {WardRuntime.Now:0}.";
                default:
                    return $"unknown command {args[0]}";
            }
        }

        /// <summary>One stock hostile beside the party, for a combat in any area; the fight itself is entirely native.</summary>
        private static string SpawnFoe(string guid, float meters)
        {
            BlueprintUnit blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintUnit>(guid);
            UnitEntityData lead = Game.Instance.Player.Party[0];
            if (blueprint == null)
            {
                return $"no unit blueprint {guid}";
            }
            Vector3 at = lead.Position + lead.OrientationDirection * meters;
            UnitEntityData foe = Game.Instance.EntityCreator.SpawnUnit(blueprint, at, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
            return $"SETUP spawned hostile {foe.CharacterName} ({blueprint.name}) {meters} m ahead of {lead.CharacterName} in {Game.Instance.CurrentlyLoadedArea?.name}.";
        }

        private static UnitEntityData Member(string index)
        {
            return Game.Instance.Player.Party[int.Parse(index)];
        }

        /// <summary>
        /// Read only: the Quartermaster table as the save recorded it (shelf counts and the catalogue counts it was last
        /// reconciled against), then the live shelf after the native reconciliation, with every line that differs.
        /// </summary>
        private static string Stock()
        {
            var tables = Game.Instance.Player.SharedVendorTables;
            var built = (System.Collections.IDictionary)Harmony12.AccessTools.Field(tables.GetType(), "m_Dictionary").GetValue(tables);
            bool wasBuilt = built.Contains(Blueprints.QuartermasterStock.Table);
            var saved = new System.Collections.Generic.Dictionary<Kingmaker.Blueprints.Items.BlueprintItem, int>();
            var known = new System.Collections.Generic.Dictionary<Kingmaker.Blueprints.Items.BlueprintItem, int>();
            bool persisted = false;
            foreach (object data in (System.Collections.IEnumerable)Harmony12.AccessTools.Field(tables.GetType(), "m_PersistentTables").GetValue(tables))
            {
                var t = Harmony12.Traverse.Create(data);
                if (t.Field("Table").GetValue<Kingmaker.Blueprints.Items.BlueprintSharedVendorTable>() != Blueprints.QuartermasterStock.Table)
                {
                    continue;
                }
                persisted = true;
                foreach (Kingmaker.Blueprints.Loot.LootEntry entry in t.Field("Entries").GetValue<System.Collections.Generic.List<Kingmaker.Blueprints.Loot.LootEntry>>())
                {
                    saved[entry.Item] = saved.Get(entry.Item, 0) + entry.Count;
                }
                foreach (var pair in t.Field("KnownItems").GetValue<System.Collections.Generic.Dictionary<Kingmaker.Blueprints.Items.BlueprintItem, int>>() ?? new System.Collections.Generic.Dictionary<Kingmaker.Blueprints.Items.BlueprintItem, int>())
                {
                    known[pair.Key] = pair.Value;
                }
            }
            var live = new System.Collections.Generic.Dictionary<Kingmaker.Blueprints.Items.BlueprintItem, int>();
            foreach (Kingmaker.Items.ItemEntity item in tables.GetTable(Blueprints.QuartermasterStock.Table).Items)
            {
                live[item.Blueprint] = live.Get(item.Blueprint, 0) + item.Count;
            }
            var sb = new StringBuilder();
            sb.AppendLine($"Quartermaster stock: persisted={persisted} builtBeforeThisCall={wasBuilt} savedLines={saved.Count} savedItems={saved.Values.Sum()} knownLines={known.Count} knownItems={known.Values.Sum()} liveLines={live.Count} liveItems={live.Values.Sum()}");
            foreach (var item in saved.Keys.Union(live.Keys).Union(known.Keys).Distinct().OrderBy(i => i.Name))
            {
                int s = saved.Get(item, 0), k = known.Get(item, 0), l = live.Get(item, 0);
                if (s != l || (persisted && s != k))
                {
                    sb.AppendLine($"  {item.Name}: saved {s}, catalogue then {k}, live {l}");
                }
            }
            return sb.ToString();
        }

        /// <summary>Read only: which native cursor the game is drawing now (screenshots do not capture it), and over what.</summary>
        private static string Cursor()
        {
            Texture2D shown = Kingmaker.UI.AbilityTarget.PCCursor.Instance?.Background?.sprite?.texture;
            var cursors = Game.Instance.BlueprintRoot.Cursors;
            string name = typeof(Kingmaker.Blueprints.Root.CursorRoot).GetFields()
                .Where(f => f.FieldType == typeof(Texture2D) && (Texture2D)f.GetValue(cursors) == shown)
                .Select(f => f.Name)
                .FirstOrDefault() ?? shown?.name ?? "none";
            UnitEntityData hovered = Game.Instance.State.Units.FirstOrDefault(u => u.View != null && u.View.MouseHighlighted);
            return $"cursor={name} hovered={(hovered == null ? "none" : $"{hovered.CharacterName} ({WardFixtures.IdOf(hovered) ?? "not scenery"})")} pointerMode={Game.Instance.ClickEventsController?.Mode} inCombat={Game.Instance.Player.IsInCombat}";
        }

        /// <summary>Read only: every prepared slot by level, its chosen spell and whether it is ready to cast.</summary>
        private static string Slots(UnitEntityData unit)
        {
            var sb = new StringBuilder($"Slots of {unit.CharacterName} at game time {Game.Instance.Player.GameTime.TotalSeconds:0}:\n");
            foreach (Spellbook book in unit.Descriptor.Spellbooks)
            {
                sb.AppendLine($"  {book.Blueprint.name} spontaneous={book.Blueprint.Spontaneous} max={book.MaxSpellLevel}");
                for (int level = 1; level <= book.MaxSpellLevel; level++)
                {
                    string slots = book.Blueprint.Spontaneous
                        ? $"{book.GetSpontaneousSlots(level)}/{book.GetSpellsPerDay(level)} left"
                        : string.Join(", ", book.GetMemorizedSpellSlots(level).Select(s => s.Spell == null ? "(empty)" : $"{s.Spell.Blueprint.name}{(s.Available ? "" : " [spent]")}").ToArray());
                    sb.AppendLine($"    L{level}: {slots}");
                }
            }
            return sb.ToString();
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
            unit = target.StartsWith("party:") ? Member(target.Substring(6)) : target.StartsWith("body:") ? BodyOf(target.Substring(5)) : WardFixtures.Find(target);
            return unit == null ? $"no target {target}" : null;
        }

        /// <summary>The restored or dead body that replaced a statue, by the statue's id.</summary>
        private static UnitEntityData BodyOf(string statueId)
        {
            SanctumState state = WardRuntime.State;
            return state != null && state.Statues.TryGetValue(statueId, out StatueState statue) ? WardStatues.Body(statue) : null;
        }

        /// <summary>Read only: whether the native targeting rules let this caster aim this ability at the target. Nothing is granted or cast.</summary>
        private static string CanTarget(UnitEntityData caster, string blueprintName, string target)
        {
            BlueprintAbility ability = ResourcesLibrary.GetBlueprints<BlueprintAbility>().FirstOrDefault(a => a.name == blueprintName);
            if (ability == null)
            {
                return $"no ability named {blueprintName}";
            }
            string missing = Resolve(target, out UnitEntityData victim);
            if (missing != null)
            {
                return missing;
            }
            var data = new AbilityData(ability, caster.Descriptor);
            return $"READ canTarget {ability.name} by {caster.CharacterName} on {target} ({victim.CharacterName}): {data.CanTarget(victim)}; enemy={victim.IsPlayersEnemy} faction={victim.Descriptor.Faction.name}.";
        }

        /// <summary>Read only: where a fixture, body or party member stands in game-window pixels, for aiming real clicks.</summary>
        private static string ScreenPoint(string target)
        {
            string missing = Resolve(target, out UnitEntityData unit);
            if (missing != null)
            {
                return missing;
            }
            Vector3 p = Camera.main.WorldToScreenPoint(unit.Position);
            return $"READ screen {target}: x={p.x:0} y={Screen.height - p.y:0} onScreen={p.z > 0 && p.x >= 0 && p.x <= Screen.width && p.y >= 0 && p.y <= Screen.height} ({Screen.width}x{Screen.height}).";
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

        private static string Items()
        {
            var sb = new StringBuilder();
            foreach (Kingmaker.Items.ItemEntity item in Game.Instance.Player.Inventory.Where(i => i.Blueprint == WardBlueprints.PalmImpression || i.Blueprint == WardBlueprints.BrandedHand || i.Blueprint.name.Contains("Flail")))
            {
                sb.AppendLine($"{item.Blueprint.name} count={item.Count} cost={item.Blueprint.Cost} notable={item.Blueprint.IsNotable}");
            }
            return sb.Length == 0 ? "no credential items held" : sb.ToString();
        }

        /// <summary>Independent physical hits on the A2 proxy, each its own rule, to read the hardness pipeline.</summary>
        private static string HitDoor(int[] amounts)
        {
            UnitEntityData attacker = Game.Instance.Player.MainCharacter.Value;
            var sb = new StringBuilder();
            foreach (int amount in amounts)
            {
                int before = WardRuntime.State.Door.HitPoints;
                var hit = new Kingmaker.RuleSystem.Rules.Damage.PhysicalDamage(new Kingmaker.RuleSystem.DiceFormula(0, Kingmaker.RuleSystem.DiceType.Zero), Kingmaker.Enums.Damage.PhysicalDamageForm.Bludgeoning);
                hit.AddBonus(amount);
                Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage(attacker, WardFixtures.Door, new Kingmaker.RuleSystem.Rules.Damage.DamageBundle(hit)));
                sb.AppendLine($"INJECTED ranged hit {amount}: door {before} -> {WardRuntime.State.Door.HitPoints}");
            }
            return sb.ToString();
        }

        private static string DamageKrune(int amount)
        {
            UnitEntityData krune = WardFixtures.Krune;
            int before = krune.Stats.TemporaryHitPoints.ModifiedValue;
            var hit = new Kingmaker.RuleSystem.Rules.Damage.DirectDamage(new Kingmaker.RuleSystem.DiceFormula(0, Kingmaker.RuleSystem.DiceType.Zero), amount);
            // The party cannot harm a peaceful proxy, so the labelled damage comes from the receiver itself.
            Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage(krune, krune, new Kingmaker.RuleSystem.Rules.Damage.DamageBundle(hit)));
            krune.Damage = 0;
            WardKrune.Refresh(WardRuntime.State, WardRuntime.Now);
            return $"INJECTED {amount} damage to the dormant Krune receiver: temp HP {before} -> {krune.Stats.TemporaryHitPoints.ModifiedValue}; ledger exposed {FeederRules.ExposedTemporaryHP(WardRuntime.State.Door.Feeder, WardRuntime.Now)}.";
        }

        /// <summary>
        /// A native buff on a fixture with an exact duration, cast by the first party member, for the sustained-route
        /// boundaries: the engine applies, times and expires it; only its length is chosen here.
        /// </summary>
        private static string Condition(string fixtureId, string buffName, double seconds)
        {
            UnitEntityData fixture = WardFixtures.Find(fixtureId);
            Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff buff = ResourcesLibrary.GetBlueprints<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>().FirstOrDefault(b => b.name == buffName);
            if (fixture == null || buff == null)
            {
                return $"no fixture {fixtureId} or buff {buffName}";
            }
            Kingmaker.UnitLogic.Buffs.Buff applied = fixture.Descriptor.AddBuff(buff, Game.Instance.Player.Party[0], TimeSpan.FromSeconds(seconds));
            return $"INJECTED native {buffName} on {fixtureId} for {seconds} s at {WardRuntime.Now:0.00}: applied={applied != null} ends={(applied == null ? "-" : applied.EndTime.TotalSeconds.ToString("0.00"))}.";
        }

        /// <summary>Removes a native buff early, as a break-free or dispel would: the interruption boundary.</summary>
        private static string Uncondition(string fixtureId, string buffName)
        {
            UnitEntityData fixture = WardFixtures.Find(fixtureId);
            Kingmaker.UnitLogic.Buffs.Buff buff = fixture?.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint.name == buffName);
            if (buff == null)
            {
                return $"no {buffName} on {fixtureId}";
            }
            buff.Remove();
            return $"INJECTED removal of {buffName} from {fixtureId} at {WardRuntime.Now:0.00}.";
        }

        /// <summary>A separate, unrelated temporary-HP source on Krune, to show the feeder ledger ignores it.</summary>
        private static string Shield(int amount)
        {
            UnitEntityData krune = WardFixtures.Krune;
            krune.Stats.TemporaryHitPoints.AddModifier(amount, null, Kingmaker.Enums.ModifierDescriptor.UntypedStackable);
            return $"INJECTED unrelated {amount} temporary HP on Krune; total {krune.Stats.TemporaryHitPoints.ModifiedValue}.";
        }

        private static string Units()
        {
            var sb = new StringBuilder();
            foreach (UnitEntityData unit in Game.Instance.State.Units.Where(u => u.Get<WardRestoredPart>() != null))
            {
                Vector3 p = unit.Position;
                sb.AppendLine($"{unit.Get<WardRestoredPart>().StatueId} '{unit.CharacterName}' {unit.Blueprint.name} id={unit.UniqueId} dead={unit.Descriptor.State.IsDead} hp={unit.Descriptor.HPLeft}/{unit.Stats.HitPoints.ModifiedValue} ac={unit.Stats.AC.ModifiedValue} str={unit.Stats.Strength.ModifiedValue} dex={unit.Stats.Dexterity.ModifiedValue} con={unit.Stats.Constitution.ModifiedValue} wis={unit.Stats.Wisdom.ModifiedValue} dexDmg={unit.Stats.Dexterity.Damage} dexDrain={unit.Stats.Dexterity.Drain} fort={unit.Stats.SaveFortitude.ModifiedValue} level={unit.Descriptor.Progression.CharacterLevel} align={unit.Descriptor.Alignment.Value} weapon={unit.Body.PrimaryHand.MaybeWeapon?.Blueprint.name} armor={unit.Body.Armor.MaybeArmor?.Blueprint.name} buffs=[{string.Join(",", unit.Buffs.Enumerable.Select(b => b.Blueprint.name).ToArray())}] brand={unit.Descriptor.HasFact(WardBlueprints.SihedronBrand)} grid=({p.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX:0.00},{SanctumPlan.OriginY - p.z / SanctumPlan.MetersPerSquare:0.00}) inCombat={unit.IsInCombat} commands=[{string.Join(",", unit.Commands.Raw.Where(c => c != null).Select(c => c.GetType().Name).ToArray())}]");
            }
            return sb.Length == 0 ? "no restored units" : sb.ToString();
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
