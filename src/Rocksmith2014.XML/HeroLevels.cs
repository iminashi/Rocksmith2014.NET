using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents the hero levels of a phrase iteration.
    /// </summary>
    public sealed class HeroLevels : IXmlSerializable
    {
        private readonly byte[] levels = new byte[3];

        /// <summary>
        /// Gets or sets the difficulty level for easy.
        /// </summary>
        public byte Easy
        {
            get => levels[0];
            set => levels[0] = value;
        }

        /// <summary>
        /// Gets or sets the difficulty level for medium.
        /// </summary>
        public byte Medium
        {
            get => levels[1];
            set => levels[1] = value;
        }

        /// <summary>
        /// Gets or sets the difficulty level for hard.
        /// </summary>
        public byte Hard
        {
            get => levels[2];
            set => levels[2] = value;
        }

        /// <summary>
        /// Creates a new hero levels.
        /// </summary>
        public HeroLevels() { }

        /// <summary>
        /// Creates a new hero levels with the given difficulty levels.
        /// </summary>
        /// <param name="easy">The difficulty level for easy.</param>
        /// <param name="medium">The difficulty level for medium.</param>
        /// <param name="hard">The difficulty level for hard.</param>
        public HeroLevels(byte easy, byte medium, byte hard)
        {
            Easy = easy;
            Medium = medium;
            Hard = hard;
        }

        /// <summary>
        /// Creates a new hero levels, initialized from an array.
        /// </summary>
        /// <param name="levels">An array with three difficulty levels: easy, medium and hard.</param>
        public HeroLevels(int[] levels)
        {
            Easy = (byte)levels[0];
            Medium = (byte)levels[1];
            Hard = (byte)levels[2];
        }

        public override string ToString()
            => $"Easy: {Easy}, Medium: {Medium}, Hard: {Hard}";

        /// <summary>
        /// True if any hero level's difficulty is greater than zero.
        /// </summary>
        public bool Any => Hard > 0 || Medium > 0 || Easy > 0;

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (!reader.IsEmptyElement && reader.ReadToDescendant("heroLevel"))
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    int hero = int.Parse(reader.GetAttribute("hero"), NumberFormatInfo.InvariantInfo);
                    byte difficulty = byte.Parse(reader.GetAttribute("difficulty"), NumberFormatInfo.InvariantInfo);

                    switch (hero)
                    {
                        case 1: Easy = difficulty; break;
                        case 2: Medium = difficulty; break;
                        case 3: Hard = difficulty; break;
                    }
                    reader.ReadStartElement();
                }

                // In official files, the hard level is missing when it is the same as Medium
                if (Hard == 0 && Medium != 0)
                    Hard = Medium;

                reader.ReadEndElement();
            }
            else
            {
                reader.ReadStartElement();
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("count", "3");

            writer.WriteStartElement("heroLevel");
            writer.WriteAttributeString("hero", "1");
            writer.WriteAttributeString("difficulty", Easy.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteEndElement();

            writer.WriteStartElement("heroLevel");
            writer.WriteAttributeString("hero", "2");
            writer.WriteAttributeString("difficulty", Medium.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteEndElement();

            writer.WriteStartElement("heroLevel");
            writer.WriteAttributeString("hero", "3");
            writer.WriteAttributeString("difficulty", Hard.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteEndElement();
        }

        #endregion
    }
}
