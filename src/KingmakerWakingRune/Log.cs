using System;
using UnityModManagerNet;

namespace KingmakerWakingRune
{
    /// <summary>One-line, prefixed lifecycle diagnostics in the UMM log. Never call per frame.</summary>
    internal static class Log
    {
        private static UnityModManager.ModEntry.ModLogger s_Logger;

        public static void Initialize(UnityModManager.ModEntry modEntry)
        {
            s_Logger = modEntry.Logger;
        }

        public static void Info(string message)
        {
            s_Logger?.Log(message);
        }

        public static void Error(string message, Exception ex = null)
        {
            s_Logger?.Error(ex == null ? message : message + " " + ex);
        }
    }
}
