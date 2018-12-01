using System.Collections.Generic;

namespace AnnoRDA.FileDB.Structs
{
    public struct FileSystemTags
    {
        public struct Structures
        {
            public struct IDs
            {
                public const ushort ArchiveMap = 0x0002;
                public const ushort FileTree = 0x0003;
                public const ushort PathMap = 0x0004;
                public const ushort FileMap = 0x0005;
                public const ushort ArchiveFiles = 0x0006;
                public const ushort ResidentBuffers = 0x0007;
            }
            public struct Names
            {
                public const string ArchiveMap = "ArchiveMap";
                public const string FileTree = "FileTree";
                public const string PathMap = "PathMap";
                public const string FileMap = "FileMap";
                public const string ArchiveFiles = "ArchiveFiles";
                public const string ResidentBuffers = "ResidentBuffers";
            }
            public static readonly IDictionary<ushort, string> AllEntries = new Dictionary<ushort, string>
            {
                [IDs.ArchiveMap] = Names.ArchiveMap,
                [IDs.FileTree] = Names.FileTree,
                [IDs.PathMap] = Names.PathMap,
                [IDs.FileMap] = Names.FileMap,
                [IDs.ArchiveFiles] = Names.ArchiveFiles,
                [IDs.ResidentBuffers] = Names.ResidentBuffers,
            };
        }

        public struct Attributes
        {
            public struct IDs
            {
                public const ushort FileName = 0x8001;
                public const ushort ArchiveFileIndex = 0x8002;
                public const ushort Position = 0x8003;
                public const ushort CompressedSize = 0x8004;
                public const ushort UncompressedSize = 0x8005;
                public const ushort ModificationTime = 0x8006;
                public const ushort Flags = 0x8007;
                public const ushort ResidentBufferIndex = 0x8008;
                public const ushort LastArchiveFile = 0x8009;
                public const ushort Size = 0x800A;
                public const ushort Buffer = 0x800B;
            }
            public struct Names
            {
                public const string FileName = "FileName";
                public const string ArchiveFileIndex = "ArchiveFileIndex";
                public const string Position = "Position";
                public const string CompressedSize = "CompressedSize";
                public const string UncompressedSize = "UncompressedSize";
                public const string ModificationTime = "ModificationTime";
                public const string Flags = "Flags";
                public const string ResidentBufferIndex = "ResidentBufferIndex";
                public const string LastArchiveFile = "LastArchiveFile";
                public const string Size = "Size";
                public const string Buffer = "Buffer";
            }
            public static readonly IDictionary<ushort, string> AllEntries = new Dictionary<ushort, string>
            {
                [IDs.FileName] = Names.FileName,
                [IDs.ArchiveFileIndex] = Names.ArchiveFileIndex,
                [IDs.Position] = Names.Position,
                [IDs.CompressedSize] = Names.CompressedSize,
                [IDs.UncompressedSize] = Names.UncompressedSize,
                [IDs.ModificationTime] = Names.ModificationTime,
                [IDs.Flags] = Names.Flags,
                [IDs.ResidentBufferIndex] = Names.ResidentBufferIndex,
                [IDs.LastArchiveFile] = Names.LastArchiveFile,
                [IDs.Size] = Names.Size,
                [IDs.Buffer] = Names.Buffer,
            };
        }
    }
}
