using AnnoRDA.Loader;
using AnnoRDA.Tests.TestUtil;
using Xunit;

namespace AnnoRDA.Tests.Loader
{
    public class ContainerFileBlockHeaderReaderTests
    {
        [Fact]
        public void TestReadBlockHeader()
        {
            AnnoRDA.FileEntities.BlockHeader actual;

            var data = new byte[] {
                0xFF, 0xFF, 0xFF, // gibberish data before the block, will be skipped when reading
                0x01, 0x00, 0x00, 0x00, // flags
                0x42, 0x00, 0x00, 0x00, // number of files
                0xB8, 0x22, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // compressed file headers size
                0x60, 0x90, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // uncompressed file headers size
                0xC7, 0x8A, 0xA9, 0x00, 0x00, 0x00, 0x00, 0x00, // next block offset
            };
            using (var reader = new ContainerFileLoaderReader(TestData.GetStream(data), false)) {
                actual = reader.ReadBlockHeader(3);
            }

            var expected = new AnnoRDA.FileEntities.BlockHeader() {
                Offset = 3,
                Flags = 1,
                NumFiles = 66,
                CompressedFileHeadersSize = 8888,
                UncompressedFileHeadersSize = 36960,
                NextBlockOffset = 11111111,
            };

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestReadInvalidBlockHeader()
        {
            var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x42, 0x00, 0x00, 0x00, 0xB8, 0x22, 0x00, 0x00, 0x00, 0x00 };
            System.Exception exception = Assert.Throws<AnnoRDA.FileFormatException>(() => {
                using (var reader = new ContainerFileLoaderReader(TestData.GetStream(data), false)) {
                    reader.ReadBlockHeader(0);
                }
            });
            Assert.Equal(new AnnoRDA.FileFormatException(AnnoRDA.FileFormatException.EntityType.BlockHeader, AnnoRDA.FileFormatException.Error.UnexpectedEndOfFile, 14), exception);

            data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x42, 0x00, 0x00, 0x00, 0xB8, 0x22, 0x00, 0x00, 0x00, 0x00 };
            exception = Assert.Throws<AnnoRDA.FileFormatException>(() => {
                using (var reader = new ContainerFileLoaderReader(TestData.GetStream(data), false)) {
                    reader.ReadBlockHeader(data.Length);
                }
            });
            Assert.Equal(new AnnoRDA.FileFormatException(AnnoRDA.FileFormatException.EntityType.BlockHeader, AnnoRDA.FileFormatException.Error.UnexpectedEndOfFile, data.Length), exception);
        }
    }
}
