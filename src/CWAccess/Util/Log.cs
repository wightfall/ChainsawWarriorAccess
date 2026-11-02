using System;
using System.IO;
using System.Text;

namespace CWAccess
{
    /// <summary>Writes CWAccess\CWAccess.log (new file each start).</summary>
    internal static class Log
    {
        private static StreamWriter writer;

        /// <summary>When on, every spoken line is also written to the log (setting LogSpeech).</summary>
        public static bool LogSpeech = true;

        public static string Path { get; private set; }

        public static void Init(string dir)
        {
            try
            {
                Path = System.IO.Path.Combine(dir, "CWAccess.log");
                writer = new StreamWriter(Path, false, new UTF8Encoding(false)) { AutoFlush = true };
            }
            catch (Exception)
            {
                writer = null;
            }
        }

        private static void Write(string level, string msg)
        {
            if (writer == null) return;
            try
            {
                writer.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " [" + level + "] " + msg);
            }
            catch (Exception)
            {
                // Disk full or file locked: logging is optional.
            }
        }

        public static void Info(string msg)
        {
            Write("Info", msg);
        }

        public static void Warn(string msg)
        {
            Write("Warn", msg);
        }

        public static void Error(string msg)
        {
            Write("Error", msg);
        }

        public static void Speech(string msg)
        {
            if (LogSpeech) Write("say", msg);
        }

        public static void Close()
        {
            try
            {
                if (writer != null) writer.Close();
            }
            catch (Exception)
            {
                // Shutting down.
            }
            writer = null;
        }
    }
}
