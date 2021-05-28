using System;
using System.Collections.Generic;

namespace Rocksmith2014.XML.Extensions
{
    public static class GeneralExtensions
    {
        /// <summary>
        /// Returns true if the string contains the given substring (case ignored).
        /// </summary>
        /// <param name="this">The string to being tested.</param>
        /// <param name="substring">The substring to find.</param>
        public static bool IgnoreCaseContains(this string @this, string substring)
            => @this.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// Returns the index of the element at the time to find in a list ordered by time.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="elements"></param>
        /// <param name="timeToFind">The time for the element to find.</param>
        /// <returns>Index of the element, -1 if not found.</returns>
        public static int FindIndexByTime<T>(this IList<T> elements, int timeToFind)
            where T : IHasTimeCode
        {
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element.Time == timeToFind)
                    return i;
                else if (element.Time > timeToFind)
                    return -1;
            }

            return -1;
        }

        /// <summary>
        /// Finds the first element that has the given time from a list ordered by time.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="elements"></param>
        /// <param name="timeToFind">The time for the element to find.</param>
        /// <returns>The found element or null if not found.</returns>
        public static T? FindByTime<T>(this IList<T> elements, int timeToFind)
            where T : class, IHasTimeCode
        {
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element.Time == timeToFind)
                    return element;
                else if (element.Time > timeToFind)
                    return default;
            }

            return default;
        }

        /// <summary>
        /// Inserts an element into a list ordered by time.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="elements"></param>
        /// <param name="element">The element to insert.</param>
        public static void InsertByTime<T>(this List<T> elements, T element)
            where T : IHasTimeCode
        {
            int insertIndex = elements.FindIndex(hs => hs.Time > element.Time);
            if (insertIndex != -1)
                elements.Insert(insertIndex, element);
            else
                elements.Add(element);
        }
    }
}
