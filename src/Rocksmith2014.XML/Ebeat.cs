using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents a beat.
    /// </summary>
    public sealed class Ebeat : IXmlSerializable, IEquatable<Ebeat>, IHasTimeCode
    {
        /// <summary>
        /// Gets or sets the time code of the beat.
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// Gets or sets the measure number of the beat. -1 for weak beats.
        /// </summary>
        public short Measure { get; set; } = -1;

        /// <summary>
        /// Creates a new beat.
        /// </summary>
        public Ebeat() { }

        /// <summary>
        /// Creates a new beat with the given properties.
        /// </summary>
        /// <param name="time">The time code of the beat.</param>
        /// <param name="measure">The measure number of the beat.</param>
        public Ebeat(int time, short measure)
        {
            Time = time;
            Measure = measure;
        }

        public override string ToString()
        {
            string result = Utils.TimeCodeToString(Time);

            if (Measure != -1)
                result += $": Measure: {Measure}";

            return result;
        }

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
                    case "measure":
                        Measure = short.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                }
            }

            reader.MoveToElement();

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));

            if (Measure != -1)
                writer.WriteAttributeString("measure", Measure.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("measure", "-1");
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
            => obj is Ebeat other && Equals(other);

        public bool Equals(Ebeat other)
        {
            if (ReferenceEquals(this, other))
                return true;

            return !(other is null) && Time == other.Time && Measure == other.Measure;
        }

        public override int GetHashCode()
            => Utils.ShiftAndWrap(Time.GetHashCode(), 2) ^ Measure.GetHashCode();

        public static bool operator ==(Ebeat left, Ebeat right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(Ebeat left, Ebeat right)
            => !(left == right);

        #endregion
    }
}
