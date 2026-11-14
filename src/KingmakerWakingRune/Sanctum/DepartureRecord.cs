namespace KingmakerWakingRune.Sanctum
{
    /// <summary>What the saved departure record means for the next use of the Refuge Token.</summary>
    internal enum DepartureReadiness
    {
        /// <summary>No record: ready the expedition now, once.</summary>
        Refresh,

        /// <summary>This departure was already readied (a transfer that did not complete is being retried): nothing again.</summary>
        Reuse,

        /// <summary>A record of the earlier two-use flow ("rested, awaiting the second use"): dropped, then ready once.</summary>
        Legacy,
    }

    /// <summary>
    /// The departure's saved record (WR-PLAYTEST-01). Using the token readies the expedition once and writes a marker; the
    /// marker stays until the arrival is committed, so a retry after a failed transfer, or a reload of the checkpoint written
    /// before leaving, departs as readied and never refreshes twice. Any other payload is the earlier flow's selections
    /// record, which is dropped without reopening its dialogues. Pure, so the decision is unit-tested.
    /// </summary>
    internal static class DepartureRecord
    {
        public const string ReadyPrefix = "ready:";

        public static string Ready(double gameTimeSeconds)
        {
            return ReadyPrefix + gameTimeSeconds.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static DepartureReadiness Classify(string payload)
        {
            if (payload == null)
            {
                return DepartureReadiness.Refresh;
            }
            return payload.StartsWith(ReadyPrefix, System.StringComparison.Ordinal) ? DepartureReadiness.Reuse : DepartureReadiness.Legacy;
        }
    }
}
