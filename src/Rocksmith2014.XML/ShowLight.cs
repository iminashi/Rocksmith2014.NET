using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents a show light.
    /// </summary>
    public sealed class ShowLight : IHasTimeCode, IXmlSerializable
    {
        /// <summary>
        /// The time code of the show light.
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// The note of the show light.
        ///
        /// <para>
        /// Valid values: 24-35, 42, 48-59, 66-67.
        /// </para>
        /// </summary>
        public byte Note { get; set; }

        /// <summary>
        /// Creates a new show light.
        /// </summary>
        public ShowLight() { }

        /// <summary>
        /// Creates a new show light with the given properties.
        /// </summary>
        /// <param name="time">The time code of the show light.</param>
        /// <param name="note">The note of the show light.</param>
        public ShowLight(int time, byte note)
        {
            Time = time;
            Note = note;
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: {Note}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                switch (reader.Name)
                {
                    case "time":
                        Time = Utils.TimeCodeFromFloatString(reader.Value);
                        break;
                    case "note":
                        Note = byte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                }
            }

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("note", Note.ToString(NumberFormatInfo.InvariantInfo));
        }

        #endregion
    }
}
