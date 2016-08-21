using AnnoRDA.IO.Encryption;
using Xunit;

namespace AnnoRDA.Tests.IO.Encryption
{
    public class LinearCongruentialGeneratorTests
    {
        private LinearCongruentialGenerator lcg;

        public LinearCongruentialGeneratorTests()
        {
            this.lcg = new LinearCongruentialGenerator(seed: 0x71C71C71);
        }

        [Fact]
        public void TestGetCurrent()
        {
            this.lcg.MoveNext();
            Assert.Equal(0x63B2, this.lcg.Current);
        }

        [Fact]
        public void TestCurrentReturnsSameValue()
        {
            this.lcg.MoveNext();
            Assert.Equal(0x63B2, this.lcg.Current);
            Assert.Equal(0x63B2, this.lcg.Current);
        }

        [Fact]
        public void TestMoveNext()
        {
            this.lcg.MoveNext();
            Assert.Equal(0x63B2, this.lcg.Current);
            this.lcg.MoveNext();
            Assert.Equal(0x19F1, this.lcg.Current);
        }

        [Fact]
        public void TestReset()
        {
            this.lcg.MoveNext();
            Assert.Equal(0x63B2, this.lcg.Current);
            this.lcg.MoveNext();
            Assert.Equal(0x19F1, this.lcg.Current);

            this.lcg.Reset();

            this.lcg.MoveNext();
            Assert.Equal(0x63B2, this.lcg.Current);
            this.lcg.MoveNext();
            Assert.Equal(0x19F1, this.lcg.Current);
        }

        [Fact]
        public void TestCustomParams()
        {
            var lcg = new LinearCongruentialGenerator(seed: 73258, multiplier: 312988, increment: 92122);
            lcg.MoveNext();
            Assert.Equal(0x56AC, lcg.Current);
            lcg.MoveNext();
            Assert.Equal(0x2479, lcg.Current);
        }
    }
}
