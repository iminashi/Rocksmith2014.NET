using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents an anchor that sets the position and width of the anchor zone.
    /// </summary>
    public sealed class Anchor : IXmlSerializable, IHasTimeCode, IEquatable<Anchor>
    {
        /// <summary>
        /// Gets or sets the fret position of the anchor.
        /// </summary>
        public sbyte Fret { get; set; }

        /// <summary>
        /// Gets or sets the time code of the anchor.
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// Gets or sets the width of the anchor.
        /// </summary>
        public sbyte Width { get; set; }

        /// <summary>
        /// Creates a new anchor.
        /// </summary>
        public Anchor() { }

        /// <summary>
        /// Creates a new anchor with the given properties.
        /// </summary>
        /// <param name="fret">The fret position of the anchor.</param>
        /// <param name="time">The time code of the anchor.</param>
        /// <param name="width">The width of the anchor (default: 4).</param>
        public Anchor(sbyte fret, int time, sbyte width = 4)
        {
            Fret = fret;
            Time = time;
            Width = width;
        }

        /// <summary>
        /// Creates a new anchor by copying the values from another anchor.
        /// </summary>
        /// <param name="other">The other anchor to copy.</param>
        public Anchor(Anchor other)
        {
            Fret = other.Fret;
            Time = other.Time;
            Width = other.Width;
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: Fret {Fret}, Width: {Width:F3}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Time = Utils.TimeCodeFromFloatString(reader.GetAttribute("time"));
            Fret = sbyte.Parse(reader.GetAttribute("fret"), NumberFormatInfo.InvariantInfo);
            Width = (sbyte)float.Parse(reader.GetAttribute("width"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("fret", Fret.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("width", Width.ToString("F3", NumberFormatInfo.InvariantInfo));
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
            => obj is Anchor other && Equals(other);

        public bool Equals(Anchor other)
        {
            if (other is null)
                return false;

            return Fret == other.Fret
                && Width == other.Width
                && Time == other.Time;
        }

        public override int GetHashCode()
            => (Time, Fret, Width).GetHashCode();

        public static bool operator ==(Anchor left, Anchor right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(Anchor left, Anchor right)
            => !(left == right);

        #endregion
    }
}
