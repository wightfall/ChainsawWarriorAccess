using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CWAccess.Game
{
    /// <summary>
    /// Translation of the game's own texts (menus, cards, messages, tutorials) for speech only: the screen keeps the
    /// game's English, whose fonts have no other alphabets. File CWAccess\Languages\game\code.txt:
    ///   [keys]   game text key = translation          (the keys of the game's text table)
    ///   [texts]  English text on screen = translation  (texts the game does not take from its table)
    /// Texts with %name parts (filled in by the game) are matched with those parts as wildcards; a filled-in part
    /// that is itself a known text is translated too. Languages\game\template.txt is written on the player's
    /// computer from the game's own table and the screens' texts (the game's texts are not shipped with the mod).
    /// </summary>
    internal static class GameText
    {
        private sealed class Pattern
        {
            public Regex Match;
            public string Translation;
            public string[] Names;
        }

        private static Dictionary<string, string> byKey = new Dictionary<string, string>();
        private static Dictionary<string, string> byText = new Dictionary<string, string>();
        private static List<Pattern> patterns;
        private static readonly Regex Token = new Regex(@"%([A-Za-z]+)");
        private static readonly SortedDictionary<string, bool> screenTexts = new SortedDictionary<string, bool>(StringComparer.Ordinal);
        private static string modDir;
        private static int templateTexts = -1;

        /// <summary>The file in use, for live reloading (null when none).</summary>
        public static string FilePath { get; private set; }

        public static bool Active => byKey.Count > 0 || byText.Count > 0;

        /// <summary>The game's text table (key -> English), null before the game loaded it.</summary>
        public static Dictionary<string, string> Table => R.GetStatic<Dictionary<string, string>>(typeof(CLocalisationManager), "mpCurrentLanguage");

        public static int KeyCount => byKey.Count;

        public static int TextCount => byText.Count;

        public static void Use(string dir, string code)
        {
            modDir = dir;
            byKey = new Dictionary<string, string>();
            byText = new Dictionary<string, string>();
            patterns = null;
            FilePath = null;
            if (dir == null || code == "en") return;
            string path = Path.Combine(GameDir(dir), code + ".txt");
            if (!File.Exists(path)) return;
            FilePath = path;
            try
            {
                Load(LanguageFile.Load(path));
                Log.Info("Game texts: " + byKey.Count + " keys and " + byText.Count + " screen texts translated (" + path + ")");
            }
            catch (Exception e)
            {
                Log.Error("Could not read " + path + ": " + e.Message);
            }
        }

        /// <summary>Takes the translations of a parsed file (also used by the tests).</summary>
        public static void Load(LanguageFile f)
        {
            byKey = new Dictionary<string, string>();
            byText = new Dictionary<string, string>();
            patterns = null;
            if (f.Sections.TryGetValue("keys", out Dictionary<string, string> keys))
            {
                foreach (KeyValuePair<string, string> kv in keys) if (kv.Value.Length > 0) byKey[kv.Key] = kv.Value;
            }
            if (f.Sections.TryGetValue("texts", out Dictionary<string, string> texts))
            {
                foreach (KeyValuePair<string, string> kv in texts) if (kv.Value.Length > 0) byText[TextUtil.CleanRaw(kv.Key)] = kv.Value;
            }
        }

        public static string GameDir(string dir)
        {
            return Path.Combine(Path.Combine(dir, "Languages"), "game");
        }

        /// <summary>The translation of a game text key, or the English text given.</summary>
        public static string ForKey(string key, string english)
        {
            return key != null && byKey.TryGetValue(key, out string t) ? t : english;
        }

        /// <summary>Translates a text as shown on screen (whole text or a known pattern); unchanged when unknown.</summary>
        public static string Speak(string text)
        {
            if (!Active || string.IsNullOrEmpty(text)) return text;
            string clean = TextUtil.CleanRaw(text);
            BuildPatterns();
            if (byText.TryGetValue(clean, out string t)) return t;
            if (patterns == null) return text;
            foreach (Pattern p in patterns)
            {
                Match m = p.Match.Match(clean);
                if (!m.Success) continue;
                string result = p.Translation;
                foreach (string name in p.Names)
                {
                    string value = m.Groups[name].Value;
                    result = result.Replace("%" + name, byText.TryGetValue(value, out string v) ? v : value);
                }
                return result;
            }
            return text;
        }

        /// <summary>From every translated key: its English text (as the game shows it) and translation.</summary>
        private static void BuildPatterns()
        {
            if (patterns != null) return;
            Dictionary<string, string> table = Table;
            if (table == null) return;
            patterns = new List<Pattern>();
            foreach (KeyValuePair<string, string> kv in byKey)
            {
                if (!table.TryGetValue(kv.Key, out string english)) continue;
                english = TextUtil.CleanRaw(english);
                if (english.Length == 0) continue;
                if (english.IndexOf('%') < 0)
                {
                    if (!byText.ContainsKey(english)) byText[english] = kv.Value;
                    continue;
                }
                Pattern p = MakePattern(english, kv.Value);
                if (p != null) patterns.Add(p);
            }
            // Longer patterns first: they are more specific.
            patterns.Sort((a, b) => b.Match.ToString().Length.CompareTo(a.Match.ToString().Length));
        }

        private static Pattern MakePattern(string english, string translation)
        {
            var names = new List<string>();
            string rx = "^" + Token.Replace(Regex.Escape(english), m =>
            {
                string n = m.Groups[1].Value;
                if (names.Contains(n)) return @"\k<" + n + ">";
                names.Add(n);
                return "(?<" + n + ">.+?)";
            }) + "$";
            try
            {
                return new Pattern { Match = new Regex(rx), Translation = translation, Names = names.ToArray() };
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        // ------------------------------------------------------------------ template for translators

        /// <summary>
        /// Called when a scene starts: remembers the texts of its screens that are not in the game's text table and
        /// keeps Languages\game\template.txt up to date with them and with the table.
        /// </summary>
        public static void CollectScene()
        {
            try
            {
                Dictionary<string, string> table = Table;
                var known = new HashSet<string>();
                if (table != null) foreach (string v in table.Values) known.Add(TextUtil.CleanRaw(v));
                foreach (UnityEngine.Object o in Resources.FindObjectsOfTypeAll(typeof(UILabel)))
                {
                    var l = o as UILabel;
                    if (l == null) continue;
                    string t = TextUtil.CleanRaw(l.text);
                    if (IsTranslatable(t) && !known.Contains(t)) screenTexts[t] = true;
                }
                if (modDir != null && table != null && screenTexts.Count != templateTexts) WriteTemplate(modDir);
            }
            catch (Exception e)
            {
                Log.Error("Collecting screen texts failed: " + e.Message);
            }
        }

        /// <summary>Texts worth translating: something with letters, not a placeholder of the scene files.</summary>
        public static bool IsTranslatable(string t)
        {
            if (string.IsNullOrEmpty(t) || t == "New Label" || t == "Label" || t == "Text") return false;
            int letters = 0;
            foreach (char c in t) if (char.IsLetter(c)) letters++;
            return letters >= 2;
        }

        /// <summary>The "##### " headings of the game's text keys, in order.</summary>
        public static readonly string[] CategoryOrder =
        {
            "Menus and screens", "Character creation", "Messages", "Enemies", "Traps", "Rooms", "Equipment", "End of game and achievements", "Other texts",
        };

        /// <summary>The heading a key of the game's text table belongs under (by the part before its first "_").</summary>
        public static string Category(string key)
        {
            int u = key.IndexOf('_');
            string p = u > 0 ? key.Substring(0, u) : key;
            switch (p)
            {
                case "Universal": case "FrontEnd": case "Splash": case "Options": case "Pause": case "Save": case "Load": case "Quit":
                case "Difficulty": case "Help": case "Credits": case "SocialMedia": case "MoGraphics": case "Reveal": case "LeaderBoard":
                    return "Menus and screens";
                case "CharCreation": case "Attribute": case "ST": case "Skill":
                    return "Character creation";
                case "Announcement": case "PlayerAction": case "LuckyShot": case "ExtraWound": case "Tutorial": case "CardBack":
                    return "Messages";
                case "Enemy": case "EnemySelect":
                    return "Enemies";
                case "Trap":
                    return "Traps";
                case "Other":
                    return "Rooms";
                case "Item": case "EquipmentSelect":
                    return "Equipment";
                case "EndGame": case "PersonalStats":
                    return "End of game and achievements";
            }
            return p.StartsWith("Achievement", StringComparison.Ordinal) ? "End of game and achievements" : "Other texts";
        }

        /// <summary>Writes the template: the game's whole text table under [keys] and the screens' texts under [texts].</summary>
        public static string WriteTemplate(string dir)
        {
            string folder = GameDir(dir);
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "template.txt");
            var sb = new StringBuilder();
            sb.Append("# Language: (name of your language)\r\n");
            sb.Append("# Translation of Chainsaw Warrior's own texts, spoken by the mod (the screen stays English).\r\n");
            sb.Append("# Copy this file to <code>.txt (th.txt, de.txt ...) next to it and translate the right side of each line.\r\n");
            sb.Append("# Keep %words (the game fills them in) and \\n (line breaks). Lines you leave in English stay English.\r\n");
            sb.Append("# Written by the mod from the game on this computer; it grows as you visit more screens.\r\n");
            sb.Append("# Search for ##### to jump between the groups.\r\n");
            sb.Append("[keys]\r\n");
            Dictionary<string, string> table = Table;
            if (table != null)
            {
                var lines = new List<KeyValuePair<string, string>>();
                foreach (KeyValuePair<string, string> kv in table) lines.Add(new KeyValuePair<string, string>(kv.Key, OneLine(kv.Value)));
                LanguageFile.AppendGrouped(sb, lines, Category, CategoryOrder);
            }
            sb.Append("\r\n##### Screen texts (not in the game's table)\r\n");
            sb.Append("[texts]\r\n");
            foreach (string t in screenTexts.Keys)
            {
                // A line's key ends at the first "=", and "[...]" or "#..." lines mean something else: such texts cannot be listed.
                if (t.IndexOf('=') < 0 && !t.StartsWith("[") && !t.StartsWith("#")) sb.Append(t).Append(" = ").Append(t).Append("\r\n");
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
            templateTexts = screenTexts.Count;
            return path;
        }

        private static string OneLine(string s)
        {
            return (s ?? string.Empty).Replace("\r", string.Empty).Replace("\n", "\\n");
        }
    }
}
