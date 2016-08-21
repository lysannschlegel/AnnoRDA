using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using AnnoRDA.Tests.TestUtil;

namespace Xunit
{
    public partial class Assert
    {
        private static readonly string PathSeparator = "/";

        public static void Empty(AnnoRDA.FileSystem actual)
        {
            NotNull(actual);
            Empty(actual.Root);
        }
        internal static void Empty(AnnoRDA.Folder actual)
        {
            FolderAndFileCountAreEqual(0, 0, actual);
        }

        internal static void FolderAndFileCountAreEqual(int expectedFolders, int expectedFiles, AnnoRDA.FileSystem actual)
        {
            NotNull(actual);
            FolderAndFileCountAreEqual(expectedFolders, expectedFiles, actual.Root);
        }
        internal static void FolderAndFileCountAreEqual(int expectedFolders, int expectedFiles, AnnoRDA.Folder actual)
        {
            NotNull(actual);
            Equal(expectedFolders, actual.Folders.Count());
            Equal(expectedFiles, actual.Files.Count());
        }


        internal static void ContainsFolder(AnnoRDA.FileSystem fileSystem, string path)
        {
            if (path == null) {
                throw new ArgumentNullException("path");
            }
            ContainsFolder(fileSystem, Enumerable.Repeat(path, 1));
        }
        internal static void ContainsFolder(AnnoRDA.FileSystem fileSystem, IEnumerable<string> path)
        {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            ContainsFolder(fileSystem.Root, path);
        }
        internal static void ContainsFolder(AnnoRDA.Folder folder, string path)
        {
            if (path == null) {
                throw new ArgumentNullException("path");
            }
            ContainsFolder(folder, Enumerable.Repeat(path, 1));
        }
        internal static void ContainsFolder(AnnoRDA.Folder folder, IEnumerable<string> path)
        {
            if (path == null) {
                throw new ArgumentNullException("path");
            }
            if (!path.Any()) {
                throw new ArgumentException("path cannot be empty", "path");
            }

            ContainsFolder(folder, path, Enumerable.Empty<string>());
        }

        private static AnnoRDA.Folder ContainsFolder(AnnoRDA.Folder folder, IEnumerable<string> path, IEnumerable<string> pathSoFar)
        {
            if (folder == null) {
                throw new ArgumentNullException("folder");
            }
            if (path == null) {
                throw new ArgumentNullException("path");
            }
            if (pathSoFar == null) {
                throw new ArgumentNullException("pathSoFar");
            }

            string first = path.First();
            IEnumerable<string> newPathSoFar = pathSoFar.Concat(Enumerable.Repeat(first, 1));

            AnnoRDA.Folder subFolder = folder.Folders.FirstOrDefault((f) => f.Name == first);
            if (subFolder == null) {
                throw new Xunit.Sdk.AssertActualExpectedException(
                    String.Join(PathSeparator, newPathSoFar),
                    folder,
                    "Assert.ContainsFolder() Failure",
                    "Not found",
                    "In value"
                );
            }

            IEnumerable<string> restPath = path.Skip(1);
            if (restPath.Any()) {
                return ContainsFolder(subFolder, restPath, newPathSoFar);
            } else {
                return subFolder;
            }
        }

        internal static void ContainsFile(AnnoRDA.FileSystem fileSystem, FileSpec fileSpec)
        {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            ContainsFile(fileSystem.Root, fileSpec);
        }
        internal static void ContainsFile(AnnoRDA.Folder folder, FileSpec fileSpec)
        {
            if (fileSpec == null) {
                throw new ArgumentNullException("fileSpec");
            }

            IEnumerable<string> folderPath;
            string fileName = fileSpec.Path.SplitOffLast(out folderPath);

            AnnoRDA.Folder subFolder = folderPath.Any() ? ContainsFolder(folder, folderPath, Enumerable.Empty<string>()) : folder;
            if (subFolder == null) {
                throw new Xunit.Sdk.AssertActualExpectedException(
                    String.Join(PathSeparator, folderPath),
                    folder,
                    "Assert.ContainsFile() Failure",
                    "Folder not found",
                    "In value"
                );
            } else {
                AnnoRDA.File file = subFolder.Files.FirstOrDefault((f) => f.Name == fileName);
                FileMatches(fileSpec, file, folder);
            }
        }

        internal static void FileMatches(FileSpec expected, AnnoRDA.File actual, AnnoRDA.Folder searchRootFolder)
        {
            if (expected == null) {
                throw new ArgumentNullException("expected");
            }

            if (actual == null) {
                throw new Xunit.Sdk.AssertActualExpectedException(
                    String.Join(PathSeparator, expected.Path),
                    searchRootFolder,
                    "Assert.ContainsFile() Failure",
                    "File not found",
                    "In value"
                );
            } else {
                if (expected.DataOffset.HasValue) {
                    Equal(expected.DataOffset.Value, actual.ContentsSource.PositionInBlock);
                }
                if (expected.IsCompressed.HasValue) {
                    Equal(expected.IsCompressed.Value, actual.ContentsSource.BlockContentsSource.Flags.IsCompressed);
                }
                if (expected.IsEncrypted.HasValue) {
                    Equal(expected.IsEncrypted.Value, actual.ContentsSource.BlockContentsSource.Flags.IsEncrypted);
                }
                if (expected.IsDeleted.HasValue) {
                    Equal(expected.IsDeleted.Value, actual.ContentsSource.BlockContentsSource.Flags.IsDeleted);
                }
                if (expected.CompressedFileSize.HasValue) {
                    Equal(expected.CompressedFileSize.Value, actual.ContentsSource.CompressedSize);
                }
                if (expected.UncompressedFileSize.HasValue) {
                    Equal(expected.UncompressedFileSize.Value, actual.ContentsSource.UncompressedSize);
                }
                if (expected.ModificationDate.HasValue) {
                    Equal(expected.ModificationDate.Value, actual.ModificationDate);
                }
                if (expected.ModificationTimestamp.HasValue) {
                    Equal(expected.ModificationTimestamp.Value, actual.ModificationTimestamp);
                }
                if (expected.Contents != null) {
                    using (Stream contentStream = actual.ContentsSource.GetReadStream()) {
                        using (StreamReader reader = new StreamReader(contentStream)) {
                            Equal(expected.Contents, reader.ReadToEnd());
                        }
                    }
                }
            }
        }


        internal class FileSpec
        {
            public IEnumerable<string> Path { get; }
            public long? DataOffset { get; set; } = null;
            public int? Flags { get; set; } = null;
            public bool? IsCompressed {
                get {
                    if (this.Flags.HasValue) {
                        return (this.Flags.Value & 1) != 0;
                    } else {
                        return default(bool?);
                    }
                }
                set {
                    if (value.HasValue) {
                        int newFlags = this.Flags.HasValue ? this.Flags.Value : 0;
                        if (value.Value) {
                            newFlags |= 1;
                        } else {
                            newFlags &= ~1;
                        }
                        this.Flags = newFlags;
                    } else {
                        this.Flags = null;
                    }
                }
            }
            public bool? IsEncrypted {
                get {
                    if (this.Flags.HasValue) {
                        return (this.Flags.Value & 2) != 0;
                    } else {
                        return default(bool?);
                    }
                }
                set {
                    if (value.HasValue) {
                        int newFlags = this.Flags.HasValue ? this.Flags.Value : 0;
                        if (value.Value) {
                            newFlags |= 2;
                        } else {
                            newFlags &= ~2;
                        }
                        this.Flags = newFlags;
                    } else {
                        this.Flags = null;
                    }
                }
            }
            public bool? IsDeleted {
                get {
                    if (this.Flags.HasValue) {
                        return (this.Flags.Value & 8) != 0;
                    } else {
                        return default(bool?);
                    }
                }
                set {
                    if (value.HasValue) {
                        int newFlags = this.Flags.HasValue ? this.Flags.Value : 0;
                        if (value.Value) {
                            newFlags |= 8;
                        } else {
                            newFlags &= ~8;
                        }
                        this.Flags = newFlags;
                    } else {
                        this.Flags = null;
                    }
                }
            }
            public long? CompressedFileSize { get; set; } = null;
            public long? UncompressedFileSize { get; set; } = null;
            public DateTime? ModificationDate { get; set; } = null;
            public long? ModificationTimestamp { get; set; } = null;
            public string Contents { get; set; } = null;

            public FileSpec(IEnumerable<string> path)
            {
                if (path == null) {
                    throw new ArgumentNullException("path");
                } else if (!path.Any()) {
                    throw new ArgumentException("path cannot be empty", "path");
                }
                this.Path = path;
            }

            public FileSpec(params string[] path)
            {
                if (path == null) {
                    throw new ArgumentNullException("path");
                } else if (!path.Any()) {
                    throw new ArgumentException("path cannot be empty", "path");
                }
                this.Path = path;
            }
        }
    }
}
