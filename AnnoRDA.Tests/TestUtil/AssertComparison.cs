using System;

namespace Xunit
{
    public partial class Assert
    {
        public static void LessThan<T>(IComparable<T> a, T b)
        {
            int result = a.CompareTo(b);
            if (result >= 0) {
                throw new Xunit.Sdk.AssertActualExpectedException(
                    String.Format("< {0}", b),
                    a,
                    "Assert.LessThan() Failure"
                );
            }
        }
        public static void LessThanOrEqual<T>(IComparable<T> a, T b)
        {
            int result = a.CompareTo(b);
            if (result > 0) {
                throw new Xunit.Sdk.AssertActualExpectedException(
                    String.Format("<= {0}", b),
                    a,
                    "Assert.LessThanOrEqual() Failure"
                );
            }
        }
        public static void GreaterThan<T>(IComparable<T> a, T b)
        {
            int result = a.CompareTo(b);
            if (result <= 0) {
                throw new Xunit.Sdk.AssertActualExpectedException(
                    String.Format("> {0}", b),
                    a,
                    "Assert.GreaterThan() Failure"
                );
            }
        }
        public static void GreaterThanOrEqual<T>(IComparable<T> a, T b)
        {
            int result = a.CompareTo(b);
            if (result < 0) {
                throw new Xunit.Sdk.AssertActualExpectedException(
                    String.Format(">= {0}", b),
                    a,
                    "Assert.GreaterThanOrEqual() Failure"
                );
            }
        }
    }
}
