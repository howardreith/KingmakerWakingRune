using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using TurnBased.Controllers;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// The native lifecycle of the effects that hold a status on an Alacrity plate: when each began, on the engine's own
    /// clock, and when one was removed, never later than its scheduled end. A sustained route is then measured from the
    /// effect itself rather than from the polling observation, so an effect that expires exactly on time is credited in
    /// full and one broken a moment early is not. Kept in memory only: after a reload, observations fall back to what
    /// they saw.
    /// </summary>
    internal sealed class RuneConditionClock : IUnitBuffHandler
    {
        private static readonly Dictionary<Buff, double> s_AppliedAt = new Dictionary<Buff, double>();
        private static readonly Dictionary<string, double> s_EndedAt = new Dictionary<string, double>();

        public static void Forget()
        {
            s_AppliedAt.Clear();
            s_EndedAt.Clear();
        }

        public static bool Supplies(Buff buff, UnitCondition native)
        {
            return buff.Blueprint.ComponentsArray.OfType<Kingmaker.UnitLogic.FactLogic.AddCondition>().Any(c => c.Condition == native)
                || buff.Blueprint.ComponentsArray.OfType<Kingmaker.Designers.Mechanics.Buffs.BuffStatusCondition>().Any(c => c.Condition == native);
        }

        public static double AppliedAt(Buff buff)
        {
            return buff != null && s_AppliedAt.TryGetValue(buff, out double at) ? at : -1d;
        }

        /// <summary>When the latest effect holding this status on this plate was removed, consumed by the reading.</summary>
        public static double TakeEndedAt(string focusId, RuneCondition condition)
        {
            string key = Key(focusId, condition);
            if (!s_EndedAt.TryGetValue(key, out double at))
            {
                return -1d;
            }
            s_EndedAt.Remove(key);
            return at;
        }

        public void HandleBuffDidAdded(Buff buff)
        {
            Guard(() =>
            {
                if (AlacrityFocus(buff) != null && Conditions(buff).Any())
                {
                    // The engine dates a buff from the current turn's start in turn-based combat; match it.
                    TimeSpan started = CombatController.IsInTurnBasedCombat() ? Game.Instance.TurnBasedCombatController.TurnStartTime : Game.Instance.TimeController.GameTime;
                    s_AppliedAt[buff] = started.TotalSeconds;
                }
            });
        }

        public void HandleBuffDidRemoved(Buff buff)
        {
            Guard(() =>
            {
                string focusId = AlacrityFocus(buff);
                if (focusId == null)
                {
                    return;
                }
                s_AppliedAt.Remove(buff);
                double now = Game.Instance.TimeController.GameTime.TotalSeconds;
                double ended = buff.IsPermanent ? now : Math.Min(now, buff.EndTime.TotalSeconds);
                foreach (RuneCondition condition in Conditions(buff))
                {
                    s_EndedAt[Key(focusId, condition)] = ended;
                    Log.Info($"Rune {focusId}: {condition} effect {buff.Blueprint.name} removed at {now:0.00}; scheduled end {(buff.IsPermanent ? "none" : buff.EndTime.TotalSeconds.ToString("0.00"))}; credited to {ended:0.00}.");
                }
            });
        }

        private static IEnumerable<RuneCondition> Conditions(Buff buff)
        {
            if (Supplies(buff, UnitCondition.Entangled))
            {
                yield return RuneCondition.Entangled;
            }
            if (Supplies(buff, UnitCondition.Staggered))
            {
                yield return RuneCondition.Staggered;
            }
            if (Supplies(buff, UnitCondition.Paralyzed))
            {
                yield return RuneCondition.Paralyzed;
            }
        }

        private static string AlacrityFocus(Buff buff)
        {
            if (!SanctumScope.IsSanctumLoaded)
            {
                return null;
            }
            string id = WardFixtures.IdOf(buff.Owner?.Unit);
            return id != null && KruneProjection.Foci.Any(f => f.Id == id && f.Kind == RuneKind.Alacrity) ? id : null;
        }

        private static string Key(string focusId, RuneCondition condition)
        {
            return focusId + "/" + condition;
        }

        private static void Guard(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.Error("Rune condition clock failed.", ex);
            }
        }
    }
}
