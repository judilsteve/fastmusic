using System.Collections.Generic;
using System.Linq;

namespace fastmusic
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> GetSlice<T>(this IEnumerable<T> sequence, int startIndex, int sliceLength)
        {
            IEnumerator<T> enumerator = sequence.GetEnumerator();
            int i = -1; // Enumerator starts before the first element
            int stopIndex = startIndex + sliceLength;
            if(stopIndex > sequence.Count())
            {
                stopIndex = sequence.Count();
            }
            while(i < stopIndex)
            {
                if(i >= startIndex)
                {
                    yield return enumerator.Current;
                }
                i++;
                enumerator.MoveNext();
            }
        }

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

        public static IEnumerable<IEnumerable<T>> GetSlices<T>(this IEnumerable<T> sequence, int sliceLength)
        {
            int i = 0;
            while(i < sequence.Count())
            {
                yield return sequence.GetSlice(i, sliceLength);
                i += sliceLength;
            }
        }

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