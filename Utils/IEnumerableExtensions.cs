using System.Collections.Generic;
using System.Linq;

namespace fastmusic
{
    // TODO Another great candidate for unit tests
    // TODO Why doesn't this class throw warnings for missing XMLDoc
    /// <summary>
    /// A set of useful extension methods for IEnumerable and some of its derived types.
    /// </summary>
    public static class IEnumerableExtensions
    {
         /// <summary>
         /// Creates a new IEnumerable that will allow traversal of part of @param sequence,
         /// beginning at @param startIndex and ending up to @param sliceLength elements later.
         /// Slice will end earlier if @param sliceLength would take the slice beyond the end of @param sequence.
         /// @note this method must start at the beginning of @param sequence and seek to @param startIndex.
         /// </summary>
         /// <param name="sequence">A sequence of objects.</param>
         /// <param name="startIndex">Desired start of the slice.</param>
         /// <param name="sliceLength">Desired length of the slice.</param>
         /// <returns>A continuous sub-sequence of @param sequence.</returns>
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

         /// <summary>
         /// @see The other GetSlice extension for IEnumerable.
         /// </summary>
         /// <param name="sequence">A sequence of objects.</param>
         /// <param name="currentPos">An enumerator already pointing to the first element in the slice.</param>
         /// <param name="sliceLength">Desired length of the slice.</param>
         /// <returns>A continuous sub-sequence of @param sequence.</returns>
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

         /// <summary>
         /// @see The GetSlice extension for IEnumerable.
         /// This version has List specific optimisations:
         /// It does not have to start at the beginning of @param list and seek to @param startIndex.
         /// </summary>
         /// <param name="list">A List of objects.</param>
         /// <param name="startIndex">Desired start of the slice.</param>
         /// <param name="sliceLength">Desired length of the slice.</param>
         /// <returns>A continuous sub-sequence of @param sequence.</returns>
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

         /// <summary>
         /// Splits @param sequence into slices up to @param sliceLength long.
         /// The final slice may be shorter than @param sliceLength to avoid going beyond the end of @param sequence.
         /// </summary>
         /// <param name="sequence">A sequence of objects.</param>
         /// <param name="sliceLength">Desired length of each slice.</param>
         /// <returns>A sequence of IEnumerables that each allow the traversal of a part of @param sequence.</returns>
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

         /// <summary>
         /// @see The GetSlices extension for IEnumerable
         /// This version has List-specific optimisations
         /// </summary>
         /// <param name="list">A list of objects.</param>
         /// <param name="sliceLength">Desired length of each slice.</param>
         /// <returns>A sequence of Lists that each allow the traversal of a part of @param list.</returns>
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