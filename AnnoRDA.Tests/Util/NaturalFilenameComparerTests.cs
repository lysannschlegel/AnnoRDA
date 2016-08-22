using Xunit;

namespace AnnoRDA.Tests.Util
{
    public class NaturalFilenameComparerTests
    {
        [Fact]
        public void TestSingleCharaterStringComparison()
        {
            Assert.Equal(0, new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("a", "a"));
            Assert.LessThanOrEqual(new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("a", "b"), -1);
            Assert.GreaterThanOrEqual(new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("b", "a"), 1);

            Assert.Equal(0, new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("A", "a"));
            Assert.LessThanOrEqual(new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("1", "a"), -1);
            Assert.LessThanOrEqual(new AnnoRDA.Util.NaturalFilenameStringComparer().Compare(".", "a"), -1);
        }

        [Fact]
        public void TestMultiCharaterStringComparison()
        {
            Assert.Equal(0, new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("aaa", "aaa"));
            Assert.GreaterThanOrEqual(new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("aaaa", "aaa"), 1);
            Assert.LessThanOrEqual(new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("aaa", "aab"), -1);
            Assert.LessThanOrEqual(new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("aaaa", "aab"), -1);
        }
        
        [Fact]
        public void TestFileNameWithNumbersStringComparison()
        {
            Assert.LessThanOrEqual(new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("data1.rda", "data2.rda"), -1);
            Assert.LessThanOrEqual(new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("data1.rda", "data10.rda"), -1);
            Assert.LessThanOrEqual(new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("data2.rda", "data10.rda"), -1);
        }

        [Fact]
        public void TestFileNameWithUnderscoresStringComparison()
        {
            Assert.LessThanOrEqual(new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("arctic_global02.xml", "arctic_global02_bak.xml"), -1);
            Assert.LessThanOrEqual(new AnnoRDA.Util.NaturalFilenameStringComparer().Compare("fpp_session01.xml", "fpp_session01_change_sunpower.xml"), -1);
        }
    }
}
