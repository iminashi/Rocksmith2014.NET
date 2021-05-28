
using FluentAssertions;

using System.Collections.Generic;
using System.IO;

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
            var sl = new ShowLight(50, ShowLight.FogMin);

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

        [Fact]
        public static void ListOfShowlightsCanBeSavedToXmlFile()
        {
            var showlights = new List<ShowLight> {
                new(10_00, ShowLight.BeamMin),
                new(10_000, ShowLight.FogMax),
                new(12_000, ShowLight.BeamOff),
            };

            ShowLights.Save("showlights_save_test.xml", showlights);
            var content = File.ReadAllText("showlights_save_test.xml");

            content.Should().Contain("<showlights count=\"3\">");
            content.Should().Contain("<showlight time=\"12.000\" note=\"42\" />");
        }

        [Fact]
        public static void ListOfShowlightsCanBeReadFromXmlFile()
        {
            var showlights = ShowLights.Load("Showlights.xml");

            showlights.Should().HaveCount(226);

            //  <showlight time="18.731" note="35" />
            showlights[5].Time.Should().Be(18_731);
            showlights[5].Note.Should().Be(35);
        }
    }
}
