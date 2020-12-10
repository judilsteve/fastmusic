using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fastmusic
{
    // TODO Another great candidate for unit tests
    /// <summary>
    /// A set of useful extension methods for IEnumerable and some of its derived types.
    /// </summary>
    public static class IEnumerableExtensions
    {
         /// <summary>
         /// Creates a new IEnumerable that will allow traversal of part of <paramref name="sequence"/>,
         /// beginning at <paramref name="startIndex"/> and ending up to <paramref name="sliceLength"/> elements later.
         /// Slice will end earlier if <paramref name="sliceLength"/> would take the slice beyond the end of <paramref name="sequence"/>.
         /// Note: this method must start at the beginning of <paramref name="sequence"/> and seek to <paramref name="startIndex"/>.
         /// </summary>
         /// <param name="sequence">A sequence of objects.</param>
         /// <param name="startIndex">Desired start of the slice.</param>
         /// <param name="sliceLength">Desired length of the slice.</param>
         /// <returns>A continuous sub-sequence of <paramref name="sequence"/>.</returns>
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
         /// See <see cref="GetSlice{T}(IEnumerable{T},int,int)"/>
         /// </summary>
         /// <param name="sequence">A sequence of objects.</param>
         /// <param name="currentPos">An enumerator already pointing to the first element in the slice.</param>
         /// <param name="sliceLength">Desired length of the slice.</param>
         /// <returns>A continuous sub-sequence of <paramref name="sequence"/>.</returns>
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
         /// See <see cref="GetSlice{T}(IEnumerable{T},int,int)"/>
         /// This version has List specific optimisations:
         /// It does not have to start at the beginning of <paramref name="list"/> and seek to <paramref name="startIndex"/>.
         /// </summary>
         /// <param name="list">A List of objects.</param>
         /// <param name="startIndex">Desired start of the slice.</param>
         /// <param name="sliceLength">Desired length of the slice.</param>
         /// <returns>A continuous sub-sequence of <paramref name="list"/>.</returns>
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
         /// Splits <paramref name="sequence"/> into slices up to <paramref name="sliceLength"/> long.
         /// The final slice may be shorter than <paramref name="sliceLength"/> to avoid going beyond the end of <paramref name="sequence"/>.
         /// </summary>
         /// <param name="sequence">A sequence of objects.</param>
         /// <param name="sliceLength">Desired length of each slice.</param>
         /// <returns>A sequence of IEnumerables that each allow the traversal of a part of <paramref name="sequence"/>.</returns>
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
         /// See <see cref="GetSlices{T}(IEnumerable{T},int)"/>
         /// This version has List-specific optimisations
         /// </summary>
         /// <param name="list">A list of objects.</param>
         /// <param name="sliceLength">Desired length of each slice.</param>
         /// <returns>A sequence of Lists that each allow the traversal of a part of <paramref name="list"/>.</returns>
        public static IEnumerable<IEnumerable<T>> GetSlices<T>(this List<T> list, int sliceLength)
        {
            int i = 0;
            while(i < list.Count())
            {
                yield return list.GetSlice(i, sliceLength);
                i += sliceLength;
            }
        }

        /// <summary>
        /// Returns true iff. <paramref name="sequence"/> is null or contains no elements
        /// </summary>
        /// <param name="sequence">Sequence to test</param>
        /// <typeparam name="T">Element type of <paramref name="sequence"/></typeparam>
        /// <returns>true iff. <paramref name="sequence"/> is null or contains no elements</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? sequence) =>
            sequence is null || !sequence.Any();

        /// <summary>
        /// Returns true iff. <paramref name="s"/> is null or contains no elements
        /// </summary>
        /// <param name="s">String to test</param>
        /// <returns>true iff. <paramref name="s"/> is null or contains no elements</returns>
        public static bool IsNullOrEmpty(this string? s) =>
            string.IsNullOrEmpty(s);

        /// <summary>
        /// Asynchronously turns the sequence into a HashSet of unique elements
        /// </summary>
        /// <param name="sequence">Sequence to condense</param>
        /// <typeparam name="T">Element Type</typeparam>
        /// <returns>HashSet of unique elements in the sequence</returns>
        public static async Task<HashSet<T>> ToHashSetAsync<T>(this IAsyncEnumerable<T> sequence)
        {
            var hashSet = new HashSet<T>();
            await foreach(var element in sequence)
            {
                hashSet.Add(element);
            }
            return hashSet;
        }

        /// <summary>
        /// Adds all elements in <paramref name="elements"/> to <oaramref name="hashSet"/>
        /// </summary>
        /// <param name="hashSet">Set to add elements to</param>
        /// <param name="elements">Elements to add</param>
        /// <typeparam name="T">Element type</typeparam>
        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> elements)
        {
            foreach(var element in elements)
            {
                hashSet.Add(element);
            }
        }
    }
}