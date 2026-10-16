using System;

using FluentAssertions;

using Xunit;

namespace Rocksmith2014.XML.Tests
{
    public static class StringLayoutTests
    {
        [Fact]
        public static void SixStringOrderIsStorageOrder()
        {
            StringLayout.LowToHigh(6).Should().Equal(0, 1, 2, 3, 4, 5);
        }

        [Fact]
        public static void SevenStringOrderStartsWithLowB()
        {
            StringLayout.LowToHigh(7).Should().Equal(6, 0, 1, 2, 3, 4, 5);
        }

        [Fact]
        public static void MusicalRankPlacesLowBFirst()
        {
            StringLayout.MusicalRank(6, 7).Should().Be(0);
            StringLayout.MusicalRank(0, 7).Should().Be(1);
            StringLayout.MusicalRank(5, 7).Should().Be(6);
            StringLayout.MusicalRank(3, 6).Should().Be(3);
        }

        [Fact]
        public static void StandardMidiNotesMatchStandardTuning()
        {
            StringLayout.StandardMidiNote(0).Should().Be(40); // E2
            StringLayout.StandardMidiNote(4).Should().Be(59); // B3
            StringLayout.StandardMidiNote(5).Should().Be(64); // E4
            StringLayout.StandardMidiNote(6).Should().Be(35); // B1
        }

        [Fact]
        public static void ToLowToHighReordersValues()
        {
            var frets = new sbyte[] { 0, 1, 2, 3, 4, 5, 9 };

            StringLayout.ToLowToHigh(frets).Should().Equal(9, 0, 1, 2, 3, 4, 5);
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(8)]
        public static void UnsupportedStringCountsAreRejected(int stringCount)
        {
            StringLayout.IsSupportedStringCount(stringCount).Should().BeFalse();
            Assert.Throws<ArgumentOutOfRangeException>(() => StringLayout.LowToHigh(stringCount));
        }

        [Fact]
        public static void RankOutsideLayoutIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StringLayout.MusicalRank(6, 6));
        }
    }
}
