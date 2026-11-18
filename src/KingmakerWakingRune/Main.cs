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
            HarmonyInstance harmony = HarmonyInstance.Create(modEntry.Info.Id);
            try
            {
                Log.Info($"Loading {modEntry.Info.Id} {modEntry.Info.Version} ({BuildInfo()})");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Sanctum.Wards.KurshuNotice.Install(modEntry.Info.Id);
                Diagnostics.HarnessProbe.StartIfRequested();
                return true;
            }
            catch (Exception ex)
            {
                // All or nothing: never leave a partially patched game.
                harmony.UnpatchAll(modEntry.Info.Id);
                Log.Error("Failed to apply patches; all patches were removed and the Waking Rune entry is unavailable.", ex);
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
