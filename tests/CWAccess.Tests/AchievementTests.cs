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
        public void EachAchievementIsSaidOnceAlthoughTheGameAndSteamBothReportIt()
        {
            string id = "ACHIEVEMENT_TEST_" + System.Guid.NewGuid().ToString("N");
            Assert.True(Achievements.FirstTime(id));
            Assert.False(Achievements.FirstTime(id));
            Assert.False(Achievements.FirstTime(id.ToLowerInvariant()));
            Assert.False(Achievements.FirstTime(null));
            Assert.False(Achievements.FirstTime(string.Empty));
        }

        [Theory]
        [InlineData(0u, 0u, true)]
        [InlineData(10u, 50u, false)]
        [InlineData(50u, 50u, false)]
        public void OnlySteamReportsWithoutProgressAreUnlocks(uint current, uint max, bool unlock)
        {
            Assert.Equal(unlock, Achievements.IsUnlock(current, max));
        }

        [Fact]
        public void ProgressIsSaidWithItsNumbers()
        {
            Loc.Use("en");
            Assert.Equal("Achievement progress: Zombie Zapper, 50 of 200.", Loc.Get("achievement_progress", "Zombie Zapper", 50u, 200u));
        }

        [Fact]
        public void TheAnnouncementHasTitleAndDescription()
        {
            Loc.Use("en");
            Assert.Equal("Achievement unlocked: Fisticuffs. Win a fight with your fists.", Loc.Get("achievement_unlocked", "Fisticuffs", "Win a fight with your fists."));
        }
    }
}
