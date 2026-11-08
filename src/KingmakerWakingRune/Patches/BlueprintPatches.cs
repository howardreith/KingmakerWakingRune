using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Localization;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Diagnostics;
using KingmakerWakingRune.Lodge;

namespace KingmakerWakingRune.Patches
{
    [HarmonyPatch(typeof(LibraryScriptableObject), nameof(LibraryScriptableObject.LoadDictionary))]
    internal static class LibraryLoadDictionaryPatch
    {
        private static void Postfix(LibraryScriptableObject __instance)
        {
            ScenarioBlueprints.Register(__instance);
            if (ScenarioBlueprints.IsRegistered)
            {
                LodgeSession.Subscribe();
                Sanctum.SanctumSession.Subscribe();
            }
            Recon.DumpBlueprints(__instance);

        }
    }

    [HarmonyPatch(typeof(LocalizationManager), "LoadPack")]
    internal static class LocalizationLoadPackPatch
    {
        private static void Postfix(LocalizationPack __result)
        {
            LocalizedText.InjectInto(__result);
        }
    }

    /// <summary>Resolves the Waking Rune story type to its own DLC-style data; every native type is untouched.</summary>
    [HarmonyPatch(typeof(DlcRoot), nameof(DlcRoot.Get))]
    internal static class DlcRootGetPatch
    {
        private static void Postfix(DlcType type, ref DlcRoot.DlcData __result)
        {
            if (type == ScenarioIds.StoryType && ScenarioBlueprints.StoryDlcData != null)
            {
                __result = ScenarioBlueprints.StoryDlcData;
            }
        }
    }
}
