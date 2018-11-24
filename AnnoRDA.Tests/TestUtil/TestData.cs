using System.IO;

namespace AnnoRDA.Tests.TestUtil
{
    internal static class TestData
    {
        internal static string GetPath(string filename)
        {
            return "../../../MockData/" + filename;
        }

        internal static Stream GetStream(string filename)
        {
            return new FileStream(GetPath(filename), FileMode.Open, FileAccess.Read);
        }

        internal static Stream GetStream(byte[] data)
        {
            return new MemoryStream(data);
        }

        internal static Stream GetStream()
        {
            return GetStream(new byte[0]);
        }

        internal static BinaryReader GetReader(string filename)
        {
            return new BinaryReader(GetStream(filename));
        }

        internal static BinaryReader GetReader(byte[] data)
        {
            return new BinaryReader(GetStream(data));
        }

        internal static BinaryReader GetReader()
        {
            return GetReader(new byte[0]);
        }

        internal static AnnoRDA.BlockContentsSource GetDummyBlockContentsSource()
        {
            var flags = new AnnoRDA.BlockContentsSource.BlockFlags(false, false, false, false);
            return new AnnoRDA.BlockContentsSource("dummy.rda", flags);
        }
    }
}
