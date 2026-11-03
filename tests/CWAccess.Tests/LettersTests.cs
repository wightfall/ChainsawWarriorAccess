using CWAccess.Nav;
using Xunit;

namespace CWAccess.Tests
{
    /// <summary>A letter jumps to an item by what is said or by its text on screen (the game's English).</summary>
    public class LettersTests
    {
        [Fact]
        public void TheSpokenTextMatchesInEnglish()
        {
            var item = new NavItem { Text = () => "Achievements and best scores, button" };
            Assert.True(Letters.Matches(item, 'A'));
            Assert.True(Letters.Matches(item, 'a'));
            Assert.False(Letters.Matches(item, 'b'));
        }

        [Fact]
        public void InThaiTheEnglishOnScreenStillMatches()
        {
            var item = new NavItem { Text = () => "ความสำเร็จและคะแนนสูงสุด, ปุ่ม", ScreenText = () => "Achievements and best scores" };
            Assert.True(Letters.Matches(item, 'A'));
            Assert.False(Letters.Matches(item, 'N'));
        }

        [Fact]
        public void ItemsWithoutScreenTextOrWithAFaultyOneOnlyMatchTheirSpeech()
        {
            Assert.False(Letters.Matches(new NavItem { Text = () => "ภาษา" }, 'L'));
            Assert.False(Letters.Matches(new NavItem { Text = () => "ภาษา", ScreenText = () => string.Empty }, 'L'));
            Assert.False(Letters.Matches(new NavItem { Text = () => "ภาษา", ScreenText = () => throw new System.InvalidOperationException() }, 'L'));
            Assert.False(Letters.Matches(null, 'L'));
        }
    }
}
