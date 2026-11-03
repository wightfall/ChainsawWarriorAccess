using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CWAccess.Game;
using CWAccess.Nav;
using Xunit;

namespace CWAccess.Tests
{
    /// <summary>The game's text table is a private static field; the tests give it a few texts of their own.</summary>
    internal static class FakeGameTable
    {
        private static readonly FieldInfo Field = typeof(CLocalisationManager).GetField("mpCurrentLanguage", BindingFlags.Static | BindingFlags.NonPublic);

        public static void Set(Dictionary<string, string> table)
        {
            Field.SetValue(null, table);
        }
    }

    [Collection("game table")]
    public class GameTextTests : IDisposable
    {
        public GameTextTests()
        {
            FakeGameTable.Set(new Dictionary<string, string>
            {
                ["FrontEnd_NewGame"] = "New Game",
                ["Announcement_DefaultText1"] = "Continue your mission!\\n\\nCurrent Deck: %deckNumber\\nCurrent Card: %cardCurrent/%cardTotal",
                ["Announcement_EncounterEnemy"] = "Current encounter - %name.",
                ["Enemy_Zombie_Name"] = "Zombie",
                ["Untranslated"] = "Stays English",
            });
            GameText.Load(LanguageFile.Parse(
                "[keys]\nFrontEnd_NewGame = เกมใหม่\n" +
                "Announcement_DefaultText1 = เดินหน้าภารกิจต่อไป!\\n\\nกองปัจจุบัน: %deckNumber\\nการ์ดปัจจุบัน: %cardCurrent/%cardTotal\n" +
                "Announcement_EncounterEnemy = การเผชิญหน้าปัจจุบัน - %name\n" +
                "Enemy_Zombie_Name = ซอมบี้\n" +
                "[texts]\nOne = หนึ่ง\nAbout = เกี่ยวกับเกม\n"));
        }

        public void Dispose()
        {
            GameText.Load(LanguageFile.Parse(string.Empty));
            FakeGameTable.Set(null);
        }

        [Fact]
        public void KeysAreTranslated()
        {
            Assert.Equal("ซอมบี้", GameText.ForKey("Enemy_Zombie_Name", "Zombie"));
            Assert.Equal("Stays English", GameText.ForKey("Untranslated", "Stays English"));
        }

        [Fact]
        public void ScreenTextsAreTranslatedWholeOrNotAtAll()
        {
            Assert.Equal("เกมใหม่", GameText.Speak("New Game"));
            Assert.Equal("เกี่ยวกับเกม", GameText.Speak("[ff0000]About[-]"));
            Assert.Equal("New Game now", GameText.Speak("New Game now"));
        }

        /// <summary>Texts are remembered once translated; a new translation (live reload, other language) replaces them.</summary>
        [Fact]
        public void RememberedTranslationsFollowANewLanguageFile()
        {
            Assert.Equal("เกมใหม่", GameText.Speak("New Game"));
            Assert.Equal("เกมใหม่", GameText.Speak("New Game"));
            Assert.Equal("การเผชิญหน้าปัจจุบัน - ซอมบี้", GameText.Speak("Current encounter - Zombie."));
            Assert.Equal("Untranslated text", GameText.Speak("Untranslated text"));
            Assert.Equal("Untranslated text", GameText.Speak("Untranslated text"));

            GameText.Load(LanguageFile.Parse("[keys]\nFrontEnd_NewGame = Neues Spiel\nAnnouncement_EncounterEnemy = Begegnung - %name\n"));
            Assert.Equal("Neues Spiel", GameText.Speak("New Game"));
            Assert.Equal("Begegnung - Zombie", GameText.Speak("Current encounter - Zombie."));
        }

        [Fact]
        public void FilledInTextsAreMatchedAndTheirPartsTranslated()
        {
            // As the game shows it: %deckNumber became "One", the numbers were filled in, line breaks are spaces.
            string shown = "Continue your mission! Current Deck: One Current Card: 3/54";
            Assert.Equal("เดินหน้าภารกิจต่อไป! กองปัจจุบัน: หนึ่ง การ์ดปัจจุบัน: 3/54", TextUtil.CleanRaw(GameText.Speak(shown)));
            // A filled-in part that is a known text is translated too.
            Assert.Equal("การเผชิญหน้าปัจจุบัน - ซอมบี้", GameText.Speak("Current encounter - Zombie."));
            Assert.Equal("การเผชิญหน้าปัจจุบัน - Unknown thing", GameText.Speak("Current encounter - Unknown thing."));
        }

        [Fact]
        public void TemplateListsTheTableAndTheScreenTexts()
        {
            string dir = TestPaths.NewTempDir();
            try
            {
                string path = GameText.WriteTemplate(dir);
                LanguageFile f = LanguageFile.Parse(File.ReadAllText(path, Encoding.UTF8));
                Assert.Equal(5, f.Sections["keys"].Count);
                Assert.Equal("Continue your mission!\n\nCurrent Deck: %deckNumber\nCurrent Card: %cardCurrent/%cardTotal", f.Sections["keys"]["Announcement_DefaultText1"].Replace("\\n", "\n"));
                Assert.True(f.Sections.ContainsKey("texts"));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Theory]
        [InlineData("New Game", true)]
        [InlineData("000000", false)]
        [InlineData("New Label", false)]
        [InlineData("A", false)]
        [InlineData("Score:", true)]
        public void OnlyRealTextsGoIntoTheTemplate(string text, bool expected)
        {
            Assert.Equal(expected, GameText.IsTranslatable(text));
        }
    }

    public class ThaiGameTextTests
    {
        private static readonly string ThaiGame = Path.Combine(TestPaths.ModSource, "Languages", "game", "th.txt");

        [Fact]
        public void ThaiGameFileIsWellFormed()
        {
            LanguageFile f = LanguageFile.Parse(File.ReadAllText(ThaiGame, Encoding.UTF8));
            Assert.Equal("ไทย (Thai)", f.Name);
            Assert.True(f.Sections["keys"].Count > 500, "game text keys");
            Assert.True(f.Sections["texts"].Count > 50, "screen texts");
            Assert.Empty(f.Entries);
        }

        /// <summary>When the game has written its template on this computer, every Thai key exists and keeps its %words.</summary>
        [Fact]
        public void ThaiGameKeysMatchTheGame()
        {
            string template = Path.Combine(Path.GetDirectoryName(TestPaths.Managed), "..", "CWAccess", "Languages", "game", "template.txt");
            if (!File.Exists(template)) return;
            Dictionary<string, string> english = LanguageFile.Parse(File.ReadAllText(template, Encoding.UTF8)).Sections["keys"];
            Dictionary<string, string> thai = LanguageFile.Parse(File.ReadAllText(ThaiGame, Encoding.UTF8)).Sections["keys"];
            var problems = new List<string>();
            var token = new Regex("%[A-Za-z]+");
            foreach (KeyValuePair<string, string> kv in thai)
            {
                if (!english.TryGetValue(kv.Key, out string en))
                {
                    problems.Add("unknown key " + kv.Key);
                    continue;
                }
                string a = string.Join(",", token.Matches(en).Cast<Match>().Select(m => m.Value).Distinct().OrderBy(x => x));
                string b = string.Join(",", token.Matches(kv.Value).Cast<Match>().Select(m => m.Value).Distinct().OrderBy(x => x));
                if (a != b) problems.Add(kv.Key + ": " + a + " / " + b);
            }
            Assert.True(problems.Count == 0, string.Join("; ", problems));
        }
    }

    [Collection("game table")]
    public class LiveReloadTests
    {
        [Fact]
        public void SavingTheLanguageFileReloadsIt()
        {
            string dir = TestPaths.NewTempDir();
            try
            {
                Directory.CreateDirectory(Path.Combine(dir, "Languages"));
                string file = Path.Combine(dir, "Languages", "xx.txt");
                File.WriteAllText(file, "# Language: Test\nstate_on = first\n", Encoding.UTF8);
                Loc.Init(dir, "xx");
                Assert.Equal("first", Loc.Get("state_on"));
                Assert.False(Loc.WatchFiles(10000f));

                File.WriteAllText(file, "# Language: Test\nstate_on = second\n", Encoding.UTF8);
                File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddSeconds(5));
                // Checked every 2 seconds of game time.
                Assert.True(Loc.WatchFiles(10003f));
                Assert.Equal("second", Loc.Get("state_on"));

                // The player's strings.txt is watched too, and wins over the language file.
                File.WriteAllText(Path.Combine(dir, "strings.txt"), "state_on = mine\n", Encoding.UTF8);
                Assert.True(Loc.WatchFiles(10006f));
                Assert.Equal("mine", Loc.Get("state_on"));
            }
            finally
            {
                Loc.Use("en");
                Directory.Delete(dir, true);
            }
        }
    }

    [Collection("game table")]
    public class SettingsPanelTests
    {
        /// <summary>The bug of 0.1.0: cue names stayed in the old language after switching it in the open settings.</summary>
        [Fact]
        public void CueNamesFollowALanguageChange()
        {
            string dir = TestPaths.NewTempDir();
            try
            {
                Directory.CreateDirectory(Path.Combine(dir, "Languages"));
                File.Copy(TestPaths.Thai, Path.Combine(dir, "Languages", "th.txt"));
                Loc.Init(dir, "th");
                List<NavItem> items = SettingsPanel.Build();
                NavItem night = items.First(i => i.Key == "cue_Night");
                Assert.Contains("กลางคืน", night.Speak());
                Loc.Use("en");
                Assert.Equal("Night sound volume, 100", night.Speak());
                Assert.Equal("Sound cues", items.First(i => i.Key == "set_group_sound").Speak());
            }
            finally
            {
                Loc.Use("en");
                Directory.Delete(dir, true);
            }
        }
    }

    /// <summary>
    /// Languages\template.txt is every English text of the mod and the setup, for translators. Run the tests with
    /// CWACCESS_UPDATE_LANGUAGES=1 to rewrite it after adding texts.
    /// </summary>
    public class TemplateTests
    {
        public static string Expected()
        {
            var sb = new StringBuilder();
            sb.Append("# Language: (name of your language, for example Deutsch (German))\r\n");
            sb.Append("#\r\n");
            sb.Append("# Chainsaw Warrior Access - template for a translation of the mod and its setup.\r\n");
            sb.Append("# Copy this file to <code>.txt (de.txt, es.txt ...) in this folder, translate the right side of each line,\r\n");
            sb.Append("# and choose the language with F2 in the game. Keep {0}, {1} ... as they are; \\n is a line break.\r\n");
            sb.Append("# Saving the file while the language is in use reloads it in the game within 2 seconds.\r\n");
            sb.Append("# The game's own texts (cards, messages) are translated in Languages\\game\\<code>.txt; the mod writes\r\n");
            sb.Append("# Languages\\game\\template.txt for them on your computer.\r\n");
            sb.Append("# Search for ##### to jump between the groups of texts.\r\n");
            LanguageFile.AppendGrouped(sb, Loc.All, Loc.Category, Loc.CategoryOrder);
            sb.Append("\r\n##### The setup program\r\n[setup]\r\n");
            var seen = new HashSet<string>();
            foreach (string path in Directory.GetFiles(TestPaths.InstallerSource, "*.cs").OrderBy(p => p, StringComparer.Ordinal))
            {
                foreach (Match m in Regex.Matches(File.ReadAllText(path), "SetupText\\.[TF]\\(\"([a-z_]+)\",\\s*\"((?:[^\"\\\\]|\\\\.)*)\""))
                {
                    if (seen.Add(m.Groups[1].Value)) sb.Append(m.Groups[1].Value).Append(" = ").Append(m.Groups[2].Value.Replace("\\\"", "\"")).Append("\r\n");
                }
            }
            return sb.ToString();
        }

        [Fact]
        public void TemplateIsCurrent()
        {
            string path = Path.Combine(TestPaths.ModSource, "Languages", "template.txt");
            string expected = Expected();
            if (Environment.GetEnvironmentVariable("CWACCESS_UPDATE_LANGUAGES") == "1") File.WriteAllText(path, expected, new UTF8Encoding(false));
            Assert.True(File.Exists(path), "Languages\\template.txt is missing: run the tests with CWACCESS_UPDATE_LANGUAGES=1");
            Assert.True(expected == File.ReadAllText(path, Encoding.UTF8), "Languages\\template.txt is out of date: run the tests with CWACCESS_UPDATE_LANGUAGES=1");
        }

        [Fact]
        public void TemplateIsNotOfferedAsALanguage()
        {
            string dir = TestPaths.NewTempDir();
            try
            {
                Directory.CreateDirectory(Path.Combine(dir, "Languages"));
                File.WriteAllText(Path.Combine(dir, "Languages", "template.txt"), Expected(), Encoding.UTF8);
                Loc.Init(dir, "en");
                Assert.DoesNotContain(Loc.Available(), c => c.Code == "template");
            }
            finally
            {
                Loc.Use("en");
                Directory.Delete(dir, true);
            }
        }
    }
}
