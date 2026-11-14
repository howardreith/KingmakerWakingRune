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
    /// <summary>
    /// SETUP controlled roll: the next native d20 rolled by one fixture (its saving throw) comes up as given. The rule
    /// itself, the bonus, the DC and every consequence stay native. Fires once, then disarms.
    /// </summary>
    [Harmony12.HarmonyPatch(typeof(Kingmaker.RuleSystem.Rules.RuleRollD20), "Roll")]
    internal static class ControlledRoll
    {
        private static string s_FixtureId;
        private static int s_Value;

        public static string Arm(string fixtureId, int value)
        {
            if (WardFixtures.Find(fixtureId) == null || value < 1 || value > 20)
            {
                return $"no fixture {fixtureId} or no d20 value {value}";
            }
            s_FixtureId = fixtureId;
            s_Value = value;
            return $"SETUP controlled roll armed: the next d20 rolled by {fixtureId} will be {value}.";
        }

        private static void Prefix(Kingmaker.RuleSystem.Rules.RuleRollD20 __instance)
        {
            if (s_FixtureId == null || WardFixtures.IdOf(__instance.Initiator) != s_FixtureId)
            {
                return;
            }
            Harmony12.Traverse.Create(__instance).Field("m_PreRolledResult").SetValue((int?)s_Value);
            Log.Info($"SETUP controlled roll used: {s_FixtureId} d20 = {s_Value}.");
            s_FixtureId = null;
        }
    }

    /// <summary>
    /// INJECTED ordering fixture: armed on one member, it waits for the native rule by which restoration heals that member's
    /// Intelligence, and in the same rule call delivers her slam's drain for a fixed amount through the production drain path,
    /// so no periodic recovery check can run in between. Fires once, then disarms.
    /// </summary>
    internal sealed class SlamAfterRestoration : Kingmaker.PubSubSystem.IGlobalRulebookHandler<Kingmaker.RuleSystem.Rules.RuleHealStatDamage>
    {
        private static SlamAfterRestoration s_Armed;
        private UnitEntityData m_Target;
        private int m_Points;
        private bool m_Fired;

        public static string Arm(UnitEntityData target, int points)
        {
            if (s_Armed != null)
            {
                Kingmaker.PubSubSystem.EventBus.Unsubscribe(s_Armed);
            }
            s_Armed = new SlamAfterRestoration { m_Target = target, m_Points = points };
            Kingmaker.PubSubSystem.EventBus.Subscribe(s_Armed);
            return $"INJECTED armed: the next native Intelligence heal on {target.CharacterName} is followed at once by a {points}-point slam drain.";
        }

        public void OnEventAboutToTrigger(Kingmaker.RuleSystem.Rules.RuleHealStatDamage evt)
        {
        }

        public void OnEventDidTrigger(Kingmaker.RuleSystem.Rules.RuleHealStatDamage evt)
        {
            if (m_Fired || evt.Target != m_Target || evt.StatType != Kingmaker.EntitySystem.Stats.StatType.Intelligence || evt.HealedDrain <= 0)
            {
                return;
            }
            // Not unsubscribed here, inside the bus's own dispatch; the next Arm unsubscribes it.
            m_Fired = true;
            var stat = m_Target.Stats.Intelligence;
            KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.Note($"INJECTED ordering fixture: native heal on {m_Target.CharacterName} (all {evt.HealAll}, drain healed {evt.HealedDrain}); before the slam: Intelligence {stat.ModifiedValue}, drain {stat.Drain}, comatose {m_Target.Buffs.GetBuff(KingmakerWakingRune.Sanctum.Encounter.KurshuBlueprints.MindDrained) != null}.");
            UnitEntityData kurshu = KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.Kurshu ?? evt.Initiator;
            KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.DrainIntelligence(kurshu, m_Target, new Kingmaker.RuleSystem.DiceFormula(m_Points, Kingmaker.RuleSystem.DiceType.One), "INJECTED slam right after restoration on");
            KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.Note($"INJECTED ordering fixture result: {m_Target.CharacterName} Intelligence {stat.ModifiedValue}, drain {stat.Drain}, comatose {m_Target.Buffs.GetBuff(KingmakerWakingRune.Sanctum.Encounter.KurshuBlueprints.MindDrained) != null}.");
        }
    }

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
                    return Perform(args[1], args[2], args.Length > 3 ? Member(args[3]) : null);
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
                case "kurshudefeat":
                    return "INJECTED Kurshu defeat fixture: " + RitualRules.DefeatKurshu(WardRuntime.State.Ritual, WardRuntime.Now);
                case "kurshu":
                {
                    // Read only: the encounter's saved state and her live sheet.
                    UnitEntityData kurshu = KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.Kurshu;
                    return "READ Kurshu state: " + WardStore.Dump(WardRuntime.State?.Kurshu) + "\n" + (kurshu == null ? "no Kurshu entity" : KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.Report(kurshu));
                }
                case "hurt":
                    return Hurt(Member(args[1]), args[2], int.Parse(args[3]));
                case "blueprint":
                    return DumpBlueprint(args[1]);
                case "catalog":
                    return Catalog(args[1]);
                case "a4ai":
                    // Read only: the latest brain scores, optionally for one defender.
                    return "READ A4 brain scores:\n" + string.Join("\n", KingmakerWakingRune.Sanctum.Lashmistress.LashmistressEncounter.LastScores
                        .Where(p => args.Length < 2 || p.Key.IndexOf(args[1], StringComparison.OrdinalIgnoreCase) >= 0)
                        .OrderBy(p => p.Key).Select(p => p.Key + " = " + p.Value).ToArray());
                case "a4":
                {
                    // Read only: the A4 state and every defender's live sheet.
                    var sb = new StringBuilder("READ A4 state: " + WardStore.Dump(WardRuntime.State?.Lashmistress) + "\n");
                    sb.AppendLine($"ritual: aware {WardRuntime.State?.Ritual.VandianaAware} doorHeard {WardRuntime.State?.Ritual.A4DoorHeard} confrontation {WardRuntime.State?.Ritual.ConfrontationStarted} roll {WardRuntime.State?.Ritual.ConfrontationRoll} deadline {WardRuntime.State?.Ritual.ConfrontationDeadline:0} global {WardRuntime.State?.Ritual.GlobalDeadline:0} due {WardRuntime.State?.Ritual.AwakeningDue} reason {WardRuntime.State?.Ritual.Reason} now {WardRuntime.Now:0}");
                    sb.AppendLine($"fixture Vandiana: indexed {WardFixtures.Find("Vandiana") != null}, entity {WardFixtures.FindRetired("Vandiana")?.UniqueId ?? "none"} inGame {WardFixtures.FindRetired("Vandiana")?.IsInGame}; resolver -> {WardFixtures.Vandiana?.UniqueId} ({WardFixtures.Vandiana?.Blueprint.name})");
                    foreach (string key in KingmakerWakingRune.Sanctum.Lashmistress.DefenderKeys.All)
                    {
                        if (args.Length < 2 || key.EndsWith(args[1], StringComparison.OrdinalIgnoreCase))
                        {
                            sb.AppendLine(KingmakerWakingRune.Sanctum.Lashmistress.LashmistressEncounter.Report(KingmakerWakingRune.Sanctum.Lashmistress.LashmistressEncounter.Find(key)));
                        }
                    }
                    return sb.ToString();
                }
                case "a4cast":
                    return A4Cast(args[1], args[2], args.Skip(3).ToArray());
                case "a4attack":
                {
                    // INJECTED decision: a defender attacks this target with its own weapons; every roll and effect is native.
                    UnitEntityData unit = Defender(args[1]);
                    string missing = Resolve(args[2], out UnitEntityData victim);
                    if (unit == null || missing != null)
                    {
                        return missing ?? $"no defender {args[1]}";
                    }
                    unit.Commands.InterruptAiCommands();
                    unit.Commands.Run(new UnitAttack(victim));
                    return $"INJECTED decision: {unit.CharacterName} attacks {victim.CharacterName}; the attacks and their effects are native.";
                }
                case "a4order":
                {
                    // HARNESS-COMMANDED: an order to a defender the party now controls directly, dispatched as a command (not UI input).
                    UnitEntityData unit = Defender(args[1]);
                    string missing = Resolve(args[2], out UnitEntityData victim);
                    if (unit == null || missing != null)
                    {
                        return missing ?? $"no defender {args[1]}";
                    }
                    if (!unit.IsDirectlyControllable)
                    {
                        return $"{unit.CharacterName} is not under the player's direct control (faction {unit.Descriptor.Faction?.name}).";
                    }
                    unit.Commands.Run(new UnitAttack(victim));
                    return $"HARNESS-COMMANDED order: {unit.CharacterName} (dominated) attacks {victim.CharacterName}.";
                }
                case "areas":
                {
                    // Read only: every live area effect, and for each party member and defender whether its shape holds them
                    // and whether the fog-of-war line of sight from its centre is blocked (the native inclusion test).
                    var sb = new StringBuilder("READ area effects:\n");
                    foreach (AreaEffectEntityData area in Game.Instance.State.AreaEffects)
                    {
                        var view = area.View;
                        Vector3 centre = area.Position;
                        sb.AppendLine($"  {area.Blueprint.name} {area.UniqueId} view {view != null} shape {view?.Shape != null} ended {area.IsEnded} at ({centre.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX:0.0},{SanctumPlan.OriginY - centre.z / SanctumPlan.MetersPerSquare:0.0}) y {centre.y:0.00}");
                        if (view?.Shape == null)
                        {
                            continue;
                        }
                        var found = new System.Collections.Generic.List<Kingmaker.EntitySystem.EntityDataBase>();
                        Bounds bounds = view.Shape.GetBounds();
                        Game.Instance.CurrentScene.Area.InteractiveObjectGrid.GetInBounds(bounds, found);
                        sb.AppendLine($"    grid search in bounds {bounds.center} size {bounds.size}: {found.Count} entities [{string.Join(", ", found.OfType<UnitEntityData>().Select(u => u.CharacterName).ToArray())}]");
                        foreach (UnitEntityData u in Game.Instance.State.Units.Where(u => u.IsInGame && (u.IsPlayerFaction || KingmakerWakingRune.Sanctum.Lashmistress.LashmistressEncounter.IsDefender(u))))
                        {
                            sb.AppendLine($"    {u.CharacterName}: in shape {view.Shape.Contains(u.Position, u.View.Corpulence)}, sight blocked {Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance.HasObstacle(view.transform.position, u.Position)}, {Vector3.Distance(centre, u.Position):0.0} m, unit y {u.Position.y:0.00}");
                        }
                    }
                    return sb.ToString();
                }
                case "setnonlethal":
                {
                    // INJECTED negative control: the game's own nonlethal total set directly, standing for damage from another
                    // source (another campaign mechanic or mod). It carries no whip ledger, so this mod must leave it alone.
                    string missing = Resolve(args[1], out UnitEntityData unit);
                    if (missing != null)
                    {
                        return missing;
                    }
                    unit.Stats.DamageNonLethal.BaseValue = int.Parse(args[2]);
                    return $"INJECTED foreign nonlethal on {unit.CharacterName}: total {unit.Stats.DamageNonLethal.BaseValue}, whip's share {KingmakerWakingRune.Sanctum.Lashmistress.WhipNonlethal.Owned(unit.Descriptor)}, hp {unit.HPLeft}/{unit.Stats.HitPoints.ModifiedValue}.";
                }
                case "rest":
                {
                    // INJECTED: the game's own per-unit rest application (what a camp rest runs for each member), on one unit.
                    string missing = Resolve(args[1], out UnitEntityData unit);
                    if (missing != null)
                    {
                        return missing;
                    }
                    int before = unit.Stats.DamageNonLethal.BaseValue;
                    Kingmaker.Controllers.Rest.RestController.ApplyRest(unit.Descriptor);
                    return $"INJECTED native rest on {unit.CharacterName}: nonlethal {before} -> {unit.Stats.DamageNonLethal.BaseValue}, whip's share {KingmakerWakingRune.Sanctum.Lashmistress.WhipNonlethal.Owned(unit.Descriptor)}.";
                }
                case "damage":
                {
                    // INJECTED: lethal damage through the native rule, from the creature itself (no one engages), to set a test's
                    // starting hit points.
                    string missing = Resolve(args[1], out UnitEntityData unit);
                    if (missing != null)
                    {
                        return missing;
                    }
                    var hit = new Kingmaker.RuleSystem.Rules.Damage.DirectDamage(new Kingmaker.RuleSystem.DiceFormula(0, Kingmaker.RuleSystem.DiceType.Zero), int.Parse(args[2]));
                    Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage(unit, unit, new Kingmaker.RuleSystem.Rules.Damage.DamageBundle(hit)));
                    return $"INJECTED {args[2]} lethal damage on {unit.CharacterName}: hp {unit.HPLeft}/{unit.Stats.HitPoints.ModifiedValue}.";
                }
                case "armdamage":
                {
                    // INJECTED timing fixture: waits until this unit's spell is being cast (its command running, the spell not
                    // yet released), then in that frame deals lethal damage through the native rule from the unit itself, so
                    // the game's own concentration check on damage takes place. Fires once.
                    string missing = Resolve(args[1], out UnitEntityData unit);
                    if (missing != null)
                    {
                        return missing;
                    }
                    int amount = int.Parse(args[2]);
                    KingmakerWakingRune.Sanctum.Wards.WardRuntime.TestHook = () =>
                    {
                        if (!(unit.Commands.Standard is UnitUseAbility cast) || !cast.IsRunning || cast.IsActed)
                        {
                            return;
                        }
                        KingmakerWakingRune.Sanctum.Wards.WardRuntime.TestHook = null;
                        var hit = new Kingmaker.RuleSystem.Rules.Damage.DirectDamage(new Kingmaker.RuleSystem.DiceFormula(0, Kingmaker.RuleSystem.DiceType.Zero), amount);
                        Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage(unit, unit, new Kingmaker.RuleSystem.Rules.Damage.DamageBundle(hit)));
                        Log.Info($"INJECTED timing fixture: {amount} damage on {unit.CharacterName} while casting {cast.Spell.Blueprint.name} (not yet released); hp {unit.HPLeft}.");
                    };
                    return $"INJECTED timing fixture armed: {amount} damage on {unit.CharacterName} when a spell of theirs is next being cast.";
                }
                case "armcontrol":
                {
                    // INJECTED timing fixture: waits for the director's own command to be running for this defender, then in the
                    // same tick applies a native control buff (as a failed save would), so the director's cancellation of a
                    // running command can be observed. Fires once.
                    UnitEntityData unit = Defender(args[1]);
                    string key = KingmakerWakingRune.Sanctum.Lashmistress.LashmistressEncounter.KeyOf(unit);
                    var buff = ResourcesLibrary.GetBlueprints<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>().FirstOrDefault(b => b.name == args[2]);
                    if (key == null || buff == null)
                    {
                        return $"no defender {args[1]} or buff {args[2]}";
                    }
                    double seconds = double.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
                    UnitEntityData caster = Member(args.Length > 4 ? args[4] : "0");
                    KingmakerWakingRune.Sanctum.Lashmistress.LashmistressEncounter.TestHook = () =>
                    {
                        if (!KingmakerWakingRune.Sanctum.Lashmistress.LashmistressEncounter.DirectorCommandRunning(key))
                        {
                            return;
                        }
                        KingmakerWakingRune.Sanctum.Lashmistress.LashmistressEncounter.TestHook = null;
                        var running = unit.Commands.Raw.Where(c => c != null && !c.IsFinished).Select(c => c.GetType().Name + (c is UnitUseAbility u ? ":" + u.Spell.Blueprint.name : string.Empty)).ToArray();
                        var applied = unit.Descriptor.AddBuff(buff, caster, TimeSpan.FromSeconds(seconds));
                        KingmakerWakingRune.Sanctum.Lashmistress.LashmistressEncounter.Note($"INJECTED timing fixture: {args[2]} applied to {unit.CharacterName} ({applied != null}) while the director's command ran [{string.Join(", ", running)}]; still running after the buff: {unit.Commands.Raw.Count(c => c != null && !c.IsFinished)}.");
                    };
                    return $"INJECTED timing fixture armed: {args[2]} on {unit.CharacterName} when the director next has a command running.";
                }
                case "ai":
                    // HARNESS-COMMANDED: sets the per-character AI property the portrait toggle sets (not UI input).
                    Member(args[1]).IsAIEnabled = args[2] == "on";
                    return $"HARNESS-COMMANDED setting: {Member(args[1]).CharacterName} AI {(Member(args[1]).IsAIEnabled ? "on" : "off")}.";
                case "buff":
                {
                    // INJECTED: a native buff on any unit, cast by a party member, for a chosen time: a failed save's effect is
                    // applied directly (no roll). What the buff does from then on (a change of faction, a condition, its expiry)
                    // is the game's own.
                    string missing = Resolve(args[1], out UnitEntityData unit);
                    Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff buff = ResourcesLibrary.GetBlueprints<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>().FirstOrDefault(b => b.name == args[2]);
                    if (missing != null || buff == null)
                    {
                        return missing ?? $"no buff {args[2]}";
                    }
                    UnitEntityData caster = Member(args.Length > 4 ? args[4] : "0");
                    Kingmaker.UnitLogic.Buffs.Buff applied = unit.Descriptor.AddBuff(buff, caster, TimeSpan.FromSeconds(double.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture)));
                    return $"INJECTED native {args[2]} on {unit.CharacterName} from {caster.CharacterName} for {args[3]} s: applied {applied != null}; faction now {unit.Descriptor.Faction?.name}.";
                }
                case "unbuff":
                {
                    // INJECTED: one native buff removed from any unit, as its expiry, a break-free or a dispel would.
                    string missing = Resolve(args[1], out UnitEntityData unit);
                    Kingmaker.UnitLogic.Buffs.Buff buff = unit?.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint.name == args[2]);
                    if (missing != null || buff == null)
                    {
                        return missing ?? $"no {args[2]} on {args[1]}";
                    }
                    buff.Remove();
                    return $"INJECTED removal of {args[2]} from {unit.CharacterName} at {WardRuntime.Now:0.0}.";
                }
                case "a4take":
                {
                    // INJECTED: one potion is taken from a defender before the fight, for the unavailable-preparation check.
                    UnitEntityData unit = Defender(args[1]);
                    Kingmaker.Items.ItemEntity item = unit?.Body.QuickSlots.Select(s => s.MaybeItem).FirstOrDefault(i => i != null && i.Blueprint.name == args[2]);
                    if (item == null)
                    {
                        return $"no {args[2]} in {args[1]}'s quick slots";
                    }
                    unit.Inventory.Remove(item);
                    return $"INJECTED removal: {args[2]} taken from {unit.CharacterName}; left {unit.Body.QuickSlots.Select(s => s.MaybeItem).Count(i => i != null && i.Blueprint.name == args[2])}.";
                }
                case "give":
                {
                    // SETUP: an item placed in the shared inventory (a member equips it as a player would).
                    Kingmaker.Blueprints.Items.BlueprintItem blueprint = ResourcesLibrary.GetBlueprints<Kingmaker.Blueprints.Items.BlueprintItem>().FirstOrDefault(b => b.name == args[1]);
                    if (blueprint == null)
                    {
                        return $"no item {args[1]}";
                    }
                    Game.Instance.Player.Inventory.Add(blueprint, args.Length > 2 ? int.Parse(args[2]) : 1);
                    return $"SETUP granted item {blueprint.name} to the party's inventory.";
                }
                case "equip":
                {
                    // HARNESS-COMMANDED: a member wields an item from the shared inventory in the primary hand (not UI input).
                    UnitEntityData member = Member(args[1]);
                    Kingmaker.Items.ItemEntity item = Game.Instance.Player.Inventory.FirstOrDefault(i => i.Blueprint.name == args[2]);
                    if (item == null)
                    {
                        return $"no {args[2]} in the party's inventory";
                    }
                    bool ok = member.Body.PrimaryHand.CanInsertItem(item);
                    if (ok)
                    {
                        member.Body.PrimaryHand.InsertItem(item);
                    }
                    return $"HARNESS-COMMANDED equip: {member.CharacterName} wields {args[2]}: {ok}.";
                }
                case "nonlethal":
                {
                    // Read only: every creature with nonlethal damage, and its hit points.
                    var sb = new StringBuilder("READ nonlethal:\n");
                    foreach (UnitEntityData u in Game.Instance.State.Units.Where(u => u.Stats.DamageNonLethal.BaseValue > 0 || u.IsPlayerFaction))
                    {
                        sb.AppendLine($"  {u.CharacterName}: nonlethal {u.Stats.DamageNonLethal.BaseValue}, hp {u.HPLeft}/{u.Stats.HitPoints.ModifiedValue}, lethal {u.Damage}, life {u.Descriptor.State.LifeState}, AC armor {KingmakerWakingRune.Sanctum.Lashmistress.WhipNonlethal.ArmorBonus(u)} natural {KingmakerWakingRune.Sanctum.Lashmistress.WhipNonlethal.NaturalArmorBonus(u)}");
                    }
                    return sb.ToString();
                }
                case "builds":
                    return Builds();
                case "grab":
                {
                    // INJECTED: a tail hit on this member is supplied; whether she may grab, the roll and the hold are hers.
                    UnitEntityData kurshu = KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.Kurshu;
                    if (kurshu == null)
                    {
                        return "no Kurshu entity";
                    }
                    KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.TryGrab(kurshu, Member(args[1]));
                    return $"INJECTED tail hit on {Member(args[1]).CharacterName}; the grab rule decided (see the log); held now: '{WardRuntime.State?.Kurshu.HeldName}'.";
                }
                case "drain":
                {
                    // INJECTED: her slam's drain, for a fixed amount, on this member; the native rule and the comatose policy apply.
                    UnitEntityData kurshu = KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.Kurshu;
                    UnitEntityData member = Member(args[1]);
                    if (kurshu == null)
                    {
                        return "no Kurshu entity";
                    }
                    KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.DrainIntelligence(kurshu, member, new Kingmaker.RuleSystem.DiceFormula(int.Parse(args[2]), Kingmaker.RuleSystem.DiceType.One), "INJECTED drain fixture on");
                    var coma = member.Buffs.GetBuff(KingmakerWakingRune.Sanctum.Encounter.KurshuBlueprints.MindDrained)?.SelectComponents<KingmakerWakingRune.Sanctum.Encounter.KwrMindDrained>().FirstOrDefault();
                    return $"INJECTED drain {args[2]} on {member.CharacterName}: Intelligence {member.Stats.Intelligence.ModifiedValue} (raw {member.Stats.Intelligence.ModifiedValueRaw}, drain {member.Stats.Intelligence.Drain}), life {member.Descriptor.State.LifeState}, helpless {member.Descriptor.State.IsHelpless}, comatose {coma != null}{(coma == null ? string.Empty : $" (held back {coma.Pending}, visible drain when held back {coma.DrainWhenHeldBack})")}.";
                }
                case "armslam":
                    return SlamAfterRestoration.Arm(Member(args[1]), int.Parse(args[2]));
                case "kurshufixture":
                    return KurshuFixture(int.Parse(args[1]), args.Skip(2).ToArray());
                case "combatsave":
                    return CombatSave(args[1]);
                case "paused":
                    return $"READ paused={Game.Instance.IsPaused} mode={Game.Instance.CurrentMode} turnBased={TurnBased.Controllers.CombatController.IsInTurnBasedCombat()} time={WardRuntime.Now:0.0}";
                case "corpse":
                    return Corpse(args[1], args.Length > 2 ? float.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture) : 3f);
                case "items":
                    return Items();
                case "hitdoor":
                    return HitDoor(args.Skip(1).Select(int.Parse).ToArray());
                case "krune":
                    return KruneReport();
                case "krunesheet":
                    // Read only: the actor's native statistics.
                    return "READ " + KingmakerWakingRune.Sanctum.Krune.KruneAwakening.Sheet(KingmakerWakingRune.Sanctum.Krune.KruneAwakening.Actor);
                case "concealment":
                {
                    // Read only: the game's own concealment rule (with the darkness patch) for one attacker against one target,
                    // as an attack roll asks it; the darkness answer and its reason go to the log.
                    string a = Resolve(args[1], out UnitEntityData attacker), t = Resolve(args[2], out UnitEntityData defender);
                    if (a != null || t != null)
                    {
                        return a ?? t;
                    }
                    var result = Kingmaker.UnitLogic.Parts.UnitPartConcealment.Calculate(attacker, defender, true);
                    return $"READ concealment {attacker.CharacterName} -> {defender.CharacterName}: {result}; target in darkness {defender.Buffs.GetBuff(KingmakerWakingRune.Sanctum.Lashmistress.LashmistressBlueprints.InDarkness) != null}; attacker sees in darkness {KingmakerWakingRune.Sanctum.Lashmistress.KwrDarkvisionSense.Present(attacker)}; distance {attacker.DistanceTo(defender) / SanctumPlan.MetersPerFoot:0} ft.";
                }
                case "krunecl":
                    return KruneCasterProbe(args.Length > 1 ? args[1] : "MageArmor");
                case "krunerelease":
                    return KingmakerWakingRune.Sanctum.Krune.KruneAwakening.ReleaseForDiagnostics();
                case "krunedeadline":
                    return KruneDeadline(double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture));
                case "krunebook":
                {
                    // Read only: his book as it stands (ready slots by level and metamagic), and the one-time setup note.
                    UnitEntityData k = KingmakerWakingRune.Sanctum.Krune.KruneAwakening.Actor;
                    var lines = KingmakerWakingRune.Sanctum.Krune.KruneSpellbook.Ready(k)
                        .GroupBy(s => $"L{s.SpellLevel} {s.Spell.Blueprint.name}{(s.Spell.MetamagicData != null && s.Spell.MetamagicData.NotEmpty ? " [" + s.Spell.MetamagicData.MetamagicMask + "]" : string.Empty)}")
                        .Select(g => g.Key + (g.Count() > 1 ? " x" + g.Count() : string.Empty));
                    return $"READ book: {WardRuntime.State?.Krune.CombatInitializedNote}\n  ready: {string.Join("; ", lines.ToArray())}\n  phase {WardRuntime.State?.Krune.DirectorPhase}, combat since {WardRuntime.State?.Krune.CombatStartedAt:0.0}, walls {WardRuntime.State?.Walls.Count}";
                }
                case "objectroll":
                    return ControlledRoll.Arm(args[1], int.Parse(args[2]));
                case "hitgem":
                {
                    // INJECTED: one physical hit of a fixed amount from a party member on the emerald, through the native damage
                    // rule and the production object pipeline (hardness 20, then its 40 hit points). For a party that cannot
                    // otherwise beat the gem's hardness; the awakening that follows is the production path.
                    UnitEntityData by = Member(args[1]);
                    UnitEntityData gem = WardFixtures.Gem;
                    if (gem == null)
                    {
                        return "no emerald";
                    }
                    var blow = new Kingmaker.RuleSystem.Rules.Damage.PhysicalDamage(new Kingmaker.RuleSystem.DiceFormula(0, Kingmaker.RuleSystem.DiceType.Zero), Kingmaker.Enums.Damage.PhysicalDamageForm.Bludgeoning);
                    blow.AddBonus(int.Parse(args[2]));
                    Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage(by, gem, new Kingmaker.RuleSystem.Rules.Damage.DamageBundle(blow)));
                    return $"INJECTED {args[2]} bludgeoning from {by.CharacterName} on the emerald: {WardRuntime.State.Gem.HitPoints}/{WardProfile.GemHitPoints} left, destroyed {WardRuntime.State.Gem.Destroyed}; awakening due {WardRuntime.State.Ritual.AwakeningDue} ({WardRuntime.State.Ritual.Reason}), stagger {WardRuntime.State.Ritual.AwakeningOpeningStagger}.";
                }
                case "krunepause":
                    // SETUP: the game pauses the moment the awakening reaches this stage, for a save there.
                    KingmakerWakingRune.Sanctum.Krune.KruneAwakening.HarnessPauseAt = (KingmakerWakingRune.Sanctum.Krune.AwakeningStage)Enum.Parse(typeof(KingmakerWakingRune.Sanctum.Krune.AwakeningStage), args[1]);
                    return "SETUP: the game will pause when the awakening reaches " + args[1] + ".";
                case "setmask":
                    return SetMask(int.Parse(args[1]));
                case "manifest":
                    return "READ Krune manifest:\n" + string.Join("\n", KingmakerWakingRune.Sanctum.Krune.KruneManifest.Lines().ToArray());
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
                    return $"HARNESS-COMMANDED move order: {Member(args[1]).CharacterName} to ({args[2]},{args[3]}).";
                case "stock":
                    return Stock();
                case "stockaudit":
                    return StockAudit.Write(args[1]);
                case "deep":
                {
                    BlueprintScriptableObject bp = ResourcesLibrary.LibraryObject.BlueprintsByAssetId.Values.FirstOrDefault(b => b.name == args[1]);
                    if (bp == null)
                    {
                        return $"no blueprint named {args[1]}";
                    }
                    var deep = new StringBuilder($"READ deep {bp.name} ({bp.GetType().Name}, {bp.AssetGuid})\n");
                    foreach (BlueprintComponent c in bp.ComponentsArray)
                    {
                        Deep(deep, c, 1, new System.Collections.Generic.HashSet<object>());
                    }
                    return deep.ToString();
                }
                case "addknown":
                {
                    // SETUP: a spell added to a member's spellbook at the given level (as copying its scroll would).
                    UnitEntityData member = Member(args[1]);
                    BlueprintAbility spell = ResourcesLibrary.GetBlueprints<BlueprintAbility>().FirstOrDefault(a => a.name == args[2]);
                    Spellbook book = member.Descriptor.Spellbooks.FirstOrDefault(b => !b.Blueprint.Spontaneous);
                    if (spell == null || book == null)
                    {
                        return $"no spell {args[2]} or no prepared spellbook";
                    }
                    book.AddKnown(int.Parse(args[3]), spell);
                    return $"SETUP {member.CharacterName} knows {spell.name} at level {args[3]}.";
                }
                case "rodon":
                {
                    // HARNESS-COMMANDED: a carried rod into a free quick slot, and its native metamagic toggle switched on.
                    UnitEntityData member = Member(args[1]);
                    Kingmaker.Items.ItemEntity rod = Game.Instance.Player.Inventory.FirstOrDefault(i => i.Blueprint.name == args[2]);
                    var slot = member.Body.QuickSlots.FirstOrDefault(s => !s.HasItem && s.CanInsertItem(rod));
                    if (rod == null || slot == null)
                    {
                        return $"no {args[2]} carried or no free quick slot";
                    }
                    slot.InsertItem(rod);
                    var toggle = member.Descriptor.ActivatableAbilities.Enumerable.FirstOrDefault(a => a.Blueprint == ((Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipment)rod.Blueprint).ActivatableAbility);
                    if (toggle == null)
                    {
                        return $"HARNESS-COMMANDED {rod.Name} in a quick slot, but no toggle was granted";
                    }
                    toggle.IsOn = true;
                    return $"HARNESS-COMMANDED {member.CharacterName} readies {rod.Name}: toggle {toggle.Blueprint.name} on={toggle.IsOn}, charges {rod.Charges}.";
                }
                case "entity":
                {
                    // Read only: a unit by id prefix among all units the state holds, in game or not.
                    var found = Game.Instance.State.Units.All.Where(u => u.UniqueId.StartsWith(args[1], StringComparison.Ordinal)).ToList();
                    if (found.Count == 0)
                    {
                        return $"READ entity {args[1]}: not held by the state at all";
                    }
                    return string.Join("\n", found.Select(u => $"READ entity {u.UniqueId} '{u.CharacterName}' inGame={u.IsInGame} dead={u.Descriptor.State.IsDead} view={(u.View != null)} viewActive={(u.View != null && u.View.gameObject.activeInHierarchy)} pos=({u.Position.x:0.0},{u.Position.y:0.0},{u.Position.z:0.0}) faction={u.Descriptor.Faction?.name} brain={u.IsBrainActive} commands=[{string.Join(",", u.Commands.Raw.Where(c => c != null).Select(c => c.GetType().Name).ToArray())}] part={(u.Get<WardRestoredPart>() != null)}").ToArray());
                }
                case "statuesave":
                    WardStatues.ForcedSurvival = args[1] == "pass";
                    return $"SETUP the next Stone to Flesh survival save is forced to {(WardStatues.ForcedSurvival.Value ? "pass" : "fail")} (once; the roll is still made and logged).";
                case "slots":
                    return Slots(Member(args[1]));
                case "known":
                {
                    // Read only: every spell each spellbook knows, by level.
                    UnitEntityData member = Member(args[1]);
                    var sb = new StringBuilder($"READ known spells of {member.CharacterName}:\n");
                    foreach (Spellbook book in member.Descriptor.Spellbooks)
                    {
                        for (int level = 0; level <= book.MaxSpellLevel; level++)
                        {
                            sb.AppendLine($"  {book.Blueprint.name} L{level}: {string.Join(", ", book.GetKnownSpells(level).Select(s => s.Blueprint.name).ToArray())}");
                        }
                    }
                    return sb.ToString();
                }
                case "cursor":
                    return Cursor();
                case "spell":
                    return SpellFromBook(Member(args[1]), args[2], args.Skip(3).ToArray());
                case "departure":
                {
                    // READ: the saved departure record (members and number of recorded choices) and the token count.
                    string payload = Game.Instance.Player.MainCharacter.Value?.Descriptor.Get<KingmakerWakingRune.Sanctum.DepartureRecordPart>()?.Payload;
                    string members = payload == null ? "none" : string.Join(", ", KingmakerWakingRune.Sanctum.DepartureRecord.Parse(payload).Values.Select(m => $"{Game.Instance.State.Units.All.FirstOrDefault(u => u.UniqueId == m.Unit)?.CharacterName ?? m.Unit} ({m.Entries.Count})").ToArray());
                    int tokens = Game.Instance.Player.Inventory.Where(i => i.Blueprint.name.Contains("RefugeToken")).Sum(i => i.Count);
                    return $"Departure record: {(payload == null ? "none" : payload.Split('|')[0])}; members: {members}; tokens in inventory: {tokens}.";
                }
                case "token":
                    // The Refuge Token ability's own callback, for when the action bar cannot be clicked.
                    KingmakerWakingRune.Sanctum.RefugeTransit.Request();
                    return "HARNESS-COMMANDED Refuge Token: the ability's own callback was invoked (not UI input).";
                case "tbm":
                    // The rook button's own handler; the setting it flips is restored with the registry at the run's end.
                    UnityEngine.Object.FindObjectOfType<Kingmaker.UI.IngameMenu.IngameMenuController>().OnTBMToggle();
                    return $"SETUP turn-based mode toggled through the rook button's handler: now {Kingmaker.UI.SettingsUI.SettingsRoot.Instance.EnableTurnBasedMode.CurrentValue}.";
                case "endturn":
                {
                    TurnBased.Controllers.TurnController turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
                    if (turn == null || !turn.Unit.IsDirectlyControllable || !turn.CanEndTurn())
                    {
                        return $"no party turn to end (current {turn?.Unit.CharacterName ?? "none"})";
                    }
                    string who = turn.Unit.CharacterName;
                    turn.ForceToEnd();
                    return $"HARNESS-COMMANDED end of {who}'s turn (ForceToEnd, not the button).";
                }
                case "turn":
                {
                    TurnBased.Controllers.TurnController turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
                    return $"READ turn: {turn?.Unit.CharacterName ?? "none"} party={turn?.Unit.IsDirectlyControllable} status={turn?.Status} inTBCombat={TurnBased.Controllers.CombatController.IsInTurnBasedCombat()} round={Game.Instance.TurnBasedCombatController.RoundNumber}.";
                }
                case "answer":
                    return AnswerBox(args[1] == "yes");
                case "memorize":
                    return Memorize(Member(args[1]), args[2], int.Parse(args[3]), args.Length > 4 && args[4] == "pending");
                case "xp":
                    // SETUP: experience granted to a member (the level-up itself is then the player's, through the native screen).
                    Member(args[1]).Descriptor.Progression.GainExperience(int.Parse(args[2]));
                    return $"SETUP {args[2]} experience granted to {Member(args[1]).CharacterName}: now {Member(args[1]).Descriptor.Progression.Experience} (level {Member(args[1]).Descriptor.Progression.CharacterLevel}).";
                case "cantarget":
                    return CanTarget(Member(args[1]), args[2], args[3]);
                case "screen":
                    return args.Length > 2 ? GridScreen(float.Parse(args[1]), float.Parse(args[2])) : ScreenPoint(args[1]);
                case "look":
                    // The camera only, as a player's own scrolling would: no unit, rule or state changes.
                    if (args.Length > 2)
                    {
                        Game.Instance.UI.GetCameraRig().ScrollToImmediately(GridWorld(float.Parse(args[1]), float.Parse(args[2])));
                        return $"CAMERA moved to grid ({args[1]},{args[2]}).";
                    }
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
                case "combat":
                    return CombatRead();
                case "area":
                    return SpawnArea(args[1], float.Parse(args[2]), float.Parse(args[3]), double.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture), args.Length > 5 ? args[5] : "krune", args.Length > 6 ? int.Parse(args[6]) : 0);
                case "place":
                {
                    string missing = Resolve(args[1], out UnitEntityData placed);
                    return missing ?? "SETUP " + Teleport(placed, float.Parse(args[2]), float.Parse(args[3]));
                }
                case "kruneuse":
                {
                    // HARNESS-COMMANDED: Krune uses one of his own ability facts (a native command; its own checks and costs).
                    // "Parent:Variant" picks one variant (his Wish).
                    UnitEntityData krune = WardFixtures.Krune;
                    string[] parts = args[1].Split(':');
                    Kingmaker.UnitLogic.Abilities.Ability fact = krune?.Descriptor.Abilities.Enumerable.FirstOrDefault(a => a.Blueprint.name == parts[0]);
                    if (fact == null)
                    {
                        return $"Krune has no ability {parts[0]}";
                    }
                    AbilityData data = fact.Data;
                    if (parts.Length > 1)
                    {
                        BlueprintAbility variant = fact.Blueprint.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityVariants>()?.Variants.FirstOrDefault(v => v.name == parts[1]);
                        if (variant == null)
                        {
                            return $"{parts[0]} has no variant {parts[1]}";
                        }
                        data = new AbilityData(fact.Data, variant);
                    }
                    TargetWrapper on = krune;
                    if (args.Length > 2 && Resolve(args[2], out UnitEntityData other) == null)
                    {
                        on = other;
                    }
                    else if (args.Length > 3)
                    {
                        on = new TargetWrapper(new Vector3(SanctumPlan.ToWorldX(float.Parse(args[2])), SanctumPlan.HeightAt(float.Parse(args[2]), float.Parse(args[3])) ?? 0f, SanctumPlan.ToWorldZ(float.Parse(args[3]))));
                    }
                    bool can = data.IsAvailableForCast && data.CanTarget(on);
                    if (can)
                    {
                        krune.Commands.Run(new UnitUseAbility(data, on));
                    }
                    return $"HARNESS-COMMANDED Krune uses {args[1]} on {(on.IsUnit ? on.Unit.CharacterName : on.Point.ToString())}: available {data.IsAvailableForCast}, can target {data.CanTarget(on)}{(can ? ", ordered" : ", not ordered")}; bond uses left {krune.Descriptor.Resources.GetResourceAmount(KingmakerWakingRune.Sanctum.Krune.KruneGear.Bond)}.";
                }
                case "takeloot":
                {
                    // INJECTED (focused checks only): everything in Krune's loot bag moved to the party's inventory, in place of
                    // walking a member to it and taking it (the PLAYER way, shown in earlier runs).
                    string id = WardRuntime.State?.Conclusion.LootContainerId;
                    var bag = Game.Instance.State.MapObjects.All.FirstOrDefault(m => m.UniqueId == id)?.View as Kingmaker.View.MapObjects.DroppedLoot;
                    if (bag == null || bag.Loot == null)
                    {
                        return $"no loot bag {id}";
                    }
                    var items = bag.Loot.Items.ToList();
                    foreach (Kingmaker.Items.ItemEntity item in items)
                    {
                        bag.Loot.Transfer(item, Game.Instance.Player.Inventory);
                    }
                    return $"INJECTED loot taken: {items.Count} items from Krune's bag into the party inventory.";
                }
                case "kruneawake":
                    // SETUP control for the off-screen death comparison only: the encounter's keep-awake can be suspended.
                    KingmakerWakingRune.Sanctum.Krune.KruneAwakening.HarnessNoKeepAwake = args[1] == "off";
                    if (args[1] == "off" && WardFixtures.Krune != null)
                    {
                        WardFixtures.Krune.Sleepless = false;
                    }
                    return $"SETUP control: Krune kept awake {(args[1] == "off" ? "SUSPENDED (pre-0.9.20 behaviour)" : "as built")}.";
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

        /// <summary>
        /// INJECTED Feed fixture: a stock creature placed a few meters in front of Kurshu and killed at once, so a real corpse of
        /// a known kind lies within her reach. "deva" (12 HD outsider) and "axiomite" are eligible; "hellhound" (too few Hit Dice)
        /// and "wolf" (not an outsider) are the controls. What she does with it is her own choice and native rules.
        /// </summary>
        private static string Corpse(string kind, float meters)
        {
            string guid;
            switch (kind)
            {
                case "deva": guid = "6af99c59cc046dc42be46ab1ebbf38d5"; break;
                case "axiomite": guid = "a97cc6e80fe9a454db9c0fb519fa4087"; break;
                case "hellhound": guid = "9c72fff76fc033640b747145c7456be6"; break;
                case "wolf": guid = "ea610d9e540af4243b1310a3e6833d9f"; break;
                default: return $"unknown corpse kind {kind}";
            }
            UnitEntityData kurshu = KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.Kurshu;
            if (kurshu == null)
            {
                return "no Kurshu entity";
            }
            Vector3 at = Kingmaker.View.ObstacleAnalyzer.GetNearestNode(kurshu.Position + kurshu.OrientationDirection * meters).clampedPosition;
            UnitEntityData body = Game.Instance.EntityCreator.SpawnUnit(ResourcesLibrary.TryGetBlueprint<BlueprintUnit>(guid), at, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
            body.Descriptor.State.MarkedForDeath = true;
            var facts = KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.Facts(body);
            return $"INJECTED corpse fixture {kind}: {body.CharacterName} {body.UniqueId} at ({at.x:0.0},{at.z:0.0}), {kurshu.DistanceTo(body):0.0} m from Kurshu; outsider {facts.Outsider}, HD {facts.HitDice}, summoned {facts.Summoned}; killed by the harness.";
        }

        /// <summary>
        /// INJECTED damage to Kurshu: one native damage rule of the named kind (acid, fire, cold, physical, magic for a +1
        /// slashing hit, coldironmagic for a +1 cold iron hit), from a party member. Her DR, resistances and regeneration
        /// apply exactly as they would to a real hit; only the hit itself is supplied.
        /// </summary>
        private static string Hurt(UnitEntityData source, string kind, int amount)
        {
            UnitEntityData kurshu = KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.Kurshu;
            if (kurshu == null)
            {
                return "no Kurshu entity";
            }
            Kingmaker.RuleSystem.Rules.Damage.BaseDamage damage;
            var dice = new Kingmaker.RuleSystem.DiceFormula(amount, Kingmaker.RuleSystem.DiceType.One);
            switch (kind)
            {
                case "acid": damage = new Kingmaker.RuleSystem.Rules.Damage.EnergyDamage(dice, Kingmaker.Enums.Damage.DamageEnergyType.Acid); break;
                case "fire": damage = new Kingmaker.RuleSystem.Rules.Damage.EnergyDamage(dice, Kingmaker.Enums.Damage.DamageEnergyType.Fire); break;
                case "cold": damage = new Kingmaker.RuleSystem.Rules.Damage.EnergyDamage(dice, Kingmaker.Enums.Damage.DamageEnergyType.Cold); break;
                default:
                {
                    var physical = new Kingmaker.RuleSystem.Rules.Damage.PhysicalDamage(dice, Kingmaker.Enums.Damage.PhysicalDamageForm.Slashing);
                    if (kind == "magic" || kind == "coldironmagic")
                    {
                        physical.Enchantment = 1;
                        physical.EnchantmentTotal = 1;
                    }
                    if (kind == "coldironmagic")
                    {
                        physical.AddMaterial(Kingmaker.Enums.Damage.PhysicalDamageMaterial.ColdIron);
                    }
                    damage = physical;
                    break;
                }
            }
            int before = kurshu.Descriptor.HPLeft;
            var rule = Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage(source, kurshu, new Kingmaker.RuleSystem.Rules.Damage.DamageBundle(damage)));
            return $"INJECTED {kind} {amount} from {source.CharacterName}: dealt {rule.Damage}; hp {before} -> {kurshu.Descriptor.HPLeft}/{kurshu.Descriptor.MaxHP}; life {kurshu.Descriptor.State.LifeState}.";
        }

        /// <summary>
        /// INJECTED live-state fixture, for the save the game refuses during combat: hit point damage written straight onto
        /// her (no rule, so no combat starts) and uses spent on both the native resource and the ledger, so a living, damaged
        /// Kurshu can be saved outside combat and its persistence checked after a restart.
        /// </summary>
        private static string KurshuFixture(int damage, string[] uses)
        {
            UnitEntityData kurshu = KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.Kurshu;
            SanctumState state = WardRuntime.State;
            if (kurshu == null || state == null)
            {
                return "no Kurshu entity";
            }
            kurshu.Descriptor.Damage += damage;
            var spent = new StringBuilder();
            foreach (string use in uses)
            {
                if (KingmakerWakingRune.Sanctum.Encounter.KurshuRules.Spend(state.Kurshu, use))
                {
                    kurshu.Descriptor.Resources.Spend(KingmakerWakingRune.Sanctum.Encounter.KurshuBlueprints.Resources[use], 1);
                    spent.Append($" {use}->{KingmakerWakingRune.Sanctum.Encounter.KurshuRules.Remaining(state.Kurshu, use)}");
                }
            }
            return $"INJECTED live-state fixture: {damage} damage written to her hit points ({kurshu.Descriptor.HPLeft}/{kurshu.Descriptor.MaxHP}), uses spent:{spent}; in combat {Game.Instance.Player.IsInCombat}.";
        }

        /// <summary>
        /// INJECTED combat pause for a save: the game refuses every save while anyone is in combat, so every unit leaves combat
        /// in this one frame and the save is requested at once. Her hit points and spent uses are whatever the real fight left;
        /// a load re-binds her and native awareness starts the fight again.
        /// </summary>
        private static string CombatSave(string name)
        {
            int left = 0;
            foreach (UnitEntityData unit in Game.Instance.State.Units.Where(u => u.CombatState.IsInCombat).ToList())
            {
                unit.CombatState.LeaveCombat();
                left++;
            }
            Game.Instance.Player.UpdateIsInCombat();
            if (!Game.Instance.SaveManager.IsSaveAllowed())
            {
                return $"INJECTED combat pause: {left} unit(s) left combat, but the game still refuses to save (in combat {Game.Instance.Player.IsInCombat}).";
            }
            var save = Game.Instance.SaveManager.CreateNewSave("KWR_AUTOMATION_" + name);
            Game.Instance.SaveGame(save);
            return $"INJECTED combat pause: {left} unit(s) left combat for one frame; SETUP save requested: '{save.Name}'.";
        }

        /// <summary>Read only: each member's classes, equipment and defenses, then the party's money and shared inventory.</summary>
        private static string Builds()
        {
            var sb = new StringBuilder("READ party builds\n");
            foreach (UnitEntityData u in Game.Instance.Player.Party)
            {
                UnitDescriptor d = u.Descriptor;
                string classes = string.Join(" ", d.Progression.Classes.Select(c => c.CharacterClass.name.Replace("Class", string.Empty) + c.Level).ToArray());
                var weapons = new[] { d.Body.PrimaryHand.MaybeWeapon, d.Body.SecondaryHand.MaybeWeapon }.Where(w => w != null)
                    .Select(w => $"{w.Blueprint.name}(+{w.EnchantmentValue} {string.Join("/", w.Enchantments.Select(e => e.Blueprint.name).ToArray())})");
                string worn = string.Join(", ", d.Body.AllSlots.Where(s => s.HasItem && !(s.Item is Kingmaker.Items.ItemEntityWeapon)).Select(s => s.Item.Blueprint.name).ToArray());
                sb.AppendLine($"{u.CharacterName}: {classes}; BAB {d.Stats.BaseAttackBonus.ModifiedValue} AC {d.Stats.AC.ModifiedValue} hp {d.HPLeft}/{d.MaxHP} Fort {d.Stats.SaveFortitude.ModifiedValue} Ref {d.Stats.SaveReflex.ModifiedValue} Will {d.Stats.SaveWill.ModifiedValue} Str {d.Stats.Strength.ModifiedValue} Dex {d.Stats.Dexterity.ModifiedValue} Int {d.Stats.Intelligence.ModifiedValue} Wis {d.Stats.Wisdom.ModifiedValue} Cha {d.Stats.Charisma.ModifiedValue}");
                sb.AppendLine($"  weapons: {string.Join("; ", weapons.ToArray())}");
                sb.AppendLine($"  worn: {worn}");
                sb.AppendLine($"  books: {string.Join("; ", d.Spellbooks.Select(b => $"{b.Blueprint.name} CL{b.CasterLevel} max L{b.MaxSpellLevel}").ToArray())}");
                sb.AppendLine($"  feats: {string.Join(", ", d.Progression.Features.Enumerable.Where(f => f.Blueprint.Groups.Contains(Kingmaker.Blueprints.Classes.FeatureGroup.Feat) || f.Blueprint.Groups.Contains(Kingmaker.Blueprints.Classes.FeatureGroup.CombatFeat)).Select(f => f.Blueprint.name).ToArray())}");
            }
            sb.AppendLine($"money {Game.Instance.Player.Money}");
            sb.AppendLine("inventory: " + string.Join(", ", Game.Instance.Player.Inventory.Where(i => i.Wielder == null).GroupBy(i => i.Blueprint.name).Select(g => $"{g.Key}x{g.Sum(i => i.Count)}").ToArray()));
            return sb.ToString();
        }

        /// <summary>Read only: a blueprint by name, with its components and their public fields, to check a native implementation.</summary>
        private static string DumpBlueprint(string name)
        {
            BlueprintScriptableObject found = ResourcesLibrary.LibraryObject.GetAllBlueprints().FirstOrDefault(b => b != null && b.name == name);
            if (found == null)
            {
                return $"no blueprint named {name}";
            }
            var sb = new StringBuilder($"READ blueprint {found.name} ({found.GetType().Name}, {found.AssetGuid})\n");
            sb.AppendLine("  " + Fields(found));
            foreach (BlueprintComponent component in found.ComponentsArray ?? new BlueprintComponent[0])
            {
                sb.AppendLine($"  [{component?.GetType().Name}] {(component == null ? string.Empty : Fields(component))}");
            }
            return sb.ToString();
        }

        /// <summary>Read only: the installed game's classes, spellbooks, spell lists, abilities, features, buffs, usable items and units, to a file.</summary>
        private static string Catalog(string path)
        {
            var all = ResourcesLibrary.LibraryObject.GetAllBlueprints().Where(b => b != null).ToList();
            var sb = new StringBuilder();
            foreach (var c in all.OfType<Kingmaker.Blueprints.Classes.BlueprintCharacterClass>())
            {
                sb.AppendLine($"CLASS|{c.name}|{c.AssetGuid}|{Safe(() => c.Name)}|prestige={c.PrestigeClass}|hd={c.HitDie}|bab={c.BaseAttackBonus?.name}|f={c.FortitudeSave?.name}|r={c.ReflexSave?.name}|w={c.WillSave?.name}|book={c.Spellbook?.name}|prog={c.Progression?.name}|arch=[{string.Join(",", (c.Archetypes ?? new Kingmaker.Blueprints.Classes.BlueprintArchetype[0]).Select(a => a.name).ToArray())}]");
                if (c.Progression != null)
                {
                    foreach (var entry in c.Progression.LevelEntries ?? new Kingmaker.Blueprints.Classes.LevelEntry[0])
                    {
                        sb.AppendLine($"  L{entry.Level}: {string.Join(",", entry.Features.Select(f => f.name).ToArray())}");
                    }
                }
            }
            foreach (var book in all.OfType<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook>())
            {
                sb.AppendLine($"BOOK|{book.name}|{book.AssetGuid}|list={book.SpellList?.name}|spontaneous={book.Spontaneous}|stat={book.CastingAttribute}|arcane={book.IsArcane}|perday={book.SpellsPerDay?.name}|known={book.SpellsKnown?.name}|cantrips={book.CantripsType}");
            }
            foreach (var list in all.OfType<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList>())
            {
                sb.AppendLine($"LIST|{list.name}|{list.AssetGuid}|" + string.Join(";", (list.SpellsByLevel ?? new Kingmaker.Blueprints.Classes.Spells.SpellLevelList[0]).Select(l => l.SpellLevel + ":" + string.Join(",", l.Spells.Where(s => s != null).Select(s => s.name).ToArray())).ToArray()));
            }
            foreach (var a in all.OfType<BlueprintAbility>())
            {
                sb.AppendLine($"ABILITY|{a.name}|{a.AssetGuid}|{Safe(() => a.Name)}|{a.Type}|{a.ActionType}|full={a.IsFullRoundAction}|range={a.Range}|variants={a.HasVariants}|school={a.School}|comps=[{string.Join(",", (a.ComponentsArray ?? new BlueprintComponent[0]).Where(x => x != null).Select(x => x.GetType().Name).Distinct().ToArray())}]");
            }
            foreach (var f in all.OfType<Kingmaker.Blueprints.Classes.BlueprintFeature>())
            {
                sb.AppendLine($"FEATURE|{f.name}|{f.AssetGuid}|{Safe(() => f.Name)}|comps=[{string.Join(",", (f.ComponentsArray ?? new BlueprintComponent[0]).Where(x => x != null).Select(x => x.GetType().Name).Distinct().ToArray())}]");
            }
            foreach (var b in all.OfType<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>())
            {
                sb.AppendLine($"BUFF|{b.name}|{b.AssetGuid}|{Safe(() => b.Name)}|comps=[{string.Join(",", (b.ComponentsArray ?? new BlueprintComponent[0]).Where(x => x != null).Select(x => x.GetType().Name).Distinct().ToArray())}]");
            }
            foreach (var i in all.OfType<Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentUsable>())
            {
                sb.AppendLine($"USABLE|{i.name}|{i.AssetGuid}|{Safe(() => i.Name)}|{i.Type}|ability={i.Ability?.name}|cl={i.CasterLevel}|charges={i.Charges}");
            }
            foreach (var w in all.OfType<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>())
            {
                sb.AppendLine($"WEAPON|{w.name}|{w.AssetGuid}|{Safe(() => w.Name)}|type={w.Type?.name}|ench=[{string.Join(",", w.Enchantments.Select(e => e.name).ToArray())}]");
            }
            foreach (var u in all.OfType<BlueprintUnit>())
            {
                sb.AppendLine($"UNIT|{u.name}|{u.AssetGuid}|{Safe(() => u.CharacterName)}|race={u.Race?.name}|size={u.Size}|gender={u.Gender}|prefab={u.Prefab?.AssetId}|facts=[{string.Join(",", (u.AddFacts ?? new Kingmaker.Blueprints.Facts.BlueprintUnitFact[0]).Where(x => x != null).Select(x => x.name).ToArray())}]|levels=[{string.Join(",", (u.ComponentsArray ?? new BlueprintComponent[0]).OfType<Kingmaker.Blueprints.Classes.AddClassLevels>().Select(l => l.CharacterClass?.name + "x" + l.Levels).ToArray())}]");
            }
            System.IO.File.WriteAllText(path, sb.ToString());
            return $"READ catalog of {all.Count} blueprints written to {path}.";
        }

        private static string Safe(Func<string> name)
        {
            try
            {
                return name();
            }
            catch (Exception)
            {
                return "?";
            }
        }

        private static string Fields(object o)
        {
            return string.Join("; ", o.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Where(f => f.DeclaringType != typeof(UnityEngine.Object) && f.Name != "ComponentsArray")
                .Select(f =>
                {
                    object value = f.GetValue(o);
                    if (value is Kingmaker.Blueprints.Classes.Spells.SpellDescriptorWrapper wrapper)
                    {
                        value = wrapper.Value;
                    }
                    string text = value is System.Collections.IEnumerable list && !(value is string)
                        ? "[" + string.Join(",", list.Cast<object>().Select(x => (x as UnityEngine.Object)?.name ?? x?.ToString()).ToArray()) + "]"
                        : (value as UnityEngine.Object)?.name ?? value?.ToString();
                    return $"{f.Name}={text}";
                }).ToArray());
        }

        private static UnitEntityData Member(string index)
        {
            // "pet:5" is the animal companion of party member 5.
            return index.StartsWith("pet:") ? Game.Instance.Player.Party[int.Parse(index.Substring(4))].Descriptor.Pet : Game.Instance.Player.Party[int.Parse(index)];
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

        /// <summary>Read only: a component or action and every serialized field beneath it (actions, conditions), as an indented tree.</summary>
        private static void Deep(StringBuilder sb, object value, int depth, System.Collections.Generic.HashSet<object> seen)
        {
            if (value == null || depth > 9 || !seen.Add(value))
            {
                return;
            }
            string pad = new string(' ', depth * 2);
            sb.Append(pad).Append('[').Append(value.GetType().Name).Append(']');
            var nested = new System.Collections.Generic.List<object>();
            for (Type t = value.GetType(); t != null && t != typeof(object) && t != typeof(UnityEngine.Object) && t != typeof(ScriptableObject); t = t.BaseType)
            {
                foreach (System.Reflection.FieldInfo f in t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    object v = f.GetValue(value);
                    if (v is Kingmaker.ElementsSystem.ActionList list)
                    {
                        sb.Append($" {f.Name}=<{list.Actions?.Length ?? 0} actions>");
                        nested.AddRange(list.Actions ?? new Kingmaker.ElementsSystem.GameAction[0]);
                    }
                    else if (v is Kingmaker.ElementsSystem.ConditionsChecker check)
                    {
                        sb.Append($" {f.Name}=<{check.Operation} of {check.Conditions?.Length ?? 0}>");
                        nested.AddRange(check.Conditions ?? new Kingmaker.ElementsSystem.Condition[0]);
                    }
                    else if (v is Kingmaker.ElementsSystem.GameAction || v is Kingmaker.ElementsSystem.Condition)
                    {
                        sb.Append($" {f.Name}=<nested>");
                        nested.Add(v);
                    }
                    else if (v is UnityEngine.Object o)
                    {
                        sb.Append($" {f.Name}={(o ? o.name : "null")}");
                    }
                    else if (v != null && (f.FieldType.IsPrimitive || f.FieldType.IsEnum || v is string || f.FieldType.Namespace?.StartsWith("Kingmaker") == true))
                    {
                        string text = v is System.Collections.IEnumerable e && !(v is string) ? "[" + string.Join(",", e.Cast<object>().Select(x => x?.ToString()).ToArray()) + "]" : v.ToString();
                        sb.Append($" {f.Name}={text}");
                    }
                }
            }
            sb.Append('\n');
            foreach (object n in nested)
            {
                Deep(sb, n, depth + 1, seen);
            }
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
            unit = target == "krune" ? WardFixtures.Krune : target == "kurshu" ? KingmakerWakingRune.Sanctum.Encounter.KurshuEncounter.Kurshu : target.StartsWith("party:") ? Member(target.Substring(6)) : target.StartsWith("body:") ? BodyOf(target.Substring(5)) : target.StartsWith("a4:") ? Defender(target.Substring(3)) : target.StartsWith("unit:") ? Game.Instance.State.Units.FirstOrDefault(u => u.UniqueId == target.Substring(5)) : target.StartsWith("name:") ? Game.Instance.State.Units.FirstOrDefault(u => u.IsInGame && !u.Descriptor.State.IsDead && u.CharacterName.IndexOf(target.Substring(5), StringComparison.OrdinalIgnoreCase) >= 0) : WardFixtures.Find(target);
            return unit == null ? $"no target {target}" : null;
        }

        /// <summary>An A4 defender by the end of its key: "Vandiana", "Agarik", "Jorgas" or "DarkNaga".</summary>
        private static UnitEntityData Defender(string suffix)
        {
            string key = KingmakerWakingRune.Sanctum.Lashmistress.DefenderKeys.All.FirstOrDefault(k => k.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            return key == null ? null : KingmakerWakingRune.Sanctum.Lashmistress.LashmistressEncounter.Find(key);
        }

        /// <summary>
        /// INJECTED decision: one defender casts one of its own ready spells or abilities (a prepared slot, a naga slot, an
        /// ability fact) at a unit or a grid point, in place of its brain's choice. The slot or use is spent natively, and the
        /// spell's saves, spell resistance and effects all resolve natively. For the focused spell checks only.
        /// </summary>
        private static string A4Cast(string who, string name, string[] target)
        {
            UnitEntityData unit = Defender(who);
            if (unit == null)
            {
                return $"no defender {who}";
            }
            BlueprintAbility wanted = KingmakerWakingRune.Sanctum.Lashmistress.LashmistressBlueprints.PreparedAbilities.TryGetValue(name, out BlueprintAbility prepared) ? prepared : null;
            AbilityData data = null;
            foreach (Spellbook book in unit.Descriptor.Spellbooks)
            {
                foreach (SpellSlot slot in book.GetAllMemorizedSpells().Where(s => s.Available && s.Spell != null))
                {
                    if (slot.Spell.Blueprint == wanted || slot.Spell.Blueprint.name == name)
                    {
                        data = slot.Spell;
                        break;
                    }
                    // A variant (a summon's creature list) is cast through its prepared base spell.
                    BlueprintAbility variant = slot.Spell.Blueprint.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityVariants>()?.Variants.FirstOrDefault(v => v.name == name);
                    if (variant != null)
                    {
                        data = new AbilityData(slot.Spell, variant);
                        break;
                    }
                }
                if (data != null)
                {
                    break;
                }
            }
            if (data == null)
            {
                data = unit.Descriptor.Abilities.Enumerable.FirstOrDefault(a => a.Blueprint.name == name || a.Blueprint.name == "KWR_DarkNaga" + name)?.Data;
            }
            if (data == null || !data.IsAvailableForCast)
            {
                return $"{unit.CharacterName} has no ready {name} (found {data != null})";
            }
            TargetWrapper wrapper;
            string where;
            if (target.Length >= 2)
            {
                float x = float.Parse(target[0], System.Globalization.CultureInfo.InvariantCulture), y = float.Parse(target[1], System.Globalization.CultureInfo.InvariantCulture);
                wrapper = new Vector3(SanctumPlan.ToWorldX(x), SanctumPlan.HeightAt(x, y) ?? 0f, SanctumPlan.ToWorldZ(y));
                where = $"point ({x},{y})";
            }
            else
            {
                string missing = Resolve(target.Length == 0 ? "a4:" + who : target[0], out UnitEntityData victim);
                if (missing != null)
                {
                    return missing;
                }
                wrapper = victim;
                where = victim.CharacterName;
            }
            bool can = data.CanTarget(wrapper);
            unit.Commands.InterruptAiCommands();
            unit.Commands.Run(UnitUseAbility.CreateCastCommand(data, wrapper));
            return $"INJECTED decision: {unit.CharacterName} casts {data.Blueprint.name} at {where} (canTarget {can}); the slot and the spell's rules are native.";
        }

        /// <summary>The restored or dead body that replaced a statue, by the statue's id.</summary>
        private static UnitEntityData BodyOf(string statueId)
        {
            SanctumState state = WardRuntime.State;
            return state != null && state.Statues.TryGetValue(statueId, out StatueState statue) ? WardStatues.Body(statue) : null;
        }

        /// <summary>
        /// Casts a spell the caster has ready in their own spellbook, as choosing it on the action bar would: a prepared slot or a
        /// spontaneous slot is spent natively, and a variant (a summon's creature list, say) is cast through its base spell.
        /// Nothing is granted. The target is a unit (as for cast) or a grid point "x y".
        /// </summary>
        private static string SpellFromBook(UnitEntityData caster, string name, string[] target)
        {
            AbilityData data = null;
            foreach (Spellbook book in caster.Descriptor.Spellbooks)
            {
                for (int level = book.MaxSpellLevel; level >= 0 && data == null; level--)
                {
                    System.Collections.Generic.IEnumerable<AbilityData> ready = book.Blueprint.Spontaneous
                        ? book.GetKnownSpells(level)
                        : book.GetMemorizedSpells(level).Where(s => s.Available && s.Spell != null).Select(s => s.Spell);
                    foreach (AbilityData spell in ready)
                    {
                        if (spell.Blueprint.name == name)
                        {
                            data = spell;
                            break;
                        }
                        BlueprintAbility variant = spell.Blueprint.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityVariants>()?.Variants.FirstOrDefault(v => v.name == name);
                        if (variant != null)
                        {
                            data = new AbilityData(spell, variant);
                            break;
                        }
                    }
                }
            }
            if (data == null)
            {
                return $"{caster.CharacterName} has no ready {name} in a spellbook";
            }
            TargetWrapper wrapper;
            string where;
            if (target.Length >= 2)
            {
                float x = float.Parse(target[0]), y = float.Parse(target[1]);
                wrapper = new Vector3(SanctumPlan.ToWorldX(x), SanctumPlan.HeightAt(x, y) ?? 0f, SanctumPlan.ToWorldZ(y));
                where = $"point=({x},{y})";
            }
            else
            {
                string missing = Resolve(target[0], out UnitEntityData victim);
                if (missing != null)
                {
                    return missing;
                }
                wrapper = victim;
                where = $"target={target[0]}";
            }
            bool can = data.CanTarget(wrapper);
            caster.Commands.Run(UnitUseAbility.CreateCastCommand(data, wrapper));
            return $"HARNESS-COMMANDED cast from {caster.CharacterName}'s own spellbook: {data.Blueprint.name} ({data.Spellbook?.Blueprint.name}) {where} canTarget={can}; native rules resolve it and spend the slot.";
        }

        /// <summary>Presses the open message box's Yes or No through the box's own button handler, as a click would.</summary>
        private static string AnswerBox(bool yes)
        {
            Kingmaker.UI.DialogMessageBox box = UnityEngine.Object.FindObjectsOfType<Kingmaker.UI.DialogMessageBox>().FirstOrDefault(b => b.IsShown);
            if (box == null)
            {
                return "no message box is open";
            }
            Harmony12.AccessTools.Method(typeof(Kingmaker.UI.DialogMessageBox), yes ? "OnButtonYes" : "OnButtonNo").Invoke(box, null);
            return $"SETUP message box answered {(yes ? "Yes" : "No")} through its own button handler.";
        }

        /// <summary>Prepares a known spell into an empty slot of its level, as dragging it into the spellbook would.</summary>
        private static string Memorize(UnitEntityData unit, string name, int level, bool pending = false)
        {
            foreach (Spellbook book in unit.Descriptor.Spellbooks.Where(b => !b.Blueprint.Spontaneous))
            {
                AbilityData spell = book.GetKnownSpells(level).FirstOrDefault(s => s.Blueprint.name == name);
                if (spell == null)
                {
                    continue;
                }
                // An empty slot if there is one, else each slot in turn is overwritten until the native rules accept one
                // (SETUP only); the prepared slots are then marked ready, as the next rest would. "pending" leaves them as the
                // spellbook screen does, chosen but not ready until a rest (HARNESS-COMMANDED, the native Memorize call).
                foreach (SpellSlot slot in book.GetMemorizedSpellSlots(level).OrderBy(s => s.Spell == null ? 0 : 1).ToList())
                {
                    if (slot.Spell != null)
                    {
                        book.ForgetMemorized(slot);
                    }
                    if (book.Memorize(spell, slot))
                    {
                        if (pending)
                        {
                            return $"HARNESS-COMMANDED memorize {name} at level {level} for {unit.CharacterName}: chosen, not ready until a rest.";
                        }
                        foreach (SpellSlot linked in slot.LinkedSlots ?? new[] { slot })
                        {
                            linked.Available = true;
                        }
                        return $"SETUP memorize {name} at level {level} for {unit.CharacterName}: prepared and ready.";
                    }
                }
            }
            return $"{unit.CharacterName} cannot prepare {name} at level {level} into an empty slot";
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
        private static Vector3 GridWorld(float x, float y)
        {
            return new Vector3(SanctumPlan.ToWorldX(x), SanctumPlan.HeightAt(x, y) ?? 0f, SanctumPlan.ToWorldZ(y));
        }

        /// <summary>READ: where a sanctum grid point is on the screen (to aim a player's click), and whether it is in view.</summary>
        private static string GridScreen(float x, float y)
        {
            Vector3 p = Camera.main.WorldToScreenPoint(GridWorld(x, y));
            return $"READ screen grid ({x},{y}): x={p.x:0} y={Screen.height - p.y:0} onScreen={p.z > 0 && p.x >= 0 && p.x <= Screen.width && p.y >= 0 && p.y <= Screen.height} ({Screen.width}x{Screen.height}).";
        }

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
        private static string Perform(string fixtureId, string key, UnitEntityData actor)
        {
            WardAction action = WardActions.For(fixtureId).FirstOrDefault(a => a.Key == key);
            if (action == null)
            {
                return $"no action {key} on {fixtureId}";
            }
            action.Run(actor ?? Game.Instance.Player.MainCharacter.Value);
            WardInteract.RunPending();
            return $"SETUP performed {key} on {fixtureId} as {(actor ?? Game.Instance.Player.MainCharacter.Value).CharacterName}.";
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
            bool receiver = !KingmakerWakingRune.Sanctum.Krune.KruneAwakening.Authoritative;
            if (receiver)
            {
                // The receiver's own hit points are a proxy's; only the woken Krune keeps the damage it takes.
                krune.Damage = 0;
            }
            WardKrune.Refresh(WardRuntime.State, WardRuntime.Now);
            return $"INJECTED {amount} damage to {(receiver ? "the dormant Krune receiver" : "the real Krune (hp " + krune.Descriptor.HPLeft + "/" + krune.Descriptor.MaxHP + ")")}: temp HP {before} -> {krune.Stats.TemporaryHitPoints.ModifiedValue}; ledger exposed {FeederRules.ExposedTemporaryHP(WardRuntime.State.Door.Feeder, WardRuntime.Now)}.";
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

        /// <summary>Read only: the awakening's saved state, the actor's native sheet and the retired receiver.</summary>
        private static string KruneReport()
        {
            SanctumState state = WardRuntime.State;
            if (state == null)
            {
                return "READ Krune: no sanctum state in this game.";
            }
            UnitEntityData actor = KingmakerWakingRune.Sanctum.Krune.KruneAwakening.Actor;
            UnitEntityData receiver = WardFixtures.FindRetired("Krune");
            int kruneUnits = Game.Instance.State.Units.All.Count(KingmakerWakingRune.Sanctum.Krune.KruneAwakening.IsKrune);
            RitualState r = state.Ritual;
            string grants = string.Join("; ", state.Door.Feeder.Grants.Select(g => g.Amount + "->" + g.Remaining + " until " + g.ExpiresAt.ToString("0")).ToArray());
            var sb = new StringBuilder("READ Krune: " + WardStore.Dump(state.Krune) + "\n");
            sb.AppendLine($"ritual: due {r.AwakeningDue} {r.Reason} at {r.AwakeningAt:0}, stagger flag {r.AwakeningOpeningStagger}, global {r.GlobalDeadline:0}, confrontation {r.ConfrontationDeadline:0}, now {WardRuntime.Now:0.0}; mask {state.RuneMask}; emerald destroyed {state.Gem.Destroyed}; feeder exposed {FeederRules.ExposedTemporaryHP(state.Door.Feeder, WardRuntime.Now)} grants [{grants}]");
            sb.AppendLine($"real Krune units in the area: {kruneUnits}; receiver {(receiver == null ? "none" : receiver.UniqueId + " inGame " + receiver.IsInGame + " indexed " + (WardFixtures.Find("Krune") != null))}");
            sb.AppendLine(KingmakerWakingRune.Sanctum.Krune.KruneAwakening.Sheet(actor));
            sb.Append($"checkpoint holds {KingmakerWakingRune.Sanctum.Krune.KruneAwakening.CheckpointHolds}, paused {Game.Instance.IsPaused}, mode {Game.Instance.CurrentMode}, turn-based {TurnBased.Controllers.CombatController.IsInTurnBasedCombat()}, party in combat {Game.Instance.Player.IsInCombat}");
            return sb.ToString();
        }

        /// <summary>Read only: what the native rule gives his caster level for one spell from his own spellbook, with a level-dependent output.</summary>
        private static string KruneCasterProbe(string abilityName)
        {
            UnitEntityData actor = KingmakerWakingRune.Sanctum.Krune.KruneAwakening.Actor;
            BlueprintAbility ability = ResourcesLibrary.GetBlueprints<BlueprintAbility>().FirstOrDefault(a => a.name == abilityName);
            if (actor == null || ability == null)
            {
                return actor == null ? "no real Krune" : $"no ability {abilityName}";
            }
            Spellbook book = actor.Descriptor.GetSpellbook(KingmakerWakingRune.Sanctum.Krune.KruneBlueprints.WizardClass);
            var data = new AbilityData(ability, book);
            var rule = Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.Abilities.RuleCalculateAbilityParams(actor, data));
            int cl = rule.Result.CasterLevel;
            return $"READ native ability params for Krune's {abilityName}: wizard (class) level {book?.CasterLevel}, caster level {cl}, spell level {rule.Result.SpellLevel}, DC {rule.Result.DC}, concentration {rule.Result.Concentration}; level-dependent output: an hour-per-level duration of {cl} h, a dispel check of d20+{System.Math.Min(10, cl)} (dispel magic) or d20+{System.Math.Min(20, cl)} (greater). Revival loss now {KingmakerWakingRune.Sanctum.Krune.KruneAwakening.CasterLevelLoss()}.";
        }

        /// <summary>SETUP (accelerated): Kurshu's 90-minute deadline brought closer. The ritual's own tick still triggers it.</summary>
        private static string KruneDeadline(double seconds)
        {
            RitualState ritual = WardRuntime.State.Ritual;
            if (!ritual.KurshuDefeated || ritual.GlobalDeadline < 0d)
            {
                return "Kurshu's deadline has not started";
            }
            double before = ritual.GlobalDeadline;
            ritual.GlobalDeadline = System.Math.Min(ritual.GlobalDeadline, WardRuntime.Now + seconds);
            return $"SETUP accelerated deadline: global {before:0} -> {ritual.GlobalDeadline:0} (now {WardRuntime.Now:0}); the native tick will trigger it.";
        }

        /// <summary>SETUP/INJECTED: rune foci set dark by mask before a test, as if sabotaged. Never used for the live-link check.</summary>
        private static string SetMask(int mask)
        {
            SanctumState state = WardRuntime.State;
            foreach (RuneFocus focus in KruneProjection.Foci)
            {
                RuneFocusState f = state.Focus(focus.Id);
                if ((mask & (1 << focus.Bit)) != 0 && !f.Disabled)
                {
                    f.Disabled = true;
                    f.DisabledRoute = "INJECTED setmask";
                    f.DisabledAt = WardRuntime.Now;
                }
            }
            state.RecomputeMask();
            WardRuntime.RefreshRuneVisuals(state);
            KingmakerWakingRune.Sanctum.Krune.KruneAwakening.SyncLinks(state, WardRuntime.Now);
            return $"INJECTED rune mask: now {state.RuneMask} (requested {mask}; lit foci are only ever darkened).";
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

        /// <summary>
        /// READ: every living creature in the sanctum with what the combat repairs turn on: its own speed and the speed the game
        /// moves it at, haste, solid fog and the tentacles' hold, freedom of movement, sleeping, whether Krune detects it now;
        /// then the areas in play, Krune's life state and the spear's dance, the rod's charges and the turn-based 5-foot step.
        /// </summary>
        private static string CombatRead()
        {
            var sb = new StringBuilder("READ combat:\n");
            UnitEntityData krune = WardFixtures.Krune;
            foreach (UnitEntityData u in Game.Instance.State.Units.Where(u => u.IsInGame && WardFixtures.IdOf(u) == null && u.Get<WardRestoredPart>() == null))
            {
                Vector3 p = u.Position;
                string held = u.Descriptor.Get<KingmakerWakingRune.Sanctum.Krune.KwrConjuredMagicPart>()?.HeldByArea ?? string.Empty;
                sb.AppendLine($"'{u.CharacterName}' id={u.UniqueId} {u.Blueprint.name} hp={u.Descriptor.HPLeft}/{u.MaxHP} dmg={u.Damage} life={u.Descriptor.State.LifeState} dead={u.Descriptor.State.IsDead} speedStat={u.Stats.Speed.ModifiedValue}ft modifiedMps={u.ModifiedSpeedMps:0.00} combatMps={u.CombatSpeedMps:0.00}"
                    + $" hasted={u.Buffs.Enumerable.Any(b => b.Blueprint.name.StartsWith("Haste", StringComparison.Ordinal))} fog={KingmakerWakingRune.Sanctum.Krune.KwrSolidFogLogic.In(u)} free={KingmakerWakingRune.Sanctum.Krune.KwrSolidFogLogic.Free(u)} grappled={u.Buffs.HasFact(KingmakerWakingRune.Sanctum.Krune.KruneLocalSpells.TentaclesGrappled)}{(held.Length > 0 ? " heldBy=" + held : string.Empty)}"
                    + $" invisible={u.Descriptor.State.HasCondition(UnitCondition.Invisible)} untargetable={(bool)u.Descriptor.State.IsUntargetable} sleeping={u.IsSleeping} sleepless={u.Sleepless} inCombat={u.IsInCombat}"
                    + (krune != null && u != krune && krune.IsEnemy(u) ? $" kruneMemory={krune.Memory.Contains(u)} kruneSees={krune.Memory.ContainsVisible(u)}" : string.Empty)
                    + $" grid=({p.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX:0.00},{SanctumPlan.OriginY - p.z / SanctumPlan.MetersPerSquare:0.00}) weapon={u.Body.PrimaryHand.MaybeItem?.Blueprint.name}");
            }
            foreach (Kingmaker.EntitySystem.Entities.AreaEffectEntityData a in Game.Instance.State.AreaEffects)
            {
                Vector3 p = a.Position;
                sb.AppendLine($"area {a.Blueprint.name} id={a.UniqueId} caster={a.Context?.MaybeCaster?.CharacterName} r={a.Blueprint.Size.Meters:0.0}m grid=({p.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX:0.00},{SanctumPlan.OriginY - p.z / SanctumPlan.MetersPerSquare:0.00}) inside=[{string.Join(",", a.UnitsInside.Select(x => x.CharacterName).ToArray())}]");
            }
            KingmakerWakingRune.Sanctum.Krune.KruneState k = WardRuntime.State?.Krune;
            if (k != null)
            {
                sb.AppendLine("director: " + KingmakerWakingRune.Sanctum.Krune.KruneDirector.DebugState() + " rod: " + KingmakerWakingRune.Sanctum.Krune.RuneRod.DebugState());
                sb.AppendLine($"krune: stage={k.Stage} dancingForm={k.DancingFormId} until={k.DancingUntil:0.0} lockedUntil={k.DanceLockedUntil:0.0} droppedBag={k.DroppedSpearBagId} rodCharges={k.RodCharges} rodRenewedAt={k.RodRenewedAt:0} now={WardRuntime.Now:0.0} hand={krune?.Body.PrimaryHand.MaybeItem?.Blueprint.name} keepAwake={!KingmakerWakingRune.Sanctum.Krune.KruneAwakening.HarnessNoKeepAwake}");
            }
            TurnBased.Controllers.TurnController turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            if (turn != null)
            {
                sb.AppendLine($"turn: {turn.Unit.CharacterName} restrictFiveFootStep={turn.ShouldRestrictFiveFootStep()} hasFiveFootStep={turn.HasFiveFootStep()}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// INJECTED: one of Krune's local spell areas (or any area blueprint) at a grid point for a time, with the caster's own
        /// context (Krune's by default, or a party member's: "party:3"). Its rules run natively from then on. For the focused
        /// spell checks only.
        /// </summary>
        private static string SpawnArea(string name, float x, float y, double seconds, string casterKey, int casterLevel)
        {
            Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect area = ResourcesLibrary.GetBlueprints<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect>().FirstOrDefault(a => a.name == name);
            string missing = Resolve(casterKey, out UnitEntityData caster);
            if (area == null || missing != null)
            {
                return area == null ? $"no area {name}" : missing;
            }
            var context = new Kingmaker.UnitLogic.Mechanics.MechanicsContext(caster, caster.Descriptor, area);
            context.Recalculate();
            if (casterLevel > 0 && context.Params != null)
            {
                context.Params.CasterLevel = casterLevel;
            }
            var point = new Vector3(SanctumPlan.ToWorldX(x), SanctumPlan.HeightAt(x, y) ?? 0f, SanctumPlan.ToWorldZ(y));
            Kingmaker.EntitySystem.Entities.AreaEffectEntityData spawned = Kingmaker.Controllers.AreaEffectsController.Spawn(context, area, point, TimeSpan.FromSeconds(seconds));
            return $"INJECTED area {name} by {caster.CharacterName} at ({x},{y}) for {seconds} s (caster level {context.Params?.CasterLevel}): id {spawned?.UniqueId}.";
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
            return $"HARNESS-COMMANDED attack: {attacker.CharacterName} -> {target}.";
        }
    }
}
