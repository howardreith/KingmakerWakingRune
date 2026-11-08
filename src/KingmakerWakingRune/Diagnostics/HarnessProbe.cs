using System;
using System.IO;
using UnityEngine;

namespace KingmakerWakingRune.Diagnostics
{
    /// <summary>
    /// Test-harness reporting, inert unless a 'capture.flag' file exists in the mod folder. Answers
    /// 'capture.request' with a frame rendered by the game and 'state.request' with a read-only state
    /// report. It never changes game state and is absent from a normal install.
    /// </summary>
    internal sealed class HarnessProbe : MonoBehaviour
    {
        private const int PollFrames = 10;
        private int m_Frames;

        public static void StartIfRequested()
        {
            if (Main.Mod == null || !File.Exists(Path.Combine(Main.Mod.Path, "capture.flag")))
            {
                return;
            }
            var host = new GameObject("KWR_HarnessProbe");
            DontDestroyOnLoad(host);
            host.AddComponent<HarnessProbe>();
            Log.Info("Harness probe active (capture.flag present).");
        }

        private void Update()
        {
            if (++m_Frames < PollFrames)
            {
                return;
            }
            m_Frames = 0;
            Answer("capture.request", ScreenCapture.CaptureScreenshot);
            Answer("state.request", target => File.WriteAllText(target, StateReport.Build()));
            ReconAreas.Poll();
        }

        private static void Answer(string requestFile, Action<string> respond)
        {
            string request = Path.Combine(Main.Mod.Path, requestFile);
            if (!File.Exists(request))
            {
                return;
            }
            try
            {
                string target = File.ReadAllText(request).Trim();
                File.Delete(request);
                respond(target);
            }
            catch (Exception ex)
            {
                Log.Error($"Harness probe could not answer {requestFile}.", ex);
            }
        }
    }
}
