using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// The version of the file. Default: 8
        /// </summary>
        public byte Version { get; set; } = 8;

        /// <summary>
        /// The name of the arrangement: Lead, Rhythm, Combo or Bass.
        /// </summary>
        public string? Arrangement { get; set; }

        /// <summary>
        /// The part number in a similarly named arrangements (e.g. 2 in "Combo 2").
        /// </summary>
        public short Part { get; set; }

        /// <summary>
        /// The tuning offset in cents from 440Hz.
        /// </summary>
        public int CentOffset { get; set; }

        /// <summary>
        /// The length of the arrangement in milliseconds.
        /// </summary>
        public int SongLength { get; set; }

        /// <summary>
        /// The average tempo of the arrangement in beats per minute.
        /// </summary>
        public float AverageTempo { get; set; } = 120.000f;

        /// <summary>
        /// The tuning of the arrangement.
        /// </summary>
        public Tuning Tuning { get; set; } = new Tuning();

        /// <summary>
        /// The fret where the capo is set. 0 for no capo.
        /// </summary>
        public sbyte Capo { get; set; }

        /// <summary>
        /// The title of the arrangement.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The title of the arrangement when sorting.
        /// </summary>
        public string? TitleSort { get; set; }

        /// <summary>
        /// The artist name.
        /// </summary>
        public string? ArtistName { get; set; }

        /// <summary>
        /// The artist name when sorting.
        /// </summary>
        public string? ArtistNameSort { get; set; }

        /// <summary>
        /// The album name. Not displayed in the game.
        /// </summary>
        public string? AlbumName { get; set; }

        /// <summary>
        /// The album name when sorting. Not used by the game.
        /// </summary>
        public string? AlbumNameSort { get; set; }

        /// <summary>
        /// The year the album/song was released.
        /// </summary>
        public int AlbumYear { get; set; }

        /// <summary>
        /// Path to the image file for the album art.
        /// </summary>
        public string? AlbumArt { get; set; }

        // Other metadata:
        //
        // Offset - Start beat * -1. Handled automatically.
        // WaveFilePath - Used only in official files.
        // InternalName - Used only in official files.
        // CrowdSpeed - Completely purposeless since it does not have an equivalent in the SNG files or manifest files.
        //              The crowd speed is controlled with events e0, e1 and e2.

        /// <summary>
        /// Gets the time code for the first beat in the song.
        /// </summary>
        public int StartBeat => Ebeats.Count > 0 ? Ebeats[0].Time : 0;

        /// <summary>
        /// Contains various metadata about the arrangement.
        /// </summary>
        public ArrangementProperties ArrangementProperties { get; set; } = new ArrangementProperties();

        /// <summary>
        /// The date the arrangement was converted into SNG (or XML).
        /// </summary>
        public string? LastConversionDateTime { get; set; }

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
                        Title = reader.ReadElementContentAsString();
                        break;
                    case "arrangement":
                        Arrangement = reader.ReadElementContentAsString();
                        break;
                    case "part":
                        Part = short.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "centOffset":
                        CentOffset = int.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "songLength":
                        SongLength = Utils.TimeCodeFromFloatString(reader.ReadElementContentAsString());
                        break;
                    case "songNameSort":
                        TitleSort = reader.ReadElementContentAsString();
                        break;
                    case "averageTempo":
                        AverageTempo = float.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "tuning":
                        ((IXmlSerializable)Tuning).ReadXml(reader);
                        break;
                    case "capo":
                        Capo = sbyte.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;

                    case "artistName":
                        ArtistName = reader.ReadElementContentAsString();
                        break;
                    case "artistNameSort":
                        ArtistNameSort = reader.ReadElementContentAsString();
                        break;
                    case "albumName":
                        AlbumName = reader.ReadElementContentAsString();
                        break;
                    case "albumNameSort":
                        AlbumNameSort = reader.ReadElementContentAsString();
                        break;
                    case "albumYear":
                        string content = reader.ReadElementContentAsString();
                        if (!string.IsNullOrEmpty(content))
                            AlbumYear = int.Parse(content, NumberFormatInfo.InvariantInfo);
                        break;
                    case "albumArt":
                        AlbumArt = reader.ReadElementContentAsString();
                        break;

                    case "arrangementProperties":
                        ArrangementProperties = new ArrangementProperties();
                        ((IXmlSerializable)ArrangementProperties).ReadXml(reader);
                        break;

                    case "lastConversionDateTime":
                        LastConversionDateTime = reader.ReadElementContentAsString();
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

            writer.WriteElementString("title", Title);
            writer.WriteElementString("arrangement", Arrangement);
            writer.WriteElementString("part", Part.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteElementString("offset", (StartBeat / -1000f).ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteElementString("centOffset", CentOffset.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteElementString("songLength", Utils.TimeCodeToString(SongLength));

            if (TitleSort != null)
            {
                writer.WriteElementString("songNameSort", TitleSort);
            }

            writer.WriteElementString("startBeat", Utils.TimeCodeToString(StartBeat));
            writer.WriteElementString("averageTempo", AverageTempo.ToString("F3", NumberFormatInfo.InvariantInfo));

            if (Tuning != null)
            {
                writer.WriteStartElement("tuning");
                ((IXmlSerializable)Tuning).WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteElementString("capo", Capo.ToString(NumberFormatInfo.InvariantInfo));

            writer.WriteElementString("artistName", ArtistName);
            if (ArtistNameSort != null)
            {
                writer.WriteElementString("artistNameSort", ArtistNameSort);
            }

            writer.WriteElementString("albumName", AlbumName);
            if (AlbumNameSort != null)
            {
                writer.WriteElementString("albumNameSort", AlbumNameSort);
            }

            writer.WriteElementString("albumYear", AlbumYear.ToString(NumberFormatInfo.InvariantInfo));

            if (AlbumArt != null)
            {
                writer.WriteElementString("albumArt", AlbumArt);
            }

            writer.WriteElementString("crowdSpeed", "1");

            writer.WriteStartElement("arrangementProperties");
            ((IXmlSerializable)ArrangementProperties).WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteElementString("lastConversionDateTime", LastConversionDateTime);

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
