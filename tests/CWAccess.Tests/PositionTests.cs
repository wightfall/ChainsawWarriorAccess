using System.Linq;
using CWAccess.Nav;
using Xunit;

namespace CWAccess.Tests
{
    /// <summary>"New Game, button, 1 of 10": the place of an item in the list of the screen, when the setting is on.</summary>
    public class PositionTests
    {
        public PositionTests()
        {
            Loc.Use("en");
        }

        [Fact]
        public void ThePositionIsCountedFromOne()
        {
            Assert.Equal("New Game, button, 1 of 10", Position.Add("New Game, button", 0, 10, false, true));
            Assert.Equal("Quit, button, 10 of 10", Position.Add("Quit, button", 9, 10, false, true));
        }

        [Fact]
        public void NothingIsAddedWhenOffOrWhenTheItemSaysItsOwnPlace()
        {
            Assert.Equal("New Game, button", Position.Add("New Game, button", 0, 10, false, false));
            Assert.Equal("Chainsaw, cost 1, 1 of 7", Position.Add("Chainsaw, cost 1, 1 of 7", 3, 15, true, true));
        }

        [Fact]
        public void UnknownPlacesAddNothing()
        {
            Assert.Equal("x", Position.Add("x", -1, 10, false, true));
            Assert.Equal("x", Position.Add("x", 10, 10, false, true));
            Assert.Equal("x", Position.Add("x", 0, 0, false, true));
            Assert.Equal(string.Empty, Position.Add(string.Empty, 0, 3, false, true));
        }

        [Fact]
        public void TheSettingIsOnByDefaultAndInTheSettingsList()
        {
            Assert.True(Settings.AnnouncePosition.Default);
            Assert.Contains(SettingsPanel.Build(), i => i.Key == "set_position");
        }
    }

    /// <summary>Every Steam achievement of the game has its name and description in the mod's language files.</summary>
    public class AchievementNameTests
    {
        public static readonly string[] SteamIds =
        {
            "WIN_EASY", "WIN_MEDIUM", "WIN_HARD", "ALL_ENDINGS", "BYPASS_TRAPS", "CHAIN_OF_SKULLS", "THRONE_OF_SKULLS",
            "DAMAGE_RADIATION", "DAMAGE_VENOM", "DAMAGE_WOUND", "FISTO", "JUST_IN_SLIME", "KILLS_AGENT", "KILLS_DARKNESS",
            "KILLS_MEATMACHINE", "KILLS_MUTANT", "KILLS_RAT", "KILLS_SLIME", "KILLS_TOTAL", "KILLS_TOTAL_2000", "KILLS_ZOMBIE",
            "KNIFE_FIGHT", "SEEMS_IRONIC",
        };

        [Fact]
        public void AllTwentyThreeHaveANameAndADescription()
        {
            Loc.Use("en");
            Assert.Equal(23, SteamIds.Length);
            var missing = SteamIds.Where(id => Loc.TryGet("achname_ACHIEVEMENT_" + id) == null || Loc.TryGet("achdesc_ACHIEVEMENT_" + id) == null).ToList();
            Assert.True(missing.Count == 0, "No name or description: " + string.Join(", ", missing));
            Assert.Equal("The New York Chainsaw Massacre", Loc.Get("achname_ACHIEVEMENT_KILLS_TOTAL_2000"));
        }
    }
}
