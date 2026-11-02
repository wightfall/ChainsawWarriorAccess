using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CWAccess.Speech
{
    /// <summary>
    /// Talks to NVDA through NV Access's official controller client (32-bit build, renamed
    /// nvdaControllerClient32.dll). Speech and braille both go through it.
    /// </summary>
    internal sealed class NvdaBackend : ISpeechBackend
    {
        private const string Dll = "nvdaControllerClient32.dll";

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryW(string path);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        private static extern int nvdaController_testIfRunning();

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int nvdaController_speakText(string text);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        private static extern int nvdaController_cancelSpeech();

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int nvdaController_brailleMessage(string text);

        private bool loaded;

        public string Name => "NVDA";

        public bool Load(string pluginDir)
        {
            if (loaded) return true;
            try
            {
                string path = Path.Combine(pluginDir, Dll);
                if (!File.Exists(path)) return false;
                if (LoadLibraryW(path) == IntPtr.Zero) return false;
                loaded = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsAvailable()
        {
            if (!loaded) return false;
            try
            {
                return nvdaController_testIfRunning() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Speak(string text, bool interrupt)
        {
            try
            {
                if (interrupt) nvdaController_cancelSpeech();
                bool ok = nvdaController_speakText(text) == 0;
                nvdaController_brailleMessage(text);
                return ok;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Stop()
        {
            try
            {
                nvdaController_cancelSpeech();
            }
            catch (Exception)
            {
                // NVDA closed: nothing to stop.
            }
        }

        public void Dispose()
        {
        }
    }
}
