using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents a chord template.
    /// </summary>
    public sealed class ChordTemplate : IXmlSerializable
    {
        /// <summary>
        /// Gets or sets the name of the chord.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the "display name" of the chord.
        /// <para>
        /// May contain suffixes "-arp" for arpeggios and "-nop" for double stop panels.
        /// </para>
        /// </summary>
        public string DisplayName
        {
            get => displayName;
            set { displayName = value; IsArpeggio = value.EndsWith("-arp"); }
        }

        /// <summary>
        /// Returns true if the display name has a "-arp" suffix.
        /// </summary>
        public bool IsArpeggio { get; private set; }

        /// <summary>
        /// The fingering of the chord template.
        /// </summary>
        public readonly sbyte[] Fingers = { -1, -1, -1, -1, -1, -1 };

        /// <summary>
        /// The frets of the chord template.
        /// </summary>
        public readonly sbyte[] Frets = { -1, -1, -1, -1, -1, -1 };

        private string displayName = string.Empty;

        /// <summary>
        /// Creates a new chord template.
        /// </summary>
        public ChordTemplate() { }

        /// <summary>
        /// Creates a new chord template with the given properties.
        /// </summary>
        /// <param name="chordName">The name of the chord.</param>
        /// <param name="displayName">The display name of the chord.</param>
        /// <param name="fingers">The fingering of the chord.</param>
        /// <param name="frets">The frets of the chord.</param>
        public ChordTemplate(string chordName, string displayName, sbyte[] fingers, sbyte[] frets)
        {
            Name = chordName;
            DisplayName = displayName;
            Array.Copy(frets, Frets, 6);
            Array.Copy(fingers, Fingers, 6);
        }

        /// <summary>
        /// Sets the fingering of the chord template.
        /// </summary>
        /// <param name="lowE">The low E string in standard tuning.</param>
        /// <param name="A">The A string in standard tuning.</param>
        /// <param name="D">The D string in standard tuning.</param>
        /// <param name="G">The G string in standard tuning.</param>
        /// <param name="B">The B string in standard tuning.</param>
        /// <param name="highE">The high E string in standard tuning.</param>
        public void SetFingering(sbyte lowE, sbyte A, sbyte D, sbyte G, sbyte B, sbyte highE)
        {
            Fingers[0] = lowE;
            Fingers[1] = A;
            Fingers[2] = D;
            Fingers[3] = G;
            Fingers[4] = B;
            Fingers[5] = highE;
        }

        /// <summary>
        /// Sets the frets of the chord template.
        /// </summary>
        /// <param name="lowE">The low E string in standard tuning.</param>
        /// <param name="A">The A string in standard tuning.</param>
        /// <param name="D">The D string in standard tuning.</param>
        /// <param name="G">The G string in standard tuning.</param>
        /// <param name="B">The B string in standard tuning.</param>
        /// <param name="highE">The high E string in standard tuning.</param>
        public void SetFrets(sbyte lowE, sbyte A, sbyte D, sbyte G, sbyte B, sbyte highE)
        {
            Frets[0] = lowE;
            Frets[1] = A;
            Frets[2] = D;
            Frets[3] = G;
            Frets[4] = B;
            Frets[5] = highE;
        }

        public override string ToString()
        {
            string result = $"Name: \"{Name}\", Display Name: \"{DisplayName}\", ";

            for (int i = 0; i < 6; i++)
            {
                if (Frets[i] != -1)
                    result += $"Fret{i}={Frets[i]} ";

                if (Fingers[i] != -1)
                    result += $"Finger{i}={Fingers[i]} ";
            }

            return result;
        }

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                switch (reader.Name)
                {
                    case "chordName":
                        Name = reader.Value;
                        break;
                    case "displayName":
                        DisplayName = reader.Value;
                        break;
                    case "finger0":
                        Fingers[0] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "finger1":
                        Fingers[1] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "finger2":
                        Fingers[2] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "finger3":
                        Fingers[3] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "finger4":
                        Fingers[4] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "finger5":
                        Fingers[5] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;

                    case "fret0":
                        Frets[0] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "fret1":
                        Frets[1] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "fret2":
                        Frets[2] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "fret3":
                        Frets[3] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "fret4":
                        Frets[4] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "fret5":
                        Frets[5] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                }
            }

            reader.MoveToElement();

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("chordName", Name);
            writer.WriteAttributeString("displayName", DisplayName);

            if (Fingers[0] != -1)
                writer.WriteAttributeString("finger0", Fingers[0].ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("finger0", "-1");

            if (Fingers[1] != -1)
                writer.WriteAttributeString("finger1", Fingers[1].ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("finger1", "-1");

            if (Fingers[2] != -1)
                writer.WriteAttributeString("finger2", Fingers[2].ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("finger2", "-1");

            if (Fingers[3] != -1)
                writer.WriteAttributeString("finger3", Fingers[3].ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("finger3", "-1");

            if (Fingers[4] != -1)
                writer.WriteAttributeString("finger4", Fingers[4].ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("finger4", "-1");

            if (Fingers[5] != -1)
                writer.WriteAttributeString("finger5", Fingers[5].ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("finger5", "-1");

            if (Frets[0] != -1)
                writer.WriteAttributeString("fret0", Frets[0].ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("fret0", "-1");

            if (Frets[1] != -1)
                writer.WriteAttributeString("fret1", Frets[1].ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("fret1", "-1");

            if (Frets[2] != -1)
                writer.WriteAttributeString("fret2", Frets[2].ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("fret2", "-1");

            if (Frets[3] != -1)
                writer.WriteAttributeString("fret3", Frets[3].ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("fret3", "-1");

            if (Frets[4] != -1)
                writer.WriteAttributeString("fret4", Frets[4].ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("fret4", "-1");

            if (Frets[5] != -1)
                writer.WriteAttributeString("fret5", Frets[5].ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("fret5", "-1");
        }

        #endregion
    }
}
