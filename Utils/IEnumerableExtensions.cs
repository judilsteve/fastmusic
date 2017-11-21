using System.Collections.Generic;
using System.Linq;

namespace fastmusic
{
    // TODO Another great candidate for unit tests
    /**
     * A set of useful extension methods for IEnumerable and some of its derived types
     */
    public static class IEnumerableExtensions
    {
        /**
         * @return A new IEnumerable that will allow traversal of part of @param sequence,
         * beginning at @param startIndex and ending up to @param sliceLength elements later
         * Slice will end earlier if @param sliceLength would take the slice beyond the end of @param sequence
         * Note that this method must start at the beginning of @param sequence and seek to @param startIndex
         */
        public static IEnumerable<T> GetSlice<T>(this IEnumerable<T> sequence, int startIndex, int sliceLength)
        {
            using( IEnumerator<T> enumerator = sequence.GetEnumerator() )
            {
                int i = -1; // Enumerator starts before the first element
                while(i < startIndex)
                {
                    i++;
                    enumerator.MoveNext();
                }
                return GetSlice(sequence, enumerator, sliceLength);
            }
        }

        /**
         * @see The other GetSlice extension for IEnumerable
         * @param currentPos is expected point to the first element in the slice
         */
        private static IEnumerable<T> GetSlice<T>(this IEnumerable<T> sequence, IEnumerator<T> currentPos, int sliceLength)
        {
            int i = 0;
            do
            {
                yield return currentPos.Current;
                i++;
            }
            while(i < sliceLength && currentPos.MoveNext());
        }

        /**
         * @see The GetSlice extension for IEnumerable
         * This version has List specific optimisations
         * (it does not have to start at the beginning of @param list and seek to @param startIndex)
         */
        public static IEnumerable<T> GetSlice<T>(this List<T> list, int startIndex, int sliceLength)
        {
            int i = startIndex;
            int stopIndex = startIndex + sliceLength;
            if(stopIndex > list.Count())
            {
                stopIndex = list.Count();
            }
            while(i < stopIndex)
            {
                yield return list[i];
                i++;
            }
        }

        /**
         * @return A sequence of IEnumerables that each allow the traversal of a part of @param sequence
         * Essentially, splits @param sequence into slices up to @param sliceLength long
         * The final slice may be shorter than @param sliceLength to avoid going beyond the end of @param sequence
         */
        public static IEnumerable<IEnumerable<T>> GetSlices<T>(this IEnumerable<T> sequence, int sliceLength)
        {
            using( IEnumerator<T> enumerator = sequence.GetEnumerator() )
            {
                while(enumerator.MoveNext())
                {
                    // Exploit the fact that GetSlice will seek enumerator to the start of the next slice
                    // to avoid creating a new enumerator and seeking all the way up for every slice
                    yield return GetSlice(sequence, enumerator, sliceLength);
                }
            }
        }

        /**
         * @see The GetSlices extension for IEnumerable
         * This version has List-specific optimisations
         */
        public static IEnumerable<IEnumerable<T>> GetSlices<T>(this List<T> list, int sliceLength)
        {
            int i = 0;
            while(i < list.Count())
            {
                yield return list.GetSlice(i, sliceLength);
                i += sliceLength;
            }
        }
    }
}