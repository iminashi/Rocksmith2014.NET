using System;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Describes string storage order for instrumental arrangements.
    /// Seven-string guitar stores its additional low B at index 6.
    /// <para>
    /// Storage indices 0–5 keep their stock Rocksmith meaning (low E … high E).
    /// Storage index 6 is the additional low B string of a seven-string guitar.
    /// Musical algorithms must use <see cref="LowToHigh"/> or <see cref="MusicalRank"/>
    /// instead of assuming that a lower storage index means a lower string.
    /// </para>
    /// </summary>
    public static class StringLayout
    {
        /// <summary>
        /// The number of strings in a stock Rocksmith 2014 guitar arrangement.
        /// </summary>
        public const int StockStringCount = 6;

        /// <summary>
        /// The maximum number of strings supported by the library.
        /// </summary>
        public const int MaxStringCount = 7;

        // E2 A2 D3 G3 B3 E4, then the extended low B1.
        private static readonly int[] StandardMidiNotes = [40, 45, 50, 55, 59, 64, 35];

        private static readonly int[] SixStringLowToHigh = [0, 1, 2, 3, 4, 5];
        private static readonly int[] SevenStringLowToHigh = [6, 0, 1, 2, 3, 4, 5];

        /// <summary>
        /// Returns true if the string count is supported (6 or 7).
        /// </summary>
        public static bool IsSupportedStringCount(int stringCount)
            => stringCount is StockStringCount or MaxStringCount;

        /// <summary>
        /// Throws if the string count is not supported.
        /// </summary>
        public static void ValidateStringCount(int stringCount)
        {
            if (!IsSupportedStringCount(stringCount))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stringCount),
                    stringCount,
                    $"Only {StockStringCount}-string and {MaxStringCount}-string arrangements are supported.");
            }
        }

        /// <summary>
        /// Returns the storage indices of the strings ordered from the lowest pitched string to the highest.
        /// </summary>
        public static int[] LowToHigh(int stringCount)
        {
            ValidateStringCount(stringCount);
            var order = stringCount == MaxStringCount ? SevenStringLowToHigh : SixStringLowToHigh;
            return (int[])order.Clone();
        }

        /// <summary>
        /// Returns the position of the string counted from the lowest pitched string (0 = lowest).
        /// </summary>
        public static int MusicalRank(int stringIndex, int stringCount)
        {
            ValidateStringCount(stringCount);

            if (stringIndex < 0 || stringIndex >= stringCount)
                throw new ArgumentOutOfRangeException(nameof(stringIndex), stringIndex, "String index is outside the string layout.");

            if (stringCount == MaxStringCount)
                return stringIndex == StockStringCount ? 0 : stringIndex + 1;

            return stringIndex;
        }

        /// <summary>
        /// Returns the MIDI note of the open string in E standard tuning (B1 for the seventh string).
        /// </summary>
        public static int StandardMidiNote(int stringIndex)
        {
            if (stringIndex < 0 || stringIndex >= StandardMidiNotes.Length)
                throw new ArgumentOutOfRangeException(nameof(stringIndex), stringIndex, "String index is outside the supported range.");

            return StandardMidiNotes[stringIndex];
        }

        /// <summary>
        /// Reorders values stored by string index into low-to-high musical order.
        /// The length of the input array defines the string count.
        /// </summary>
        public static T[] ToLowToHigh<T>(T[] valuesByStringIndex)
        {
            if (valuesByStringIndex is null)
                throw new ArgumentNullException(nameof(valuesByStringIndex));

            var order = LowToHigh(valuesByStringIndex.Length);
            var result = new T[order.Length];

            for (var i = 0; i < order.Length; i++)
                result[i] = valuesByStringIndex[order[i]];

            return result;
        }
    }
}
