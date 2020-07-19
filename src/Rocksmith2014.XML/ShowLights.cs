using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Contains methods for saving and loading a list of show lights to and from an XML file.
    /// </summary>
    public static class ShowLights
    {
        /// <summary>
        /// Saves a show light list into an XML file.
        /// </summary>
        /// <param name="fileName">The target filename.</param>
        /// <param name="showLights">The list of show lights to save.</param>
        public static void Save(string fileName, List<ShowLight> showLights)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            using XmlWriter writer = XmlWriter.Create(fileName, settings);

            writer.WriteStartDocument();
            Utils.SerializeWithCount(showLights, "showlights", "showlight", writer);
        }

        /// <summary>
        /// Loads a list of show lights from an XML file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        /// <returns>A list of show lights deserialized from the file.</returns>
        public static List<ShowLight> Load(string fileName)
        {
            var settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            using XmlReader reader = XmlReader.Create(fileName, settings);

            reader.MoveToContent();
            var showLights = new List<ShowLight>();
            Utils.DeserializeCountList(showLights, reader);
            return showLights;
        }
    }
}
