using FluentAssertions;

using Xunit;

namespace Rocksmith2014.XML.Tests
{
    public static class ReadMetaData
    {
        [Fact]
        public static void CanReadMetaData()
        {
            var metaData = MetaData.Read("instrumental.xml");

            metaData.Title.Should().Be("Test Instrumental");
            metaData.AverageTempo.Should().Be(160.541f);
            metaData.ArtistNameSort.Should().Be("Test");
            metaData.LastConversionDateTime.Should().Be("5-17-20 15:21");
        }
    }
}
