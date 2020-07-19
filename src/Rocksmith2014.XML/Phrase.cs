using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// A mask for phrase attributes.
    /// </summary>
    [Flags]
    public enum PhraseMask : byte
    {
        /// <summary>
        /// Empty mask.
        /// </summary>
        None = 0,

        /// <summary>
        /// A disparity phrase. Purpose unknown.
        /// </summary>
        Disparity = 1 << 0,

        /// <summary>
        /// An ignored phrase. Purpose unknown.
        /// </summary>
        Ignore = 1 << 1,

        /// <summary>
        /// A solo phrase.
        /// </summary>
        Solo = 1 << 2
    }

    /// <summary>
    /// Represents a phrase.
    /// </summary>
    public sealed class Phrase : IXmlSerializable
    {
        #region Quick Access Properties

        /// <summary>
        /// Gets whether this is a disparity phrase.
        /// </summary>
        public bool IsDisparity => (Mask & PhraseMask.Disparity) != 0;

        /// <summary>
        /// Gets whether this is an ignored phrase.
        /// </summary>
        public bool IsIgnore => (Mask & PhraseMask.Ignore) != 0;

        /// <summary>
        /// Gets whether this is a solo phrase.
        /// </summary>
        public bool IsSolo => (Mask & PhraseMask.Solo) != 0;

        #endregion

        /// <summary>
        /// Gets or sets the mask.
        /// </summary>
        public PhraseMask Mask { get; set; }

        /// <summary>
        /// Gets or sets the maximum difficulty of the phrase.
        /// </summary>
        public byte MaxDifficulty { get; set; }

        /// <summary>
        /// Gets or sets the name of the phrase.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new phrase.
        /// </summary>
        public Phrase() { }

        /// <summary>
        /// Creates a new phrase with the given properties.
        /// </summary>
        /// <param name="name">The name of the phrase.</param>
        /// <param name="maxDifficulty">The maximum difficulty of the phrase.</param>
        /// <param name="mask">The phrase mask.</param>
        public Phrase(string name, byte maxDifficulty, PhraseMask mask)
        {
            Name = name;
            MaxDifficulty = maxDifficulty;
            Mask = mask;
        }

        public override string ToString()
            => $"{Name}, Max Diff: {MaxDifficulty}, Mask: {Mask}";

        #region IXmlSerializable Serializable

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader.HasAttributes)
            {
                for (int i = 0; i < reader.AttributeCount; i++)
                {
                    reader.MoveToAttribute(i);

                    switch (reader.Name)
                    {
                        case "disparity":
                            Mask |= (PhraseMask)Utils.ParseBinary(reader.Value);
                            break;
                        case "ignore":
                            Mask |= (PhraseMask)(Utils.ParseBinary(reader.Value) << 1);
                            break;
                        case "solo":
                            Mask |= (PhraseMask)(Utils.ParseBinary(reader.Value) << 2);
                            break;
                        case "maxDifficulty":
                            MaxDifficulty = byte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                            break;
                        case "name":
                            Name = reader.Value;
                            break;
                    }
                }

                reader.MoveToElement();
            }

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("maxDifficulty", MaxDifficulty.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("name", Name);

            if (IsDisparity)
                writer.WriteAttributeString("disparity", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("disparity", "0");

            if (IsIgnore)
                writer.WriteAttributeString("ignore", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("ignore", "0");

            if (IsSolo)
                writer.WriteAttributeString("solo", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("solo", "0");
        }

        #endregion
    }
}
