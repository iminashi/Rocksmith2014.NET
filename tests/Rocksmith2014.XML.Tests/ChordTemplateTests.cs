using System;
using System.IO;

using FluentAssertions;

using Xunit;

namespace Rocksmith2014.XML.Tests
{
    public static class ChordTemplateTests
    {
        private static string TempXmlPath()
            => Path.Combine(Path.GetTempPath(), $"chordtemplate-{Guid.NewGuid():N}.xml");

        private static InstrumentalArrangement SaveAndLoad(InstrumentalArrangement arrangement, bool abridged)
        {
            var path = TempXmlPath();
            try
            {
                arrangement.Save(path, abridged);
                return InstrumentalArrangement.Load(path);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public static void DefaultTemplateHasSixUnusedStrings()
        {
            var template = new ChordTemplate();

            template.StringCount.Should().Be(6);
            template.Frets.Should().AllBeEquivalentTo((sbyte)-1);
            template.Fingers.Should().AllBeEquivalentTo((sbyte)-1);
        }

        [Fact]
        public static void ConstructorWithSevenValuesCreatesSevenStringTemplate()
        {
            var template = new ChordTemplate(
                "B5", "B5",
                [3, -1, -1, -1, -1, -1, 1],
                [2, -1, -1, -1, -1, -1, 0]);

            template.StringCount.Should().Be(7);
            template.Frets[6].Should().Be(0);
            template.Fingers[6].Should().Be(1);
        }

        [Fact]
        public static void EnsureStringCountOnlyGrows()
        {
            var template = new ChordTemplate("E", "E", new sbyte[6], new sbyte[6]);

            template.EnsureStringCount(7);
            template.EnsureStringCount(6);

            template.StringCount.Should().Be(7);
            template.Frets[6].Should().Be(-1);
            template.Frets[0].Should().Be(0);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void SevenStringTemplateRoundTripsThroughXml(bool abridged)
        {
            var arrangement = new InstrumentalArrangement();

            arrangement.MetaData.Tuning.Strings = new short[] { 0, 0, 0, 0, 0, 0, -1 };
            arrangement.ChordTemplates.Add(new ChordTemplate(
                "Power", "Power",
                [3, -1, -1, -1, -1, -1, 1],
                [2, -1, -1, -1, -1, -1, 2]));

            // The seventh string is unused in this template; the string count must still survive.
            arrangement.ChordTemplates.Add(new ChordTemplate(
                "E", "E",
                [-1, 2, 3, 1, -1, -1, -1],
                [0, 2, 2, 1, 0, 0, -1]));

            var loaded = SaveAndLoad(arrangement, abridged);

            loaded.MetaData.Tuning.Strings.Should().Equal(0, 0, 0, 0, 0, 0, -1);
            loaded.ChordTemplates[0].StringCount.Should().Be(7);
            loaded.ChordTemplates[0].Frets.Should().Equal(2, -1, -1, -1, -1, -1, 2);
            loaded.ChordTemplates[0].Fingers.Should().Equal(3, -1, -1, -1, -1, -1, 1);
            loaded.ChordTemplates[1].StringCount.Should().Be(7);
            loaded.ChordTemplates[1].Frets[6].Should().Be(-1);
        }

        [Fact]
        public static void LoadNormalizesSixStringTemplatesInSevenStringArrangement()
        {
            var arrangement = new InstrumentalArrangement();
            arrangement.MetaData.Tuning.Strings = new short[7];
            arrangement.ChordTemplates.Add(new ChordTemplate("E", "E", new sbyte[6], new sbyte[6]));

            var loaded = SaveAndLoad(arrangement, abridged: true);

            loaded.ChordTemplates[0].StringCount.Should().Be(7);
            loaded.ChordTemplates[0].Frets[6].Should().Be(-1);
            loaded.ChordTemplates[0].Fingers[6].Should().Be(-1);
        }

        [Fact]
        public static void SixStringXmlOutputHasNoExtendedAttributes()
        {
            var arrangement = new InstrumentalArrangement();
            arrangement.ChordTemplates.Add(new ChordTemplate("E", "E", new sbyte[6], new sbyte[6]));
            var path = TempXmlPath();

            try
            {
                arrangement.Save(path, writeAbridgedXml: false);
                var xml = File.ReadAllText(path);

                xml.Should().Contain("fret5=");
                xml.Should().NotContain("fret6");
                xml.Should().NotContain("finger6");
                xml.Should().NotContain("string6");

                InstrumentalArrangement.Load(path).ChordTemplates[0].StringCount.Should().Be(6);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public static void UnsupportedStringIndexIsRejected()
        {
            var path = TempXmlPath();
            File.WriteAllText(
                path,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<song version=\"8\"><chordTemplates count=\"1\">" +
                "<chordTemplate chordName=\"X\" displayName=\"X\" fret7=\"1\" />" +
                "</chordTemplates></song>");

            try
            {
                Assert.Throws<NotSupportedException>(() => InstrumentalArrangement.Load(path));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
