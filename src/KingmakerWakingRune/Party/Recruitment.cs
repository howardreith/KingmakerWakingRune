using System;
using Kingmaker;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.View;
using KingmakerWakingRune.Lodge;
using UnityEngine;

namespace KingmakerWakingRune.Party
{
    /// <summary>
    /// The Coordinator's free recruitment: the native custom-companion flow, given the level-target XP
    /// directly. Nothing global is switched, so ordinary mercenary costs and progression are untouched,
    /// and a cancelled character creation never reaches the success callback.
    /// </summary>
    internal static class Recruitment
    {
        public static void RequestRecruit()
        {
            // Runs after the dialogue has closed, so no two modal flows overlap.
            Game.Instance.ScheduleAction(Open);
        }

        public static void RequestPartySelection()
        {
            Game.Instance.ScheduleAction(() => ScriptableObject.CreateInstance<ShowPartySelection>().RunAction());
        }

        private static void Open()
        {
            Player player = Game.Instance.Player;
            long moneyBefore = player.Money;
            Log.Info($"Recruitment opened: treasury {moneyBefore} gp, xp target {StartingGrants.LevelTargetXp}, party {player.PartyCharacters.Count}.");
            player.CreateCustomCompanion(() => OnCompleted(moneyBefore), StartingGrants.LevelTargetXp);
        }

        /// <summary>Called by the native flow only after a character was actually committed.</summary>
        private static void OnCompleted(long moneyBefore)
        {
            Player player = Game.Instance.Player;
            long refund = WakingPolicies.RecruitGoldRefund(moneyBefore, player.Money);
            if (refund > 0)
            {
                // Native character creation grants class starting gold; the treasury is a fixed budget here.
                player.SpendMoney(refund);
            }
            UnitEntityData recruit = player.RemoteCompanions.Count > 0 ? player.RemoteCompanions[player.RemoteCompanions.Count - 1].Value : null;
            if (recruit == null)
            {
                Log.Error("Recruitment committed but no new companion was found.");
                return;
            }
            Log.Info($"Recruit completed: {LodgeSession.Describe(recruit)} xp={recruit.Descriptor.Progression.Experience} treasury {player.Money} gp (native starting gold refunded: {refund}).");
            if (player.PartyCharacters.Count >= 6)
            {
                Log.Info("Party is full; the recruit stays in reserve for the native party selection.");
                return;
            }
            JoinParty(recruit);
        }

        private static void JoinParty(UnitEntityData recruit)
        {
            Player player = Game.Instance.Player;
            UnitEntityData main = player.MainCharacter.Value;
            player.AddCompanion(recruit);
            recruit.IsInGame = true;
            Vector3 position = main.Position;
            if (AstarPath.active != null)
            {
                FreePlaceSelector.PlaceSpawnPlaces(2, recruit.View.Corpulence, position);
                position = FreePlaceSelector.GetRelaxedPosition(1, projectOnGround: true);
            }
            recruit.Position = position;
            if (recruit.GroupId != main.GroupId)
            {
                recruit.GroupId = main.GroupId;
            }
            Log.Info($"Recruit joined the party: party size {player.PartyCharacters.Count}.");
        }
    }

    /// <summary>
    /// The lodge's night of rest before departure: the native scripted party rest an inn gives, so prepared casters can
    /// ready the spells their new levels brought. Neither the lodge's donor area nor the sanctum allows camping.
    /// </summary>
    internal static class LodgeRestService
    {
        public static void RequestRest()
        {
            Game.Instance.ScheduleAction(() =>
            {
                Log.Info("Lodge rest: native scripted party rest started.");
                Game.Instance.RestController.StartScripted(immediate: false);
            });
        }
    }

    /// <summary>Opens the native trade window once the Quartermaster's dialogue has closed.</summary>
    internal static class QuartermasterService
    {
        public static void RequestOpen()
        {
            Game.Instance.ScheduleAction(() =>
            {
                UnitEntityData vendor = LodgeSession.FindCastMember("quartermaster");
                if (vendor == null)
                {
                    Log.Error("Quartermaster unit not found; the shop cannot open.");
                    return;
                }
                EventBus.RaiseEvent(delegate(IVendorUIHandler h)
                {
                    h.HandleTradeStarted(vendor);
                });
            });
        }
    }
}
