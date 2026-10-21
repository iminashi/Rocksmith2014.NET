using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using FluentAssertions;

using Xunit;

namespace Rocksmith2014.XML.Tests
{
    public static class TuningTests
    {
        private const string StockStrings =
            "string0=\"0\" string1=\"0\" string2=\"0\" string3=\"0\" string4=\"0\" string5=\"0\"";

        private static Tuning Read(string xml)
        {
            var tuning = new Tuning();
            using var reader = XmlReader.Create(new StringReader(xml));
            reader.MoveToContent();
            ((IXmlSerializable)tuning).ReadXml(reader);
            return tuning;
        }

        private static string Write(Tuning tuning)
        {
            var sb = new StringBuilder();
            using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("tuning");
                ((IXmlSerializable)tuning).WriteXml(writer);
                writer.WriteEndElement();
            }
            return sb.ToString();
        }

        [Fact]
        public static void FillerString6WithSixDeclaredStringsStaysSixString()
        {
            // What Charter writes for every six-string arrangement.
            var tuning = Read("<tuning string0=\"-2\" string1=\"0\" string2=\"0\" string3=\"0\" string4=\"0\" string5=\"0\" " +
                              "string6=\"0\" string7=\"0\" string8=\"0\" strings=\"6\" />");

            tuning.Strings.Should().HaveCount(6);
            tuning.Strings[0].Should().Be(-2);
            Write(tuning).Should().NotContain("string6").And.NotContain("strings=");
        }

        [Fact]
        public static void FillerString6WithFourDeclaredStringsStaysSixString()
        {
            // What Charter writes for a bass arrangement.
            var tuning = Read($"<tuning {StockStrings} string6=\"0\" string7=\"0\" string8=\"0\" strings=\"4\" />");

            tuning.Strings.Should().HaveCount(6);
        }

        [Fact]
        public static void DeclaredSevenStringsReadsTheLowB()
        {
            var tuning = Read($"<tuning {StockStrings} string6=\"-1\" strings=\"7\" />");

            tuning.Strings.Should().HaveCount(7);
            tuning.Strings[6].Should().Be(-1);
        }

        [Fact]
        public static void String6WithoutDeclaredCountIsSevenString()
        {
            var tuning = Read($"<tuning {StockStrings} string6=\"0\" />");

            tuning.Strings.Should().HaveCount(7);
        }

        [Fact]
        public static void DeclaredSevenStringsWithoutString6Throws()
        {
            Action read = () => Read($"<tuning {StockStrings} strings=\"7\" />");

            read.Should().Throw<XmlException>();
        }

        [Fact]
        public static void MoreThanSevenDeclaredStringsThrows()
        {
            Action read = () => Read($"<tuning {StockStrings} string6=\"0\" string7=\"0\" strings=\"8\" />");

            read.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public static void SixStringWritesOnlyTheStockAttributes()
        {
            Write(new Tuning()).Should().Be($"<tuning {StockStrings} />");
        }

        [Fact]
        public static void SevenStringRoundTrips()
        {
            var tuning = Read($"<tuning {StockStrings} string6=\"-2\" strings=\"7\" />");

            var written = Write(tuning);
            var reread = Read(written);

            written.Should().Contain("string6=\"-2\"").And.Contain("strings=\"7\"");
            reread.Strings.Should().Equal(tuning.Strings);
        }
    }
}
