using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents a bend value with a time code and a strength.
    /// </summary>
    public struct BendValue : IXmlSerializable, IEquatable<BendValue>, IHasTimeCode
    {
        /// <summary>
        /// Gets the time code of the bend value.
        /// </summary>
        public int Time { get; private set; }

        /// <summary>
        /// Gets the strength of the bend in half steps.
        /// </summary>
        public float Step { get; private set; }

        /// <summary>
        /// Creates a new bend value with the given properties.
        /// </summary>
        /// <param name="time">The time code of the bend value.</param>
        /// <param name="step">The strength of the bend value.</param>
        public BendValue(int time, float step)
        {
            Time = time;
            Step = step;
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: {Step:F2}";

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
                    case "step":
                        Step = float.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                }
            }

            reader.MoveToElement();

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            if (Step != 0f || !InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("step", Step.ToString("F3", NumberFormatInfo.InvariantInfo));
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
            => obj is BendValue other && Equals(other);

        public bool Equals(BendValue other)
            => Time == other.Time && Step == other.Step;

        public override int GetHashCode()
            => Utils.ShiftAndWrap(Time.GetHashCode(), 2) ^ Step.GetHashCode();

        public static bool operator ==(BendValue left, BendValue right)
            => left.Equals(right);

        public static bool operator !=(BendValue left, BendValue right)
            => !(left == right);

        #endregion
    }
}
