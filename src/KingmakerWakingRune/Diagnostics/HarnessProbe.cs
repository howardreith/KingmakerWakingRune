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
            AnswerCommand();
        }

        /// <summary>'command.request' holds the response path on its first line and the labelled test command on its second.</summary>
        private static void AnswerCommand()
        {
            string request = Path.Combine(Main.Mod.Path, "command.request");
            if (!File.Exists(request))
            {
                return;
            }
            string[] lines = File.ReadAllLines(request);
            File.Delete(request);
            string result;
            try
            {
                result = WardHarness.Run(lines.Length > 1 ? lines[1] : string.Empty);
            }
            catch (Exception ex)
            {
                result = "command failed: " + ex;
                Log.Error($"Harness command failed: {string.Join(" ", lines)}", ex);
            }
            Log.Info($"HARNESS {string.Join(" | ", lines)} => {result}");
            File.WriteAllText(lines[0], result);
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
