using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// Shared with the setup program (INSTALLER defined: internal there, so the tests see one copy) and the tests.
namespace CWAccess
{
    /// <summary>
    /// A language file: "key = text" lines. The header line "# Language: ไทย (Thai)" names the language; other lines
    /// starting with # are comments; "[section]" lines are allowed and ignored by the mod's own texts. In the text,
    /// \n is a line break. Only the first " = " (or "=") separates the key from the text.
    /// </summary>
#if INSTALLER
    internal
#else
    public
#endif
    sealed class LanguageFile
    {
        private static readonly Regex Placeholder = new Regex(@"\{(\d+)\}");

        public readonly Dictionary<string, string> Entries = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Entries per [section]; texts before any section are in "".</summary>
        public readonly Dictionary<string, Dictionary<string, string>> Sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>From "# Language: ..."; empty when missing.</summary>
        public string Name = string.Empty;

        /// <summary>The file name without .txt.</summary>
        public string Code = string.Empty;

        public static LanguageFile Parse(string text)
        {
            var f = new LanguageFile();
            string section = string.Empty;
            f.Sections[section] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string raw in (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = raw.Trim().TrimStart('\uFEFF');
                if (line.Length == 0) continue;
                if (line.StartsWith("#"))
                {
                    string c = line.TrimStart('#').Trim();
                    if (c.StartsWith("Language:", StringComparison.OrdinalIgnoreCase)) f.Name = c.Substring("Language:".Length).Trim();
                    continue;
                }
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    if (!f.Sections.ContainsKey(section)) f.Sections[section] = new Dictionary<string, string>(StringComparer.Ordinal);
                    continue;
                }
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim().Replace("\\n", "\n");
                if (key.Length == 0) continue;
                f.Sections[section][key] = value;
                if (section.Length == 0) f.Entries[key] = value;
            }
            return f;
        }

        public static LanguageFile Load(string path)
        {
            LanguageFile f = Parse(File.ReadAllText(path, Encoding.UTF8));
            f.Code = Path.GetFileNameWithoutExtension(path);
            if (f.Name.Length == 0) f.Name = f.Code;
            return f;
        }

        /// <summary>True when both texts use the same {0}, {1} ... placeholders (a translation must keep them all).</summary>
        public static bool PlaceholdersMatch(string english, string translated)
        {
            return PlaceholderSet(english) == PlaceholderSet(translated);
        }

        private static string PlaceholderSet(string s)
        {
            var found = new SortedDictionary<int, bool>();
            foreach (Match m in Placeholder.Matches(s ?? string.Empty)) found[int.Parse(m.Groups[1].Value)] = true;
            var sb = new StringBuilder();
            foreach (int k in found.Keys) sb.Append(k).Append(',');
            return sb.ToString();
        }

        /// <summary>A file in the same format, every key with its English text (for translators).</summary>
        /// <summary>
        /// Writes "key = text" lines under "##### Category" headings (search for ##### to jump between them): the
        /// categories in the given order, unknown ones last, each keeping the order of its entries.
        /// </summary>
        public static void AppendGrouped(StringBuilder sb, IEnumerable<KeyValuePair<string, string>> entries, Func<string, string> category, IList<string> order)
        {
            var groups = new List<string>();
            var byGroup = new Dictionary<string, List<KeyValuePair<string, string>>>();
            foreach (KeyValuePair<string, string> kv in entries)
            {
                string c = category(kv.Key);
                if (!byGroup.TryGetValue(c, out List<KeyValuePair<string, string>> list))
                {
                    list = new List<KeyValuePair<string, string>>();
                    byGroup[c] = list;
                    groups.Add(c);
                }
                list.Add(kv);
            }
            foreach (string c in groups.OrderBy(g => order.IndexOf(g) < 0 ? int.MaxValue : order.IndexOf(g)))
            {
                sb.Append("\r\n##### ").Append(c).Append("\r\n");
                foreach (KeyValuePair<string, string> kv in byGroup[c]) sb.Append(kv.Key).Append(" = ").Append(kv.Value.Replace("\n", "\\n")).Append("\r\n");
            }
        }

        public static string Template(IEnumerable<KeyValuePair<string, string>> english, string header)
        {
            var sb = new StringBuilder();
            sb.Append(header);
            foreach (KeyValuePair<string, string> kv in english)
            {
                sb.Append(kv.Key).Append(" = ").Append(kv.Value.Replace("\n", "\\n")).Append("\r\n");
            }
            return sb.ToString();
        }
    }
}
