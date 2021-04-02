using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    public static class Utils
    {
        /// <summary>
        /// Parses a number from a string that is assumed to be either "0" or "1".
        /// </summary>
        /// <param name="text">The input string.</param>
        /// <returns>Either 0 or 1.</returns>
        public static byte ParseBinary(string text)
        {
            //Debug.Assert(text.Length == 1 && (text[0] == '0' || text[0] == '1'));

            char c = text[0];
            unchecked
            {
                c -= '0';
            }

            if (c >= 1)
                return 1;
            else
                return 0;
        }

        /// <summary>
        /// Parses a boolean from a string that is assumed to be either "0" or "1".
        /// </summary>
        /// <param name="text">The input string.</param>
        /// <returns>False if the string is "0", true otherwise.</returns>
        internal static bool ParseBinaryBoolean(string text) => ParseBinary(text) == 1;

        /// <summary>
        /// Converts a boolean into a binary string.
        /// </summary>
        /// <param name="b">The boolean value.</param>
        /// <returns>"1" if true, "0" if false.</returns>
        internal static string BooleanToBinaryString(bool b) => b ? "1" : "0";

        /// <summary>
        /// Converts a time in milliseconds into a string in seconds with three decimal places.
        /// </summary>
        /// <param name="timeCode">The time in milliseconds.</param>
        /// <returns>A string containing the time in seconds.</returns>
        public static string TimeCodeToString(int timeCode)
        {
            string str = timeCode.ToString();
            if (str.Length == 1)
            {
                return "0.00" + str;
            }
            else if (str.Length == 2)
            {
                return "0.0" + str;
            }
            else if (str.Length == 3)
            {
                return "0." + str;
            }
            else
            {
                // Do the hot path without string concatenation
                Span<char> result = stackalloc char[str.Length + 1];

                str.AsSpan(0, str.Length - 3).CopyTo(result);
                result[^4] = '.';

                Span<char> slice = result.Slice(result.Length - 3);
                str.AsSpan(str.Length - 3).CopyTo(slice);

                return result.ToString();
            }
        }

        /// <summary>
        /// Parses a time in milliseconds from a string that is in seconds.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The parsed time in milliseconds.</returns>
        public static int TimeCodeFromFloatString(string input)
        {
            int separatorIndex = input.IndexOf('.');

            // No separator, just convert from seconds to milliseconds
            if (separatorIndex == -1)
                return int.Parse(input) * 1000;

            // Copy the numbers before the decimal separator
            Span<char> temp = stackalloc char[separatorIndex + 3];
            input.AsSpan(0, separatorIndex).CopyTo(temp);

            // Copy at most three numbers after the decimal separator
            var decimals = input.AsSpan(separatorIndex + 1, Math.Min(input.Length - 1 - separatorIndex, 3));
            decimals.CopyTo(temp.Slice(separatorIndex));

            // If there were less than three numbers after the decimal separator, fill the end with zeros
            int i = temp.Length - 1;
            while (temp[i] == '\0')
                temp[i--] = '0';

            return int.Parse(temp);
        }

        internal static int ShiftAndWrap(int value, int positions)
        {
            positions &= 0x1F;

            // Save the existing bit pattern, but interpret it as an unsigned integer.
            uint number = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);

            // Preserve the bits to be discarded.
            uint wrapped = number >> (32 - positions);

            // Shift and wrap the discarded bits.
            return BitConverter.ToInt32(BitConverter.GetBytes((number << positions) | wrapped), 0);
        }

        /// <summary>
        /// Serializes a list into the XML writer with a count attribute.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list.</param>
        /// <param name="listName">The tag name for the list.</param>
        /// <param name="elementName">The tag name for the elements in the list.</param>
        /// <param name="writer">The XML writer.</param>
        internal static void SerializeWithCount<T>(IList<T> list, string listName, string elementName, XmlWriter writer)
            where T : IXmlSerializable
        {
            writer.WriteStartElement(listName);
            writer.WriteAttributeString("count", list.Count.ToString(NumberFormatInfo.InvariantInfo));

            for (int i = 0; i < list.Count; i++)
            {
                writer.WriteStartElement(elementName);
                list[i].WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // </listName>
        }

        /// <summary>
        /// Deserializes a list of elements from an XML reader.
        /// If a count attribute is present, it is used to set the capacity of the list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list.</param>
        /// <param name="reader">The XML reader.</param>
        internal static void DeserializeCountList<T>(List<T> list, XmlReader reader)
            where T : IXmlSerializable, new()
        {
            if (reader.IsEmptyElement)
            {
                reader.ReadStartElement();
                return;
            }

            int.TryParse(reader.GetAttribute("count"), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out int count);
            if (count > 0)
                list.Capacity = count;

            reader.ReadStartElement();

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                T element = new T();
                element.ReadXml(reader);
                list.Add(element);
            }

            reader.ReadEndElement();
        }

        /// <summary>
        /// Throws an exception if the name of the root node for an instrumental arrangement is not "song".
        /// </summary>
        /// <param name="reader">An XML reader located at the node whose name to validate.</param>
        internal static void ValidateRootNameAndVersion(XmlReader reader)
        {
            if (reader.LocalName != "song")
                throw new InvalidOperationException("Expected root node of the XML file to be \"song\", instead found: " + reader.LocalName);
            if (byte.Parse(reader.GetAttribute("version"), NumberFormatInfo.InvariantInfo) < 7)
                throw new NotSupportedException("Expected song version to be 7 or greater. Rocksmith 1 XML files are not supported.");
        }
    }
}
