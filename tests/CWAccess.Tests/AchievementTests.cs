using CWAccess.Patches;
using Xunit;

namespace CWAccess.Tests
{
    /// <summary>Achievements the game unlocks are said by name; unknown ones get a readable name from their Steam id.</summary>
    public class AchievementTests
    {
        [Theory]
        [InlineData("ACHIEVEMENT_WIN_EASY", "Win easy")]
        [InlineData("ACHIEVEMENT_CHAIN_OF_SKULLS", "Chain of skulls")]
        [InlineData("", "")]
        public void UnknownIdsGetAReadableName(string id, string name)
        {
            Assert.Equal(name, Achievements.NameOf(id));
        }

        [Fact]
        public void TheAnnouncementHasTitleAndDescription()
        {
            Loc.Use("en");
            Assert.Equal("Achievement unlocked: Fisticuffs. Win a fight with your fists.", Loc.Get("achievement_unlocked", "Fisticuffs", "Win a fight with your fists."));
        }
    }
}
