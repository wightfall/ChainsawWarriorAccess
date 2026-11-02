using System.Collections.Generic;
using System.Linq;
using CWAccess.Nav;
using Xunit;

namespace CWAccess.Tests
{
    /// <summary>Page Up / Page Down: between the groups of a list, or ten items in a long list without groups.</summary>
    public class PagingTests
    {
        public PagingTests()
        {
            Loc.Use("en");
        }

        private static List<NavItem> Items(params string[] groups)
        {
            return groups.Select((g, i) => new NavItem { Key = "i" + i, Text = () => "x", Group = g }).ToList();
        }

        [Fact]
        public void PageDownGoesToTheNextGroupAndPageUpToTheStartOfAGroup()
        {
            List<NavItem> items = Items("a", "a", "a", "b", "b", "c");
            Assert.Equal(3, Paging.Target(items, 0, 1));
            Assert.Equal(3, Paging.Target(items, 1, 1));
            Assert.Equal(5, Paging.Target(items, 3, 1));
            Assert.Equal(-1, Paging.Target(items, 5, 1));
            // Inside a group Page Up goes to its start first, then to the previous group.
            Assert.Equal(3, Paging.Target(items, 4, -1));
            Assert.Equal(0, Paging.Target(items, 3, -1));
            Assert.Equal(0, Paging.Target(items, 2, -1));
            Assert.Equal(-1, Paging.Target(items, 0, -1));
        }

        [Fact]
        public void ALongListWithoutGroupsMovesTenItems()
        {
            List<NavItem> items = Items(Enumerable.Repeat<string>(null, 25).ToArray());
            Assert.False(Paging.HasGroups(items));
            Assert.Equal(10, Paging.Target(items, 0, 1));
            Assert.Equal(24, Paging.Target(items, 20, 1));
            Assert.Equal(-1, Paging.Target(items, 24, 1));
            Assert.Equal(5, Paging.Target(items, 15, -1));
            Assert.Equal(0, Paging.Target(items, 3, -1));
            Assert.Equal(-1, Paging.Target(items, 0, -1));
            Assert.Equal(-1, Paging.Target(new List<NavItem>(), 0, 1));
        }

        [Fact]
        public void OneGroupIsNoGroups()
        {
            Assert.False(Paging.HasGroups(Items("a", "a", "a")));
            Assert.True(Paging.HasGroups(Items("a", null)));
        }

        [Fact]
        public void HeadingsStartGroups()
        {
            var items = new List<NavItem>
            {
                new NavItem { Key = "h1" }, new NavItem { Key = "x" }, new NavItem { Key = "h2" }, new NavItem { Key = "y" }, new NavItem { Key = "z" },
            };
            Paging.GroupByHeadings(items, i => i.Key.StartsWith("h"));
            Assert.Equal(new[] { "h1", "h1", "h2", "h2", "h2" }, items.Select(i => i.Group));
            Assert.Equal(2, Paging.Target(items, 1, 1));
        }

        [Fact]
        public void F1SaysThePageKeysOnlyWhereTheyWork()
        {
            Assert.Equal(Loc.Get("keys_page_groups"), KeyHelp.PageLine(Items("a", "b")));
            Assert.Equal(Loc.Get("keys_page_items", 10), KeyHelp.PageLine(Items(Enumerable.Repeat<string>(null, 11).ToArray())));
            Assert.Null(KeyHelp.PageLine(Items(null, null, null)));

            // In the F1 text the line comes right after the line about moving.
            List<NavItem> buttons = Items("a", "a", "b");
            foreach (NavItem b in buttons) b.Activate = () => { };
            string text = KeyHelp.ScreenText(new TestScreen(), buttons);
            Assert.StartsWith(TextUtil.Join(Loc.Get("keys_move"), Loc.Get("keys_page_groups")), text);
            Assert.Equal(1, CountOf(text, Loc.Get("keys_page_groups")));
        }

        [Fact]
        public void TheShiftF1ListAndTheSettingsHaveAGroupPerSection()
        {
            List<NavItem> keys = KeyHelp.AllKeysItems();
            Assert.Equal(new[] { "global", "menu", "board", "cards", "panel", "settings", "cues" }, keys.Select(i => i.Group).Distinct());
            Assert.Equal("h_menu", keys[Paging.Target(keys, 1, 1)].Key);

            List<NavItem> settings = SettingsPanel.Build();
            Assert.Equal(new[] { "set_group_speech", "set_group_sound", "set_group_other" }, settings.Select(i => i.Group).Distinct());
            Assert.Equal("set_group_sound", settings[Paging.Target(settings, 3, 1)].Key);
        }

        private static int CountOf(string text, string part)
        {
            int n = 0;
            for (int i = text.IndexOf(part); i >= 0; i = text.IndexOf(part, i + part.Length)) n++;
            return n;
        }

        private sealed class TestScreen : ScreenContext
        {
            public override string Id => "test";

            public override string Title()
            {
                return "Test";
            }

            public override List<NavItem> Build()
            {
                return new List<NavItem>();
            }
        }
    }
}
