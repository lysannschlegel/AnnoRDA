using System;
using AnnoRDA.Loader;
using AnnoRDA.Tests.TestUtil;
using System.Linq;
using Xunit;

namespace AnnoRDA.Tests.Loader
{
    public class ContainerFileFileHeaderLoaderTests
    {
        [Fact]
        public void TestReadFileHeader()
        {
            AnnoRDA.FileEntities.FileHeader actual;
            using (var reader = new ContainerFileLoaderStructureReader(TestData.GetReader("FileHeaders/2.2_file_header.bin"), false)) {
                actual = reader.ReadFileHeader();
            }
            var expected = new AnnoRDA.FileEntities.FileHeader() {
                Path = "path/to/file.txt",
                DataOffset = 11111111,
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationTimestamp = 1448398881,
            };
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestReadInvalidFileHeader()
        {
            var shortData = new byte[] { 0x70, 0x00, 0x61, 0x00, 0x74, 0x00, 0x68, 0x00, 0x2F, 0x00, 0x74, 0x00, 0x6F, 0x00, 0x2F, 0x00, 0x66, 0x00, 0x69, 0x00 };
            var exception = Assert.Throws<AnnoRDA.FileFormatException>(() => {
                using (var reader = new ContainerFileLoaderStructureReader(TestData.GetReader(shortData), false)) {
                    reader.ReadFileHeader();
                }
            });
            Assert.Equal(new AnnoRDA.FileFormatException(AnnoRDA.FileFormatException.EntityType.FileHeader, AnnoRDA.FileFormatException.Error.UnexpectedEndOfFile, 20), exception);
        }


        [Fact]
        public void TestLoadFileHeaders()
        {
            var loader = new ContainerFileLoader();
            AnnoRDA.FileSystem fileSystem;
            using (var context = new ContainerFileLoader.Context("dummy.rda", TestData.GetStream("FileHeaders/2.2_multiple_file_headers.bin"), false, new PassThroughFileHeaderTransformer())) {
                var block = new AnnoRDA.FileEntities.BlockHeader() {
                    Offset = context.Reader.StreamLength,
                    IsCompressed = false,
                    NumFiles = 2,
                    CompressedFileHeadersSize = 1120,
                    UncompressedFileHeadersSize = 1120,
                };
                loader.LoadFileHeaders(context, block);
                fileSystem = context.FileSystem;
            }

            Assert.ContainsFile(fileSystem, new Assert.FileSpec("path", "to", "file1.txt") {
                DataOffset = 11111111,
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
            });
            Assert.ContainsFile(fileSystem, new Assert.FileSpec("path", "to", "file2.txt") {
                DataOffset = 11148071,
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 22, DateTimeKind.Utc),
            });
        }

        [Fact]
        public void TestLoadCompressedFileHeaders()
        {
            var loader = new ContainerFileLoader();
            AnnoRDA.FileSystem fileSystem;
            using (var context = new ContainerFileLoader.Context("dummy.rda", TestData.GetStream("FileHeaders/2.2_multiple_file_headers_compressed.bin"), false, new PassThroughFileHeaderTransformer())) {
                var block = new AnnoRDA.FileEntities.BlockHeader() {
                    Offset = context.Reader.StreamLength,
                    IsCompressed = true,
                    NumFiles = 2,
                    CompressedFileHeadersSize = context.Reader.StreamLength,
                    UncompressedFileHeadersSize = 1120,
                };
                loader.LoadFileHeaders(context, block);
                fileSystem = context.FileSystem;
            }

            Assert.ContainsFile(fileSystem, new Assert.FileSpec("path", "to", "file1.txt") {
                DataOffset = 11111111,
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
            });
            Assert.ContainsFile(fileSystem, new Assert.FileSpec("path", "to", "file2.txt") {
                DataOffset = 11148071,
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 22, DateTimeKind.Utc),
            });
        }

        [Fact]
        public void TestLoadEncryptedFileHeaders()
        {
            var loader = new ContainerFileLoader();
            AnnoRDA.FileSystem fileSystem;
            using (var context = new ContainerFileLoader.Context("dummy.rda", TestData.GetStream("FileHeaders/2.2_multiple_file_headers_encrypted.bin"), false, new PassThroughFileHeaderTransformer())) {
                var block = new AnnoRDA.FileEntities.BlockHeader() {
                    Offset = context.Reader.StreamLength,
                    IsEncrypted = true,
                    NumFiles = 2,
                    CompressedFileHeadersSize = 1120,
                    UncompressedFileHeadersSize = 1120,
                };
                loader.LoadFileHeaders(context, block);
                fileSystem = context.FileSystem;
            }

            Assert.ContainsFile(fileSystem, new Assert.FileSpec("path", "to", "file1.txt") {
                DataOffset = 11111111,
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
            });
            Assert.ContainsFile(fileSystem, new Assert.FileSpec("path", "to", "file2.txt") {
                DataOffset = 11148071,
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 22, DateTimeKind.Utc),
            });
        }

        [Fact]
        public void TestLoadCompressedAndEncryptedFileHeaders()
        {
            var loader = new ContainerFileLoader();
            AnnoRDA.FileSystem fileSystem;
            using (var context = new ContainerFileLoader.Context("dummy.rda", TestData.GetStream("FileHeaders/2.2_multiple_file_headers_compressed_encrypted.bin"), false, new PassThroughFileHeaderTransformer())) {
                var block = new AnnoRDA.FileEntities.BlockHeader() {
                    Offset = context.Reader.StreamLength,
                    IsCompressed = true,
                    IsEncrypted = true,
                    NumFiles = 2,
                    CompressedFileHeadersSize = context.Reader.StreamLength,
                    UncompressedFileHeadersSize = 1120,
                };
                loader.LoadFileHeaders(context, block);
                fileSystem = context.FileSystem;
            }

            Assert.ContainsFile(fileSystem, new Assert.FileSpec("path", "to", "file1.txt") {
                DataOffset = 11111111,
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 21, DateTimeKind.Utc),
            });
            Assert.ContainsFile(fileSystem, new Assert.FileSpec("path", "to", "file2.txt") {
                DataOffset = 11148071,
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationDate = new DateTime(2015, 11, 24, 21, 1, 22, DateTimeKind.Utc),
            });
        }

        [Fact]
        public void TestAddFileToEmptyFileSystem()
        {
            var file = new AnnoRDA.FileEntities.FileHeader() {
                Path = "path/to/file.txt",
                DataOffset = 11111111,
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationTimestamp = 1448398881,
            };

            var loader = new ContainerFileLoader();
            AnnoRDA.FileSystem fileSystem;
            using (var context = new ContainerFileLoader.Context("dummy.rda", TestData.GetStream(), false, new PassThroughFileHeaderTransformer())) {
                loader.AddFileToFileSystem(context, file, TestData.GetDummyBlockContentsSource());
                fileSystem = context.FileSystem;
            }

            Assert.FolderAndFileCountAreEqual(1, 0, fileSystem);
            Assert.ContainsFile(fileSystem, new Assert.FileSpec("path", "to", "file.txt"));
        }

        [Fact]
        public void TestOverwriteFileInFileSystem()
        {
            var folderPath = new AnnoRDA.Folder("path");
            {
                var folderTo = new AnnoRDA.Folder("to");
                {
                    folderTo.Add(new AnnoRDA.File("file.txt") {
                        ModificationDate = new DateTime(),
                    });
                }
                folderPath.Add(folderTo);
            }

            var file = new AnnoRDA.FileEntities.FileHeader() {
                Path = "path/to/file.txt",
                DataOffset = 11111111,
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationTimestamp = 1448398881,
            };

            var loader = new ContainerFileLoader();
            AnnoRDA.FileSystem fileSystem;
            using (var context = new ContainerFileLoader.Context("dummy.rda", TestData.GetStream(), false, new PassThroughFileHeaderTransformer())) {
                fileSystem = context.FileSystem;
                fileSystem.Root.Add(folderPath);
                loader.AddFileToFileSystem(context, file, TestData.GetDummyBlockContentsSource());
            }

            Assert.FolderAndFileCountAreEqual(1, 0, fileSystem.Root);
            Assert.FolderAndFileCountAreEqual(1, 0, fileSystem.Root.Folders.First());
            Assert.FolderAndFileCountAreEqual(0, 1, fileSystem.Root.Folders.First().Folders.First());
            Assert.ContainsFile(fileSystem, new Assert.FileSpec("path", "to", "file.txt") {
                CompressedFileSize = 36960,
                UncompressedFileSize = 36960,
                ModificationTimestamp = 1448398881,
            });
        }
    }
}
