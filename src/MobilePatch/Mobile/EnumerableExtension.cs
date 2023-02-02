using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#nullable enable
namespace System.Linq
{
    public static class EnumerableExtension
    {
        /// <summary>Produces a sequence of tuples with elements from the two specified sequences.</summary>
        /// <param name="first">The first sequence to merge.</param>
        /// <param name="second">The second sequence to merge.</param>
        /// <typeparam name="TFirst">The type of the elements of the first input sequence.</typeparam>
        /// <typeparam name="TSecond">The type of the elements of the second input sequence.</typeparam>
        /// <returns>A sequence of tuples with elements taken from the first and second sequences, in that order.</returns>
        public static IEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(
            this IEnumerable<TFirst> first,
            IEnumerable<TSecond> second)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");
            return ZipIterator<TFirst, TSecond>(first, second);
        }
#nullable disable
        private static IEnumerable<(TFirst First, TSecond Second)> ZipIterator<TFirst, TSecond>(
            IEnumerable<TFirst> first,
            IEnumerable<TSecond> second)
        {
            using (IEnumerator<TFirst> e1 = first.GetEnumerator())
            {
                using (IEnumerator<TSecond> e2 = second.GetEnumerator())
                {
                    while (e1.MoveNext() && e2.MoveNext())
                        yield return (e1.Current, e2.Current);
                }
            }
        }
    }
}
