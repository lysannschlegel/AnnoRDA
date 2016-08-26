using System.Linq;
using System;
using Xunit;

namespace AnnoRDA.Tests.Loader
{
    public class ContainerDirectoryLoaderTests
    {
        [Fact]
        public void TestFileNameSorting()
        {
            string[] actual = AnnoRDA.Loader.ContainerDirectoryLoader.SortContainerPaths(new string[] {
                "data1.rda",
                "data10.rda",
                "data2.rda",
                "data3.rda",
                "data8.rda",
                "data5.rda",
                "data4.rda",
                "data6.rda",
                "data7.rda",
                "data9.rda",
                "data11.rda",
            }).ToArray();

            string[] expected = new string[] {
                "data1.rda",
                "data2.rda",
                "data3.rda",
                "data4.rda",
                "data5.rda",
                "data6.rda",
                "data7.rda",
                "data8.rda",
                "data9.rda",
                "data10.rda",
                "data11.rda",
            };

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestMergeEmptyFileSystems()
        {
            AnnoRDA.FileSystem baseFS = new FileSystem();
            AnnoRDA.FileSystem overwriteFS = new FileSystem();

            baseFS.OverwriteWith(overwriteFS, null, System.Threading.CancellationToken.None);
            Assert.Empty(baseFS);
        }

        [Fact]
        public void TestMergeFileSystemsWithFilesWithoutConflicts()
        {
            AnnoRDA.FileSystem baseFS = new FileSystem();
            baseFS.Root.Add(new File("1503") { ModificationDate = new DateTime(2003, 3, 23, 0, 0, 0, DateTimeKind.Utc) });
            baseFS.Root.Add(new File("1602") { ModificationDate = new DateTime(1998, 9, 24, 0, 0, 0, DateTimeKind.Utc) });
            baseFS.Root.Add(new File("1701") { ModificationDate = new DateTime(2006, 10, 25, 0, 0, 0, DateTimeKind.Utc) });

            AnnoRDA.FileSystem overwriteFS = new FileSystem();
            overwriteFS.Root.Add(new File("1404") { ModificationDate = new DateTime(2009, 6, 25, 0, 0, 0, DateTimeKind.Utc) });
            overwriteFS.Root.Add(new File("2205") { ModificationDate = new DateTime(2015, 11, 3, 0, 0, 0, DateTimeKind.Utc) });
            overwriteFS.Root.Add(new File("2070") { ModificationDate = new DateTime(2011, 11, 17, 0, 0, 0, DateTimeKind.Utc) });

            baseFS.OverwriteWith(overwriteFS, null, System.Threading.CancellationToken.None);
            Assert.FolderAndFileCountAreEqual(0, 6, baseFS);
            Assert.ContainsFile(baseFS, new Assert.FileSpec("1503") { ModificationDate = new DateTime(2003, 3, 23, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("1602") { ModificationDate = new DateTime(1998, 9, 24, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("1701") { ModificationDate = new DateTime(2006, 10, 25, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("1404") { ModificationDate = new DateTime(2009, 6, 25, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("2205") { ModificationDate = new DateTime(2015, 11, 3, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("2070") { ModificationDate = new DateTime(2011, 11, 17, 0, 0, 0, DateTimeKind.Utc) });
        }

        [Fact]
        public void TestMergeFileSystemsWithFilesWithConflicts()
        {
            AnnoRDA.FileSystem baseFS = new FileSystem();
            baseFS.Root.Add(new File("1503") { ModificationDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
            baseFS.Root.Add(new File("1602") { ModificationDate = new DateTime(1998, 9, 24, 0, 0, 0, DateTimeKind.Utc) });

            AnnoRDA.FileSystem overwriteFS = new FileSystem();
            overwriteFS.Root.Add(new File("1404") { ModificationDate = new DateTime(2009, 6, 25, 0, 0, 0, DateTimeKind.Utc) });
            overwriteFS.Root.Add(new File("1503") { ModificationDate = new DateTime(2003, 3, 23, 0, 0, 0, DateTimeKind.Utc) });
            overwriteFS.Root.Add(new File("2070") { ModificationDate = new DateTime(2011, 11, 17, 0, 0, 0, DateTimeKind.Utc) });

            baseFS.OverwriteWith(overwriteFS, null, System.Threading.CancellationToken.None);
            Assert.FolderAndFileCountAreEqual(0, 4, baseFS);
            Assert.ContainsFile(baseFS, new Assert.FileSpec("1503") { ModificationDate = new DateTime(2003, 3, 23, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("1602") { ModificationDate = new DateTime(1998, 9, 24, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("1404") { ModificationDate = new DateTime(2009, 6, 25, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("2070") { ModificationDate = new DateTime(2011, 11, 17, 0, 0, 0, DateTimeKind.Utc) });
        }

        [Fact]
        public void TestMergeFileSystemsWithFilesAndFolders()
        {
            AnnoRDA.FileSystem baseFS = new FileSystem();
            baseFS.Root.Add(new File("root file"));
            var baseFSFolder1 = new AnnoRDA.Folder("Max Design");
            {
                baseFSFolder1.Add(new File("1602") { ModificationDate = new DateTime(1998, 9, 24, 0, 0, 0, DateTimeKind.Utc) });
            }
            baseFS.Root.Add(baseFSFolder1);
            var baseFSFolder2 = new AnnoRDA.Folder("Related Designs");
            {
                baseFSFolder2.Add(new File("1701") { ModificationDate = new DateTime(2006, 10, 25, 0, 0, 0, DateTimeKind.Utc) });
                baseFSFolder2.Add(new File("2070") { ModificationDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
            }
            baseFS.Root.Add(baseFSFolder2);

            AnnoRDA.FileSystem overwriteFS = new FileSystem();
            var overwriteFSFolder1 = new AnnoRDA.Folder("Max Design");
            {
                overwriteFSFolder1.Add(new File("1503") { ModificationDate = new DateTime(2003, 3, 23, 0, 0, 0, DateTimeKind.Utc) });
            }
            overwriteFS.Root.Add(overwriteFSFolder1);
            var overwriteFSFolder2 = new AnnoRDA.Folder("Related Designs");
            {
                overwriteFSFolder2.Add(new File("1404") { ModificationDate = new DateTime(2009, 6, 25, 0, 0, 0, DateTimeKind.Utc) });
                overwriteFSFolder2.Add(new File("2070") { ModificationDate = new DateTime(2011, 11, 17, 0, 0, 0, DateTimeKind.Utc) });
            }
            overwriteFS.Root.Add(overwriteFSFolder2);
            var overwriteFSFolder3 = new AnnoRDA.Folder("Blue Byte");
            {
                overwriteFSFolder3.Add(new File("1404") { ModificationDate = new DateTime(2009, 6, 25, 0, 0, 0, DateTimeKind.Utc) });
                overwriteFSFolder3.Add(new File("2070") { ModificationDate = new DateTime(2011, 11, 17, 0, 0, 0, DateTimeKind.Utc) });
                overwriteFSFolder3.Add(new File("2205") { ModificationDate = new DateTime(2015, 11, 3, 0, 0, 0, DateTimeKind.Utc) });
            }
            overwriteFS.Root.Add(overwriteFSFolder3);

            baseFS.OverwriteWith(overwriteFS, null, System.Threading.CancellationToken.None);
            Assert.FolderAndFileCountAreEqual(3, 1, baseFS);
            Assert.ContainsFile(baseFS, new Assert.FileSpec("root file"));
            Assert.ContainsFile(baseFS, new Assert.FileSpec("Max Design", "1503") { ModificationDate = new DateTime(2003, 3, 23, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("Max Design", "1602") { ModificationDate = new DateTime(1998, 9, 24, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("Related Designs", "1701") { ModificationDate = new DateTime(2006, 10, 25, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("Related Designs", "1404") { ModificationDate = new DateTime(2009, 6, 25, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("Related Designs", "2070") { ModificationDate = new DateTime(2011, 11, 17, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("Blue Byte", "1404") { ModificationDate = new DateTime(2009, 6, 25, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("Blue Byte", "2070") { ModificationDate = new DateTime(2011, 11, 17, 0, 0, 0, DateTimeKind.Utc) });
            Assert.ContainsFile(baseFS, new Assert.FileSpec("Blue Byte", "2205") { ModificationDate = new DateTime(2015, 11, 3, 0, 0, 0, DateTimeKind.Utc) });
        }
    }
}
