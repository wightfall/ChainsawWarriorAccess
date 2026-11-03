using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CWAccess.Game;
using CWAccess.Installer;
using Xunit;

namespace CWAccess.Tests
{
    /// <summary>
    /// A future language that is only partly translated, or has mistakes, still works: every line that is missing,
    /// empty or broken is said in English. And any language loads and is spoken about as fast as English.
    /// </summary>
    [Collection("game table")]
    public class FutureLanguageTests : IDisposable
    {
        private readonly string dir = TestPaths.NewTempDir();
        private readonly Xunit.Abstractions.ITestOutputHelper output;

        public FutureLanguageTests(Xunit.Abstractions.ITestOutputHelper output)
        {
            this.output = output;
        }

        public void Dispose()
        {
            Loc.Use("en");
            GameText.Load(LanguageFile.Parse(string.Empty));
            FakeGameTable.Set(null);
            Directory.Delete(dir, true);
        }

        [Fact]
        public void ModTextsThatAreMissingEmptyOrBrokenAreEnglish()
        {
            Directory.CreateDirectory(Path.Combine(dir, "Languages"));
            File.WriteAllText(Path.Combine(dir, "Languages", "xx.txt"),
                "# Language: Future\n" +
                "state_on = AN\n" +
                "state_off =\n" +                                   // empty
                "position = {0} de {9}\n" +                         // wrong placeholders
                "stat_wounds = Blessures {0} { sur {1}\n",           // a stray brace
                Encoding.UTF8);
            Loc.Init(dir, "xx");
            Assert.Equal("AN", Loc.Get("state_on"));
            Assert.Equal("off", Loc.Get("state_off"));
            Assert.Equal("1 of 10", Loc.Get("position", 1, 10));
            Assert.Equal("Wounds 3 of 9", Loc.Get("stat_wounds", 3, 9));
            Assert.Equal("Shift+F1: every key and sound cue.", Loc.Get("keys_more"));
        }

        [Fact]
        public void GameTextsThatAreMissingEmptyOrLoseAFilledInPartAreEnglish()
        {
            FakeGameTable.Set(new Dictionary<string, string>
            {
                ["FrontEnd_NewGame"] = "New Game",
                ["FrontEnd_Options"] = "Options",
                ["Announcement_EncounterEnemy"] = "Current encounter - %name.",
                ["Enemy_Zombie_Name"] = "Zombie",
            });
            GameText.Load(LanguageFile.Parse(
                "[keys]\nFrontEnd_NewGame = Nouvelle partie\nFrontEnd_Options =\n" +
                "Announcement_EncounterEnemy = Rencontre en cours.\n" +             // %name forgotten
                "Enemy_Zombie_Name = Zombi\n"));
            Assert.Equal("Nouvelle partie", GameText.Speak("New Game"));
            Assert.Equal("Options", GameText.Speak("Options"));
            Assert.Equal("Current encounter - Zombie.", GameText.Speak("Current encounter - Zombie."));
            Assert.Equal("Current encounter - %name.", GameText.ForKey("Announcement_EncounterEnemy", "Current encounter - %name."));
            Assert.Equal("Zombi", GameText.ForKey("Enemy_Zombie_Name", "Zombie"));
            Assert.Equal("Not in the table", GameText.Speak("Not in the table"));
        }

        [Theory]
        [InlineData("Current encounter - %name.", "Rencontre - %name", true)]
        [InlineData("%a and %b", "%b et %a", true)]
        [InlineData("Current encounter - %name.", "Rencontre", false)]
        [InlineData("No words", "Pas de mots %extra", false)]
        public void FilledInPartsMustAllStay(string english, string translation, bool same)
        {
            Assert.Equal(same, GameText.SameWords(english, translation));
        }

        [Fact]
        public void SetupLinesThatAreBrokenAreEnglish()
        {
            string game = TestPaths.NewTempDir();
            try
            {
                Directory.CreateDirectory(Path.Combine(game, "CWAccess", "Languages"));
                File.WriteAllText(Path.Combine(game, "CWAccess", "Languages", "xx.txt"),
                    "# Language: Future\n[setup]\nev_game = Dossier { du jeu : {0}\nev_installed = Version : {0}\n", Encoding.UTF8);
                Assert.True(SetupText.Use(game, "xx"));
                Assert.Equal("Game folder: D:/Games", SetupText.F("ev_game", "Game folder: {0}", "D:/Games"));
                Assert.Equal("Version : 0.3.7", SetupText.F("ev_installed", "Installed mod version: {0}.", "0.3.7"));
                Assert.Equal("Checking GitHub for updates...", SetupText.T("ev_checking", "Checking GitHub for updates..."));
            }
            finally
            {
                SetupText.Use(game, "en");
                Directory.Delete(game, true);
            }
        }

        /// <summary>
        /// The real size: the game's whole text table (from the template the mod wrote on this computer) and the Thai
        /// files. Loading a language and speaking every text of the game must stay quick; texts said again (the screens
        /// are read again several times a second) cost almost nothing.
        /// </summary>
        [Fact]
        public void ALanguageLoadsAndSpeaksAboutAsFastAsEnglish()
        {
            string template = Path.Combine(Path.GetDirectoryName(TestPaths.Managed), "..", "CWAccess", "Languages", "game", "template.txt");
            if (!File.Exists(template)) return; // Needs the game's texts, which are not part of the repository.
            LanguageFile english = LanguageFile.Parse(File.ReadAllText(template, Encoding.UTF8));
            Dictionary<string, string> table = english.Sections["keys"];
            FakeGameTable.Set(table);
            List<string> shown = table.Values.Where(v => v.Length > 0).Select(v => v.Replace("%name", "Zombie").Replace("%cards", "3")).ToList();

            var load = Stopwatch.StartNew();
            Directory.CreateDirectory(Path.Combine(dir, "Languages", "game"));
            File.Copy(TestPaths.Thai, Path.Combine(dir, "Languages", "th.txt"));
            File.Copy(Path.Combine(TestPaths.ModSource, "Languages", "game", "th.txt"), Path.Combine(dir, "Languages", "game", "th.txt"));
            Loc.Init(dir, "th");
            GameText.Use(dir, "th");
            GameText.Speak("warm up");
            load.Stop();

            var first = Stopwatch.StartNew();
            foreach (string t in shown) GameText.Speak(t);
            first.Stop();
            var again = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                foreach (string t in shown) GameText.Speak(t);
            }
            again.Stop();

            output.WriteLine("Thai: loading " + load.Elapsed.TotalMilliseconds.ToString("0.0") + " ms, every text of the game (" + shown.Count + ") " + first.Elapsed.TotalMilliseconds.ToString("0.0") + " ms, ten more times " + again.Elapsed.TotalMilliseconds.ToString("0.0") + " ms");
            Assert.True(load.ElapsedMilliseconds < 2000, "loading took " + load.ElapsedMilliseconds + " ms");
            Assert.True(first.ElapsedMilliseconds < 2000, "first pass took " + first.ElapsedMilliseconds + " ms");
            // Ten more passes over every text of the game cost less than the first one: they are remembered.
            Assert.True(again.ElapsedTicks < first.ElapsedTicks * 2, "remembered passes took " + again.ElapsedMilliseconds + " ms");
            Assert.Equal(GameText.Speak(shown[0]), GameText.Speak(shown[0]));
        }
    }
}
