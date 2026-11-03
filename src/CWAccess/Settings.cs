using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CWAccess
{
    internal interface ISetting
    {
        string Key { get; }
        string Description { get; }
        string Text { get; }
        void Load(string text);
        void Reset();
    }

    /// <summary>One option; changing Value saves the settings file.</summary>
    internal sealed class Setting<T> : ISetting
    {
        private readonly T defaultValue;
        private T value;

        public Setting(string key, T defaultValue, string description)
        {
            Key = key;
            this.defaultValue = defaultValue;
            value = defaultValue;
            Description = description;
            Settings.Register(this);
        }

        public string Key { get; }

        public string Description { get; }

        public T Default => defaultValue;

        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                Settings.Save();
            }
        }

        public void Load(string text)
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

        public void Reset()
        {
            value = defaultValue;
        }

        public string Text => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Options in CWAccess\CWAccess.ini (all changeable in the game with F2).</summary>
    internal static class Settings
    {
        private static readonly List<ISetting> all = new List<ISetting>();

        public static IList<ISetting> All => all;

        internal static void Register(ISetting s)
        {
            all.Add(s);
        }

        // ---- Speech
        public static readonly Setting<string> Language = new Setting<string>("Language", "auto",
            "Language of the mod: auto (the system language when the mod has it), en, th, or the code of any file in CWAccess\\Languages.");

        public static readonly Setting<bool> SpeakRoles = new Setting<bool>("SpeakRoles", true,
            "Say the kind of control after its name (button, slider...).");

        public static readonly Setting<bool> AnnouncePosition = new Setting<bool>("AnnouncePosition", true,
            "Say where the item is in the list of the screen (for example New Game, button, 1 of 10).");

        public static readonly Setting<bool> AnnounceActions = new Setting<bool>("AnnounceActions", true,
            "Say the available choices (Shoot, Hand-to-hand, Escape...) whenever they change.");

        public static readonly Setting<bool> AnnounceDice = new Setting<bool>("AnnounceDice", true,
            "Say every dice roll, yours and the enemy's.");

        public static readonly Setting<bool> AnnounceStats = new Setting<bool>("AnnounceStats", true,
            "Say changes of wounds, endurance, venom, radiation, skills and equipment points.");

        public static readonly Setting<bool> AnnounceEquipment = new Setting<bool>("AnnounceEquipment", true,
            "Say equipment you gain or lose and changes of uses and spare clips.");

        public static readonly Setting<bool> AnnounceRoom = new Setting<bool>("AnnounceRoom", true,
            "Say the room on the back of each new house card.");

        public static readonly Setting<bool> FullCardOnTurn = new Setting<bool>("FullCardOnTurn", true,
            "Read the whole card text when a card is turned over (off: only its name and kind; press C for the rest).");

        public static readonly Setting<int> TimeEveryMinutes = new Setting<int>("TimeEveryMinutes", 10,
            "Say the time left whenever it passes a multiple of this many minutes (0 = never).");

        public static readonly Setting<bool> ReadTutorials = new Setting<bool>("ReadTutorials", true,
            "Read the tutorial boxes that point at parts of the screen.");

        public static readonly Setting<bool> RepeatMessageOnReturn = new Setting<bool>("RepeatMessageOnReturn", true,
            "Back on the game board from a menu or card screen, say the last message again.");

        public static readonly Setting<bool> DescribeComic = new Setting<bool>("DescribeComic", true,
            "Describe the pictures of the intro comic.");

        // ---- Sound
        public static readonly Setting<bool> SoundCues = new Setting<bool>("SoundCues", true,
            "Play short sounds for moving, edges, cards, enemies, dice, damage and more.");

        public static readonly Setting<int> CueVolume = new Setting<int>("CueVolume", 70,
            "Overall volume of the sound cues, 0 to 100.");

        // ---- Debug
        public static readonly Setting<bool> LogSpeech = new Setting<bool>("LogSpeech", true,
            "Write everything the mod says to CWAccess.log.");

        public static readonly Setting<bool> CommandFile = new Setting<bool>("CommandFile", false,
            "Developer testing: read commands from CWAccess\\commands.txt and keep running while the window is in the background.");

        /// <summary>Volume of each sound cue (0-100), key "Cue.Name".</summary>
        public static readonly Dictionary<Audio.Cue, Setting<int>> CueVolumes = MakeCueVolumes();

        private static string path;
        private static bool loading = true;

        private static Dictionary<Audio.Cue, Setting<int>> MakeCueVolumes()
        {
            var d = new Dictionary<Audio.Cue, Setting<int>>();
            foreach (Audio.Cue c in Enum.GetValues(typeof(Audio.Cue)))
            {
                d[c] = new Setting<int>("Cue." + c, 100, "Volume of the " + c + " sound, 0 to 100.");
            }
            return d;
        }

        public static void Load(string dir)
        {
            path = Path.Combine(dir, "CWAccess.ini");
            loading = true;
            try
            {
                if (File.Exists(path))
                {
                    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                    {
                        string line = raw.Trim();
                        if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";") || line.StartsWith("[")) continue;
                        int eq = line.IndexOf('=');
                        if (eq > 0) values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                    }
                    foreach (ISetting s in all)
                    {
                        if (values.TryGetValue(s.Key, out string v)) s.Load(v);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Could not read settings: " + e.Message);
            }
            loading = false;
            Log.LogSpeech = LogSpeech.Value;
            if (CommandFile.Value) RunInBackground();
            Save();
        }

        /// <summary>Separate so that loading settings works outside Unity too (the tests).</summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RunInBackground()
        {
            UnityEngine.Application.runInBackground = true;
        }

        /// <summary>Every option back to its default (CommandFile, a developer switch, is kept).</summary>
        public static void ResetAll()
        {
            bool cmd = CommandFile.Value;
            loading = true;
            foreach (ISetting s in all) s.Reset();
            loading = false;
            CommandFile.Load(cmd ? "True" : "False");
            Log.LogSpeech = LogSpeech.Value;
            Save();
        }

        public static void Save()
        {
            if (loading || path == null) return;
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# Chainsaw Warrior Access settings. All of them can be changed in the game with F2.");
                foreach (ISetting s in all)
                {
                    sb.AppendLine();
                    sb.AppendLine("# " + s.Description);
                    sb.AppendLine(s.Key + " = " + s.Text);
                }
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Log.Error("Could not save settings: " + e.Message);
            }
        }
    }
}
