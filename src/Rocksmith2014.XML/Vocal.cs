using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents a vocal in a vocals arrangement.
    /// </summary>
    public sealed class Vocal : IHasTimeCode, IXmlSerializable
    {
        /// <summary>
        /// The time code of the vocal.
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// The MIDI note of the vocal. Not used in Rocksmith 2014.
        /// </summary>
        public byte Note { get; set; }

        /// <summary>
        /// The length of the vocal in milliseconds.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// The lyric string of the vocal.
        /// </summary>
        public string Lyric { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new vocal.
        /// </summary>
        public Vocal() { }

        /// <summary>
        /// Creates a new vocal with the given properties.
        /// </summary>
        /// <param name="time">The time code of the vocal.</param>
        /// <param name="length">The length of the vocal in milliseconds.</param>
        /// <param name="lyric">The lyric string of the vocal.</param>
        /// <param name="note">The MIDI note of the vocal (default: 60, middle C).</param>
        public Vocal(int time, int length, string lyric, byte note = 60)
        {
            Time = time;
            Note = note;
            Length = length;
            Lyric = lyric;
        }

        /// <summary>
        /// Creates a new vocal by copying the properties of another vocal.
        /// </summary>
        /// <param name="other">The vocal to copy.</param>
        public Vocal(Vocal other)
        {
            Time = other.Time;
            Note = other.Note;
            Length = other.Length;
            Lyric = other.Lyric;
        }

        public override string ToString()
            => $"Time: {Utils.TimeCodeToString(Time)}, Length: {Utils.TimeCodeToString(Length)}: {Lyric}";

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
                        Time = Utils.TimeCodeFromFloatString(reader.Value); //float.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "note":
                        Note = byte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "length":
                        Length = Utils.TimeCodeFromFloatString(reader.Value); //float.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "lyric":
                        Lyric = reader.Value;
                        break;
                }
            }

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("note", Note.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("length", Utils.TimeCodeToString(Length));
            writer.WriteAttributeString("lyric", Lyric);
        }

        #endregion
    }
}
