using FluentAssertions;

using System.Collections.Generic;
using System.IO;

using Xunit;

namespace Rocksmith2014.XML.Tests
{
    public static class VocalTests
    {
        [Fact]
        public static void CopyConstructorCopiesAllValues()
        {
            var v1 = new Vocal(12345, 500, "test", 66);

            var v2 = new Vocal(v1);

            v2.Time.Should().Be(12345);
            v2.Length.Should().Be(500);
            v2.Lyric.Should().Be("test");
            v2.Note.Should().Be(66);
        }

        [Fact]
        public static void ListOfVocalsCanBeSavedToXmlFile()
        {
            var vocals = new List<Vocal> {
                new Vocal(),
                new Vocal(12340, 500, "Test", 66),
                new Vocal(25678, 500, "Test 2", 66)
            };

            Vocals.Save("vocals_save_test.xml", vocals);
            var content = File.ReadAllText("vocals_save_test.xml");

            content.Should().Contain("<vocals count=\"3\">");
            content.Should().Contain("<vocal time=\"12.340\" note=\"66\" length=\"0.500\" lyric=\"Test\" />");
        }

        [Fact]
        public static void ListOfVocalsCanBeReadFromXmlFile()
        {
            var vocals = Vocals.Load("Vocals.xml");

            vocals.Should().HaveCount(8);

            //  <vocal time="28.780" note="254" length="0.600" lyric="sum+"/>
            vocals[5].Time.Should().Be(28_780);
            vocals[5].Note.Should().Be(254);
            vocals[5].Length.Should().Be(600);
            vocals[5].Lyric.Should().Be("sum+");
        }
    }
}
