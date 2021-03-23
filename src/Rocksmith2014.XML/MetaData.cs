using System;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    public sealed class MetaData
    {
        // Other metadata that is not included here:
        //
        // Offset - Start beat * -1. Handled automatically.
        // WaveFilePath - Used only in official files.
        // InternalName - Used only in official files.
        // CrowdSpeed - Completely purposeless since it does not have an equivalent in the SNG files or manifest files.
        //              The crowd speed is controlled with the events e0, e1 and e2.

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
        public float CentOffset { get; set; }

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

        /// <summary>
        /// Contains various metadata about the arrangement.
        /// </summary>
        public ArrangementProperties ArrangementProperties { get; set; } = new ArrangementProperties();

        /// <summary>
        /// The date the arrangement was converted into SNG (or XML).
        /// </summary>
        public string? LastConversionDateTime { get; set; }

        /// <summary>
        /// Reads only the meta data from a Rocksmith 2014 instrumental XML file.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>Meta data read from the file.</returns>
        public static MetaData Read(string fileName)
        {
            using XmlReader reader = XmlReader.Create(fileName);

            reader.MoveToContent();

            Utils.ValidateRootName(reader);

            var metaData = new MetaData();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "title":
                            metaData.Title = reader.ReadElementContentAsString();
                            break;
                        case "arrangement":
                            metaData.Arrangement = reader.ReadElementContentAsString();
                            break;
                        case "part":
                            metaData.Part = short.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                            break;
                        case "centOffset":
                            metaData.CentOffset = float.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                            break;
                        case "songLength":
                            metaData.SongLength = Utils.TimeCodeFromFloatString(reader.ReadElementContentAsString());
                            break;
                        case "songNameSort":
                            metaData.TitleSort = reader.ReadElementContentAsString();
                            break;
                        case "averageTempo":
                            metaData.AverageTempo = float.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                            break;
                        case "tuning":
                            ((IXmlSerializable)metaData.Tuning).ReadXml(reader);
                            break;
                        case "capo":
                            metaData.Capo = sbyte.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                            break;
                        case "artistName":
                            metaData.ArtistName = reader.ReadElementContentAsString();
                            break;
                        case "artistNameSort":
                            metaData.ArtistNameSort = reader.ReadElementContentAsString();
                            break;
                        case "albumName":
                            metaData.AlbumName = reader.ReadElementContentAsString();
                            break;
                        case "albumNameSort":
                            metaData.AlbumNameSort = reader.ReadElementContentAsString();
                            break;
                        case "albumYear":
                            string content = reader.ReadElementContentAsString();
                            if (!string.IsNullOrEmpty(content))
                                metaData.AlbumYear = int.Parse(content, NumberFormatInfo.InvariantInfo);
                            break;
                        case "albumArt":
                            metaData.AlbumArt = reader.ReadElementContentAsString();
                            break;
                        case "arrangementProperties":
                            metaData.ArrangementProperties = new ArrangementProperties();
                            ((IXmlSerializable)metaData.ArrangementProperties).ReadXml(reader);
                            break;
                        case "lastConversionDateTime":
                            metaData.LastConversionDateTime = reader.ReadElementContentAsString();
                            break;
                        // The metadata should come before the phrases.
                        case "phrases":
                        case "phraseIterations":
                            return metaData;
                    }
                }
            }

            return metaData;
        }
    }
}
