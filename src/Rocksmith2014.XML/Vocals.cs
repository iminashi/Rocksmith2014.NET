using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Contains methods for saving and loading a list of vocals to and from an XML file.
    /// </summary>
    public static class Vocals
    {
        /// <summary>
        /// Saves this vocals list into an XML file.
        /// </summary>
        /// <param name="fileName">The target filename.</param>
        /// <param name="vocals">The list of vocals to save.</param>
        public static void Save(string fileName, List<Vocal> vocals)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            using XmlWriter writer = XmlWriter.Create(fileName, settings);

            writer.WriteStartDocument();
            Utils.SerializeWithCount(vocals, "vocals", "vocal", writer);
        }

        /// <summary>
        /// Loads a list of vocals from an XML file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        /// <returns>A list of vocals deserialized from the file.</returns>
        public static List<Vocal> Load(string fileName)
        {
            var settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            using XmlReader reader = XmlReader.Create(fileName, settings);

            reader.MoveToContent();
            var vocals = new List<Vocal>();
            Utils.DeserializeCountList(vocals, reader);
            return vocals;
        }
    }
}
