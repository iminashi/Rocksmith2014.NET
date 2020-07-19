using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents a list of linked difficulty levels.
    /// </summary>
    public sealed class NewLinkedDiff : IXmlSerializable
    {
        /// <summary>
        /// Gets or sets the difficulty level where the link is broken.
        /// </summary>
        public sbyte LevelBreak { get; set; } = -1;

        /// <summary>
        /// Unused, no equivalent in SNG.
        /// </summary>
        public string? Ratio { get; set; }

        /// <summary>
        /// Gets the number of linked phrases.
        /// </summary>
        public int PhraseCount => PhraseIds.Count;

        /// <summary>
        /// A list of the phrase IDs of the linked difficulty levels.
        /// </summary>
        public List<int> PhraseIds { get; set; } = new List<int>();

        /// <summary>
        /// Creates a new linked difficulty.
        /// </summary>
        public NewLinkedDiff() { }

        /// <summary>
        /// Creates a new linked difficulty with the given properties.
        /// </summary>
        /// <param name="levelBreak">The level break.</param>
        /// <param name="phraseIds">A sequence of phrase IDs.</param>
        public NewLinkedDiff(sbyte levelBreak, IEnumerable<int> phraseIds)
        {
            LevelBreak = levelBreak;
            PhraseIds.AddRange(phraseIds);
        }

        private string FormatIDs() => string.Join(',', PhraseIds.Select(i => i.ToString()));

        public override string ToString()
            => $"Level Break: {LevelBreak}, Phrase IDs: {FormatIDs()}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            LevelBreak = sbyte.Parse(reader.GetAttribute("levelBreak"), NumberFormatInfo.InvariantInfo);
            Ratio = reader.GetAttribute("ratio");

            if (!reader.IsEmptyElement && reader.ReadToDescendant("nld_phrase"))
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    PhraseIds.Add(int.Parse(reader.GetAttribute("id"), NumberFormatInfo.InvariantInfo));
                    reader.Read();
                }

                reader.ReadEndElement();
            }
            else
            {
                reader.ReadStartElement();
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("levelBreak", LevelBreak.ToString(NumberFormatInfo.InvariantInfo));
            if (Ratio is null)
                writer.WriteAttributeString("ratio", "1.000");
            else
                writer.WriteAttributeString("ratio", Ratio);
            writer.WriteAttributeString("phraseCount", PhraseCount.ToString(NumberFormatInfo.InvariantInfo));

            if (PhraseCount > 0)
            {
                foreach (var id in PhraseIds)
                {
                    writer.WriteStartElement("nld_phrase");
                    writer.WriteAttributeString("id", id.ToString(NumberFormatInfo.InvariantInfo));
                    writer.WriteEndElement();
                }
            }
        }

        #endregion
    }
}
