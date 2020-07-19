using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents properties for a RS1 phrase.
    /// </summary>
    public sealed class PhraseProperty : IXmlSerializable
    {
        public int PhraseId { get; set; }
        public short Redundant { get; set; }
        public sbyte LevelJump { get; set; }
        public int Empty { get; set; }
        public int Difficulty { get; set; }

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            PhraseId = int.Parse(reader.GetAttribute("phraseId"), NumberFormatInfo.InvariantInfo);
            Redundant = short.Parse(reader.GetAttribute("redundant"), NumberFormatInfo.InvariantInfo);
            LevelJump = sbyte.Parse(reader.GetAttribute("levelJump"), NumberFormatInfo.InvariantInfo);
            Empty = int.Parse(reader.GetAttribute("empty"), NumberFormatInfo.InvariantInfo);
            Difficulty = int.Parse(reader.GetAttribute("difficulty"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("phraseId", PhraseId.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("redundant", Redundant.ToString(NumberFormatInfo.InvariantInfo));

            writer.WriteAttributeString("levelJump", LevelJump.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("empty", Empty.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("difficulty", Difficulty.ToString(NumberFormatInfo.InvariantInfo));
        }

        #endregion
    }
}
