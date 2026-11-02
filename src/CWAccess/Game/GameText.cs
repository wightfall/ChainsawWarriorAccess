using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CWAccess.Game
{
    /// <summary>
    /// Optional translation of the game's own texts (cards, messages, menus) for speech only: the screen keeps the
    /// game's English, whose fonts have no other alphabets. File CWAccess\Languages\game\code.txt:
    ///   [keys]   game text key = translation      (keys as in the game's text table; F12 debug "gametemplate")
    ///   [texts]  English text on screen = translation   (texts the game does not take from its table)
    /// Texts with %name parts (filled in by the game) are matched with those parts as wildcards.
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

        public static bool Active => byKey.Count > 0 || byText.Count > 0;

        public static void Use(string modDir, string code)
        {
            byKey = new Dictionary<string, string>();
            byText = new Dictionary<string, string>();
            patterns = null;
            if (modDir == null || code == "en") return;
            string path = Path.Combine(Path.Combine(Path.Combine(modDir, "Languages"), "game"), code + ".txt");
            if (!File.Exists(path)) return;
            try
            {
                LanguageFile f = LanguageFile.Load(path);
                if (f.Sections.TryGetValue("keys", out Dictionary<string, string> keys))
                {
                    foreach (KeyValuePair<string, string> kv in keys) if (kv.Value.Length > 0) byKey[kv.Key] = kv.Value;
                }
                if (f.Sections.TryGetValue("texts", out Dictionary<string, string> texts))
                {
                    foreach (KeyValuePair<string, string> kv in texts) if (kv.Value.Length > 0) byText[TextUtil.CleanRaw(kv.Key)] = kv.Value;
                }
                Log.Info("Game texts: " + byKey.Count + " keys and " + byText.Count + " texts translated (" + path + ")");
            }
            catch (Exception e)
            {
                Log.Error("Could not read " + path + ": " + e.Message);
            }
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
            if (byText.TryGetValue(clean, out string t)) return t;
            BuildPatterns();
            foreach (Pattern p in patterns)
            {
                Match m = p.Match.Match(clean);
                if (!m.Success) continue;
                string result = p.Translation;
                foreach (string name in p.Names) result = result.Replace("%" + name, m.Groups[name].Value);
                return result;
            }
            return text;
        }

        /// <summary>From every translated key: its English text (as the game shows it) and translation.</summary>
        private static void BuildPatterns()
        {
            if (patterns != null) return;
            patterns = new List<Pattern>();
            var table = R.GetStatic<Dictionary<string, string>>(typeof(CLocalisationManager), "mpCurrentLanguage");
            if (table == null) return;
            foreach (KeyValuePair<string, string> kv in byKey)
            {
                if (!table.TryGetValue(kv.Key, out string english)) continue;
                english = TextUtil.CleanRaw(english);
                if (english.IndexOf('%') < 0)
                {
                    if (!byText.ContainsKey(english)) byText[english] = kv.Value;
                    continue;
                }
                var names = new List<string>();
                string rx = "^" + Token.Replace(Regex.Escape(english).Replace(@"\%", "%"), m =>
                {
                    string n = m.Groups[1].Value;
                    if (names.Contains(n)) return @"\k<" + n + ">";
                    names.Add(n);
                    return "(?<" + n + ">.+?)";
                }) + "$";
                try
                {
                    patterns.Add(new Pattern { Match = new Regex(rx), Translation = kv.Value, Names = names.ToArray() });
                }
                catch (ArgumentException)
                {
                    // Not a usable pattern; the text stays English.
                }
            }
            // Longer patterns first: they are more specific.
            patterns.Sort((a, b) => b.Match.ToString().Length.CompareTo(a.Match.ToString().Length));
        }

        /// <summary>Debug: writes every game text as a translation template (stays on the player's computer).</summary>
        public static string WriteTemplate(string modDir)
        {
            var table = R.GetStatic<Dictionary<string, string>>(typeof(CLocalisationManager), "mpCurrentLanguage");
            string dir = Path.Combine(Path.Combine(modDir, "Languages"), "game");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "template.txt");
            var sb = new System.Text.StringBuilder();
            sb.Append("# Language: (name of your language)\r\n# Translation of the game's texts for speech. Keep %words as they are.\r\n[keys]\r\n");
            if (table != null)
            {
                foreach (KeyValuePair<string, string> kv in table) sb.Append(kv.Key).Append(" = ").Append(kv.Value.Replace("\r", string.Empty).Replace("\n", "\\n")).Append("\r\n");
            }
            sb.Append("[texts]\r\n");
            File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(true));
            return path;
        }
    }
}
