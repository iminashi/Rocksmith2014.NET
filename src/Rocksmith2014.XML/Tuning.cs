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

            var seventhString = reader.GetAttribute("string6");
            if (seventhString is not null)
            {
                var strings = Strings;
                Array.Resize(ref strings, 7);
                Strings = strings;
                Strings[6] = short.Parse(seventhString, NumberFormatInfo.InvariantInfo);
            }

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            for (var i = 0; i < Strings.Length; i++)
                writer.WriteAttributeString($"string{i}", Strings[i].ToString(NumberFormatInfo.InvariantInfo));
        }

        #endregion
    }
}
