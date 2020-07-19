using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents a section.
    /// </summary>
    public sealed class Section : IXmlSerializable, IHasTimeCode
    {
        /// <summary>
        /// The name of the section.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The ordering number of the section.
        /// </summary>
        public short Number { get; set; }

        /// <summary>
        /// The time code for the start of the section.
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// Creates a new section.
        /// </summary>
        public Section() { }

        /// <summary>
        /// Creates a new section with the given properties.
        /// </summary>
        /// <param name="name">The name of the section.</param>
        /// <param name="startTime">The star time of the section.</param>
        /// <param name="number">The number of the section.</param>
        public Section(string name, int startTime, short number)
        {
            Name = name;
            Number = number;
            Time = startTime;
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: {Name} #{Number}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Name = reader.GetAttribute("name");
            Number = short.Parse(reader.GetAttribute("number"), NumberFormatInfo.InvariantInfo);
            Time = Utils.TimeCodeFromFloatString(reader.GetAttribute("startTime"));

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("number", Number.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("startTime", Utils.TimeCodeToString(Time));
        }

        #endregion
    }
}