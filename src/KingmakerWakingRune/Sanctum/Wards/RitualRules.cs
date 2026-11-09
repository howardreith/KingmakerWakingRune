using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerWakingRune.Sanctum.Wards
{
    internal enum AwarenessCause
    {
        Alarm,
        Messenger,
        Hound,
        Glyph,
        Confrontation,
    }

    internal enum AwakeningReason
    {
        None,
        GlobalDeadline,
        ConfrontationDeadline,
        EmeraldDestroyed,
    }

    internal sealed class AwarenessEntry
    {
        public AwarenessCause Cause;
        public double At;
    }

    internal sealed class RitualState
    {
        public bool VandianaAware;
        public double AwareAt = -1d;
        public List<AwarenessEntry> Causes = new List<AwarenessEntry>();
        public bool A4DoorHeard;
        public double A4DoorHeardAt = -1d;
        public bool CatsGraceConsumed;
        public bool NagaInvisibilityIntent;
        public bool DarkvisionPrepared;

        public bool KurshuDefeated;
        public double GlobalDeadline = -1d;
        public bool ConfrontationStarted;
        public double ConfrontationStartedAt = -1d;
        public int ConfrontationRoll;
        public bool AccelerationApplied;
        public double ConfrontationDeadline = -1d;

        public bool AwakeningDue;
        public AwakeningReason Reason = AwakeningReason.None;
        public double AwakeningAt = -1d;
        public bool AwakeningOpeningStagger;
    }

    /// <summary>
    /// Shared awareness and the two printed clocks. Everything here is idempotent, so repeated causes, reloads and repeat
    /// clicks cannot re-trigger preparation or reroll a countdown.
    /// </summary>
    internal static class RitualRules
    {
        /// <summary>Records a cause. Returns true only for the first awareness, which is what prepares Darkvision and warns allies.</summary>
        public static bool RaiseAwareness(RitualState state, AwarenessCause cause, double now)
        {
            if (!state.Causes.Any(c => c.Cause == cause))
            {
                state.Causes.Add(new AwarenessEntry { Cause = cause, At = now });
            }
            if (state.VandianaAware)
            {
                return false;
            }
            state.VandianaAware = true;
            state.AwareAt = now;
            state.DarkvisionPrepared = true;
            return true;
        }

        /// <summary>Hearing the east door. Returns true once: she drinks Cat's Grace and the naga's preparation is recorded.</summary>
        public static bool HearDoor(RitualState state, double now)
        {
            if (state.A4DoorHeard)
            {
                return false;
            }
            state.A4DoorHeard = true;
            state.A4DoorHeardAt = now;
            state.CatsGraceConsumed = true;
            state.NagaInvisibilityIntent = true;
            return true;
        }

        /// <summary>The Kurshu fight's event, which only a labelled test fixture supplies in this milestone.</summary>
        public static bool DefeatKurshu(RitualState state, double now)
        {
            if (state.KurshuDefeated)
            {
                return false;
            }
            state.KurshuDefeated = true;
            state.GlobalDeadline = now + WardProfile.GlobalDeadlineSeconds;
            return true;
        }

        /// <summary>
        /// Commits the A4 confrontation with an already-rolled 1d6+6. A naturally expired feeder grant makes it one round
        /// sooner, once. The same event a finished encounter will call.
        /// </summary>
        public static bool CommitConfrontation(RitualState state, FeederState feeder, int rolledRounds, double now)
        {
            if (state.ConfrontationStarted)
            {
                return false;
            }
            FeederRules.Expire(feeder, now);
            state.ConfrontationStarted = true;
            state.ConfrontationStartedAt = now;
            state.ConfrontationRoll = rolledRounds;
            state.AccelerationApplied = feeder.AccelerationEligible;
            int rounds = Math.Max(0, rolledRounds - (state.AccelerationApplied ? 1 : 0));
            state.ConfrontationDeadline = now + rounds * WardProfile.RoundSeconds;
            return true;
        }

        /// <summary>The emerald's destruction: awakening is due at once, whatever the clocks say.</summary>
        public static bool RequestImmediateAwakening(RitualState state, double now)
        {
            state.AwakeningOpeningStagger = true;
            return Due(state, AwakeningReason.EmeraldDestroyed, now);
        }

        /// <summary>Marks awakening due the first time a deadline has passed. Returns true exactly once.</summary>
        public static bool Tick(RitualState state, double now)
        {
            if (state.AwakeningDue)
            {
                return false;
            }
            bool global = state.GlobalDeadline >= 0d && now >= state.GlobalDeadline;
            bool confrontation = state.ConfrontationDeadline >= 0d && now >= state.ConfrontationDeadline;
            if (!global && !confrontation)
            {
                return false;
            }
            AwakeningReason reason;
            if (global && confrontation)
            {
                reason = state.GlobalDeadline <= state.ConfrontationDeadline ? AwakeningReason.GlobalDeadline : AwakeningReason.ConfrontationDeadline;
            }
            else
            {
                reason = global ? AwakeningReason.GlobalDeadline : AwakeningReason.ConfrontationDeadline;
            }
            return Due(state, reason, now);
        }

        /// <summary>Seconds left on the earlier active clock, or null when neither has begun.</summary>
        public static double? RemainingSeconds(RitualState state, double now)
        {
            double best = double.MaxValue;
            if (state.GlobalDeadline >= 0d)
            {
                best = Math.Min(best, state.GlobalDeadline);
            }
            if (state.ConfrontationDeadline >= 0d)
            {
                best = Math.Min(best, state.ConfrontationDeadline);
            }
            return best == double.MaxValue ? (double?)null : Math.Max(0d, best - now);
        }

        private static bool Due(RitualState state, AwakeningReason reason, double now)
        {
            if (state.AwakeningDue)
            {
                return false;
            }
            state.AwakeningDue = true;
            state.Reason = reason;
            state.AwakeningAt = now;
            return true;
        }
    }
}
