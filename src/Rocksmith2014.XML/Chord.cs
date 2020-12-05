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
    /// A mask for chord attributes.
    /// </summary>
    [Flags]
    public enum ChordMask : byte
    {
        /// <summary>
        /// Empty mask.
        /// </summary>
        None = 0,

        /// <summary>
        /// The chord contains at least one note that has link next.
        /// </summary>
        LinkNext = 1 << 0,

        /// <summary>
        /// The chord is accented.
        /// </summary>
        Accent = 1 << 1,

        /// <summary>
        /// The chord is fret-hand muted.
        /// </summary>
        FretHandMute = 1 << 2,

        /// <summary>
        /// Leftover from RS1. Has no purpose.
        /// </summary>
        HighDensity = 1 << 3,

        /// <summary>
        /// The chord is ignored.
        /// </summary>
        Ignore = 1 << 4,

        /// <summary>
        /// The chord is palm-muted.
        /// </summary>
        PalmMute = 1 << 5,

        /// <summary>
        /// The chord is a hammer-on or pull-off. Unused.
        /// </summary>
        Hopo = 1 << 6
    }

    /// <summary>
    /// Represents a chord.
    /// </summary>
    public class Chord : IXmlSerializable, IComparable<Chord>, IHasTimeCode
    {
        #region Quick Access Properties

        /// <summary>
        /// Gets or sets whether the chord is link next.
        /// </summary>
        public bool IsLinkNext
        {
            get => (Mask & ChordMask.LinkNext) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.LinkNext;
                else
                    Mask &= ~ChordMask.LinkNext;
            }
        }

        /// <summary>
        /// Gets or sets whether the chord is accented.
        /// </summary>
        public bool IsAccent
        {
            get => (Mask & ChordMask.Accent) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.Accent;
                else
                    Mask &= ~ChordMask.Accent;
            }
        }

        /// <summary>
        /// Gets or sets whether the chord is fret-hand muted.
        /// </summary>
        public bool IsFretHandMute
        {
            get => (Mask & ChordMask.FretHandMute) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.FretHandMute;
                else
                    Mask &= ~ChordMask.FretHandMute;
            }
        }

        /// <summary>
        /// Gets or sets whether the chord is high density.
        /// </summary>
        public bool IsHighDensity
        {
            get => (Mask & ChordMask.HighDensity) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.HighDensity;
                else
                    Mask &= ~ChordMask.HighDensity;
            }
        }

        /// <summary>
        /// Gets or sets whether the chord ignored.
        /// </summary>
        public bool IsIgnore
        {
            get => (Mask & ChordMask.Ignore) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.Ignore;
                else
                    Mask &= ~ChordMask.Ignore;
            }
        }

        /// <summary>
        /// Gets or sets whether the chord is palm-muted.
        /// </summary>
        public bool IsPalmMute
        {
            get => (Mask & ChordMask.PalmMute) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.PalmMute;
                else
                    Mask &= ~ChordMask.PalmMute;
            }
        }

        /// <summary>
        /// Gets or sets whether the chord is hammer-on or pull-off.
        /// </summary>
        public bool IsHopo
        {
            get => (Mask & ChordMask.Hopo) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.Hopo;
                else
                    Mask &= ~ChordMask.Hopo;
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the mask of the chord.
        /// </summary>
        public ChordMask Mask { get; set; }

        /// <summary>
        /// Gets or sets the time code of the chord.
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// Gets or sets chord ID.
        /// </summary>
        public short ChordId { get; set; }

        /// <summary>
        /// A list of notes in the chord.
        /// </summary>
        public List<Note>? ChordNotes { get; set; }

        /// <summary>
        /// Creates a new chord.
        /// </summary>
        public Chord() { }

        /// <summary>
        /// Creates a new chord by copying the properties of another chord.
        /// </summary>
        /// <param name="other">The chord to copy.</param>
        public Chord(Chord other)
        {
            Mask = other.Mask;
            Time = other.Time;
            ChordId = other.ChordId;
            if (other.ChordNotes is not null)
            {
                ChordNotes = new List<Note>(other.ChordNotes.Count);
                ChordNotes.AddRange(other.ChordNotes.Select(cn => new Note(cn)));
            }
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: Id: {ChordId}";

        public int CompareTo(Chord other)
            => Time.CompareTo(other.Time);

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader.HasAttributes)
            {
                for (int i = 0; i < reader.AttributeCount; i++)
                {
                    reader.MoveToAttribute(i);

                    switch (reader.Name)
                    {
                        case "time":
                            Time = Utils.TimeCodeFromFloatString(reader.Value);
                            break;
                        case "chordId":
                            ChordId = short.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                            break;
                        case "linkNext":
                            Mask |= (ChordMask)Utils.ParseBinary(reader.Value);
                            break;
                        case "accent":
                            Mask |= (ChordMask)(Utils.ParseBinary(reader.Value) << 1);
                            break;
                        case "fretHandMute":
                            Mask |= (ChordMask)(Utils.ParseBinary(reader.Value) << 2);
                            break;
                        case "highDensity":
                            Mask |= (ChordMask)(Utils.ParseBinary(reader.Value) << 3);
                            break;
                        case "ignore":
                            Mask |= (ChordMask)(Utils.ParseBinary(reader.Value) << 4);
                            break;
                        case "palmMute":
                            Mask |= (ChordMask)(Utils.ParseBinary(reader.Value) << 5);
                            break;
                        case "hopo":
                            Mask |= (ChordMask)(Utils.ParseBinary(reader.Value) << 6);
                            break;
                    }
                }

                reader.MoveToElement();
            }

            if (!reader.IsEmptyElement && reader.ReadToDescendant("chordNote"))
            {
                ChordNotes = new List<Note>();

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    Note cn = new Note();
                    ((IXmlSerializable)cn).ReadXml(reader);
                    ChordNotes.Add(cn);
                }

                reader.ReadEndElement();
            }
            else
            {
                reader.ReadStartElement();
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("chordId", ChordId.ToString(NumberFormatInfo.InvariantInfo));

            if (IsLinkNext)
                writer.WriteAttributeString("linkNext", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("linkNext", "0");

            if (IsAccent)
                writer.WriteAttributeString("accent", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("accent", "0");

            if (IsFretHandMute)
                writer.WriteAttributeString("fretHandMute", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("fretHandMute", "0");

            if (IsHighDensity)
                writer.WriteAttributeString("highDensity", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("highDensity", "0");

            if (IsIgnore)
                writer.WriteAttributeString("ignore", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("ignore", "0");

            if (IsPalmMute)
                writer.WriteAttributeString("palmMute", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("palmMute", "0");

            if (IsHopo)
                writer.WriteAttributeString("hopo", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("hopo", "0");

            if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("strum", "down");

            if (ChordNotes?.Count > 0)
            {
                foreach (var chordNote in ChordNotes)
                {
                    writer.WriteStartElement("chordNote");
                    ((IXmlSerializable)chordNote).WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
        }

        #endregion
    }
}
