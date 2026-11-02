using System;
using System.IO;
using CWAccess.Game;
using CWAccess.Nav;
using CWAccess.Speech;
using UnityEngine;

namespace CWAccess
{
    /// <summary>
    /// Developer testing only (config Debug / CommandFile, off by default): reads commands from
    /// CWAccess\commands.txt so the mod can be driven without the game window having focus.
    /// Commands, one per line: "key UpArrow", "key E shift", "dump", "state", "say text".
    /// </summary>
    internal static class DebugCommands
    {
        private static float nextPoll;
        private static string path;

        public static void Tick()
        {
            if (!Settings.CommandFile.Value) return;
            if (Time.realtimeSinceStartup < nextPoll) return;
            nextPoll = Time.realtimeSinceStartup + 0.2f;
            if (path == null) path = Path.Combine(Entry.ModDir, "commands.txt");
            if (!File.Exists(path)) return;
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
                File.Delete(path);
            }
            catch (Exception)
            {
                return;
            }
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                Log.Info("[cmd] " + line);
                try
                {
                    Run(line);
                }
                catch (Exception e)
                {
                    Log.Error("Command failed: " + e);
                }
            }
        }

        private static void Run(string line)
        {
            string[] p = line.Split(' ');
            switch (p[0])
            {
                case "key":
                    {
                        var key = (KeyCode)Enum.Parse(typeof(KeyCode), p[1], true);
                        bool shift = Array.IndexOf(p, "shift") > 0;
                        bool ctrl = Array.IndexOf(p, "ctrl") > 0;
                        AccessController.InjectKey(key, shift, ctrl);
                        break;
                    }
                case "dump":
                    Diagnostics.DumpScreen();
                    break;
                case "state":
                    Log.Info("Screen " + G.CurrentScreenName + " context " + (Navigator.Context != null ? Navigator.Context.Id : "none")
                             + (G.InGame ? " game state " + G.Game.meGameState : string.Empty));
                    break;
                case "endscreen":
                    {
                        // Shows the game-over screen without ending the game (no scores or Steam stats are written).
                        GameObject end = CUIManager.GetScreen("EndGame");
                        if (end == null) break;
                        end.GetComponent<CEndScreen>().SetEndText(EEndGameType.lose_time, 1234);
                        CUIManager.SetActiveScreen("EndGame", 0.1f, 0.1f, false, true);
                        break;
                    }
                case "speech":
                    Speaker.Force(p.Length > 1 ? p[1] : string.Empty);
                    break;
                case "dumpgo":
                    Diagnostics.DumpObject(line.Substring(7));
                    break;
                case "say":
                    Speaker.Say(line.Substring(4));
                    break;
            }
        }
    }
}
