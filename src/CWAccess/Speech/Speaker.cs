using System;
using System.Collections.Generic;

namespace CWAccess.Speech
{
    /// <summary>
    /// Front door for all speech. Picks NVDA when it runs, otherwise the SAPI/JAWS helper,
    /// keeps a history of everything spoken and lets callers queue or interrupt.
    /// </summary>
    internal static class Speaker
    {
        private static readonly NvdaBackend nvda = new NvdaBackend();
        private static HostBackend host;
        private static ISpeechBackend current;
        private static float nextCheck;
        private static string lastSpoken = string.Empty;
        private static float lastSpokenTime;

        public const int HistorySize = 100;

        /// <summary>Everything spoken, newest last.</summary>
        public static readonly List<string> History = new List<string>();

        public static string BackendName => current != null ? current.Name : "none";

        public static void Initialize(string pluginDir)
        {
            nvda.Load(pluginDir);
            host = new HostBackend(pluginDir);
            Detect();
        }

        private static bool forceHost;

        /// <summary>Testing: "host" uses the SAPI/JAWS helper even when NVDA runs; anything else goes back to automatic.</summary>
        public static void Force(string which)
        {
            forceHost = which == "host";
            Detect();
        }

        /// <summary>Choose the backend again (NVDA may have been started or closed).</summary>
        public static void Detect()
        {
            if (forceHost && host != null && host.IsAvailable()) current = host;
            else if (nvda.IsAvailable()) current = nvda;
            else if (host != null && host.IsAvailable()) current = host;
            else current = null;
            Log.Info("Speech backend: " + BackendName);
        }

        /// <summary>Called every frame: re-checks NVDA every few seconds so starting or closing it while playing works.</summary>
        public static void Tick(float now)
        {
            if (now < nextCheck || forceHost) return;
            nextCheck = now + 5f;
            bool nvdaUp = nvda.IsAvailable();
            if (nvdaUp && current != nvda) Detect();
            else if (!nvdaUp && current == nvda) Detect();
        }

        /// <summary>Speak text, cutting off whatever is being said.</summary>
        public static void Say(string text)
        {
            Output(text, true);
        }

        /// <summary>Speak text after whatever is being said.</summary>
        public static void Queue(string text)
        {
            Output(text, false);
        }

        public static void Output(string text, bool interrupt)
        {
            text = TextUtil.Clean(text);
            if (string.IsNullOrEmpty(text)) return;
            float now = UnityEngine.Time.realtimeSinceStartup;
            // The game sometimes sets the same text twice in one frame; say it once.
            if (!interrupt && text == lastSpoken && now - lastSpokenTime < 0.3f) return;
            lastSpoken = text;
            lastSpokenTime = now;
            History.Add(text);
            if (History.Count > HistorySize) History.RemoveAt(0);
            Log.Speech(text);
            if (current == null) return;
            try
            {
                if (!current.Speak(text, interrupt))
                {
                    Detect();
                    if (current != null) current.Speak(text, interrupt);
                }
            }
            catch (Exception e)
            {
                Log.Error("Speech failed: " + e.Message);
            }
        }

        public static void Stop()
        {
            if (current != null) current.Stop();
        }

        public static void Shutdown()
        {
            if (host != null) host.Dispose();
        }
    }
}
