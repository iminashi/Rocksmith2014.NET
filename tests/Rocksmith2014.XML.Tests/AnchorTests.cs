using FluentAssertions;

using Xunit;

namespace Rocksmith2014.XML.Tests
{
    public static class AnchorTests
    {
        [Fact]
        public static void CopyConstructorCopiesAllValues()
        {
            var a1 = new Anchor(22, 4567, 6);

            var a2 = new Anchor(a1);

            a2.Fret.Should().Be(22);
            a2.Time.Should().Be(4567);
            a2.Width.Should().Be(6);
            (a1 == a2).Should().BeTrue();
        }

        [Fact]
        public static void UsesStructuralEquality()
        {
            var a1 = new Anchor(22, 4567, 6);
            var a2 = new Anchor(22, 4567, 6);

            (a1 == a2).Should().BeTrue();
            (a1 != a2).Should().BeFalse();
        }
    }
}
