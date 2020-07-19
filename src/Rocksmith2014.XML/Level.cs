using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents a difficulty level with notes, chords, anchors and hand shapes.
    /// </summary>
    public sealed class Level : IXmlSerializable
    {
        /// <summary>
        /// Gets or sets the difficulty level.
        /// </summary>
        public sbyte Difficulty { get; set; }

        /// <summary>
        /// A list of notes in the level.
        /// </summary>
        public List<Note> Notes { get; set; }

        /// <summary>
        /// A list of chords in the level.
        /// </summary>
        public List<Chord> Chords { get; set; }

        /// <summary>
        /// A list of anchors in the level.
        /// </summary>
        public List<Anchor> Anchors { get; set; }

        /// <summary>
        /// A list of hand shapes in the level.
        /// </summary>
        public List<HandShape> HandShapes { get; set; }

        /// <summary>
        /// Creates a new level.
        /// </summary>
        public Level() : this(-1) { }

        /// <summary>
        /// Creates a new level with the given difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty level.</param>
        public Level(sbyte difficulty) : this(difficulty, new List<Note>(), new List<Chord>(), new List<Anchor>(), new List<HandShape>()) { }

        /// <summary>
        /// Creates a new level with the given difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty level.</param>
        /// <param name="notes">The notes in the level.</param>
        /// <param name="chords">The chords in the level.</param>
        /// <param name="anchors">The anchors in the level.</param>
        /// <param name="handshapes">The hand shapes in the level.</param>
        public Level(sbyte difficulty, List<Note> notes, List<Chord> chords, List<Anchor> anchors, List<HandShape> handshapes)
        {
            Difficulty = difficulty;
            Notes = notes;
            Chords = chords;
            Anchors = anchors;
            HandShapes = handshapes;
        }

        public override string ToString()
            => $"Difficulty: {Difficulty}, Notes: {Notes.Count}, Chords: {Chords.Count}, Handshapes: {HandShapes.Count}, Anchors: {Anchors.Count}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Difficulty = sbyte.Parse(reader.GetAttribute("difficulty"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                switch (reader.LocalName)
                {
                    case "notes":
                        Utils.DeserializeCountList(Notes, reader);
                        break;

                    case "chords":
                        Utils.DeserializeCountList(Chords, reader);
                        break;

                    case "anchors":
                        Utils.DeserializeCountList(Anchors, reader);
                        break;

                    case "handShapes":
                        Utils.DeserializeCountList(HandShapes, reader);
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.ReadEndElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("difficulty", Difficulty.ToString(NumberFormatInfo.InvariantInfo));

            Utils.SerializeWithCount(Notes, "notes", "note", writer);
            Utils.SerializeWithCount(Chords, "chords", "chord", writer);
            Utils.SerializeWithCount(Anchors, "anchors", "anchor", writer);
            Utils.SerializeWithCount(HandShapes, "handShapes", "handShape", writer);
        }

        #endregion
    }
}
