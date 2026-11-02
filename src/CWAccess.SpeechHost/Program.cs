using System;
using System.Diagnostics;
using System.IO;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;

namespace CWAccess.SpeechHost
{
    /// <summary>
    /// Speech for Chainsaw Warrior Access when NVDA is not running. The game's old Mono runtime cannot use
    /// System.Speech or COM, so the mod starts this helper and sends it lines on standard input:
    ///   "S text" speak now (interrupt), "Q text" speak after the current speech, "X" stop, "QUIT" exit.
    /// Uses the JAWS API when JAWS is running, Windows SAPI otherwise. Exits with the game.
    /// </summary>
    internal static class Program
    {
        private static SpeechSynthesizer sapi;
        private static object jaws;
        private static DateTime nextJawsCheck;

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int gamePid)) WatchGame(gamePid);
            try
            {
                sapi = new SpeechSynthesizer();
                sapi.SetOutputToDefaultAudioDevice();
            }
            catch (Exception)
            {
                sapi = null;
            }
            var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
            string line;
            while ((line = input.ReadLine()) != null)
            {
                if (line == "QUIT") break;
                try
                {
                    Handle(line);
                }
                catch (Exception)
                {
                    // A failed line must not stop later speech.
                }
            }
            return 0;
        }

        private static void Handle(string line)
        {
            if (line == "X")
            {
                Stop();
                return;
            }
            if (line.Length < 2) return;
            bool interrupt = line[0] == 'S';
            string text = line.Substring(2);
            if (UseJaws())
            {
                dynamic j = jaws;
                j.SayString(text, interrupt);
                return;
            }
            if (sapi == null) return;
            if (interrupt) sapi.SpeakAsyncCancelAll();
            sapi.SpeakAsync(text);
        }

        private static void Stop()
        {
            if (UseJaws())
            {
                dynamic j = jaws;
                j.StopSpeech();
            }
            if (sapi != null) sapi.SpeakAsyncCancelAll();
        }

        /// <summary>JAWS is used while it runs; checked every few seconds so starting it mid-game works.</summary>
        private static bool UseJaws()
        {
            if (DateTime.Now < nextJawsCheck) return jaws != null;
            nextJawsCheck = DateTime.Now.AddSeconds(3);
            bool running = Process.GetProcessesByName("jfw").Length > 0;
            if (!running)
            {
                jaws = null;
                return false;
            }
            if (jaws == null)
            {
                try
                {
                    Type t = Type.GetTypeFromProgID("FreedomSci.JawsApi");
                    jaws = t != null ? Activator.CreateInstance(t) : null;
                }
                catch (Exception)
                {
                    jaws = null;
                }
            }
            return jaws != null;
        }

        /// <summary>Ends this helper when the game closes, even if it could not say goodbye.</summary>
        private static void WatchGame(int pid)
        {
            var t = new Thread(() =>
            {
                try
                {
                    Process game = Process.GetProcessById(pid);
                    game.WaitForExit();
                }
                catch (Exception)
                {
                    // Already gone.
                }
                Environment.Exit(0);
            }) { IsBackground = true };
            t.Start();
        }
    }
}
