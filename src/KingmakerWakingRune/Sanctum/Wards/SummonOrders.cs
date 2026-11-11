using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// "Direct my summoned creatures to attack this rune": a character's own living summons are given real native attack
    /// commands against one plate, renewed after each round of attacks and at each of their turn-based turns, until the
    /// rune goes dark, the plate is gone, the summon ends or leaves its summoner's control, or the player recalls them.
    /// Nothing else about the summons changes: they walk, attack, meet hardness and expire as natively as ever, and are
    /// never made selectable. Orders are saved by identity and checked again on every load before any is resumed.
    /// </summary>
    internal sealed class SummonOrders : IUnitCommandEndHandler, ITurnBasedModeHandler, IUnitHandler, IUnitFactionHandler
    {
        /// <summary>Set by a load; the first tick afterwards checks every saved order and resumes the ones still valid.</summary>
        private static bool s_ResumeDue;

        private static List<SummonOrderRecord> Orders => WardRuntime.State?.SummonOrders;

        public static void AfterLoad()
        {
            s_ResumeDue = true;
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
                return part != null && part.IsLinkedToSummoner && part.Summoner == summoner && !u.Descriptor.IsPet && u.IsInGame && !u.Descriptor.State.IsDead && u.Descriptor.State.IsConscious && OutOfControl(u, summoner) == null;
            }).ToList();
        }

        public static bool HasOrders(string targetId)
        {
            return Orders != null && Orders.Any(o => o.TargetId == targetId);
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
                Orders.RemoveAll(o => o.SummonId == summon.UniqueId);
                Orders.Add(new SummonOrderRecord { SummonId = summon.UniqueId, SummonerId = summoner.UniqueId, SummonerName = summoner.CharacterName, TargetId = targetId });
                Issue(summon, "ordered");
            }
            WardRuntime.Say(summons.Count == 1
                ? $"{summoner.CharacterName}'s summoned creature turns on the rune plate."
                : $"{summoner.CharacterName}'s summoned creatures turn on the rune plate.");
        }

        public static void Recall(string targetId)
        {
            foreach (SummonOrderRecord order in Orders.Where(o => o.TargetId == targetId).ToList())
            {
                End(order, "recalled by the player", interrupt: true);
            }
            WardRuntime.Say("The summoned creatures break off from the rune plate.");
        }

        /// <summary>
        /// Ends orders whose rune has gone dark, whose plate or summon has gone, or whose summon is no longer its summoner's
        /// to command, stopping the attack in hand; after a load, resumes the orders that survive those checks.
        /// </summary>
        public static void Tick()
        {
            List<SummonOrderRecord> orders = Orders;
            if (orders == null || orders.Count == 0)
            {
                s_ResumeDue = false;
                return;
            }
            bool resume = s_ResumeDue;
            s_ResumeDue = false;
            foreach (SummonOrderRecord order in orders.ToList())
            {
                UnitEntityData summon = Find(order.SummonId);
                string reason = TargetGone(order.TargetId) ?? SummonGone(summon, order);
                if (reason != null)
                {
                    End(order, reason, interrupt: true);
                }
                else if (resume)
                {
                    Log.Info($"Summon order: {summon.CharacterName} ({summon.UniqueId}) of {order.SummonerName} against {order.TargetId} found in the save and still valid.");
                    if (MayIssueNow(summon))
                    {
                        Issue(summon, "resumed after loading");
                    }
                }
            }
        }

        public void HandleUnitCommandDidEnd(UnitCommand command)
        {
            UnitEntityData summon = command?.Executor;
            SummonOrderRecord order = OrderOf(summon);
            if (summon == null || !(command is UnitAttack) || order == null)
            {
                return;
            }
            // Only an attack that ran its course is renewed. An interrupted one was replaced or stopped by someone (a new order,
            // a turn start, a recall) whose own step decides what follows; renewing it would replace the replacement, forever.
            if (command.Result == UnitCommand.ResultType.Interrupt)
            {
                return;
            }
            if (command.Result == UnitCommand.ResultType.Fail)
            {
                End(order, "it could not carry out the attack", interrupt: false);
                return;
            }
            // Renewed once this command has fully left the queue, never from inside its own completion.
            Game.Instance.ScheduleAction(() => Issue(summon, "renewed after its attacks"));
        }

        public void HandleTurnStarted(UnitEntityData unit)
        {
            if (unit == null || OrderOf(unit) == null)
            {
                return;
            }
            // The turn start has already cleared the queue and let the AI choose; the standing order takes its place.
            unit.Commands.InterruptAll();
            Issue(unit, "renewed for its turn");
        }

        public void HandleUnitDeath(UnitEntityData unit)
        {
            SummonOrderRecord order = OrderOf(unit);
            if (order != null)
            {
                End(order, "the summon died", interrupt: false);
            }
        }

        public void HandleUnitDestroyed(UnitEntityData unit)
        {
            SummonOrderRecord order = OrderOf(unit);
            if (order != null)
            {
                End(order, "the summon is gone", interrupt: false);
            }
        }

        public void HandleUnitControlChanged(UnitEntityData unit)
        {
            Recheck(unit);
        }

        public void HandleFactionChanged(UnitEntityData unit)
        {
            Recheck(unit);
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

        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck perceptionCheck)
        {
        }

        private static void Recheck(UnitEntityData unit)
        {
            SummonOrderRecord order = OrderOf(unit);
            string reason = order == null ? null : SummonGone(unit, order);
            if (reason != null)
            {
                End(order, reason, interrupt: true);
            }
        }

        private static SummonOrderRecord OrderOf(UnitEntityData unit)
        {
            return unit == null ? null : Orders?.FirstOrDefault(o => o.SummonId == unit.UniqueId);
        }

        private static UnitEntityData Find(string id)
        {
            return Game.Instance.State.Units.FirstOrDefault(u => u.UniqueId == id);
        }

        /// <summary>Outside turn-based combat at once; inside it only in the summon's own turn, so no action is added.</summary>
        private static bool MayIssueNow(UnitEntityData summon)
        {
            return !TurnBased.Controllers.CombatController.IsInTurnBasedCombat() || TurnBased.Controllers.CombatController.CurrentUnit == summon;
        }

        private static void Issue(UnitEntityData summon, string why)
        {
            SummonOrderRecord order = OrderOf(summon);
            if (order == null)
            {
                return;
            }
            string gone = TargetGone(order.TargetId) ?? SummonGone(summon, order);
            if (gone != null)
            {
                End(order, gone, interrupt: true);
                return;
            }
            UnitEntityData target = WardFixtures.Find(order.TargetId);
            if (summon.Commands.Raw.Any(c => c is UnitAttack attack && !attack.IsFinished && attack.TargetUnit == target))
            {
                // Already on its way or striking: replacing a running attack with the same one would only interrupt it.
                return;
            }
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
                End(order, "it has no attack", interrupt: false);
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

        private static string SummonGone(UnitEntityData summon, SummonOrderRecord order)
        {
            UnitPartSummonedMonster part = summon?.Get<UnitPartSummonedMonster>();
            if (part == null || !summon.IsInGame || summon.Descriptor.State.IsDead)
            {
                return "the summon has ended";
            }
            if (!part.IsLinkedToSummoner || part.Summoner == null || part.Summoner.UniqueId != order.SummonerId)
            {
                return "it is no longer bound to the character who gave the order";
            }
            return OutOfControl(summon, part.Summoner) ?? (summon.Descriptor.State.IsConscious ? null : "the summon cannot act");
        }

        /// <summary>
        /// Someone or something else now decides what the summon does: it has changed allegiance away from its summoner, a
        /// faction effect holds it, or a condition with its own controller (confusion, panic) moves it.
        /// </summary>
        private static string OutOfControl(UnitEntityData summon, UnitEntityData summoner)
        {
            // A summon's faction is its own blueprint's, and its group is not saved (a reload puts it in a group of its own), so
            // allegiance is read from hostility either way and from any effect that has taken its faction.
            if (summon.IsEnemy(summoner) || summoner.IsEnemy(summon) || summon.Buffs.RawFacts.Any(b => b.Blueprint.ComponentsArray.OfType<Kingmaker.UnitLogic.FactLogic.ChangeFaction>().Any()))
            {
                return "it has changed allegiance";
            }
            if (summon.Descriptor.State.IsPanicked || summon.Descriptor.State.HasCondition(UnitCondition.Confusion))
            {
                return "it is under another effect's control";
            }
            return null;
        }

        /// <summary>Removes the order and, where asked, the attack on that plate in hand and the manual target it set.</summary>
        private static void End(SummonOrderRecord order, string reason, bool interrupt)
        {
            if (Orders == null || !Orders.Remove(order))
            {
                return;
            }
            UnitEntityData summon = Find(order.SummonId);
            if (summon != null && interrupt)
            {
                summon.Commands.InterruptAll(c => c is UnitAttack attack && attack.TargetUnit != null && WardFixtures.IdOf(attack.TargetUnit) == order.TargetId);
            }
            if (summon != null && summon.CombatState.ManualTarget != null && WardFixtures.IdOf(summon.CombatState.ManualTarget) == order.TargetId)
            {
                summon.CombatState.ManualTarget = null;
            }
            Log.Info($"Summon order ended: {summon?.CharacterName ?? order.SummonId} against {order.TargetId}: {reason}.");
        }
    }
}
