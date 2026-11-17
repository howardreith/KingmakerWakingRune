using System.Reflection;
using Harmony12;
using Kingmaker.EntitySystem.Persistence;
using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Patches
{
    /// <summary>
    /// The native writer creates the record a requested save becomes inside its own routine (SaveManager.SaveRoutine calls
    /// PrepareSave on a new SaveInfo). A checkpoint takes that record as its own only while its exact routine is the loading
    /// queue's current process, so an unrelated or older save with the same name can never stand in for it. Observation
    /// only: the native save is unchanged.
    /// </summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.PrepareSave))]
    internal static class CheckpointPreparePatch
    {
        private static void Postfix(SaveInfo save)
        {
            CheckpointOperation.ObservePrepared(save);
        }
    }

    /// <summary>The harness's injected write error (inert unless armed): the native commit of that one archive throws, and the native failure path runs.</summary>
    [HarmonyPatch]
    internal static class CheckpointCommitFaultPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(SaveManager).Assembly.GetType("Kingmaker.EntitySystem.Persistence.ZipSaver", true), "Save");
        }

        private static void Prefix(object __instance)
        {
            CheckpointOperation.BeforeCommit(__instance);
        }
    }
}
