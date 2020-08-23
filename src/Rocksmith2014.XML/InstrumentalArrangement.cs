using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents a Rocksmith 2014 instrumental arrangement.
    /// </summary>
    [XmlRoot("song")]
    public class InstrumentalArrangement : IXmlSerializable
    {
        /// <summary>
        /// Sets whether to use abridged XML when writing a file.
        /// </summary>
        internal static bool UseAbridgedXml { get; set; }

        /// <summary>
        /// A list of comments found after the root node of the XML file.
        /// </summary>
        public List<RSXmlComment> XmlComments { get; } = new List<RSXmlComment>();

        /// <summary>
        /// Meta data about the arrangement.
        /// </summary>
        public MetaData MetaData = new MetaData();

        /// <summary>
        /// The version of the file. Default: 8
        /// </summary>
        public byte Version { get; set; } = 8;

        /// <summary>
        /// Gets the time code for the first beat in the song.
        /// </summary>
        public int StartBeat => Ebeats.Count > 0 ? Ebeats[0].Time : 0;

        /// <summary>
        /// A list of phrases in the arrangement.
        /// </summary>
        public List<Phrase> Phrases { get; set; } = new List<Phrase>();

        /// <summary>
        /// A list of phrase iterations in the arrangement.
        /// </summary>
        public List<PhraseIteration> PhraseIterations { get; set; } = new List<PhraseIteration>();

        /// <summary>
        /// A list of linked difficulty levels in the arrangement.
        /// </summary>
        public List<NewLinkedDiff> NewLinkedDiffs { get; set; } = new List<NewLinkedDiff>();

        /// <summary>
        /// Leftover from RS1. Used in some early RS2014 files.
        /// </summary>
        public List<LinkedDiff>? LinkedDiffs { get; set; }

        /// <summary>
        /// Leftover from RS1. Used in some early RS2014 files.
        /// </summary>
        public List<PhraseProperty>? PhraseProperties { get; set; }

        /// <summary>
        /// A list of chord templates in the arrangement.
        /// </summary>
        public List<ChordTemplate> ChordTemplates { get; set; } = new List<ChordTemplate>();

        /// <summary>
        /// A list of beats in the arrangement.
        /// </summary>
        public List<Ebeat> Ebeats { get; set; } = new List<Ebeat>();

        /// <summary>
        /// The tone names and changes for the arrangement.
        /// </summary>
        public ToneInfo Tones { get; } = new ToneInfo();

        /// <summary>
        /// A list of sections in the arrangement.
        /// </summary>
        public List<Section> Sections { get; set; } = new List<Section>();

        /// <summary>
        /// A list of events in the arrangement.
        /// </summary>
        public List<Event> Events { get; set; } = new List<Event>();

        /// <summary>
        /// Contains the transcription of the arrangement (i.e. only the highest difficulty level of all the phrases).
        /// </summary>
        public Level? TranscriptionTrack { get; set; }

        /// <summary>
        /// The difficulty levels of the arrangement.
        /// </summary>
        public List<Level> Levels { get; set; } = new List<Level>();

        /// <summary>
        /// Saves this Rocksmith 2014 arrangement into the given file.
        /// </summary>
        /// <param name="fileName">The target file name.</param>
        /// <param name="writeAbridgedXml">Controls whether to write an abridged XML file or not. Default: true.</param>
        public void Save(string fileName, bool writeAbridgedXml = true)
        {
            UseAbridgedXml = writeAbridgedXml;

            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            using XmlWriter writer = XmlWriter.Create(fileName, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("song");
            ((IXmlSerializable)this).WriteXml(writer);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Loads a Rocksmith 2014 arrangement from the given file name.
        /// </summary>
        /// <param name="fileName">The file name of a Rocksmith 2014 instrumental arrangement.</param>
        /// <returns>A Rocksmith 2014 arrangement parsed from the XML file.</returns>
        public static InstrumentalArrangement Load(string fileName)
        {
            var settings = new XmlReaderSettings
            {
                IgnoreComments = false,
                IgnoreWhitespace = true
            };

            using XmlReader reader = XmlReader.Create(fileName, settings);

            reader.MoveToContent();
            var arr = new InstrumentalArrangement();
            ((IXmlSerializable)arr).ReadXml(reader);
            return arr;
        }

        /// <summary>
        /// Loads a Rocksmith 2014 arrangement from the given file name on a background thread.
        /// </summary>
        /// <param name="fileName">The file name of a Rocksmith 2014 instrumental arrangement.</param>
        /// <returns>A Rocksmith 2014 arrangement parsed from the XML file.</returns>
        public static Task<InstrumentalArrangement> LoadAsync(string fileName)
            => Task.Run(() => Load(fileName));

        /// <summary>
        /// Reads the tone names from the given file using an XmlReader.
        /// </summary>
        /// <param name="fileName">The file name of a Rocksmith 2014 instrumental arrangement.</param>
        /// <returns>A tone info object with the tone names. Null represents the absence of a tone name.</returns>
        public static ToneInfo ReadToneNames(string fileName)
        {
            using XmlReader reader = XmlReader.Create(fileName);

            reader.MoveToContent();

            if (reader.LocalName != "song")
                throw new InvalidOperationException("Expected root node of the XML file to be \"song\", instead found: " + reader.LocalName);

            var tones = new ToneInfo();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "tonebase":
                            tones.BaseToneName = reader.ReadElementContentAsString();
                            break;
                        case "tonea":
                            tones.Names[0] = reader.ReadElementContentAsString();
                            break;
                        case "toneb":
                            tones.Names[1] = reader.ReadElementContentAsString();
                            break;
                        case "tonec":
                            tones.Names[2] = reader.ReadElementContentAsString();
                            break;
                        case "toned":
                            tones.Names[3] = reader.ReadElementContentAsString();
                            break;
                        // The tone names should come before the sections.
                        case "sections":
                        case "levels":
                            return tones;
                    }
                }
            }

            return tones;
        }

        public void FixHighDensity()
        {
            static void removeHighDensity(Chord chord, bool removeChordNotes)
            {
                if (chord.IsHighDensity)
                {
                    chord.IsHighDensity = false;
                    if (removeChordNotes)
                    {
                        // Set the chord as ignored if it has any harmonics in it
                        if (chord.ChordNotes?.Count > 0 && chord.ChordNotes.Any(cn => cn.IsHarmonic))
                            chord.IsIgnore = true;

                        chord.ChordNotes = null;
                    }
                }
            }

            // Make sure that the version of the XML file is 8
            Version = 8;

            foreach (var level in Levels)
            {
                foreach (var hs in level.HandShapes)
                {
                    var chordsInHs =
                        from chord in level.Chords
                        where chord.Time >= hs.StartTime && chord.Time < hs.EndTime
                        select chord;

                    bool startsWithMute = false;
                    int chordNum = 0;

                    foreach (var chord in chordsInHs)
                    {
                        chordNum++;

                        if (chordNum == 1)
                        {
                            // If the handshape starts with a fret hand mute, we need to be careful
                            if (chord.IsFretHandMute)
                            {
                                startsWithMute = true;
                                // Frethand-muted chords without techniques should not have chord notes
                                if (chord.ChordNotes?.All(cn => cn.Sustain == 0) == true)
                                    chord.ChordNotes = null;
                            }
                            else
                            {
                                // Do not remove the chord notes even if the first chord somehow has "high density"
                                removeHighDensity(chord, false);
                                continue;
                            }
                        }

                        if (startsWithMute && !chord.IsFretHandMute)
                        {
                            // Do not remove the chord notes on the first non-muted chord after muted chord(s)
                            removeHighDensity(chord, false);
                            startsWithMute = false;
                        }
                        else
                        {
                            removeHighDensity(chord, true);
                        }
                    }
                }
            }
        }

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Version = byte.Parse(reader.GetAttribute("version"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();

            // Only preserve comments directly after the root element
            while (reader.NodeType == XmlNodeType.Comment)
            {
                XmlComments.Add(new RSXmlComment(reader.Value));
                reader.Read();
            }

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                switch (reader.LocalName)
                {
                    case "title":
                        MetaData.Title = reader.ReadElementContentAsString();
                        break;
                    case "arrangement":
                        MetaData.Arrangement = reader.ReadElementContentAsString();
                        break;
                    case "part":
                        MetaData.Part = short.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "centOffset":
                        MetaData.CentOffset = int.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "songLength":
                        MetaData.SongLength = Utils.TimeCodeFromFloatString(reader.ReadElementContentAsString());
                        break;
                    case "songNameSort":
                        MetaData.TitleSort = reader.ReadElementContentAsString();
                        break;
                    case "averageTempo":
                        MetaData.AverageTempo = float.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "tuning":
                        ((IXmlSerializable)MetaData.Tuning).ReadXml(reader);
                        break;
                    case "capo":
                        MetaData.Capo = sbyte.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;

                    case "artistName":
                        MetaData.ArtistName = reader.ReadElementContentAsString();
                        break;
                    case "artistNameSort":
                        MetaData.ArtistNameSort = reader.ReadElementContentAsString();
                        break;
                    case "albumName":
                        MetaData.AlbumName = reader.ReadElementContentAsString();
                        break;
                    case "albumNameSort":
                        MetaData.AlbumNameSort = reader.ReadElementContentAsString();
                        break;
                    case "albumYear":
                        string content = reader.ReadElementContentAsString();
                        if (!string.IsNullOrEmpty(content))
                            MetaData.AlbumYear = int.Parse(content, NumberFormatInfo.InvariantInfo);
                        break;
                    case "albumArt":
                        MetaData.AlbumArt = reader.ReadElementContentAsString();
                        break;

                    case "arrangementProperties":
                        MetaData.ArrangementProperties = new ArrangementProperties();
                        ((IXmlSerializable)MetaData.ArrangementProperties).ReadXml(reader);
                        break;

                    case "lastConversionDateTime":
                        MetaData.LastConversionDateTime = reader.ReadElementContentAsString();
                        break;

                    case "phrases":
                        Utils.DeserializeCountList(Phrases, reader);
                        break;
                    case "phraseIterations":
                        Utils.DeserializeCountList(PhraseIterations, reader);
                        break;
                    case "newLinkedDiffs":
                        Utils.DeserializeCountList(NewLinkedDiffs, reader);
                        break;
                    case "linkedDiffs":
                        LinkedDiffs = new List<LinkedDiff>();
                        Utils.DeserializeCountList(LinkedDiffs, reader);
                        break;
                    case "phraseProperties":
                        PhraseProperties = new List<PhraseProperty>();
                        Utils.DeserializeCountList(PhraseProperties, reader);
                        break;
                    case "chordTemplates":
                        Utils.DeserializeCountList(ChordTemplates, reader);
                        break;
                    case "ebeats":
                        Utils.DeserializeCountList(Ebeats, reader);
                        break;

                    case "tonebase":
                        Tones.BaseToneName = reader.ReadElementContentAsString();
                        break;
                    case "tonea":
                        Tones.Names[0] = reader.ReadElementContentAsString();
                        break;
                    case "toneb":
                        Tones.Names[1] = reader.ReadElementContentAsString();
                        break;
                    case "tonec":
                        Tones.Names[2] = reader.ReadElementContentAsString();
                        break;
                    case "toned":
                        Tones.Names[3] = reader.ReadElementContentAsString();
                        break;
                    case "tones":
                        Utils.DeserializeCountList(Tones.Changes, reader);
                        break;

                    case "sections":
                        Utils.DeserializeCountList(Sections, reader);
                        break;
                    case "events":
                        Utils.DeserializeCountList(Events, reader);
                        break;
                    case "transcriptionTrack":
                        TranscriptionTrack = new Level();
                        ((IXmlSerializable)TranscriptionTrack).ReadXml(reader);
                        break;
                    case "levels":
                        Utils.DeserializeCountList(Levels, reader);
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
            writer.WriteAttributeString("version", Version.ToString(NumberFormatInfo.InvariantInfo));

            if (XmlComments.Count > 0)
            {
                foreach (var comment in XmlComments)
                {
                    writer.WriteComment(comment.Value);
                }
            }

            writer.WriteElementString("title", MetaData.Title);
            writer.WriteElementString("arrangement", MetaData.Arrangement);
            writer.WriteElementString("part", MetaData.Part.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteElementString("offset", (StartBeat / -1000f).ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteElementString("centOffset", MetaData.CentOffset.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteElementString("songLength", Utils.TimeCodeToString(MetaData.SongLength));

            if (MetaData.TitleSort != null)
            {
                writer.WriteElementString("songNameSort", MetaData.TitleSort);
            }

            writer.WriteElementString("startBeat", Utils.TimeCodeToString(StartBeat));
            writer.WriteElementString("averageTempo", MetaData.AverageTempo.ToString("F3", NumberFormatInfo.InvariantInfo));

            if (MetaData.Tuning != null)
            {
                writer.WriteStartElement("tuning");
                ((IXmlSerializable)MetaData.Tuning).WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteElementString("capo", MetaData.Capo.ToString(NumberFormatInfo.InvariantInfo));

            writer.WriteElementString("artistName", MetaData.ArtistName);
            if (MetaData.ArtistNameSort != null)
            {
                writer.WriteElementString("artistNameSort", MetaData.ArtistNameSort);
            }

            writer.WriteElementString("albumName", MetaData.AlbumName);
            if (MetaData.AlbumNameSort != null)
            {
                writer.WriteElementString("albumNameSort", MetaData.AlbumNameSort);
            }

            writer.WriteElementString("albumYear", MetaData.AlbumYear.ToString(NumberFormatInfo.InvariantInfo));

            if (MetaData.AlbumArt != null)
            {
                writer.WriteElementString("albumArt", MetaData.AlbumArt);
            }

            writer.WriteElementString("crowdSpeed", "1");

            writer.WriteStartElement("arrangementProperties");
            ((IXmlSerializable)MetaData.ArrangementProperties).WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteElementString("lastConversionDateTime", MetaData.LastConversionDateTime);

            Utils.SerializeWithCount(Phrases, "phrases", "phrase", writer);
            Utils.SerializeWithCount(PhraseIterations, "phraseIterations", "phraseIteration", writer);
            Utils.SerializeWithCount(NewLinkedDiffs, "newLinkedDiffs", "newLinkedDiff", writer);

            if (LinkedDiffs != null)
                Utils.SerializeWithCount(LinkedDiffs, "linkedDiffs", "linkedDiff", writer);

            if (PhraseProperties != null)
                Utils.SerializeWithCount(PhraseProperties, "phraseProperties", "phraseProperty", writer);

            Utils.SerializeWithCount(ChordTemplates, "chordTemplates", "chordTemplate", writer);
            Utils.SerializeWithCount(Ebeats, "ebeats", "ebeat", writer);

            Tones.WriteToXml(writer);

            Utils.SerializeWithCount(Sections, "sections", "section", writer);
            Utils.SerializeWithCount(Events, "events", "event", writer);

            if (TranscriptionTrack != null)
            {
                writer.WriteStartElement("transcriptionTrack");
                ((IXmlSerializable)TranscriptionTrack).WriteXml(writer);
                writer.WriteEndElement();
            }

            Utils.SerializeWithCount(Levels, "levels", "level", writer);
        }

        #endregion
    }
}
