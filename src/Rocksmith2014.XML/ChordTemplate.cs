using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Represents a chord template.
    /// <para>
    /// Frets and fingers use storage indices: 0–5 are the stock strings, 6 is the low B.
    /// </para>
    /// </summary>
    public sealed class ChordTemplate : IXmlSerializable
    {
        private const string FingerPrefix = "finger";
        private const string FretPrefix = "fret";

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
            set { displayName = value; IsArpeggio = value.EndsWith("-arp", StringComparison.Ordinal); }
        }

        /// <summary>
        /// Returns true if the display name has a "-arp" suffix.
        /// </summary>
        public bool IsArpeggio { get; private set; }

        /// <summary>
        /// The fingering of the chord template, indexed by string. -1 means the string isnt used.
        /// </summary>
        public sbyte[] Fingers { get; private set; }

        /// <summary>
        /// The frets of the chord template, indexed by string. -1 means the string isnt used.
        /// </summary>
        public sbyte[] Frets { get; private set; }

        private string displayName = string.Empty;

        /// <summary>
        /// Gets the number of strings of the template.
        /// </summary>
        public int StringCount => Frets.Length;

        /// <summary>
        /// Creates a new six string chord template.
        /// </summary>
        public ChordTemplate() : this(StringLayout.StockStringCount) { }

        /// <summary>
        /// Creates a new empty chord template for the given string count.
        /// </summary>
        /// <param name="stringCount">The number of strings (6 or 7).</param>
        public ChordTemplate(int stringCount)
        {
            StringLayout.ValidateStringCount(stringCount);
            Fingers = CreateUnused(stringCount);
            Frets = CreateUnused(stringCount);
        }

        /// <summary>
        /// Creates a new chord template with the given properties.
        /// The string count is the length of the longer array, but at least six.
        /// </summary>
        /// <param name="chordName">The name of the chord.</param>
        /// <param name="displayName">The display name of the chord.</param>
        /// <param name="fingers">The fingering of the chord, indexed by string.</param>
        /// <param name="frets">The frets of the chord, indexed by string.</param>
        public ChordTemplate(string chordName, string displayName, sbyte[] fingers, sbyte[] frets)
        {
            if (fingers is null)
                throw new ArgumentNullException(nameof(fingers));
            if (frets is null)
                throw new ArgumentNullException(nameof(frets));

            var stringCount = Math.Max(StringLayout.StockStringCount, Math.Max(fingers.Length, frets.Length));
            StringLayout.ValidateStringCount(stringCount);

            Fingers = CreateUnused(stringCount);
            Frets = CreateUnused(stringCount);
            Array.Copy(fingers, Fingers, fingers.Length);
            Array.Copy(frets, Frets, frets.Length);

            Name = chordName;
            DisplayName = displayName;
        }

        /// <summary>
        /// Expands the template to the given string count. Added strings are unused (-1).
        /// Does nothing if the template already has at least that many strings.
        /// </summary>
        /// <param name="stringCount">The target string count (6 or 7).</param>
        public void EnsureStringCount(int stringCount)
        {
            StringLayout.ValidateStringCount(stringCount);

            if (stringCount <= StringCount)
                return;

            Fingers = Resize(Fingers, stringCount);
            Frets = Resize(Frets, stringCount);
        }

        /// <summary>
        /// Sets the fingering of the stock six strings of the chord template.
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
        /// Sets the frets of the stock six strings of the chord template.
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

            for (int i = 0; i < StringCount; i++)
            {
                if (Frets[i] != -1)
                    result += $"Fret{i}={Frets[i]} ";

                if (Fingers[i] != -1)
                    result += $"Finger{i}={Fingers[i]} ";
            }

            return result;
        }

        private static sbyte[] CreateUnused(int count)
        {
            var array = new sbyte[count];
            Array.Fill(array, (sbyte)-1);
            return array;
        }

        private static sbyte[] Resize(sbyte[] source, int count)
        {
            var result = CreateUnused(count);
            Array.Copy(source, result, source.Length);
            return result;
        }

        private static bool TryGetStringIndex(string attributeName, string prefix, out int index)
        {
            index = -1;

            if (attributeName.Length != prefix.Length + 1
                || !attributeName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var digit = attributeName[attributeName.Length - 1];
            if (digit < '0' || digit > '9')
                return false;

            index = digit - '0';
            return true;
        }

        private void EnsureIndex(int index)
        {
            if (index >= StringLayout.MaxStringCount)
            {
                throw new NotSupportedException(
                    $"Chord templates with more than {StringLayout.MaxStringCount} strings are not supported (found string index {index}).");
            }

            if (index >= StringCount)
                EnsureStringCount(index + 1);
        }

        private static void WriteValue(XmlWriter writer, string prefix, int index, sbyte value, bool alwaysWrite)
        {
            var name = prefix + index.ToString(NumberFormatInfo.InvariantInfo);

            if (value != -1)
                writer.WriteAttributeString(name, value.ToString(NumberFormatInfo.InvariantInfo));
            else if (alwaysWrite || !InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString(name, "-1");
        }

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);
                var name = reader.Name;

                if (name == "chordName")
                {
                    Name = reader.Value;
                }
                else if (name == "displayName")
                {
                    DisplayName = reader.Value;
                }
                else if (TryGetStringIndex(name, FingerPrefix, out int fingerIndex))
                {
                    EnsureIndex(fingerIndex);
                    Fingers[fingerIndex] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                }
                else if (TryGetStringIndex(name, FretPrefix, out int fretIndex))
                {
                    EnsureIndex(fretIndex);
                    Frets[fretIndex] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                }
            }

            reader.MoveToElement();

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("chordName", Name);
            writer.WriteAttributeString("displayName", DisplayName);

            // Stock strings: same attribute order and abridging rules as before.
            for (int i = 0; i < StringLayout.StockStringCount; i++)
                WriteValue(writer, FingerPrefix, i, Fingers[i], alwaysWrite: false);

            for (int i = 0; i < StringLayout.StockStringCount; i++)
                WriteValue(writer, FretPrefix, i, Frets[i], alwaysWrite: false);

            // Extended strings are always written so that the string count survives abridged XML.
            for (int i = StringLayout.StockStringCount; i < StringCount; i++)
            {
                WriteValue(writer, FingerPrefix, i, Fingers[i], alwaysWrite: true);
                WriteValue(writer, FretPrefix, i, Frets[i], alwaysWrite: true);
            }
        }

        #endregion
    }
}
