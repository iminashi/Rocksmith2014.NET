using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents an event in an instrumental arrangement.
    /// </summary>
    public sealed class Event : IXmlSerializable, IHasTimeCode
    {
        /// <summary>
        /// Gets or sets the code of the event.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the time code of the event.
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// Creates a new event.
        /// </summary>
        public Event() { }

        /// <summary>
        /// Creates a new event with the given properties.
        /// </summary>
        /// <param name="code">The code of the event.</param>
        /// <param name="time">The time code of the event.</param>
        public Event(string code, int time)
        {
            Code = code;
            Time = time;
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: {Code}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Time = Utils.TimeCodeFromFloatString(reader.GetAttribute("time"));
            Code = reader.GetAttribute("code");

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("code", Code);
        }

        #endregion
    }
}
