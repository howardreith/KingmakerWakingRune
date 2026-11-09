using System.IO;

namespace KingmakerWakingRune.Diagnostics
{
    /// <summary>
    /// Test-harness faults for proving the failure paths, inert unless 'capture.flag' exists in the mod folder. The harness
    /// writes 'fault.request' naming one fault; it is consumed by the first check that matches and logged as injected.
    /// </summary>
    internal static class FaultInjection
    {
        public static bool Take(string fault)
        {
            if (Main.Mod == null || !File.Exists(Path.Combine(Main.Mod.Path, "capture.flag")))
            {
                return false;
            }
            string request = Path.Combine(Main.Mod.Path, "fault.request");
            if (!File.Exists(request) || File.ReadAllText(request).Trim() != fault)
            {
                return false;
            }
            File.Delete(request);
            return true;
        }
    }
}
