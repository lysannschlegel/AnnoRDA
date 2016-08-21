using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AnnoRDA.Tests.TestUtil
{
    public static class EnumerableExtensions
    {
        public static T SplitOffLast<T>(this IEnumerable<T> source, out IEnumerable<T> first)
        {
            T last;
            SplitOffLast(source, out first, out last);
            return last;
        }

        public static IEnumerable<T> SplitOffLast<T>(this IEnumerable<T> source, out IEnumerable<T> first, int n)
        {
            IEnumerable<T> last;
            SplitOffLast(source, out first, out last, n);
            return last;
        }

        public static void SplitOffLast<T>(this IEnumerable<T> source, out IEnumerable<T> first, out T last)
        {
            IEnumerable<T> lastColl;
            SplitOffLast(source, out first, out lastColl, 1);
            last = lastColl.FirstOrDefault();
        }

        public static void SplitOffLast<T>(this IEnumerable<T> source, out IEnumerable<T> first, out IEnumerable<T> last, int n)
        {
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            if (n < 0) {
                throw new ArgumentOutOfRangeException("n", "n must be >= 0");
            }

            Queue<T> firstQueue = new Queue<T>();
            Queue<T> lastQueue = new Queue<T>(n + 1);

            using (var e = source.GetEnumerator()) {
                while (e.MoveNext()) {
                    lastQueue.Enqueue(e.Current);
                    if (lastQueue.Count > n) {
                        firstQueue.Enqueue(lastQueue.Dequeue());
                    }
                }
            }

            first = firstQueue;
            last = lastQueue;
        }
    }

    public class EnumerableExtensionsTests
    {
        [Fact]
        public void TestSplitOffZeroFromEmpty()
        {
            IEnumerable<int> actualFirst;
            IEnumerable<int> actualLast = Enumerable.Empty<int>().SplitOffLast(out actualFirst, 0);
            Assert.False(actualFirst.Any());
            Assert.False(actualLast.Any());
        }

        [Fact]
        public void TestSplitOffZero()
        {
            int[] source = new int[] { 42, 16, 1 };
            IEnumerable<int> actualFirst;
            IEnumerable<int> actualLast = source.SplitOffLast(out actualFirst, 0);
            Assert.Equal(source.ToArray(), actualFirst.ToArray());
            Assert.False(actualLast.Any());
        }

        [Fact]
        public void TestSplitOffOne()
        {
            int[] source = new int[] { 42, 16, 1 };
            IEnumerable<int> actualFirst;
            IEnumerable<int> actualLast = source.SplitOffLast(out actualFirst, 1);
            Assert.Equal(source.Take(2).ToArray(), actualFirst.ToArray());
            Assert.Equal(source.Skip(2).ToArray(), actualLast.ToArray());
        }

        [Fact]
        public void TestSplitOffOneConvenience()
        {
            int[] source = new int[] { 42, 16, 1 };
            IEnumerable<int> actualFirst;
            int actualLast = source.SplitOffLast(out actualFirst);
            Assert.Equal(source.Take(2).ToArray(), actualFirst.ToArray());
            Assert.Equal(source.Last(), actualLast);
        }

        [Fact]
        public void TestSplitOffOneConvenienceFromEmpty()
        {
            IEnumerable<int> actualFirst;
            int actualLast = Enumerable.Empty<int>().SplitOffLast(out actualFirst);
            Assert.False(actualFirst.Any());
            Assert.Equal(0, actualLast);
        }

        [Fact]
        public void TestSplitOffMultiple()
        {
            int[] source = new int[] { 42, 16, 1, 88 };
            IEnumerable<int> actualFirst;
            IEnumerable<int> actualLast = source.SplitOffLast(out actualFirst, 3);
            Assert.Equal(source.Take(1).ToArray(), actualFirst.ToArray());
            Assert.Equal(source.Skip(1).ToArray(), actualLast.ToArray());
        }

        [Fact]
        public void TestSplitOffMultipleNotEnogh()
        {
            int[] source = new int[] { 42, 16, 1, 88 };
            IEnumerable<int> actualFirst;
            IEnumerable<int> actualLast = source.SplitOffLast(out actualFirst, 5);
            Assert.Equal(new int[] { }, actualFirst.ToArray());
            Assert.Equal(source, actualLast.ToArray());
        }
    }
}
