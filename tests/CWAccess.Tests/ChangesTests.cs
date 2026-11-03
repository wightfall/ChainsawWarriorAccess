using Xunit;

namespace CWAccess.Tests
{
    /// <summary>Equipment gained and lost is counted per kind of card.</summary>
    public class ChangesTests
    {
        [Fact]
        public void ASecondCopyIsAGain()
        {
            Assert.Equal(new[] { "Laser Lance" }, Changes.Added(new[] { "Laser Lance", "Needle" }, new[] { "Laser Lance", "Needle", "Laser Lance" }));
        }

        [Fact]
        public void TheSameCardsAgainAreNoChange()
        {
            Assert.Empty(Changes.Added(new[] { "A", "B", "B" }, new[] { "B", "A", "B" }));
        }

        [Fact]
        public void LossesAreGainsTheOtherWayRound()
        {
            Assert.Equal(new[] { "B" }, Changes.Added(new[] { "A" }, new[] { "A", "B" }));
            Assert.Equal(new[] { "B", "B" }, Changes.Added(new[] { "A" }, new[] { "B", "A", "B" }));
            Assert.Empty(Changes.Added(new[] { "A", "B" }, new[] { "A" }));
        }
    }
}
