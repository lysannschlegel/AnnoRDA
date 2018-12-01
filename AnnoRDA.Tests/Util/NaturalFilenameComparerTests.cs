using Xunit;

namespace AnnoRDA.Tests.Util
{
    public class StringComparerTests
    {
        private System.Collections.Generic.IComparer<string> comparer = new AnnoRDA.Util.InvariantIndividualCharacterStringComparer();

        [Fact]
        public void TestSingleCharaterStringComparison()
        {
            Assert.Equal(this.comparer.Compare("a", "a") ,0);
            Assert.LessThanOrEqual(this.comparer.Compare("a", "b"), -1);
            Assert.GreaterThanOrEqual(this.comparer.Compare("b", "a"), 1);
        }

        [Fact]
        public void TestCaseSensitivityStringComparison()
        {
            Assert.Equal(this.comparer.Compare("A", "a"), 0);
        }

        [Fact]
        public void TestSpecialCharaterComparison()
        {
            Assert.LessThanOrEqual(this.comparer.Compare("1", "a"), -1);
            Assert.LessThanOrEqual(this.comparer.Compare(".", "a"), -1);
            Assert.LessThanOrEqual(this.comparer.Compare("-", "."), -1);
        }

        [Fact]
        public void TestMultiCharaterStringComparison()
        {
            Assert.Equal(this.comparer.Compare("aaa", "aaa"), 0);
            Assert.GreaterThanOrEqual(this.comparer.Compare("aaaa", "aaa"), 1);
            Assert.LessThanOrEqual(this.comparer.Compare("aaa", "aab"), -1);
            Assert.LessThanOrEqual(this.comparer.Compare("aaaa", "aab"), -1);
        }

        [Fact]
        public void TestFileNameWithNumberSuffixStringComparison()
        {
            Assert.LessThanOrEqual(this.comparer.Compare("arctic_global.xml", "arctic_global02.xml"), -1);
            Assert.LessThanOrEqual(this.comparer.Compare("orbit_test_andre.xml", "orbit_test_andre1.xml"), -1);
            Assert.LessThanOrEqual(this.comparer.Compare("sector_effects_heavy_rain.xml", "sector_effects_heavy_rain02.xml"), -1);
        }

        [Fact]
        public void TestFileNameWithUnderscoresStringComparison()
        {
            Assert.LessThanOrEqual(this.comparer.Compare("arctic_global02.xml", "arctic_global02_bak.xml"), -1);
            Assert.LessThanOrEqual(this.comparer.Compare("fpp_session01.xml", "fpp_session01_change_sunpower.xml"), -1);
        }

        [Fact]
        public void TestFileNameWithDashesStringComparison()
        {
            Assert.LessThanOrEqual(this.comparer.Compare("grid-light.js", "grid.js"), -1);
        }
    }

    public class NaturalFilenameComparerTests
    {
        private System.Collections.Generic.IComparer<string> comparer = new AnnoRDA.Util.NaturalFilenameStringComparer();

        [Fact]
        public void TestCaseSensitivityStringComparison()
        {
           Assert.Equal(0, this.comparer.Compare("A", "a"));
        }

        [Fact]
        public void TestFileNameWithNumbersStringComparison()
        {
            Assert.LessThanOrEqual(this.comparer.Compare("data1.rda", "data2.rda"), -1);
            Assert.LessThanOrEqual(this.comparer.Compare("data1.rda", "data10.rda"), -1);
            Assert.LessThanOrEqual(this.comparer.Compare("data2.rda", "data10.rda"), -1);
        }
    }
}
