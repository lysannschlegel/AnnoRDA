using AnnoRDA.Loader;
using AnnoRDA.Tests.TestUtil;
using System;
using System.Threading;
using Xunit;

namespace AnnoRDA.Tests.Loader
{
    public class ContainerFileLoaderTests
    {
        private ContainerFileLoader loader = null;

        public ContainerFileLoaderTests()
        {
            this.loader = new ContainerFileLoader();
        }


        [Fact]
        public async System.Threading.Tasks.Task TestLoadContainerWithNoBlocks()
        {
            var actualFS = await this.loader.Load(TestData.GetPath("Containers/2.2_no_blocks.rda"));
            Assert.Empty(actualFS);
        }

        [Fact]
        public async System.Threading.Tasks.Task TestLoadContainerWithEmptyBlock()
        {
            var actualFS = await this.loader.Load(TestData.GetPath("Containers/2.2_empty_block.rda"));
            Assert.Empty(actualFS);
        }

        [Fact]
        public async System.Threading.Tasks.Task TestLoadContainerWithEmptyFile()
        {
            var actualFS = await this.loader.Load(TestData.GetPath("Containers/2.2_empty_file.rda"));

            Assert.FolderAndFileCountAreEqual(1, 0, actualFS);
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file.txt") {
                CompressedFileSize = 0,
                UncompressedFileSize = 0,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
                Contents = "",
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task TestLoadContainerWithFileAndContents()
        {
            var actualFS = await this.loader.Load(TestData.GetPath("Containers/2.2_single_file.rda"));

            Assert.FolderAndFileCountAreEqual(1, 0, actualFS);
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file.txt") {
                CompressedFileSize = 53,
                UncompressedFileSize = 53,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
                Contents = "This is just a test. Nothing to see here. Move along.",
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task TestLoadContainerWithMultipleFiles()
        {
            var actualFS = await this.loader.Load(TestData.GetPath("Containers/2.2_multiple_files.rda"));

            Assert.FolderAndFileCountAreEqual(2, 1, actualFS);
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file1.txt") {
                CompressedFileSize = 61,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
                Contents = "File 1: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file2.txt") {
                CompressedFileSize = 61,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 22, DateTimeKind.Utc),
                Contents = "File 2: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("root file.txt") {
                CompressedFileSize = 24,
                UncompressedFileSize = 24,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 23, DateTimeKind.Utc),
                Contents = "A file on the root level",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("another", "directory.txt") {
                CompressedFileSize = 32,
                UncompressedFileSize = 32,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 24, DateTimeKind.Utc),
                Contents = "Just a file in another directory",
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task TestLoadContainerWithCompressedFiles()
        {
            var actualFS = await this.loader.Load(TestData.GetPath("Containers/2.2_multiple_files_compressed.rda"));

            Assert.FolderAndFileCountAreEqual(2, 1, actualFS);
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file1.txt") {
                IsCompressed = true,
                CompressedFileSize = 67,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
                Contents = "File 1: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file2.txt") {
                IsCompressed = true,
                CompressedFileSize = 67,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 22, DateTimeKind.Utc),
                Contents = "File 2: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("root file.txt") {
                IsCompressed = true,
                CompressedFileSize = 32,
                UncompressedFileSize = 24,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 23, DateTimeKind.Utc),
                Contents = "A file on the root level",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("another", "directory.txt") {
                IsCompressed = true,
                CompressedFileSize = 40,
                UncompressedFileSize = 32,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 24, DateTimeKind.Utc),
                Contents = "Just a file in another directory",
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task TestLoadContainerWithEncryptedFiles()
        {
            var actualFS = await this.loader.Load(TestData.GetPath("Containers/2.2_multiple_files_encrypted.rda"));

            Assert.FolderAndFileCountAreEqual(2, 1, actualFS);
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file1.txt") {
                IsEncrypted = true,
                CompressedFileSize = 61,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
                Contents = "File 1: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file2.txt") {
                IsEncrypted = true,
                CompressedFileSize = 61,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 22, DateTimeKind.Utc),
                Contents = "File 2: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("root file.txt") {
                IsEncrypted = true,
                CompressedFileSize = 24,
                UncompressedFileSize = 24,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 23, DateTimeKind.Utc),
                Contents = "A file on the root level",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("another", "directory.txt") {
                IsEncrypted = true,
                CompressedFileSize = 32,
                UncompressedFileSize = 32,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 24, DateTimeKind.Utc),
                Contents = "Just a file in another directory",
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task TestLoadContainerWithCompressedAndEncryptedFiles()
        {
            var actualFS = await this.loader.Load(TestData.GetPath("Containers/2.2_multiple_files_compressed_encrypted.rda"));

            Assert.FolderAndFileCountAreEqual(2, 1, actualFS);
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file1.txt") {
                IsCompressed = true,
                IsEncrypted = true,
                CompressedFileSize = 67,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
                Contents = "File 1: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file2.txt") {
                IsCompressed = true,
                IsEncrypted = true,
                CompressedFileSize = 67,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 22, DateTimeKind.Utc),
                Contents = "File 2: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("root file.txt") {
                IsCompressed = true,
                IsEncrypted = true,
                CompressedFileSize = 32,
                UncompressedFileSize = 24,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 23, DateTimeKind.Utc),
                Contents = "A file on the root level",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("another", "directory.txt") {
                IsCompressed = true,
                IsEncrypted = true,
                CompressedFileSize = 40,
                UncompressedFileSize = 32,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 24, DateTimeKind.Utc),
                Contents = "Just a file in another directory",
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task TestLoadContainerWithContiguousDataBlock()
        {
            var actualFS = await this.loader.Load(TestData.GetPath("Containers/2.2_multiple_files_contiguous.rda"));

            Assert.FolderAndFileCountAreEqual(2, 1, actualFS);
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file1.txt") {
                CompressedFileSize = 61,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
                Contents = "File 1: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file2.txt") {
                CompressedFileSize = 61,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 22, DateTimeKind.Utc),
                Contents = "File 2: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("root file.txt") {
                CompressedFileSize = 24,
                UncompressedFileSize = 24,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 23, DateTimeKind.Utc),
                Contents = "A file on the root level",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("another", "directory.txt") {
                CompressedFileSize = 32,
                UncompressedFileSize = 32,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 24, DateTimeKind.Utc),
                Contents = "Just a file in another directory",
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task TestLoadContainerWithContiguousCompressedAndEncryptedDataBlock()
        {
            var actualFS = await this.loader.Load(TestData.GetPath("Containers/2.2_multiple_files_contiguous_compressed_encrypted.rda"));
            
            Assert.FolderAndFileCountAreEqual(2, 1, actualFS);
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file1.txt") {
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
                Contents = "File 1: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file2.txt") {
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 22, DateTimeKind.Utc),
                Contents = "File 2: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("root file.txt") {
                UncompressedFileSize = 24,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 23, DateTimeKind.Utc),
                Contents = "A file on the root level",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("another", "directory.txt") {
                UncompressedFileSize = 32,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 24, DateTimeKind.Utc),
                Contents = "Just a file in another directory",
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task TestLoadContainerWithMultipleBlocks()
        {
            var actualFS = await this.loader.Load(TestData.GetPath("Containers/2.2_multiple_blocks.rda"));

            Assert.FolderAndFileCountAreEqual(2, 1, actualFS);
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file1.txt") {
                CompressedFileSize = 61,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
                Contents = "File 1: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file2.txt") {
                CompressedFileSize = 61,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 22, DateTimeKind.Utc),
                Contents = "File 2: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("root file.txt") {
                CompressedFileSize = 24,
                UncompressedFileSize = 24,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 23, DateTimeKind.Utc),
                Contents = "A file on the root level",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("another", "directory.txt") {
                CompressedFileSize = 32,
                UncompressedFileSize = 32,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 24, DateTimeKind.Utc),
                Contents = "Just a file in another directory",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file3.txt") {
                CompressedFileSize = 61,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 25, DateTimeKind.Utc),
                Contents = "File 3: This is just a test. Nothing to see here. Move along.",
            });
            Assert.ContainsFile(actualFS, new Assert.FileSpec("path", "to", "file4.txt") {
                CompressedFileSize = 61,
                UncompressedFileSize = 61,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 26, DateTimeKind.Utc),
                Contents = "File 4: This is just a test. Nothing to see here. Move along.",
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task TestLoadContainerWithInvalidHeader()
        {
            Exception exception = await Assert.ThrowsAsync<AnnoRDA.FileFormatException>(async () => {
                using (var context = new ContainerFileLoader.Context("dummy.rda", TestData.GetStream(new byte[] { }), false, new PassThroughFileHeaderTransformer())) {
                    await this.loader.Load(context, CancellationToken.None);
                }
            });
            Assert.Equal(new AnnoRDA.FileFormatException(AnnoRDA.FileFormatException.EntityType.RDAHeader, AnnoRDA.FileFormatException.Error.UnexpectedEndOfFile, 0), exception);
            
            exception = await Assert.ThrowsAsync<AnnoRDA.FileFormatException>(async () => {
                await this.loader.Load(TestData.GetPath("Containers/2.2_header_cut_off_before_first_block_offset.rda"));
            });
            Assert.Equal(new AnnoRDA.FileFormatException(AnnoRDA.FileFormatException.EntityType.RDAHeader, AnnoRDA.FileFormatException.Error.UnexpectedEndOfFile, 784), exception);

            exception = await Assert.ThrowsAsync<AnnoRDA.FileFormatException>(async () => {
                await this.loader.Load(TestData.GetPath("Containers/2.2_header_cut_off_inside_first_block_offset.rda"));
            });
            Assert.Equal(new AnnoRDA.FileFormatException(AnnoRDA.FileFormatException.EntityType.RDAHeader, AnnoRDA.FileFormatException.Error.UnexpectedEndOfFile, 791), exception);

            var stream = TestData.GetStream(new byte[] { 12, 43, 21, 0, 4, 2, 1, 54, 21, 23, 44, 1, 2, 3, 4, 5, 6, 21, 122, 99 });
            exception = await Assert.ThrowsAsync<AnnoRDA.FileFormatException>(async () => {
                using (var context = new ContainerFileLoader.Context("dummy.rda", stream, false, new PassThroughFileHeaderTransformer())) {
                    await this.loader.Load(context, CancellationToken.None);
                }
            });
            Assert.Equal(new AnnoRDA.FileFormatException(AnnoRDA.FileFormatException.EntityType.RDAHeader, AnnoRDA.FileFormatException.Error.InvalidValue, 0), exception);
        }
    }
}
