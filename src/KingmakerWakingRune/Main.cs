using System;
using System.Reflection;
using Harmony12;
using UnityModManagerNet;

namespace KingmakerWakingRune
{
    public static class Main
    {
        internal static UnityModManager.ModEntry Mod;

        // UMM entry point (Info.json EntryMethod).
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
            Log.Initialize(modEntry);
            try
            {
                Log.Info($"Loading {modEntry.Info.Id} {modEntry.Info.Version} ({BuildInfo()})");
                HarmonyInstance.Create(modEntry.Info.Id).PatchAll(Assembly.GetExecutingAssembly());
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to apply patches; the Waking Rune entry will be unavailable.", ex);
                return false;
            }
        }

        private static string BuildInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return $"mvid={assembly.ManifestModule.ModuleVersionId}";
        }
    }
}
