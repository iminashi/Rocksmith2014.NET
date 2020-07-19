using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Metadata about an instrumental arrangement.
    /// </summary>
    public sealed class ArrangementProperties : IXmlSerializable
    {
        /// <summary>
        /// Sets whether this is the main arrangement displayed in the Learn-A-Song list.
        /// </summary>
        public bool Represent { get; set; }

        /// <summary>
        /// Sets whether this is a bonus arrangement.
        /// </summary>
        public bool BonusArrangement { get; set; }

        /// <summary>
        /// Sets whether this arrangement is in standard tuning.
        /// </summary>
        public bool StandardTuning { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains non-standard chords.
        /// </summary>
        public bool NonStandardChords { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains barre chords.
        /// </summary>
        public bool BarreChords { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains power chords.
        /// </summary>
        public bool PowerChords { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains drop D power chords.
        /// </summary>
        public bool DropDPower { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains open chords.
        /// </summary>
        public bool OpenChords { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains finger picking.
        /// </summary>
        public bool FingerPicking { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains notes that have "pick direction" set to 1 (unused).
        /// </summary>
        public bool PickDirection { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains double stops.
        /// </summary>
        public bool DoubleStops { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains palm mutes.
        /// </summary>
        public bool PalmMutes { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains harmonics.
        /// </summary>
        public bool Harmonics { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains pinch harmonics.
        /// </summary>
        public bool PinchHarmonics { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains hammer-ons or pull-offs.
        /// </summary>
        public bool Hopo { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains tremolo picking.
        /// </summary>
        public bool Tremolo { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains slides.
        /// </summary>
        public bool Slides { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains unpitched slides.
        /// </summary>
        public bool UnpitchedSlides { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains bends.
        /// </summary>
        public bool Bends { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains tapping.
        /// </summary>
        public bool Tapping { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains vibratos.
        /// </summary>
        public bool Vibrato { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains fret-hand mutes.
        /// </summary>
        public bool FretHandMutes { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains slaps or pops.
        /// </summary>
        public bool SlapPop { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains two finger picking (mainly for bass).
        /// </summary>
        public bool TwoFingerPicking { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains fifths or octaves
        /// </summary>
        public bool FifthsAndOctaves { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains syncopation.
        /// </summary>
        public bool Syncopation { get; set; }

        /// <summary>
        /// Sets whether this bass arrangement is played with a pick.
        /// </summary>
        public bool BassPick { get; set; }

        /// <summary>
        /// Sets whether this arrangement contains sustains.
        /// </summary>
        public bool Sustain { get; set; }

        /// <summary>
        /// Sets whether this is a lead arrangement.
        /// </summary>
        public bool PathLead { get; set; }

        /// <summary>
        /// Sets whether this is a rhythm arrangement.
        /// </summary>
        public bool PathRhythm { get; set; }

        /// <summary>
        /// Sets whether this is a bass arrangement.
        /// </summary>
        public bool PathBass { get; set; }

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Represent = Utils.ParseBinaryBoolean(reader.GetAttribute("represent"));
            BonusArrangement = Utils.ParseBinaryBoolean(reader.GetAttribute("bonusArr"));
            StandardTuning = Utils.ParseBinaryBoolean(reader.GetAttribute("standardTuning"));
            NonStandardChords = Utils.ParseBinaryBoolean(reader.GetAttribute("nonStandardChords"));
            BarreChords = Utils.ParseBinaryBoolean(reader.GetAttribute("barreChords"));
            PowerChords = Utils.ParseBinaryBoolean(reader.GetAttribute("powerChords"));
            DropDPower = Utils.ParseBinaryBoolean(reader.GetAttribute("dropDPower"));
            OpenChords = Utils.ParseBinaryBoolean(reader.GetAttribute("openChords"));
            FingerPicking = Utils.ParseBinaryBoolean(reader.GetAttribute("fingerPicking"));
            PickDirection = Utils.ParseBinaryBoolean(reader.GetAttribute("pickDirection"));
            DoubleStops = Utils.ParseBinaryBoolean(reader.GetAttribute("doubleStops"));
            PalmMutes = Utils.ParseBinaryBoolean(reader.GetAttribute("palmMutes"));
            Harmonics = Utils.ParseBinaryBoolean(reader.GetAttribute("harmonics"));
            PinchHarmonics = Utils.ParseBinaryBoolean(reader.GetAttribute("pinchHarmonics"));
            Hopo = Utils.ParseBinaryBoolean(reader.GetAttribute("hopo"));
            Tremolo = Utils.ParseBinaryBoolean(reader.GetAttribute("tremolo"));
            Slides = Utils.ParseBinaryBoolean(reader.GetAttribute("slides"));
            UnpitchedSlides = Utils.ParseBinaryBoolean(reader.GetAttribute("unpitchedSlides"));
            Bends = Utils.ParseBinaryBoolean(reader.GetAttribute("bends"));
            Tapping = Utils.ParseBinaryBoolean(reader.GetAttribute("tapping"));
            Vibrato = Utils.ParseBinaryBoolean(reader.GetAttribute("vibrato"));
            FretHandMutes = Utils.ParseBinaryBoolean(reader.GetAttribute("fretHandMutes"));
            SlapPop = Utils.ParseBinaryBoolean(reader.GetAttribute("slapPop"));
            TwoFingerPicking = Utils.ParseBinaryBoolean(reader.GetAttribute("twoFingerPicking"));
            FifthsAndOctaves = Utils.ParseBinaryBoolean(reader.GetAttribute("fifthsAndOctaves"));
            Syncopation = Utils.ParseBinaryBoolean(reader.GetAttribute("syncopation"));
            BassPick = Utils.ParseBinaryBoolean(reader.GetAttribute("bassPick"));
            Sustain = Utils.ParseBinaryBoolean(reader.GetAttribute("sustain"));
            PathLead = Utils.ParseBinaryBoolean(reader.GetAttribute("pathLead"));
            PathRhythm = Utils.ParseBinaryBoolean(reader.GetAttribute("pathRhythm"));
            PathBass = Utils.ParseBinaryBoolean(reader.GetAttribute("pathBass"));

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("represent", Utils.BooleanToBinaryString(Represent));
            writer.WriteAttributeString("bonusArr", Utils.BooleanToBinaryString(BonusArrangement));
            writer.WriteAttributeString("standardTuning", Utils.BooleanToBinaryString(StandardTuning));
            writer.WriteAttributeString("nonStandardChords", Utils.BooleanToBinaryString(NonStandardChords));
            writer.WriteAttributeString("barreChords", Utils.BooleanToBinaryString(BarreChords));
            writer.WriteAttributeString("powerChords", Utils.BooleanToBinaryString(PowerChords));
            writer.WriteAttributeString("dropDPower", Utils.BooleanToBinaryString(DropDPower));
            writer.WriteAttributeString("openChords", Utils.BooleanToBinaryString(OpenChords));
            writer.WriteAttributeString("fingerPicking", Utils.BooleanToBinaryString(FingerPicking));
            writer.WriteAttributeString("pickDirection", Utils.BooleanToBinaryString(PickDirection));
            writer.WriteAttributeString("doubleStops", Utils.BooleanToBinaryString(DoubleStops));
            writer.WriteAttributeString("palmMutes", Utils.BooleanToBinaryString(PalmMutes));
            writer.WriteAttributeString("harmonics", Utils.BooleanToBinaryString(Harmonics));
            writer.WriteAttributeString("pinchHarmonics", Utils.BooleanToBinaryString(PinchHarmonics));
            writer.WriteAttributeString("hopo", Utils.BooleanToBinaryString(Hopo));
            writer.WriteAttributeString("tremolo", Utils.BooleanToBinaryString(Tremolo));
            writer.WriteAttributeString("slides", Utils.BooleanToBinaryString(Slides));
            writer.WriteAttributeString("unpitchedSlides", Utils.BooleanToBinaryString(UnpitchedSlides));
            writer.WriteAttributeString("bends", Utils.BooleanToBinaryString(Bends));
            writer.WriteAttributeString("tapping", Utils.BooleanToBinaryString(Tapping));
            writer.WriteAttributeString("vibrato", Utils.BooleanToBinaryString(Vibrato));
            writer.WriteAttributeString("fretHandMutes", Utils.BooleanToBinaryString(FretHandMutes));
            writer.WriteAttributeString("slapPop", Utils.BooleanToBinaryString(SlapPop));
            writer.WriteAttributeString("twoFingerPicking", Utils.BooleanToBinaryString(TwoFingerPicking));
            writer.WriteAttributeString("fifthsAndOctaves", Utils.BooleanToBinaryString(FifthsAndOctaves));
            writer.WriteAttributeString("syncopation", Utils.BooleanToBinaryString(Syncopation));
            writer.WriteAttributeString("bassPick", Utils.BooleanToBinaryString(BassPick));
            writer.WriteAttributeString("sustain", Utils.BooleanToBinaryString(Sustain));
            writer.WriteAttributeString("pathLead", Utils.BooleanToBinaryString(PathLead));
            writer.WriteAttributeString("pathRhythm", Utils.BooleanToBinaryString(PathRhythm));
            writer.WriteAttributeString("pathBass", Utils.BooleanToBinaryString(PathBass));
        }

        #endregion
    }
}
