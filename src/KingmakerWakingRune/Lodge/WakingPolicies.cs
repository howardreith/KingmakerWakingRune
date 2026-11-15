namespace KingmakerWakingRune.Lodge
{
    /// <summary>Pure once-only and briefing decisions. No game types, so they are tested directly.</summary>
    internal static class WakingPolicies
    {
        public enum CastAction
        {
            /// <summary>Spawn this member (first initialization of this save only).</summary>
            Spawn,
            /// <summary>Exactly one present, or nothing to do.</summary>
            Keep,
            /// <summary>Duplicate or a missing member after setup was recorded: report, never repair.</summary>
            Report,
        }

        /// <summary>WR-02 cast setup is recorded once per save; afterwards members are verified only.</summary>
        public static CastAction DecideMember(bool setupRecorded, int presentCount)
        {
            if (presentCount > 1)
            {
                return CastAction.Report;
            }
            if (setupRecorded)
            {
                return presentCount == 1 ? CastAction.Keep : CastAction.Report;
            }
            return presentCount == 0 ? CastAction.Spawn : CastAction.Keep;
        }

        /// <summary>A grant happens only while its save-bound flag is unset.</summary>
        public static bool ShouldGrant(bool alreadyGranted)
        {
            return !alreadyGranted;
        }

        /// <summary>XP to add so the unit reaches exactly the target; never removes earned XP.</summary>
        public static int ProtagonistXpDelta(int currentXp, int targetXp)
        {
            return currentXp < targetXp ? targetXp - currentXp : 0;
        }

        /// <summary>Money adjustment that makes the treasury exactly the configured amount.</summary>
        public static long TreasuryDelta(long currentMoney, long target)
        {
            return target - currentMoney;
        }

        /// <summary>
        /// The one-time credit for a lodge save that received the earlier, smaller grant: the difference, never a reset to
        /// the new amount, and nothing once the expedition has left (a returned party gets no second allowance).
        /// </summary>
        public static long TreasurySupplement(bool oldGrantReceived, bool supplementApplied, bool departed, long previousGrant, long currentGrant)
        {
            return oldGrantReceived && !supplementApplied && !departed && currentGrant > previousGrant ? currentGrant - previousGrant : 0;
        }

        /// <summary>Native custom-companion creation grants class starting gold; recruitment must not change the treasury.</summary>
        public static long RecruitGoldRefund(long moneyBefore, long moneyAfter)
        {
            return moneyAfter > moneyBefore ? moneyAfter - moneyBefore : 0;
        }

        /// <summary>The briefing opens only for an in-lodge session that has not finished it and whose cast is ready.</summary>
        public static bool ShouldOpenBriefing(bool inLodge, bool sessionMarked, bool briefingComplete, bool cueOwnersReady, bool modalOrLoadingBusy)
        {
            return inLodge && sessionMarked && !briefingComplete && cueOwnersReady && !modalOrLoadingBusy;
        }
    }
}
