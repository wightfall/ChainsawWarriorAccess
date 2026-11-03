using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace CWAccess.Installer
{
    /// <summary>
    /// The setup window's texts. English is built in (the second argument of T); translations are the [setup]
    /// section of the mod's language files (CWAccess\Languages\code.txt), read from the built-in package or the
    /// game folder. The first language is the mod's Language setting, else the Windows display language.
    /// </summary>
    public static class SetupText
    {
        private const string PayloadLanguages = "CWAccess/Languages/";
        private static Dictionary<string, string> table = new Dictionary<string, string>();

        public sealed class Choice
        {
            public string Code;
            public string Name;

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>The language used now ("en" for English).</summary>
        public static string Current { get; private set; } = "en";

        public static string T(string key, string english)
        {
            return table.TryGetValue(key, out string t) && t.Length > 0 && LanguageFile.PlaceholdersMatch(english, t) ? t : english;
        }

        public static string F(string key, string english, params object[] args)
        {
            try
            {
                return string.Format(T(key, english), args);
            }
            catch (FormatException)
            {
                // A translation with a stray brace: the English line instead.
                return string.Format(english, args);
            }
        }

        /// <summary>Picks the first language: the mod's setting, else Windows, when a file exists for it.</summary>
        public static void Init(string gameDir)
        {
            string setting = gameDir != null ? new ModInstaller(gameDir).ReadLanguage() : null;
            string code = setting != null && !string.Equals(setting, "auto", StringComparison.OrdinalIgnoreCase)
                ? setting
                : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (!Use(gameDir, code)) Use(gameDir, "en");
        }

        /// <summary>Switches language; false when there is no file for it (then nothing changes).</summary>
        public static bool Use(string gameDir, string code)
        {
            if (string.IsNullOrEmpty(code) || code == "en")
            {
                table = new Dictionary<string, string>();
                Current = "en";
                return true;
            }
            string text = ReadFile(gameDir, code);
            if (text == null) return false;
            LanguageFile f = LanguageFile.Parse(text);
            table = f.Sections.TryGetValue("setup", out Dictionary<string, string> s) ? s : new Dictionary<string, string>();
            Current = code;
            return true;
        }

        /// <summary>English and every language of the built-in package and the game folder.</summary>
        public static List<Choice> Available(string gameDir)
        {
            var found = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> kv in PayloadFiles()) found[kv.Key] = Name(kv.Value, kv.Key);
            if (gameDir != null)
            {
                string dir = Path.Combine(gameDir, ModInstaller.ModFolder, "Languages");
                if (Directory.Exists(dir))
                {
                    foreach (string path in Directory.GetFiles(dir, "*.txt"))
                    {
                        string code = Path.GetFileNameWithoutExtension(path);
                        if (code == "template" || code == "en") continue;
                        found[code] = Name(File.ReadAllText(path, Encoding.UTF8), code);
                    }
                }
            }
            var list = new List<Choice> { new Choice { Code = "en", Name = "English" } };
            foreach (KeyValuePair<string, string> kv in found) list.Add(new Choice { Code = kv.Key, Name = kv.Value });
            return list;
        }

        private static string Name(string text, string code)
        {
            string n = LanguageFile.Parse(text).Name;
            return string.IsNullOrEmpty(n) ? code : n;
        }

        private static string ReadFile(string gameDir, string code)
        {
            if (gameDir != null)
            {
                string path = Path.Combine(gameDir, ModInstaller.ModFolder, "Languages", code + ".txt");
                if (File.Exists(path)) return File.ReadAllText(path, Encoding.UTF8);
            }
            return PayloadFiles().TryGetValue(code, out string t) ? t : null;
        }

        /// <summary>Language files inside the built-in package: code -> text.</summary>
        private static Dictionary<string, string> PayloadFiles()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
                {
                    if (s == null) return d;
                    using (var zip = new ZipArchive(s, ZipArchiveMode.Read))
                    {
                        foreach (ZipArchiveEntry e in zip.Entries)
                        {
                            if (!e.FullName.StartsWith(PayloadLanguages, StringComparison.OrdinalIgnoreCase) || !e.Name.EndsWith(".txt")) continue;
                            string code = Path.GetFileNameWithoutExtension(e.Name);
                            if (code == "template") continue;
                            using (var r = new StreamReader(e.Open(), Encoding.UTF8)) d[code] = r.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // No usable package: English and the game folder's files.
            }
            return d;
        }
    }
}
