using System.Collections.Generic;
using System.IO;
using System.Linq;
using CWAccess.Nav;
using Xunit;

namespace CWAccess.Tests
{
    /// <summary>F1 says only the keys of the screen it is pressed on; Shift+F1 is the list of everything.</summary>
    public class KeyHelpTests
    {
        public KeyHelpTests()
        {
            Loc.Use("en");
        }

        private static NavItem Button(string name)
        {
            return new NavItem { Key = name, Text = () => name, Activate = () => { } };
        }

        private sealed class TestScreen : ScreenContext
        {
            public string BackText;
            public bool KeyList;

            public override string Id => "test";

            public override bool IsKeyList => KeyList;

            public override string Title()
            {
                return "Test";
            }

            public override List<NavItem> Build()
            {
                return new List<NavItem>();
            }

            public override string BackHelp()
            {
                return BackText;
            }
        }

        [Fact]
        public void AMenuSaysMovingPressingLettersAndBack()
        {
            var items = new List<NavItem> { Button("New game"), Button("Load game"), Button("Options"), Button("Quit") };
            var menu = new TestScreen { BackText = Loc.Get("keys_back") };
            Assert.Equal(new[] { Loc.Get("keys_move"), Loc.Get("keys_press"), Loc.Get("keys_letters"), Loc.Get("keys_back") }, menu.HelpLines(items));
        }

        [Fact]
        public void KeysForThingsNotOnTheScreenAreNotSaid()
        {
            // One button: nothing to move to, no letters, no Escape.
            Assert.Equal(new[] { Loc.Get("keys_press") }, new TestScreen().HelpLines(new List<NavItem> { Button("OK") }));

            // Options: a slider and a checkbox, no plain button.
            var options = new List<NavItem>
            {
                new NavItem { Key = "slider", Text = () => "Music", Adjust = d => { } },
                new NavItem { Key = "box", Text = () => "Subtitles", Activate = () => { }, IsSwitch = true },
            };
            Assert.Equal(new[] { Loc.Get("keys_move"), Loc.Get("keys_switch"), Loc.Get("keys_adjust") }, KeyHelp.ForItems(options, letters: true));

            // Texts only (a credits page): moving is all there is.
            var texts = new List<NavItem> { NavItem.Label("a", () => "a"), NavItem.Label("b", () => "b") };
            Assert.Equal(new[] { Loc.Get("keys_move") }, KeyHelp.ForItems(texts, letters: false));
        }

        [Fact]
        public void TheF1TextEndsWithShiftF1AndHoldsNoOtherScreensKeys()
        {
            var items = new List<NavItem> { Button("Yes"), Button("No") };
            string text = KeyHelp.ScreenText(new TestScreen { BackText = Loc.Get("keys_back_no") }, items);
            Assert.Contains(Loc.Get("keys_back_no"), text);
            Assert.EndsWith(Loc.Get("keys_more"), text);
            // Nothing of the board, the card screens or the keys that work everywhere.
            foreach (string other in KeyHelp.Lines("help_board").Concat(KeyHelp.Lines("help_global")).Concat(KeyHelp.Lines("help_cards")))
            {
                Assert.DoesNotContain(other, text);
            }
        }

        [Fact]
        public void TheListOfAllKeysDoesNotPointToItself()
        {
            var items = new List<NavItem> { Button("a"), Button("b") };
            Assert.DoesNotContain(Loc.Get("keys_more"), KeyHelp.ScreenText(new TestScreen { KeyList = true }, items));
            Assert.Contains(Loc.Get("keys_more"), KeyHelp.ScreenText(new TestScreen(), items));
        }

        [Fact]
        public void NoScreenYetSaysSo()
        {
            Assert.Equal(TextUtil.Join(Loc.Get("keys_nothing"), Loc.Get("keys_more")), KeyHelp.ScreenText(null, null));
        }

        [Fact]
        public void PanelsSayTheirOwnKeys()
        {
            var items = new List<NavItem> { NavItem.Label("a", () => "a"), NavItem.Label("b", () => "b") };
            foreach (string help in new[] { "help_panel_settings", "help_panel_keys", "help_panel_equipment", "help_panel_log" })
            {
                List<string> lines = KeyHelp.ForPanel(help, items);
                Assert.True(lines.Count >= 3, help);
                Assert.Contains("Escape", lines.Last());
            }
            Assert.Equal(new[] { Loc.Get("keys_move"), Loc.Get("keys_back_close") }, KeyHelp.ForPanel(null, items));
        }

        [Fact]
        public void TheKeysAreSaidInTheModsLanguage()
        {
            string dir = TestPaths.NewTempDir();
            try
            {
                Directory.CreateDirectory(Path.Combine(dir, "Languages"));
                File.Copy(TestPaths.Thai, Path.Combine(dir, "Languages", "th.txt"));
                Loc.Init(dir, "th");
                string thai = KeyHelp.ScreenText(new TestScreen(), new List<NavItem> { Button("a"), Button("b") });
                Assert.Contains("Shift+F1: ปุ่มลัดทั้งหมด", thai);
                Assert.DoesNotContain("Up and Down", thai);
            }
            finally
            {
                Loc.Use("en");
                Directory.Delete(dir, true);
            }
        }
    }
}
