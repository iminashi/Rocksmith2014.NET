using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents a tone change.
    /// </summary>
    public sealed class ToneChange : IXmlSerializable, IHasTimeCode
    {
        /// <summary>
        /// The time code of the tone change.
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// The ID of the tone.
        /// </summary>
        public byte Id { get; set; }

        /// <summary>
        /// The name of the tone.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new tone change.
        /// </summary>
        public ToneChange() { }

        /// <summary>
        /// Creates a new tone change with the given properties.
        /// </summary>
        /// <param name="name">The name of the tone.</param>
        /// <param name="time">The time code of the tone change.</param>
        /// <param name="id">The ID of the tone.</param>
        public ToneChange(string name, int time, byte id)
        {
            Name = name;
            Time = time;
            Id = id;
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: {Name}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Time = Utils.TimeCodeFromFloatString(reader.GetAttribute("time"));
            string? id = reader.GetAttribute("id");
            if (!string.IsNullOrEmpty(id))
                Id = byte.Parse(id, NumberFormatInfo.InvariantInfo);
            Name = reader.GetAttribute("name");

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("id", Id.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("name", Name);
        }

        #endregion
    }
}
