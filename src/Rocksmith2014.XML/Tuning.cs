using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents the tuning of an instrumental arrangement.
    /// </summary>
    public sealed class Tuning : IXmlSerializable
    {
        /// <summary>
        /// The tuning of each string, represented as an offset from E Standard.
        /// </summary>
        public short[] Strings { get; set; } = new short[6];

        /// <summary>
        /// Sets the tuning offset of the strings.
        /// </summary>
        /// <param name="lowE">The low E string in standard tuning.</param>
        /// <param name="A">The A string in standard tuning.</param>
        /// <param name="D">The D string in standard tuning.</param>
        /// <param name="G">The G string in standard tuning.</param>
        /// <param name="B">The B string in standard tuning.</param>
        /// <param name="highE">The high E string in standard tuning.</param>
        public void SetTuning(short lowE, short A, short D, short G, short B, short highE)
        {
            Strings[0] = lowE;
            Strings[1] = A;
            Strings[2] = D;
            Strings[3] = G;
            Strings[4] = B;
            Strings[5] = highE;
        }

        public override string ToString()
            => $"String0: {Strings[0]}, String1: {Strings[1]}, String2: {Strings[2]}, String3: {Strings[3]}, String4: {Strings[4]}, String5: {Strings[5]}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            for (var i = 0; i < 6; i++)
            {
                var attribute = $"string{i}";
                var value = reader.GetAttribute(attribute) ?? throw new XmlException($"Missing required tuning attribute '{attribute}'.");

                Strings[i] = short.Parse(value, NumberFormatInfo.InvariantInfo);
            }

            // Charter write string6..string8 as copies of string5 on every
            // arrangement, so the seventh string exists only when the count matches.
            // Files without a count fall back to the presence of string6.
            var seventhString = reader.GetAttribute("string6");
            var declaredCount = reader.GetAttribute("strings");
            var stringCount = declaredCount is null
                ? (seventhString is null ? StringLayout.StockStringCount : StringLayout.MaxStringCount)
                : int.Parse(declaredCount, NumberFormatInfo.InvariantInfo);

            if (stringCount > StringLayout.MaxStringCount)
            {
                throw new NotSupportedException(
                    $"Tunings with more than {StringLayout.MaxStringCount} strings are not supported (found {stringCount}).");
            }

            if (stringCount == StringLayout.MaxStringCount)
            {
                if (seventhString is null)
                    throw new XmlException("Missing required tuning attribute 'string6' for a seven-string arrangement.");

                var strings = Strings;
                Array.Resize(ref strings, StringLayout.MaxStringCount);
                Strings = strings;
                Strings[6] = short.Parse(seventhString, NumberFormatInfo.InvariantInfo);
            }

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            for (var i = 0; i < Strings.Length; i++)
                writer.WriteAttributeString($"string{i}", Strings[i].ToString(NumberFormatInfo.InvariantInfo));

            // Only a seven-string declares its count, so six-string files stay unchanged.
            if (Strings.Length == StringLayout.MaxStringCount)
                writer.WriteAttributeString("strings", Strings.Length.ToString(NumberFormatInfo.InvariantInfo));
        }

        #endregion
    }
}
