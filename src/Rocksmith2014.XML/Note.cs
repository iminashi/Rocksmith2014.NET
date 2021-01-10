using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// A mask for note attributes.
    /// </summary>
    [Flags]
    public enum NoteMask : ushort
    {
        /// <summary>
        /// Empty mask.
        /// </summary>
        None = 0,

        /// <summary>
        /// The note is linked to the next note on the same string.
        /// </summary>
        LinkNext = 1 << 0,

        /// <summary>
        /// The note is accented.
        /// </summary>
        Accent = 1 << 1,

        /// <summary>
        /// The note is a hammer-on.
        /// </summary>
        HammerOn = 1 << 2,

        /// <summary>
        /// The note is a harmonic.
        /// </summary>
        Harmonic = 1 << 3,

        /// <summary>
        /// The note is ignored.
        /// </summary>
        Ignore = 1 << 4,

        /// <summary>
        /// The note is a fret-hand mute.
        /// </summary>
        FretHandMute = 1 << 5,

        /// <summary>
        /// The note is a palm-mute.
        /// </summary>
        PalmMute = 1 << 6,

        /// <summary>
        /// The note is a pull-off.
        /// </summary>
        PullOff = 1 << 7,

        /// <summary>
        /// The note is tremolo picked.
        /// </summary>
        Tremolo = 1 << 8,

        /// <summary>
        /// The note is a pinch harmonic.
        /// </summary>
        PinchHarmonic = 1 << 9,

        /// <summary>
        /// Unused.
        /// </summary>
        PickDirection = 1 << 10,

        /// <summary>
        /// The note is a slap.
        /// </summary>
        Slap = 1 << 11,

        /// <summary>
        /// The note is pluck/pop.
        /// </summary>
        Pluck = 1 << 12,

        /// <summary>
        /// The note is played with the right hand.
        /// </summary>
        RightHand = 1 << 13
    }

    /// <summary>
    /// Represents a note.
    /// </summary>
    public class Note : IXmlSerializable, IComparable<Note>, IHasTimeCode
    {
        /// <summary>
        /// Creates a new note.
        /// </summary>
        public Note() { }

        /// <summary>
        /// Creates a new note by copying the properties of another note.
        /// </summary>
        /// <param name="other">The note to copy.</param>
        public Note(Note other)
        {
            Mask = other.Mask;
            Time = other.Time;
            String = other.String;
            Fret = other.Fret;
            Sustain = other.Sustain;

            LeftHand = other.LeftHand;
            SlideTo = other.SlideTo;
            SlideUnpitchTo = other.SlideUnpitchTo;
            Tap = other.Tap;
            Vibrato = other.Vibrato;
            MaxBend = other.MaxBend;

            if (other.BendValues is not null)
                BendValues = new List<BendValue>(other.BendValues);
        }

        #region Quick Access Properties

        /// <summary>
        /// Gets or sets whether this note is linked to the next.
        /// </summary>
        public bool IsLinkNext
        {
            get => (Mask & NoteMask.LinkNext) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.LinkNext;
                else
                    Mask &= ~NoteMask.LinkNext;
            }
        }

        /// <summary>
        /// Gets or sets whether this note is accented.
        /// </summary>
        public bool IsAccent
        {
            get => (Mask & NoteMask.Accent) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Accent;
                else
                    Mask &= ~NoteMask.Accent;
            }
        }

        /// <summary>
        /// Gets or sets whether this note is a hammer-on.
        /// </summary>
        public bool IsHammerOn
        {
            get => (Mask & NoteMask.HammerOn) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.HammerOn;
                else
                    Mask &= ~NoteMask.HammerOn;
            }
        }

        /// <summary>
        /// Gets or sets whether this note is a harmonic.
        /// </summary>
        public bool IsHarmonic
        {
            get => (Mask & NoteMask.Harmonic) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Harmonic;
                else
                    Mask &= ~NoteMask.Harmonic;
            }
        }

        /// <summary>
        /// Gets or sets whether this note is a pinch harmonic.
        /// </summary>
        public bool IsPinchHarmonic
        {
            get => (Mask & NoteMask.PinchHarmonic) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.PinchHarmonic;
                else
                    Mask &= ~NoteMask.PinchHarmonic;
            }
        }

        /// <summary>
        /// Gets or sets whether this note is ignored.
        /// </summary>
        public bool IsIgnore
        {
            get => (Mask & NoteMask.Ignore) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Ignore;
                else
                    Mask &= ~NoteMask.Ignore;
            }
        }

        /// <summary>
        /// Gets or sets whether this note is a fret-hand mute.
        /// </summary>
        public bool IsFretHandMute
        {
            get => (Mask & NoteMask.FretHandMute) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.FretHandMute;
                else
                    Mask &= ~NoteMask.FretHandMute;
            }
        }

        /// <summary>
        /// Gets or sets whether this note is a palm mute.
        /// </summary>
        public bool IsPalmMute
        {
            get => (Mask & NoteMask.PalmMute) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.PalmMute;
                else
                    Mask &= ~NoteMask.PalmMute;
            }
        }

        /// <summary>
        /// Gets or sets whether this note is a pull-off.
        /// </summary>
        public bool IsPullOff
        {
            get => (Mask & NoteMask.PullOff) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.PullOff;
                else
                    Mask &= ~NoteMask.PullOff;
            }
        }

        /// <summary>
        /// Gets or sets whether this note is tremolo picked.
        /// </summary>
        public bool IsTremolo
        {
            get => (Mask & NoteMask.Tremolo) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Tremolo;
                else
                    Mask &= ~NoteMask.Tremolo;
            }
        }

        /// <summary>
        /// Gets or sets whether this note is a slap.
        /// </summary>
        public bool IsSlap
        {
            get => (Mask & NoteMask.Slap) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Slap;
                else
                    Mask &= ~NoteMask.Slap;
            }
        }

        /// <summary>
        /// Gets or sets whether this note is a pluck/pop.
        /// </summary>
        public bool IsPluck
        {
            get => (Mask & NoteMask.Pluck) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Pluck;
                else
                    Mask &= ~NoteMask.Pluck;
            }
        }

        /// <summary>
        /// Gets or sets whether this note is played with the right hand.
        /// </summary>
        public bool IsRightHand
        {
            get => (Mask & NoteMask.RightHand) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.RightHand;
                else
                    Mask &= ~NoteMask.RightHand;
            }
        }

        /// <summary>
        /// Gets whether this note is a vibrato.
        /// </summary>
        public bool IsVibrato => Vibrato != 0;

        /// <summary>
        /// Gets whether this note is a bend.
        /// </summary>
        public bool IsBend => BendValues?.Count > 0;

        /// <summary>
        /// Gets whether this note is a slide.
        /// </summary>
        public bool IsSlide => SlideTo != -1;

        /// <summary>
        /// Gets whether this note is an unpitched slide.
        /// </summary>
        public bool IsUnpitchedSlide => SlideUnpitchTo != -1;

        /// <summary>
        /// Gets whether this note is a tap.
        /// </summary>
        public bool IsTap => Tap != 0;

        /// <summary>
        /// Gets whether this note is a hammer-on or pull-off.
        /// </summary>
        public bool IsHopo
            => (Mask & NoteMask.HammerOn) != 0 || (Mask & NoteMask.PullOff) != 0;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the time code of the note.
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// Gets or sets the sustain of the note in milliseconds.
        /// </summary>
        public int Sustain { get; set; }

        /// <summary>
        /// Gets or sets the fret of the note.
        /// </summary>
        public sbyte Fret { get; set; }

        /// <summary>
        /// Gets or sets the left hand finger used for this note (for chord notes).
        /// </summary>
        public sbyte LeftHand { get; set; } = -1;

        /// <summary>
        /// Gets or sets the fret to slide to.
        /// </summary>
        public sbyte SlideTo { get; set; } = -1;

        /// <summary>
        /// Gets or sets the fret to unpitched slide to.
        /// </summary>
        public sbyte SlideUnpitchTo { get; set; } = -1;

        /// <summary>
        /// Gets or sets the string of the note.
        /// </summary>
        public sbyte String { get; set; }

        /// <summary>
        /// Gets or sets the finger to tap the note with.
        /// </summary>
        public sbyte Tap { get; set; }

        /// <summary>
        /// Gets or sets the vibrato strength of the note.
        ///
        /// <para>
        /// Values 40 (L), 80 (M) and 120 (S) are used, but they all look the same in-game.
        /// </para>
        /// </summary>
        public byte Vibrato { get; set; }

        /// <summary>
        /// Gets or sets the mask of the note.
        /// </summary>
        public NoteMask Mask { get; set; }

        /// <summary>
        /// Gets or sets the maximum bend value of the note.
        /// </summary>
        public float MaxBend { get; set; }

        /// <summary>
        /// A list of bend values of the note.
        /// </summary>
        public List<BendValue>? BendValues { get; set; }

        #endregion

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: Fret: {Fret}, String: {String}";

        public int CompareTo(Note other)
            => Time.CompareTo(other.Time);

        #region IXmlSerializable Implementation

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                switch (reader.Name)
                {
                    case "time":
                        Time = Utils.TimeCodeFromFloatString(reader.Value);
                        break;
                    case "sustain":
                        Sustain = Utils.TimeCodeFromFloatString(reader.Value);
                        break;
                    case "fret":
                        Fret = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "leftHand":
                        LeftHand = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "slideTo":
                        SlideTo = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "slideUnpitchTo":
                        SlideUnpitchTo = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "string":
                        String = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "vibrato":
                        Vibrato = byte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "tap":
                        Tap = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "bend":
                        MaxBend = float.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;

                    case "linkNext":
                        Mask |= (NoteMask)Utils.ParseBinary(reader.Value);
                        break;
                    case "accent":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 1);
                        break;
                    case "hammerOn":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 2);
                        break;
                    case "harmonic":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 3);
                        break;
                    case "ignore":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 4);
                        break;
                    case "mute":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 5);
                        break;
                    case "palmMute":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 6);
                        break;
                    case "pullOff":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 7);
                        break;
                    case "tremolo":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 8);
                        break;
                    case "harmonicPinch":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 9);
                        break;
                    case "pickDirection":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 10);
                        break;
                    case "slap":
                        if (sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo) != -1)
                            Mask |= NoteMask.Slap;
                        break;
                    case "pluck":
                        if (sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo) != -1)
                            Mask |= NoteMask.Pluck;
                        break;
                    case "rightHand":
                        if (sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo) != -1)
                            Mask |= NoteMask.RightHand;
                        break;
                }
            }

            reader.MoveToElement();

            if (!reader.IsEmptyElement && reader.ReadToDescendant("bendValues"))
            {
                BendValues = new List<BendValue>();
                Utils.DeserializeCountList(BendValues, reader);

                reader.ReadEndElement(); // </note>
            }
            else
            {
                reader.ReadStartElement();
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("string", String.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("fret", Fret.ToString(NumberFormatInfo.InvariantInfo));

            if (Sustain > 0)
                writer.WriteAttributeString("sustain", Utils.TimeCodeToString(Sustain));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("sustain", "0.000");

            if (IsLinkNext)
                writer.WriteAttributeString("linkNext", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("linkNext", "0");

            if (IsAccent)
                writer.WriteAttributeString("accent", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("accent", "0");

            if (IsBend)
                writer.WriteAttributeString("bend", MaxBend.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("bend", "0");

            if (IsHammerOn)
                writer.WriteAttributeString("hammerOn", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("hammerOn", "0");

            if (IsHarmonic)
                writer.WriteAttributeString("harmonic", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("harmonic", "0");

            if (IsHopo)
                writer.WriteAttributeString("hopo", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("hopo", "0");

            if (IsIgnore)
                writer.WriteAttributeString("ignore", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("ignore", "0");

            if (LeftHand != -1)
                writer.WriteAttributeString("leftHand", LeftHand.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("leftHand", "-1");

            if (IsFretHandMute)
                writer.WriteAttributeString("mute", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("mute", "0");

            if (IsPalmMute)
                writer.WriteAttributeString("palmMute", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("palmMute", "0");

            if (IsPluck)
                writer.WriteAttributeString("pluck", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("pluck", "-1");

            if (IsPullOff)
                writer.WriteAttributeString("pullOff", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("pullOff", "0");

            if (IsSlap)
                writer.WriteAttributeString("slap", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("slap", "-1");

            if (IsSlide)
                writer.WriteAttributeString("slideTo", SlideTo.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("slideTo", "-1");

            if (IsTremolo)
                writer.WriteAttributeString("tremolo", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("tremolo", "0");

            if (IsPinchHarmonic)
                writer.WriteAttributeString("harmonicPinch", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("harmonicPinch", "0");

            if ((Mask & NoteMask.PickDirection) != 0)
                writer.WriteAttributeString("pickDirection", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("pickDirection", "0");

            if (IsRightHand)
                writer.WriteAttributeString("rightHand", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("rightHand", "-1");

            if (IsUnpitchedSlide)
                writer.WriteAttributeString("slideUnpitchTo", SlideUnpitchTo.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("slideUnpitchTo", "-1");

            if (IsTap)
                writer.WriteAttributeString("tap", Tap.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("tap", "0");

            if (IsVibrato)
                writer.WriteAttributeString("vibrato", Vibrato.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("vibrato", "0");

            if (BendValues?.Count > 0)
                Utils.SerializeWithCount(BendValues, "bendValues", "bendValue", writer);
        }

        XmlSchema? IXmlSerializable.GetSchema() => null;

        #endregion
    }
}
