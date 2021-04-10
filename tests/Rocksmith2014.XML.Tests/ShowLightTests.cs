
using FluentAssertions;

using Xunit;

namespace Rocksmith2014.XML.Tests
{
    public static class ShowLightTests
    {
        [Fact]
        public static void FogRangeTest()
        {
            var sl = new ShowLight();

            for (byte note = ShowLight.FogMin; note <= ShowLight.FogMax; note++)
            {
                sl.Note = note;
                sl.IsFog().Should().BeTrue();
            }

            sl.Note = ShowLight.FogMax + 1;
            sl.IsFog().Should().BeFalse();

            sl.Note = ShowLight.FogMin - 1;
            sl.IsFog().Should().BeFalse();
        }

        [Fact]
        public static void BeamRangeTest()
        {
            var sl = new ShowLight();

            for (byte note = ShowLight.BeamMin; note <= ShowLight.BeamMax; note++)
            {
                sl.Note = note;
                sl.IsBeam().Should().BeTrue();
            }

            sl.Note = ShowLight.BeamOff;
            sl.IsBeam().Should().BeTrue();

            sl.Note = ShowLight.BeamMax + 1;
            sl.IsBeam().Should().BeFalse();

            sl.Note = ShowLight.BeamMin - 1;
            sl.IsBeam().Should().BeFalse();
        }
    }
}
