using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CWAccess
{
    /// <summary>One option; changing Value saves the settings file.</summary>
    internal sealed class Setting<T>
    {
        public readonly string Key;
        public readonly string Description;
        private T value;

        public Setting(string key, T defaultValue, string description)
        {
            Key = key;
            value = defaultValue;
            Description = description;
        }

        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                Settings.Save();
            }
        }

        internal void Load(string text)
        {
            try
            {
                value = (T)Convert.ChangeType(text, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                Log.Warn("Bad value for " + Key + ": " + text);
            }
        }

        internal string Text => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Options in CWAccess\CWAccess.ini (also changeable in game with F2).</summary>
    internal static class Settings
    {
        public static readonly Setting<bool> AnnounceActions = new Setting<bool>("AnnounceActions", true,
            "Say the available choices (Shoot, Hand-to-hand, Escape...) whenever they change.");

        public static readonly Setting<bool> AnnounceDice = new Setting<bool>("AnnounceDice", true,
            "Say every dice roll, yours and the enemy's.");

        public static readonly Setting<bool> AnnounceStats = new Setting<bool>("AnnounceStats", true,
            "Say changes of wounds, endurance, venom, radiation, skills and equipment points.");

        public static readonly Setting<bool> AnnounceEquipment = new Setting<bool>("AnnounceEquipment", true,
            "Say equipment you gain or lose and changes of uses and spare clips.");

        public static readonly Setting<int> TimeEveryMinutes = new Setting<int>("TimeEveryMinutes", 10,
            "Say the time left whenever it passes a multiple of this many minutes (0 = never).");

        public static readonly Setting<bool> ReadTutorials = new Setting<bool>("ReadTutorials", true,
            "Read the tutorial boxes that point at parts of the screen.");

        public static readonly Setting<bool> LogSpeech = new Setting<bool>("LogSpeech", true,
            "Write everything the mod says to CWAccess.log.");

        public static readonly Setting<bool> CommandFile = new Setting<bool>("CommandFile", false,
            "Developer testing: read commands from CWAccess\\commands.txt and keep running while the window is in the background.");

        private static string path;
        private static bool loading;

        public static void Load(string dir)
        {
            path = Path.Combine(dir, "CWAccess.ini");
            loading = true;
            try
            {
                if (File.Exists(path))
                {
                    var values = new Dictionary<string, string>();
                    foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                    {
                        string line = raw.Trim();
                        if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";") || line.StartsWith("[")) continue;
                        int eq = line.IndexOf('=');
                        if (eq > 0) values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                    }
                    if (values.TryGetValue(AnnounceActions.Key, out string v)) AnnounceActions.Load(v);
                    if (values.TryGetValue(AnnounceDice.Key, out v)) AnnounceDice.Load(v);
                    if (values.TryGetValue(AnnounceStats.Key, out v)) AnnounceStats.Load(v);
                    if (values.TryGetValue(AnnounceEquipment.Key, out v)) AnnounceEquipment.Load(v);
                    if (values.TryGetValue(TimeEveryMinutes.Key, out v)) TimeEveryMinutes.Load(v);
                    if (values.TryGetValue(ReadTutorials.Key, out v)) ReadTutorials.Load(v);
                    if (values.TryGetValue(LogSpeech.Key, out v)) LogSpeech.Load(v);
                    if (values.TryGetValue(CommandFile.Key, out v)) CommandFile.Load(v);
                }
            }
            catch (Exception e)
            {
                Log.Error("Could not read settings: " + e.Message);
            }
            loading = false;
            Log.LogSpeech = LogSpeech.Value;
            if (CommandFile.Value) UnityEngine.Application.runInBackground = true;
            Save();
        }

        public static void Save()
        {
            if (loading || path == null) return;
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# Chainsaw Warrior Access settings. Also changeable in the game with F2.");
                Append(sb, AnnounceActions);
                Append(sb, AnnounceDice);
                Append(sb, AnnounceStats);
                Append(sb, AnnounceEquipment);
                Append(sb, TimeEveryMinutes);
                Append(sb, ReadTutorials);
                Append(sb, LogSpeech);
                Append(sb, CommandFile);
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Log.Error("Could not save settings: " + e.Message);
            }
        }

        private static void Append<T>(StringBuilder sb, Setting<T> s)
        {
            sb.AppendLine();
            sb.AppendLine("# " + s.Description);
            sb.AppendLine(s.Key + " = " + s.Text);
        }
    }
}
