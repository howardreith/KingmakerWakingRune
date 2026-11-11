using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// "Direct my summoned creatures to attack this rune": a character's own living summons are given real native attack
    /// commands against one plate, renewed after each round of attacks and at each of their turn-based turns, until the
    /// rune goes dark, the plate is gone, the summon ends, or the player recalls them. Nothing else about the summons
    /// changes: they walk, attack, meet hardness and expire as natively as ever, and are never made selectable.
    /// </summary>
    internal sealed class SummonOrders : IUnitCommandEndHandler, ITurnBasedModeHandler, IUnitHandler
    {
        private sealed class Assignment
        {
            public string TargetId;
            public string SummonerName;
        }

        /// <summary>Summon unique id to its standing order. Orders live for the session; a reload leaves the summons to their AI.</summary>
        private static readonly Dictionary<string, Assignment> s_Orders = new Dictionary<string, Assignment>();

        public static void Forget()
        {
            s_Orders.Clear();
        }

        /// <summary>A character's own summons that could take an order now: linked to them, in the area, alive and able.</summary>
        public static List<UnitEntityData> SummonsOf(UnitEntityData summoner)
        {
            if (summoner == null)
            {
                return new List<UnitEntityData>();
            }
            return Game.Instance.State.Units.Where(u =>
            {
                UnitPartSummonedMonster part = u.Get<UnitPartSummonedMonster>();
                return part != null && part.IsLinkedToSummoner && part.Summoner == summoner && !u.Descriptor.IsPet && u.IsInGame && !u.Descriptor.State.IsDead && u.Descriptor.State.IsConscious;
            }).ToList();
        }

        public static bool HasOrders(string targetId)
        {
            return s_Orders.Values.Any(o => o.TargetId == targetId);
        }

        public static void Direct(UnitEntityData summoner, string targetId)
        {
            List<UnitEntityData> summons = SummonsOf(summoner);
            if (summons.Count == 0)
            {
                WardRuntime.Say($"{summoner.CharacterName} has no summoned creatures here to direct.");
                return;
            }
            foreach (UnitEntityData summon in summons)
            {
                s_Orders[summon.UniqueId] = new Assignment { TargetId = targetId, SummonerName = summoner.CharacterName };
                Issue(summon, "ordered");
            }
            WardRuntime.Say(summons.Count == 1
                ? $"{summoner.CharacterName}'s summoned creature turns on the rune plate."
                : $"{summoner.CharacterName}'s summoned creatures turn on the rune plate.");
        }

        public static void Recall(string targetId)
        {
            foreach (KeyValuePair<string, Assignment> order in s_Orders.Where(o => o.Value.TargetId == targetId).ToList())
            {
                End(order.Key, "recalled by the player", interrupt: true);
            }
            WardRuntime.Say("The summoned creatures break off from the rune plate.");
        }

        /// <summary>Ends orders whose rune has gone dark or whose plate has gone, stopping the attack in hand.</summary>
        public static void Tick()
        {
            if (s_Orders.Count == 0)
            {
                return;
            }
            foreach (KeyValuePair<string, Assignment> order in s_Orders.ToList())
            {
                string reason = TargetGone(order.Value.TargetId);
                if (reason != null)
                {
                    End(order.Key, reason, interrupt: true);
                }
            }
        }

        public void HandleUnitCommandDidEnd(UnitCommand command)
        {
            UnitEntityData summon = command?.Executor;
            if (summon == null || !(command is UnitAttack) || !s_Orders.ContainsKey(summon.UniqueId))
            {
                return;
            }
            // Renewed once this command has fully left the queue, never from inside its own completion.
            Game.Instance.ScheduleAction(() => Issue(summon, "renewed after its attacks"));
        }

        public void HandleTurnStarted(UnitEntityData unit)
        {
            if (unit == null || !s_Orders.ContainsKey(unit.UniqueId))
            {
                return;
            }
            // The turn start has already cleared the queue and let the AI choose; the standing order takes its place.
            unit.Commands.InterruptAll();
            Issue(unit, "renewed for its turn");
        }

        public void HandleUnitDeath(UnitEntityData unit)
        {
            if (unit != null && s_Orders.ContainsKey(unit.UniqueId))
            {
                End(unit.UniqueId, "the summon died", interrupt: false);
            }
        }

        public void HandleUnitDestroyed(UnitEntityData unit)
        {
            if (unit != null && s_Orders.ContainsKey(unit.UniqueId))
            {
                End(unit.UniqueId, "the summon is gone", interrupt: false);
            }
        }

        public void HandleUnitSpawned(UnitEntityData unit)
        {
        }

        public void HandleSurpriseRoundStarted()
        {
        }

        public void HandleRoundStarted(int round)
        {
        }

        public void HandleUnitControlChanged(UnitEntityData unit)
        {
        }

        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck perceptionCheck)
        {
        }

        private static void Issue(UnitEntityData summon, string why)
        {
            if (!s_Orders.TryGetValue(summon.UniqueId, out Assignment order))
            {
                return;
            }
            string gone = TargetGone(order.TargetId) ?? SummonGone(summon);
            if (gone != null)
            {
                End(summon.UniqueId, gone, interrupt: false);
                return;
            }
            UnitEntityData target = WardFixtures.Find(order.TargetId);
            try
            {
                summon.Commands.Run(UnitAttack.CreateAttackCommand(summon, target));
                summon.CombatState.ManualTarget = target;
                Log.Info($"Summon order: {summon.CharacterName} ({summon.UniqueId}) of {order.SummonerName} attacks {order.TargetId}, {why}.");
            }
            catch (Exception ex)
            {
                // A creature with no attack of its own cannot follow the order; it is released, not forced.
                Log.Error($"Summon order: {summon.CharacterName} cannot attack {order.TargetId}.", ex);
                End(summon.UniqueId, "it has no attack", interrupt: false);
            }
        }

        private static string TargetGone(string targetId)
        {
            UnitEntityData target = WardFixtures.Find(targetId);
            if (target == null || !target.IsInGame)
            {
                return "the plate is gone";
            }
            return WardRuntime.State != null && WardRuntime.State.Focus(targetId).Disabled ? "the rune is dark" : null;
        }

        private static string SummonGone(UnitEntityData summon)
        {
            UnitPartSummonedMonster part = summon.Get<UnitPartSummonedMonster>();
            if (part == null || !summon.IsInGame || summon.Descriptor.State.IsDead)
            {
                return "the summon has ended";
            }
            return summon.Descriptor.State.IsConscious ? null : "the summon cannot act";
        }

        private static void End(string summonId, string reason, bool interrupt)
        {
            if (!s_Orders.TryGetValue(summonId, out Assignment order))
            {
                return;
            }
            s_Orders.Remove(summonId);
            UnitEntityData summon = Game.Instance.State.Units.FirstOrDefault(u => u.UniqueId == summonId);
            if (summon != null && interrupt)
            {
                summon.Commands.InterruptAll(c => c is UnitAttack attack && attack.TargetUnit != null && WardFixtures.IdOf(attack.TargetUnit) == order.TargetId);
                if (summon.CombatState.ManualTarget != null && WardFixtures.IdOf(summon.CombatState.ManualTarget) == order.TargetId)
                {
                    summon.CombatState.ManualTarget = null;
                }
            }
            Log.Info($"Summon order ended: {summon?.CharacterName ?? summonId} against {order.TargetId}: {reason}.");
        }
    }
}
