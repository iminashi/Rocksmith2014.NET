using FluentAssertions;

using Xunit;

namespace Rocksmith2014.XML.Tests
{
    public static class HandShapeTests
    {
        [Fact]
        public static void CopyConstructorCopiesAllValues()
        {
            var hs1 = new HandShape(15, 7777, 8888);

            var hs2 = new HandShape(hs1);

            hs2.ChordId.Should().Be(15);
            hs2.StartTime.Should().Be(7777);
            hs2.EndTime.Should().Be(8888);
        }
    }
}
