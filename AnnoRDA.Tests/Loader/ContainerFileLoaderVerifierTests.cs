using AnnoRDA.Loader;
using Xunit;

namespace AnnoRDA.Tests.Loader
{
    class ContainerFileLoaderVerifierTests
    {
        ContainerFileLoaderVerifier verifier;

        public ContainerFileLoaderVerifierTests()
        {
            this.verifier = new ContainerFileLoaderVerifier();
        }

        #region Block

        [Fact]
        public void TestVerifyValidBlockHeader()
        {
            var block = new AnnoRDA.FileEntities.BlockHeader() {
                Offset = 36960 + 792,
                IsCompressed = false,
                NumFiles = 66,
                CompressedFileHeadersSize = 36960,
                UncompressedFileHeadersSize = 36960,
                NextBlockOffset = 11111111,
            };
            this.verifier.VerifyBlockHeader(block);

            var blockWithCompression = new AnnoRDA.FileEntities.BlockHeader() {
                Offset = 8888 + 792,
                IsCompressed = true,
                NumFiles = 66,
                CompressedFileHeadersSize = 8888,
                UncompressedFileHeadersSize = 36960,
                NextBlockOffset = 11111111,
            };
            this.verifier.VerifyBlockHeader(blockWithCompression);
        }

        [Fact]
        public void TestVerifyInvalidBlockHeader()
        {
            var block = new AnnoRDA.FileEntities.BlockHeader() {
                Offset = 17,
                IsCompressed = true,
                NumFiles = 66,
                CompressedFileHeadersSize = 8888,
                UncompressedFileHeadersSize = 55555,
                NextBlockOffset = 11111111,
            };
            System.Exception exception = Assert.Throws<AnnoRDA.FileFormatException>(() => {
                this.verifier.VerifyBlockHeader(block);
            });
            Assert.Equal(new AnnoRDA.FileFormatException(AnnoRDA.FileFormatException.EntityType.BlockHeader, AnnoRDA.FileFormatException.Error.InvalidValue, 17, "The file headers size does not match the number of files."), exception);

            block = new AnnoRDA.FileEntities.BlockHeader() {
                Offset = 17,
                IsCompressed = false,
                NumFiles = 66,
                CompressedFileHeadersSize = 8888,
                UncompressedFileHeadersSize = 36960,
                NextBlockOffset = 11111111,
            };
            exception = Assert.Throws<AnnoRDA.FileFormatException>(() => {
                this.verifier.VerifyBlockHeader(block);
            });
            Assert.Equal(new AnnoRDA.FileFormatException(AnnoRDA.FileFormatException.EntityType.BlockHeader, AnnoRDA.FileFormatException.Error.InvalidValue, 17, "The compressed file headers size should match the uncompressed size when compression is disabled."), exception);
        }

        #endregion

        #region File

        [Fact]
        public void TestVerifyValidFileHeader()
        {
            var fileHeader = new AnnoRDA.FileEntities.FileHeader() {
                Path = "path/to/file.txt",
                DataOffset = 11111111,
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationTimestamp = 1448398881,
            };
            this.verifier.VerifyFileHeader(fileHeader, false, 42);

            var fileHeaderWithCompression = new AnnoRDA.FileEntities.FileHeader() {
                Path = "path/to/file.txt",
                DataOffset = 11111111,
                CompressedFileSize = 1234,
                UncompressedFileSize = 36960,
                ModificationTimestamp = 1448398881,
            };
            this.verifier.VerifyFileHeader(fileHeaderWithCompression, true, 42);
        }

        [Fact]
        public void TestVerifyInvalidFileHeader()
        {
            var fileHeader = new AnnoRDA.FileEntities.FileHeader() {
                Path = "path/to/file.txt",
                DataOffset = 11111111,
                CompressedFileSize = 1234,
                UncompressedFileSize = 36960,
                ModificationTimestamp = 1448398881,
            };
            var exception = Assert.Throws<AnnoRDA.FileFormatException>(() => {
                this.verifier.VerifyFileHeader(fileHeader, false, 42);
            });
            Assert.Equal(new AnnoRDA.FileFormatException(AnnoRDA.FileFormatException.EntityType.FileHeader, AnnoRDA.FileFormatException.Error.InvalidValue, 42, "The compressed file size should match the uncompressed size when compression is disabled."), exception);
        }

        #endregion
    }
}
