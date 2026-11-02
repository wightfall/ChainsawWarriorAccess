using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace CWAccess.Tests
{
    public class LanguageFileTests
    {
        [Fact]
        public void ParsesHeaderSectionsAndLineBreaks()
        {
            LanguageFile f = LanguageFile.Parse("# Language: ไทย (Thai)\n# a comment\nhello = สวัสดี\nmulti = a\\nb\n[setup]\nbtn = ปุ่ม\n");
            Assert.Equal("ไทย (Thai)", f.Name);
            Assert.Equal("สวัสดี", f.Entries["hello"]);
            Assert.Equal("a\nb", f.Entries["multi"]);
            Assert.False(f.Entries.ContainsKey("btn"));
            Assert.Equal("ปุ่ม", f.Sections["setup"]["btn"]);
        }

        [Fact]
        public void OnlyTheFirstEqualsSignSplits()
        {
            LanguageFile f = LanguageFile.Parse("key = a = b");
            Assert.Equal("a = b", f.Entries["key"]);
        }

        [Theory]
        [InlineData("{0} of {1}", "{1} จาก {0}", true)]
        [InlineData("{0} of {1}", "{0} จาก", false)]
        [InlineData("no braces", "ไม่มี", true)]
        [InlineData("{0}", "{0} {0}", true)]
        public void PlaceholdersMustMatch(string english, string translated, bool expected)
        {
            Assert.Equal(expected, LanguageFile.PlaceholdersMatch(english, translated));
        }
    }

    public class ThaiTranslationTests
    {
        private static LanguageFile Thai => LanguageFile.Parse(File.ReadAllText(TestPaths.Thai, Encoding.UTF8));

        [Fact]
        public void EveryModTextIsTranslated()
        {
            LanguageFile th = Thai;
            List<string> missing = Loc.All.Select(kv => kv.Key).Where(k => !th.Entries.ContainsKey(k) || th.Entries[k].Length == 0).ToList();
            Assert.True(missing.Count == 0, "Missing Thai texts: " + string.Join(", ", missing));
        }

        [Fact]
        public void NoStaleThaiTexts()
        {
            var english = new HashSet<string>(Loc.All.Select(kv => kv.Key));
            List<string> stale = Thai.Entries.Keys.Where(k => !english.Contains(k)).ToList();
            Assert.True(stale.Count == 0, "Thai texts without an English text: " + string.Join(", ", stale));
        }

        [Fact]
        public void ThaiPlaceholdersMatchEnglish()
        {
            LanguageFile th = Thai;
            List<string> bad = Loc.All.Where(kv => th.Entries.ContainsKey(kv.Key) && !LanguageFile.PlaceholdersMatch(kv.Value, th.Entries[kv.Key])).Select(kv => kv.Key).ToList();
            Assert.True(bad.Count == 0, "Placeholders differ: " + string.Join(", ", bad));
        }

        [Fact]
        public void HelpTextsKeepTheirLineCount()
        {
            LanguageFile th = Thai;
            foreach (KeyValuePair<string, string> kv in Loc.All.Where(kv => kv.Key.StartsWith("help_")))
            {
                Assert.Equal(kv.Value.Split('\n').Length, th.Entries[kv.Key].Split('\n').Length);
            }
        }

        [Fact]
        public void EverySetupTextIsTranslated()
        {
            Dictionary<string, string> setup = Thai.Sections["setup"];
            var keys = new Dictionary<string, string>();
            foreach (string path in Directory.GetFiles(TestPaths.InstallerSource, "*.cs"))
            {
                foreach (Match m in Regex.Matches(File.ReadAllText(path), "SetupText\\.[TF]\\(\"([a-z_]+)\",\\s*\"((?:[^\"\\\\]|\\\\.)*)\"")) keys[m.Groups[1].Value] = m.Groups[2].Value;
            }
            Assert.NotEmpty(keys);
            List<string> missing = keys.Keys.Where(k => !setup.ContainsKey(k)).ToList();
            Assert.True(missing.Count == 0, "Missing Thai setup texts: " + string.Join(", ", missing));
            List<string> bad = keys.Where(kv => setup.ContainsKey(kv.Key) && !LanguageFile.PlaceholdersMatch(kv.Value, setup[kv.Key])).Select(kv => kv.Key).ToList();
            Assert.True(bad.Count == 0, "Setup placeholders differ: " + string.Join(", ", bad));
        }
    }

    public class LocTests
    {
        [Fact]
        public void EveryTextKeyUsedInTheCodeExists()
        {
            var english = new HashSet<string>(Loc.All.Select(kv => kv.Key));
            var missing = new SortedSet<string>();
            foreach (string path in Directory.GetFiles(TestPaths.ModSource, "*.cs", SearchOption.AllDirectories))
            {
                if (path.Contains("\\obj\\") || path.Contains("\\bin\\")) continue;
                foreach (Match m in Regex.Matches(File.ReadAllText(path), "Loc\\.(?:Get|Role)\\(\"([A-Za-z0-9_]+)\"[,)]"))
                {
                    if (!english.Contains(m.Groups[1].Value)) missing.Add(m.Groups[1].Value + " (" + Path.GetFileName(path) + ")");
                }
            }
            Assert.True(missing.Count == 0, "Text keys used but not defined: " + string.Join(", ", missing));
        }

        [Fact]
        public void EnglishFormatsAreValid()
        {
            foreach (KeyValuePair<string, string> kv in Loc.All)
            {
                string s = string.Format(kv.Value, "a", "b", "c", "d", "e");
                Assert.NotNull(s);
            }
        }

        [Fact]
        public void LanguageFileSwitchesAndFallsBack()
        {
            string dir = TestPaths.NewTempDir();
            try
            {
                Directory.CreateDirectory(Path.Combine(dir, "Languages"));
                File.WriteAllText(Path.Combine(dir, "Languages", "xx.txt"), "# Language: Test\nstate_on = ON!\nedge_top = {9} broken\n", Encoding.UTF8);
                Loc.Init(dir, "xx");
                Assert.Equal("xx", Loc.Code);
                Assert.Equal("ON!", Loc.Get("state_on"));
                // A translation with wrong placeholders is ignored.
                Assert.Equal("Top.", Loc.Get("edge_top"));
                // Not translated: English.
                Assert.Equal("off", Loc.Get("state_off"));
                Loc.Use("en");
                Assert.Equal("on", Loc.Get("state_on"));
                Assert.Contains(Loc.Available(), c => c.Code == "xx" && c.Name == "Test");
            }
            finally
            {
                Loc.Use("en");
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void ThaiLoadsInTheMod()
        {
            string dir = TestPaths.NewTempDir();
            try
            {
                Directory.CreateDirectory(Path.Combine(dir, "Languages"));
                File.Copy(TestPaths.Thai, Path.Combine(dir, "Languages", "th.txt"));
                Loc.Init(dir, "th");
                Assert.Equal("เปิด", Loc.Get("state_on"));
                Assert.Equal("บาดแผล 3 จาก 10", Loc.Get("stat_wounds", 3, 10));
            }
            finally
            {
                Loc.Use("en");
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void MissingKeyReturnsTheKey()
        {
            Assert.Equal("no_such_key 5", Loc.Get("no_such_key", 5));
        }
    }

    public class TextUtilTests
    {
        [Theory]
        [InlineData("[ff0000]Red[-] text", "Red text")]
        [InlineData("Line\none\\nand  two", "Line one and two")]
        [InlineData("  ", "")]
        public void CleanRawRemovesCodesAndBreaks(string raw, string expected)
        {
            Assert.Equal(expected, TextUtil.CleanRaw(raw));
        }

        [Theory]
        [InlineData("Button_NewGame", "New Game")]
        [InlineData("Button_Save_01", "Save 01")]
        [InlineData("Text_Credits_1", "Credits 1")]
        public void ObjectNamesBecomeWords(string name, string expected)
        {
            Assert.Equal(expected, TextUtil.FromObjectName(name));
        }

        [Fact]
        public void JoinAddsFullStopsOnlyWhereNeeded()
        {
            Assert.Equal("Hello. World! Again", TextUtil.Join("Hello", "World!", "", null, "Again"));
        }
    }
}
