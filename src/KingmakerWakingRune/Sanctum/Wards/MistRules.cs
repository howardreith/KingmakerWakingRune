using System;

// Saved fields: some are only ever written by the native adapters or the JSON reader.
#pragma warning disable CS0649

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>A creature's standing with the A5 mist. Saved per creature, with no per-frame or particle-derived values.</summary>
    internal sealed class MistRecord
    {
        public string Actor = string.Empty;
        public int ConsecutiveFailures;
        public bool Asleep;
        /// <summary>When this creature's exposure next falls due in real time; turn ends advance it too, so modes cannot double-tick.</summary>
        public double NextDueAt = -1d;
        /// <summary>The simulation time this creature last ended a full interval outside the mist, or below zero.</summary>
        public double LastOutsideAt = -1d;
        /// <summary>Ever exposed to a real mist tick; gates the printed study of the vapor.</summary>
        public bool Exposed;
    }

    internal enum MistExposure
    {
        NotDue,
        NoEffect,
        Resisted,
        Staggered,
        FellAsleep,
    }

    /// <summary>
    /// One exposure per creature per simulated round. Failing three times in a row puts it to sleep; a success or a full
    /// round outside breaks the run, and waking resets it. The stagger is the printed action restriction only.
    /// </summary>
    internal static class MistRules
    {
        /// <summary>Whether a real-time six-second tick is due for this creature now. Turn ends use <see cref="EndTurn"/> instead.</summary>
        public static bool IsDue(MistRecord record, double now)
        {
            return record.NextDueAt < 0d || now >= record.NextDueAt;
        }

        /// <summary>Starts the interval after any tick or turn end.</summary>
        public static void Schedule(MistRecord record, double now)
        {
            record.NextDueAt = now + WardProfile.RoundSeconds;
        }

        /// <summary>Ends a round spent outside the mist: a full one breaks the failure streak.</summary>
        public static void EndRoundOutside(MistRecord record, double now)
        {
            if (record.Asleep)
            {
                return;
            }
            record.ConsecutiveFailures = 0;
            record.LastOutsideAt = now;
        }

        /// <summary>Judges one exposure from the real Will save. <paramref name="immune"/> covers Krune and natural exemptions.</summary>
        public static MistExposure EndRoundInside(MistRecord record, bool immune, bool savePassed, bool sleepImmune)
        {
            if (immune || record.Asleep)
            {
                return MistExposure.NoEffect;
            }
            if (savePassed)
            {
                record.ConsecutiveFailures = 0;
                return MistExposure.Resisted;
            }
            record.ConsecutiveFailures++;
            if (record.ConsecutiveFailures >= WardProfile.MistSleepFailures && !sleepImmune)
            {
                record.Asleep = true;
                return MistExposure.FellAsleep;
            }
            return MistExposure.Staggered;
        }

        /// <summary>Damage or an ally's standard action. A later sleep needs a fresh three failures.</summary>
        public static void Wake(MistRecord record)
        {
            record.Asleep = false;
            record.ConsecutiveFailures = 0;
        }

        /// <summary>+4 resistance for a creature Krune summoned, not stacking with any larger resistance bonus it has.</summary>
        public static int KruneSummonResistanceBonus(int existingResistanceBonus)
        {
            return Math.Max(0, WardProfile.MistKruneSummonResistance - Math.Max(0, existingResistanceBonus));
        }
    }
}
