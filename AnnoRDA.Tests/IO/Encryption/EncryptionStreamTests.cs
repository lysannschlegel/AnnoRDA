using AnnoRDA.IO.Encryption;
using AnnoRDA.Tests.TestUtil;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AnnoRDA.Tests.IO.Encryption
{
    public class EncryptionStreamReadTests
    {
        private byte[] origBytes      = new byte[] { 0x0F, 0x58, 0x09, 0x11, 0x2A, 0xFF, 0x01, 0x11 };
        //             keys           =              0xB2, 0x63, 0xF1, 0x19, 0x21, 0x39, 0xF7, 0x23
        private byte[] expectedCipher = new byte[] { 0xBD, 0x3B, 0xF8, 0x08, 0x0B, 0xC6, 0xF6, 0x32 };

        public EncryptionStreamReadTests()
        {
            Assert.Equal(this.origBytes.Length, this.expectedCipher.Length);
        }

        [Fact]
        public void TestReadMode()
        {
            using (var encryptionStream = new EncryptionStream(TestData.GetStream(this.origBytes), AnnoRDA.IO.StreamAccessMode.Read)) {
                Assert.False(encryptionStream.CanWrite);
                Assert.True(encryptionStream.CanRead);

                var buffer = new byte[this.origBytes.Length];
                Assert.Throws<NotSupportedException>(() => encryptionStream.Write(buffer, 0, buffer.Length));
                encryptionStream.Read(buffer, 0, buffer.Length);
            }
        }

        [Fact]
        public void TestReadEvenNumberOfBytesAllAtOnce()
        {
            Assert.Equal(0, this.origBytes.Length % 2);

            using (var encryptionStream = new EncryptionStream(TestData.GetStream(this.origBytes), AnnoRDA.IO.StreamAccessMode.Read)) {
                var actualCipher = new byte[this.origBytes.Length];
                Assert.Equal(actualCipher.Length, encryptionStream.Read(actualCipher, 0, actualCipher.Length));
                Assert.Equal(this.expectedCipher, actualCipher);

                Assert.Equal(0, encryptionStream.Read(actualCipher, 0, actualCipher.Length)); // EOF
            }
        }

        [Fact]
        public void TestReadEvenNumberOfBytesPartially()
        {
            Assert.True(this.origBytes.Length > 4);
            Assert.Equal(0, this.origBytes.Length % 2);

            using (var encryptionStream = new EncryptionStream(TestData.GetStream(this.origBytes), AnnoRDA.IO.StreamAccessMode.Read)) {
                var actualCipher = new byte[this.origBytes.Length - 4];
                Assert.Equal(actualCipher.Length, encryptionStream.Read(actualCipher, 0, actualCipher.Length));
                var expectedPartial = this.expectedCipher.Take(this.origBytes.Length - 4).ToArray();
                Assert.Equal(expectedPartial, actualCipher);

                actualCipher = new byte[4];
                Assert.Equal(actualCipher.Length, encryptionStream.Read(actualCipher, 0, actualCipher.Length));
                expectedPartial = this.expectedCipher.Skip(this.origBytes.Length - 4).ToArray();
                Assert.Equal(expectedPartial, actualCipher);

                Assert.Equal(0, encryptionStream.Read(actualCipher, 0, actualCipher.Length)); // EOF
            }
        }

        [Fact]
        public void TestReadOddNumberOfBytesAllAtOnce()
        {
            Assert.Equal(0, this.origBytes.Length % 2);
            Assert.True(this.origBytes.Length > 2);

            // drop last byte to get odd number of bytes
            this.origBytes = this.origBytes.Take(this.origBytes.Length - 1).ToArray();
            this.expectedCipher = this.expectedCipher.Take(this.expectedCipher.Length - 1).ToArray();
            // now the last byte should not be encrypted
            this.expectedCipher[this.expectedCipher.Length - 1] = this.origBytes[this.origBytes.Length - 1];

            Assert.Equal(1, this.origBytes.Length % 2);

            using (var encryptionStream = new EncryptionStream(TestData.GetStream(this.origBytes), AnnoRDA.IO.StreamAccessMode.Read)) {
                // we'll need two passes to read all data
                var actualCipher = new byte[this.origBytes.Length];
                Assert.Equal(actualCipher.Length - 1, encryptionStream.Read(actualCipher, 0, actualCipher.Length));
                Assert.Equal(1, encryptionStream.Read(actualCipher, actualCipher.Length - 1, 1));
                Assert.Equal(this.expectedCipher, actualCipher);

                Assert.Equal(0, encryptionStream.Read(actualCipher, 0, actualCipher.Length)); // EOF
            }
        }

        [Fact]
        public void TestReadOddNumberOfBytesPartially()
        {
            Assert.True(this.origBytes.Length > 3);
            Assert.Equal(0, this.origBytes.Length % 2);

            using (var encryptionStream = new EncryptionStream(TestData.GetStream(this.origBytes), AnnoRDA.IO.StreamAccessMode.Read)) {
                var actualCipher = new byte[this.origBytes.Length - 3];
                Assert.Equal(actualCipher.Length, encryptionStream.Read(actualCipher, 0, actualCipher.Length));
                var expectedPartial = this.expectedCipher.Take(this.origBytes.Length - 3).ToArray();
                Assert.Equal(expectedPartial, actualCipher);

                actualCipher = new byte[3];
                Assert.Equal(actualCipher.Length, encryptionStream.Read(actualCipher, 0, actualCipher.Length));
                expectedPartial = this.expectedCipher.Skip(this.origBytes.Length - 3).ToArray();
                Assert.Equal(expectedPartial, actualCipher);

                Assert.Equal(0, encryptionStream.Read(actualCipher, 0, actualCipher.Length)); // EOF
            }
        }

        [Fact]
        public void TestReadOneByOne()
        {
            using (var encryptionStream = new EncryptionStream(TestData.GetStream(this.origBytes), AnnoRDA.IO.StreamAccessMode.Read)) {
                var actualCipher = new byte[1];
                foreach (var expectedByte in this.expectedCipher) {
                    Assert.Equal(1, encryptionStream.Read(actualCipher, 0, 1));
                    Assert.Equal(new byte[] { expectedByte }, actualCipher);
                }
                Assert.Equal(0, encryptionStream.Read(actualCipher, 0, actualCipher.Length)); // EOF
            }
        }

        [Fact]
        public void TestReadMoreBytesThanAvailable()
        {
            using (var encryptionStream = new EncryptionStream(TestData.GetStream(this.origBytes), AnnoRDA.IO.StreamAccessMode.Read)) {
                var actualCipher = new byte[this.origBytes.Length + 3];
                Assert.Equal(this.origBytes.Length, encryptionStream.Read(actualCipher, 0, actualCipher.Length));
                Assert.Equal(this.expectedCipher, actualCipher.Take(this.origBytes.Length).ToArray());

                Assert.Equal(0, encryptionStream.Read(actualCipher, 0, actualCipher.Length)); // EOF
            }
        }

        [Fact]
        public void TestReadWithAFewRealBytes()
        {
            this.origBytes      = new byte[] { 0xD6, 0x63, 0x90, 0x19, 0x55, 0x39, 0x96, 0x23 };
            this.expectedCipher = new byte[] { 0x64, 0x00, 0x61, 0x00, 0x74, 0x00, 0x61, 0x00 };

            using (var encryptionStream = new EncryptionStream(TestData.GetStream(this.origBytes), AnnoRDA.IO.StreamAccessMode.Read)) {
                var actualCipher = new byte[this.origBytes.Length];
                encryptionStream.Read(actualCipher, 0, actualCipher.Length);
                Assert.Equal(this.expectedCipher, actualCipher);
            }
        }
    }

    public class EncryptionStreamWriteTests
    {
        private byte[] origBytes      = new byte[] { 0x0F, 0x58, 0x09, 0x11, 0x2A, 0xFF, 0x01, 0x11 };
        //             keys           =              0xB2, 0x63, 0xF1, 0x19, 0x21, 0x39, 0xF7, 0x23
        private byte[] expectedCipher = new byte[] { 0xBD, 0x3B, 0xF8, 0x08, 0x0B, 0xC6, 0xF6, 0x32 };

        public EncryptionStreamWriteTests()
        {
            Assert.Equal(this.origBytes.Length, this.expectedCipher.Length);
        }


        /// <summary>
        /// Assert that the contents of the actual stream up to the current position match the contents
        /// of the expected array. actual's position will be modified in order to read from the stream,
        /// but will be restored afterwards.
        /// </summary>
        private void AssertStreamContentsEqual(byte[] expected, Stream actual)
        {
            long previousPosition = actual.Position;
            try {
                actual.Position = 0;

                byte[] actualBuffer = new byte[previousPosition];
                actual.Read(actualBuffer, 0, (int)previousPosition);

                Assert.Equal(expected, actualBuffer);

            } finally {
                actual.Position = previousPosition;
            }
        }


        [Fact]
        public void TestWriteMode()
        {
            using (var encryptionStream = new EncryptionStream(TestData.GetStream(this.origBytes), AnnoRDA.IO.StreamAccessMode.Write)) {
                Assert.False(encryptionStream.CanRead);
                Assert.True(encryptionStream.CanWrite);

                var buffer = new byte[this.origBytes.Length];
                Assert.Throws<NotSupportedException>(() => encryptionStream.Read(buffer, 0, buffer.Length));
                encryptionStream.Write(buffer, 0, buffer.Length);
            }
        }

        [Fact]
        public void TestWriteEvenNumberOfBytesAllAtOnce()
        {
            Assert.Equal(0, this.origBytes.Length % 2);

            using (var baseStream = TestData.GetStream(new byte[this.origBytes.Length])) {
                using (var encryptionStream = new EncryptionStream(baseStream, AnnoRDA.IO.StreamAccessMode.Write, leaveOpen: true)) {
                    encryptionStream.Write(this.origBytes, 0, this.origBytes.Length);
                }
                AssertStreamContentsEqual(this.expectedCipher, baseStream);
            }
        }

        [Fact]
        public void TestWriteEvenNumberOfBytesPartially()
        {
            Assert.True(this.origBytes.Length > 4);
            Assert.Equal(0, this.origBytes.Length % 2);

            using (var baseStream = TestData.GetStream(new byte[this.origBytes.Length])) {
                using (var encryptionStream = new EncryptionStream(baseStream, AnnoRDA.IO.StreamAccessMode.Write, leaveOpen: true)) {
                    encryptionStream.Write(this.origBytes.Take(this.origBytes.Length - 4).ToArray(), 0, this.origBytes.Length - 4);
                    encryptionStream.Flush();
                    AssertStreamContentsEqual(this.expectedCipher.Take(this.origBytes.Length - 4).ToArray(), baseStream);

                    encryptionStream.Write(this.origBytes.Skip(this.origBytes.Length - 4).ToArray(), 0, 4);
                }
                AssertStreamContentsEqual(this.expectedCipher, baseStream);
            }
        }

        [Fact]
        public void TestWriteOddNumberOfBytesAllAtOnce()
        {
            Assert.Equal(0, this.origBytes.Length % 2);
            Assert.True(this.origBytes.Length > 2);

            // drop last byte to get odd number of bytes
            this.origBytes = this.origBytes.Take(this.origBytes.Length - 1).ToArray();
            this.expectedCipher = this.expectedCipher.Take(this.expectedCipher.Length - 1).ToArray();
            // now the last byte should not be encrypted
            this.expectedCipher[this.expectedCipher.Length - 1] = this.origBytes[this.origBytes.Length - 1];

            Assert.Equal(1, this.origBytes.Length % 2);

            using (var baseStream = TestData.GetStream(new byte[this.origBytes.Length])) {
                using (var encryptionStream = new EncryptionStream(baseStream, AnnoRDA.IO.StreamAccessMode.Write, leaveOpen: true)) {
                    encryptionStream.Write(this.origBytes, 0, this.origBytes.Length);
                }
                AssertStreamContentsEqual(this.expectedCipher, baseStream);
            }
        }

        [Fact]
        public void TestWriteOddNumberOfBytesPartially()
        {
            Assert.True(this.origBytes.Length > 3);
            Assert.Equal(0, this.origBytes.Length % 2);

            using (var baseStream = TestData.GetStream(new byte[this.origBytes.Length])) {
                using (var encryptionStream = new EncryptionStream(baseStream, AnnoRDA.IO.StreamAccessMode.Write, leaveOpen: true)) {
                    encryptionStream.Write(this.origBytes.Take(this.origBytes.Length - 3).ToArray(), 0, this.origBytes.Length - 3);
                    encryptionStream.Flush();

                    // There is one unprocessed byte buffered
                    AssertStreamContentsEqual(this.expectedCipher.Take(this.origBytes.Length - 3 - 1).ToArray(), baseStream);

                    encryptionStream.Write(this.origBytes.Skip(this.origBytes.Length - 3).ToArray(), 0, 3);
                }
                AssertStreamContentsEqual(this.expectedCipher, baseStream);
            }
        }

        [Fact]
        public void TestWriteOneByOne()
        {
            using (var baseStream = TestData.GetStream(new byte[this.origBytes.Length])) {
                using (var encryptionStream = new EncryptionStream(baseStream, AnnoRDA.IO.StreamAccessMode.Write, leaveOpen: true)) {
                    foreach (var origByte in this.origBytes) {
                        var buffer = new byte[] { origByte };
                        encryptionStream.Write(buffer, 0, 1);
                    }
                }
                AssertStreamContentsEqual(this.expectedCipher, baseStream);
            }
        }
    }
}
