using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CWAccess.Game;
using Xunit;

namespace CWAccess.Tests
{
    /// <summary>
    /// Every language file is grouped under "##### Category" headings, in the same order as the template, so a
    /// translator can search for ##### (or the category) to find a text. CWACCESS_UPDATE_LANGUAGES=1 regroups them.
    /// </summary>
    public class GroupingTests
    {
        private sealed class Line
        {
            public string Section = string.Empty;
            public string Key;
            public string Raw;
        }

        /// <summary>The comment lines before the first entry or section (old group headings dropped), and the entries in file order.</summary>
        private static List<Line> Read(string text, out string header)
        {
            var head = new StringBuilder();
            var lines = new List<Line>();
            string section = string.Empty;
            bool started = false;
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.Trim().TrimStart('﻿');
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    started = true;
                    section = line.Substring(1, line.Length - 2);
                    continue;
                }
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    if (!started && !line.StartsWith("#####") && !line.StartsWith("# =====") && !line.StartsWith("# Search for #####")) head.Append(line).Append("\r\n");
                    continue;
                }
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                started = true;
                lines.Add(new Line { Section = section, Key = line.Substring(0, eq).Trim(), Raw = line.Substring(eq + 1).Trim() });
            }
            header = head.ToString().TrimEnd('\r', '\n') + "\r\n";
            return lines;
        }

        private static IEnumerable<KeyValuePair<string, string>> Pairs(IEnumerable<Line> lines)
        {
            // Values stay as written (with \n escapes); AppendGrouped only turns real line breaks into \n.
            return lines.Select(l => new KeyValuePair<string, string>(l.Key, l.Raw));
        }

        /// <summary>A language file of the mod: its texts grouped like the template, then the setup's texts.</summary>
        public static string RegroupModFile(string text)
        {
            List<Line> lines = Read(text, out string header);
            List<string> order = Loc.All.Select(kv => kv.Key).ToList();
            var sb = new StringBuilder(header);
            IEnumerable<Line> mod = lines.Where(l => l.Section.Length == 0).OrderBy(l => order.IndexOf(l.Key) < 0 ? int.MaxValue : order.IndexOf(l.Key));
            LanguageFile.AppendGrouped(sb, Pairs(mod), Loc.Category, Loc.CategoryOrder);
            foreach (IGrouping<string, Line> section in lines.Where(l => l.Section.Length > 0).GroupBy(l => l.Section))
            {
                sb.Append("\r\n##### ").Append(section.Key == "setup" ? "The setup program" : section.Key).Append("\r\n[").Append(section.Key).Append("]\r\n");
                foreach (Line l in section) sb.Append(l.Key).Append(" = ").Append(l.Raw).Append("\r\n");
            }
            return sb.ToString();
        }

        /// <summary>A file of the game's texts: [keys] grouped by kind of text, then [texts].</summary>
        public static string RegroupGameFile(string text)
        {
            List<Line> lines = Read(text, out string header);
            var sb = new StringBuilder(header);
            sb.Append("# Search for ##### to jump between the groups.\r\n[keys]\r\n");
            LanguageFile.AppendGrouped(sb, Pairs(lines.Where(l => l.Section == "keys")), GameText.Category, GameText.CategoryOrder);
            sb.Append("\r\n##### Screen texts (not in the game's table)\r\n[texts]\r\n");
            foreach (Line l in lines.Where(l => l.Section == "texts")) sb.Append(l.Key).Append(" = ").Append(l.Raw).Append("\r\n");
            return sb.ToString();
        }

        private static void Check(string path, Func<string, string> regroup)
        {
            string text = File.ReadAllText(path, Encoding.UTF8);
            string grouped = regroup(text);
            if (Environment.GetEnvironmentVariable("CWACCESS_UPDATE_LANGUAGES") == "1" && grouped != text)
            {
                File.WriteAllText(path, grouped, new UTF8Encoding(false));
                text = grouped;
            }
            Assert.True(grouped == text, Path.GetFileName(path) + " is not grouped: run the tests with CWACCESS_UPDATE_LANGUAGES=1");
            // Regrouping never loses or changes a text.
            LanguageFile before = LanguageFile.Parse(text), after = LanguageFile.Parse(grouped);
            Assert.Equal(before.Sections.Sum(s => s.Value.Count), after.Sections.Sum(s => s.Value.Count));
        }

        [Fact]
        public void EveryModLanguageFileIsGrouped()
        {
            foreach (string path in Directory.GetFiles(Path.Combine(TestPaths.ModSource, "Languages"), "*.txt").Where(p => !p.EndsWith("template.txt")))
            {
                Check(path, RegroupModFile);
            }
        }

        [Fact]
        public void EveryGameLanguageFileIsGrouped()
        {
            foreach (string path in Directory.GetFiles(Path.Combine(TestPaths.ModSource, "Languages", "game"), "*.txt"))
            {
                Check(path, RegroupGameFile);
            }
        }

        [Fact]
        public void TheTemplateHasEveryHeadingInOrder()
        {
            string t = TemplateTests.Expected();
            int last = -1;
            foreach (string c in Loc.CategoryOrder)
            {
                int i = t.IndexOf("\r\n##### " + c + "\r\n", StringComparison.Ordinal);
                Assert.True(i > last, "heading " + c);
                last = i;
            }
            Assert.True(t.IndexOf("##### The setup program", StringComparison.Ordinal) > last);
        }

        [Theory]
        [InlineData("set_cues", "Mod settings")]
        [InlineData("cuedesc_Night", "Sound cues")]
        [InlineData("keys_move", "Keys and help")]
        [InlineData("card_pick", "Cards and equipment")]
        [InlineData("stat_wounds", "Game board")]
        [InlineData("mod_loaded", "General speech")]
        [InlineData("achname_ACHIEVEMENT_FISTO", "Achievements")]
        [InlineData("ach_unlocked", "Achievements")]
        [InlineData("set_position", "Mod settings")]
        public void ModTextsGoUnderTheirHeading(string key, string category)
        {
            Assert.Equal(category, Loc.Category(key));
        }

        [Theory]
        [InlineData("Enemy_Darkness_Name", "Enemies")]
        [InlineData("Trap_Lunatic_Name", "Traps")]
        [InlineData("Item_Laser_Lance", "Equipment")]
        [InlineData("AchievementTitle_1", "End of game and achievements")]
        [InlineData("Help_Text_1", "Menus and screens")]
        [InlineData("Other_Dump_Name", "Rooms")]
        [InlineData("Something", "Other texts")]
        public void GameTextsGoUnderTheirHeading(string key, string category)
        {
            Assert.Equal(category, GameText.Category(key));
        }
    }
}
