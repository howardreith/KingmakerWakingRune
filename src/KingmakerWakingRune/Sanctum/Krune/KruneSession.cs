namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// The encounter's transient combat state is never carried across a load: the director's cached plan, hold-backs and
    /// issued cast, what Krune last saw, the tentacles' round rolls and a pending rod order. Everything that must persist is
    /// in the saved sanctum state instead. Cleared whenever an area unloads and whenever one has loaded, so loading an
    /// earlier save in the same process starts from that save alone.
    /// </summary>
    internal static class KruneSession
    {
        public static void Reset(string why)
        {
            KruneDirector.ResetSession();
            KruneSight.ResetSession();
            KwrTentacleArea.ResetSession();
            RuneRod.ResetSession();
            Log.Info("Krune combat session state cleared (" + why + ").");
        }
    }
}
