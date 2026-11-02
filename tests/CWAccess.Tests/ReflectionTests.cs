using Xunit;

namespace CWAccess.Tests
{
    /// <summary>R: the cached lookups of the game's private fields (used many times a frame).</summary>
    public class ReflectionTests
    {
        private class Base
        {
#pragma warning disable 414
            private int hidden = 7;
#pragma warning restore 414
        }

        private sealed class Derived : Base
        {
            public string Name = "card";
        }

        [Fact]
        public void FindsFieldsOfTheTypeAndItsBaseTypesOnce()
        {
            var d = new Derived();
            Assert.Equal("card", R.Get<string>(d, "Name"));
            Assert.Equal(7, R.Get<int>(d, "hidden"));
            Assert.Same(R.Field(typeof(Derived), "hidden"), R.Field(typeof(Derived), "hidden"));
            R.Set(d, "Name", "enemy");
            Assert.Equal("enemy", d.Name);
        }

        [Fact]
        public void MissingFieldsAndObjectsGiveDefaults()
        {
            Assert.Null(R.Field(typeof(Derived), "noSuchField"));
            Assert.Equal(0, R.Get<int>(new Derived(), "noSuchField"));
            Assert.Null(R.Get<string>(null, "Name"));
            // The same name on another type is looked up separately.
            Assert.Null(R.Field(typeof(ReflectionTests), "Name"));
            Assert.NotNull(R.Field(typeof(Derived), "Name"));
        }
    }
}
