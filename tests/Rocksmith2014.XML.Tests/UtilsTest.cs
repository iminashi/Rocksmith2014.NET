using FluentAssertions;

using Xunit;

namespace Rocksmith2014.XML.Tests
{
    public class UtilsTest
    {
        [Theory]
        [InlineData(0, "0.000")]
        [InlineData(18, "0.018")]
        [InlineData(235, "0.235")]
        [InlineData(1000, "1.000")]
        [InlineData(1234, "1.234")]
        [InlineData(20500, "20.500")]
        [InlineData(989999, "989.999")]
        [InlineData(987456123, "987456.123")]
        public void TimeCodeToString_ConvertsCorrectly(int input, string expected)
        {
            Utils.TimeCodeToString(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("0.000", 0)]
        [InlineData("0.018", 18)]
        [InlineData("0.235", 235)]
        [InlineData("1.000", 1000)]
        [InlineData("1.234", 1234)]
        [InlineData("20.500", 20500)]
        [InlineData("989.999", 989999)]
        [InlineData("1", 1000)]
        [InlineData("8.7", 8700)]
        [InlineData("6.66", 6660)]
        [InlineData("18.00599", 18005)]
        [InlineData("254.112", 254112)]
        [InlineData("9504.11299999", 9504112)]
        public void TimeCodeFromFloatString_ParsesCorrectly(string input, int expected)
        {
            Utils.TimeCodeFromFloatString(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("1", 1)]
        [InlineData("2", 1)]
        [InlineData("9", 1)]
        public void ParseBinary_ParsesCorrectly(string input, byte expected)
        {
            Utils.ParseBinary(input).Should().Be(expected);
        }
    }
}
