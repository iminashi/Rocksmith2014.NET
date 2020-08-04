namespace Rocksmith2014.XML
{
    public sealed class MetaData
    {
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

        /// <summary>
        /// Contains various metadata about the arrangement.
        /// </summary>
        public ArrangementProperties ArrangementProperties { get; set; } = new ArrangementProperties();

        /// <summary>
        /// The date the arrangement was converted into SNG (or XML).
        /// </summary>
        public string? LastConversionDateTime { get; set; }

        // Other metadata:
        //
        // Offset - Start beat * -1. Handled automatically.
        // WaveFilePath - Used only in official files.
        // InternalName - Used only in official files.
        // CrowdSpeed - Completely purposeless since it does not have an equivalent in the SNG files or manifest files.
        //              The crowd speed is controlled with events e0, e1 and e2.
    }
}
