using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents a hand shape.
    /// </summary>
    public sealed class HandShape : IXmlSerializable, IHasTimeCode
    {
        /// <summary>
        /// Gets or sets the chord ID of the hand shape.
        /// </summary>
        public short ChordId { get; set; }

        /// <summary>
        /// Gets or sets the end time of the hand shape.
        /// </summary>
        public int EndTime { get; set; }

        /// <summary>
        /// Gets or sets the start time of the hand shape.
        /// </summary>
        public int StartTime { get; set; }

        /// <summary>
        /// Gets the time code of the hand shape.
        /// </summary>
        public int Time => StartTime;

        /// <summary>
        /// Creates a new hand shape.
        /// </summary>
        public HandShape() { }

        /// <summary>
        /// Creates a new hand shape with the given properties.
        /// </summary>
        /// <param name="chordId">The chord ID of the hand shape.</param>
        /// <param name="startTime">The start time of the hand shape.</param>
        /// <param name="endTime">The end time of the hand shape.</param>
        public HandShape(short chordId, int startTime, int endTime)
        {
            ChordId = chordId;
            StartTime = startTime;
            EndTime = endTime;
        }

        /// <summary>
        /// Creates a new hand shape by copying the properties of another hand shape.
        /// </summary>
        /// <param name="other">The hand shape to copy.</param>
        public HandShape(HandShape other)
        {
            ChordId = other.ChordId;
            StartTime = other.StartTime;
            EndTime = other.EndTime;
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(StartTime)} - {Utils.TimeCodeToString(EndTime)}: Chord ID {ChordId}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            ChordId = short.Parse(reader.GetAttribute("chordId"), NumberFormatInfo.InvariantInfo);
            StartTime = Utils.TimeCodeFromFloatString(reader.GetAttribute("startTime"));
            EndTime = Utils.TimeCodeFromFloatString(reader.GetAttribute("endTime"));

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("chordId", ChordId.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("startTime", Utils.TimeCodeToString(StartTime));
            writer.WriteAttributeString("endTime", Utils.TimeCodeToString(EndTime));
        }

        #endregion
    }
}
