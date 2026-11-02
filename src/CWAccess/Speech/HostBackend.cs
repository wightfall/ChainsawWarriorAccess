using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CWAccess.Speech
{
    /// <summary>
    /// Speech for everything that is not NVDA: a small helper process (CWAccess.SpeechHost.exe,
    /// part of this mod) that uses the JAWS API when JAWS runs and Windows SAPI otherwise.
    /// The game's old Mono runtime cannot use System.Speech or COM itself, so the helper does it.
    /// Protocol: one UTF-8 line per command on stdin: "S text" (interrupt), "Q text" (queue), "X" (stop).
    /// </summary>
    internal sealed class HostBackend : ISpeechBackend
    {
        private readonly string exePath;
        private Process process;
        private Stream input;

        public HostBackend(string pluginDir)
        {
            exePath = Path.Combine(pluginDir, "CWAccess.SpeechHost.exe");
        }

        public string Name => "SAPI/JAWS helper";

        public string LastError { get; private set; }

        public bool IsAvailable()
        {
            return File.Exists(exePath) && EnsureStarted();
        }

        private bool EnsureStarted()
        {
            if (process != null && !process.HasExited) return true;
            try
            {
                var psi = new ProcessStartInfo(exePath, Process.GetCurrentProcess().Id.ToString())
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                };
                process = Process.Start(psi);
                input = process.StandardInput.BaseStream;
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                process = null;
                input = null;
                return false;
            }
        }

        private bool Send(string line)
        {
            if (!EnsureStarted()) return false;
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(line.Replace('\r', ' ').Replace('\n', ' ') + "\n");
                input.Write(bytes, 0, bytes.Length);
                input.Flush();
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                process = null;
                return false;
            }
        }

        public bool Speak(string text, bool interrupt)
        {
            return Send((interrupt ? "S " : "Q ") + text);
        }

        public void Stop()
        {
            Send("X");
        }

        public void Dispose()
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    Send("QUIT");
                    process.WaitForExit(500);
                    if (!process.HasExited) process.Kill();
                }
            }
            catch (Exception)
            {
                // Game shutting down.
            }
            process = null;
        }
    }
}
