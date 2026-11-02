using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CWAccess
{
    internal static class TextUtil
    {
        // NGUI 2.x colour codes: [ff0000] ... [-]
        private static readonly Regex ColourCode = new Regex(@"\[(?:[0-9a-fA-F]{6}|-)\]");
        private static readonly Regex Spaces = new Regex(@"[ \t\r\n]+");

        /// <summary>Removes colour codes and line breaks, and speaks game texts in the mod's language when translated.</summary>
        public static string Clean(string text)
        {
            text = CleanRaw(text);
            return text.Length == 0 ? text : Game.GameText.Speak(text);
        }

        /// <summary>Removes colour codes and line breaks so a screen reader reads the text naturally.</summary>
        public static string CleanRaw(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            text = ColourCode.Replace(text, string.Empty);
            text = text.Replace("\\n", " ");
            text = Spaces.Replace(text, " ");
            return text.Trim();
        }

        /// <summary>Joins non-empty parts with ". " and no doubled punctuation.</summary>
        public static string Join(params string[] parts)
        {
            return Join((IEnumerable<string>)parts);
        }

        public static string Join(IEnumerable<string> parts)
        {
            var sb = new StringBuilder();
            foreach (string raw in parts)
            {
                string p = Clean(raw);
                if (p.Length == 0) continue;
                if (sb.Length > 0)
                {
                    char last = sb[sb.Length - 1];
                    if (last != '.' && last != '!' && last != '?' && last != ':' && last != ',') sb.Append('.');
                    sb.Append(' ');
                }
                sb.Append(p);
            }
            return sb.ToString();
        }

        /// <summary>"Button_NewGame" -> "New Game", "Button_Save_01" -> "Save 01".</summary>
        public static string FromObjectName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            foreach (string prefix in new[] { "Button_", "Btn_", "Toggle_", "Text_" })
            {
                if (name.StartsWith(prefix)) name = name.Substring(prefix.Length);
            }
            name = name.Replace('_', ' ');
            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (i > 0 && char.IsUpper(c) && char.IsLower(name[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            return Spaces.Replace(sb.ToString(), " ").Trim();
        }
    }
}
