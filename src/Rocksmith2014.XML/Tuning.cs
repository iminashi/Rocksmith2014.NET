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
        public readonly short[] Strings = new short[6];

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
            Strings[0] = short.Parse(reader.GetAttribute("string0"), NumberFormatInfo.InvariantInfo);
            Strings[1] = short.Parse(reader.GetAttribute("string1"), NumberFormatInfo.InvariantInfo);
            Strings[2] = short.Parse(reader.GetAttribute("string2"), NumberFormatInfo.InvariantInfo);
            Strings[3] = short.Parse(reader.GetAttribute("string3"), NumberFormatInfo.InvariantInfo);
            Strings[4] = short.Parse(reader.GetAttribute("string4"), NumberFormatInfo.InvariantInfo);
            Strings[5] = short.Parse(reader.GetAttribute("string5"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("string0", Strings[0].ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("string1", Strings[1].ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("string2", Strings[2].ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("string3", Strings[3].ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("string4", Strings[4].ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("string5", Strings[5].ToString(NumberFormatInfo.InvariantInfo));
        }

        #endregion
    }
}
