using System;
using System.Collections.Generic;
using System.Linq;

// Saved fields: some are only ever written by the native adapters or the JSON reader.
#pragma warning disable CS0649

namespace KingmakerWakingRune.Sanctum.Wards
{
    internal sealed class FeederGrant
    {
        public int Amount;
        /// <summary>Hit points still available from this grant. Spent points never return.</summary>
        public int Remaining;
        public double GrantedAt;
        public double ExpiresAt;
    }

    internal sealed class FeederState
    {
        public List<FeederGrant> Grants = new List<FeederGrant>();
        /// <summary>Set once when a grant runs out naturally with points left. Survives the buff; one round, once.</summary>
        public bool AccelerationEligible;
        public int TouchesHit;
        public int TouchesMissed;
        public double SuppressedUntil = -1d;
        public bool Disarmed;
    }

    /// <summary>
    /// The Krune Feeder at its printed high-tier block: 10d6 rolled once, half again, fixed regardless of Krune's later
    /// weakening. Same-source temporary hit points do not stack: only the strongest eligible unexpired grant shows.
    /// </summary>
    internal static class FeederRules
    {
        /// <summary>The empowered total for a single 10d6 roll, half again with normal integer rounding.</summary>
        public static int EmpoweredDamage(int rolled)
        {
            return rolled * 3 / 2;
        }

        /// <summary>Vampiric Touch steals only what the donor can give: its hit points left plus its Constitution score.</summary>
        public static int StolenAmount(int damageDealt, int donorHitPoints, int donorConstitution)
        {
            return Math.Max(0, Math.Min(damageDealt, Math.Max(0, donorHitPoints) + Math.Max(0, donorConstitution)));
        }

        public static void Grant(FeederState state, int amount, double now)
        {
            if (amount <= 0)
            {
                return;
            }
            Expire(state, now);
            state.Grants.Add(new FeederGrant { Amount = amount, Remaining = amount, GrantedAt = now, ExpiresAt = now + WardProfile.FeederTempHpSeconds });
        }

        /// <summary>Removes elapsed grants; a grant ending with points left earns the one-time acceleration.</summary>
        public static void Expire(FeederState state, double now)
        {
            foreach (FeederGrant grant in state.Grants.Where(g => now >= g.ExpiresAt).ToList())
            {
                if (grant.Remaining > 0)
                {
                    state.AccelerationEligible = true;
                }
                state.Grants.Remove(grant);
            }
        }

        public static int ExposedTemporaryHP(FeederState state, double now)
        {
            int best = 0;
            foreach (FeederGrant grant in state.Grants)
            {
                if (now < grant.ExpiresAt)
                {
                    best = Math.Max(best, grant.Remaining);
                }
            }
            return best;
        }

        /// <summary>Damage to the shield: every grant is capped at what remains, so an older grant cannot resurrect spent points.</summary>
        public static void Consume(FeederState state, int damage, double now)
        {
            int left = Math.Max(0, ExposedTemporaryHP(state, now) - damage);
            foreach (FeederGrant grant in state.Grants)
            {
                grant.Remaining = Math.Min(grant.Remaining, left);
            }
        }

        public static void Suppress(FeederState state, int rounds, double now)
        {
            state.SuppressedUntil = now + rounds * WardProfile.RoundSeconds;
        }

        public static bool IsArmed(FeederState state, double now)
        {
            return !state.Disarmed && now >= state.SuppressedUntil;
        }
    }
}
