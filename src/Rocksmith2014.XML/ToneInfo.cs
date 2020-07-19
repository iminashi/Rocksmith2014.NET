using System.Collections.Generic;
using System.Xml;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents the tone information for an instrumental arrangement.
    /// </summary>
    public sealed class ToneInfo
    {
        /// <summary>
        /// The name of the tone that the arrangement starts with.
        /// </summary>
        public string? BaseToneName { get; set; }

        /// <summary>
        /// An array of up to four tone names.
        /// </summary>
        public readonly string?[] Names = new string?[4];

        /// <summary>
        /// A list of tone changes in the arrangement.
        /// </summary>
        public List<ToneChange> Changes { get; set; } = new List<ToneChange>();

        internal void WriteToXml(XmlWriter writer)
        {
            if (BaseToneName != null)
                writer.WriteElementString("tonebase", BaseToneName);

            for (int i = 0; i < Names.Length; i++)
            {
                if (Names[i] != null)
                    writer.WriteElementString("tone" + (char)('a' + i), Names[i]);
            }

            if (Changes != null)
                Utils.SerializeWithCount(Changes, "tones", "tone", writer);
        }
    }
}
