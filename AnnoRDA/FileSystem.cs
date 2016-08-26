using AnnoRDA.IO;
using AnnoRDA.IO.Compression;
using AnnoRDA.IO.Encryption;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AnnoRDA
{
    public class FileSystem
    {
        public Folder Root { get; set; } = new Folder("");

        public async System.Threading.Tasks.Task<FileSystem> GetFileSystemByMerging(FileSystem overwriteFS, System.Threading.CancellationToken ct)
        {
            Folder newRoot = await this.Root.GetFolderByMerging(overwriteFS.Root, ct);
            return new FileSystem() { Root = newRoot };
        }

        public async System.Threading.Tasks.Task OverwriteWith(FileSystem overwriteFS, System.Threading.CancellationToken ct)
        {
            await this.Root.OverwriteWith(overwriteFS.Root, ct);
        }
    }

    public interface IFileSystemItem
    {
        string Name { get; set; }
        
        IEnumerable<IFileSystemItem> Children { get; }
        int ChildCount { get; }
    }

    public class Folder : IFileSystemItem
    {
        public string Name { get; set; }

        private List<Folder> folders = new List<Folder>();
        public IEnumerable<Folder> Folders { get { return folders; } }
        private List<File> files = new List<File>();
        public IEnumerable<File> Files { get { return files; } }
        
        public IEnumerable<IFileSystemItem> Children { get { return ((IEnumerable<IFileSystemItem>)this.folders).Concat(files); } }
        public int ChildCount { get { return this.folders.Count + this.files.Count; } }

        public Folder(string name)
        {
            this.Name = name;
        }

        public void Add(Folder folder, AddMode addMode = AddMode.NewOrReplace)
        {
            var existingFolder = this.folders.FirstOrDefault((f) => f.Name == folder.Name);

            if (existingFolder != null && addMode == AddMode.New) {
                throw new ArgumentException("A folder with this name already exists", "folder");
            } else if (existingFolder == null && addMode == AddMode.Replace) {
                throw new ArgumentException("No such folder exists", "folder");
            }

            if (existingFolder != null) {
                this.folders.Remove(existingFolder);
            }
            this.folders.Add(folder);
        }
        public void Add(File file, AddMode addMode = AddMode.NewOrReplace)
        {
            var existingFile = this.files.FirstOrDefault((f) => f.Name == file.Name);

            if (existingFile != null && addMode == AddMode.New) {
                throw new ArgumentException("A file with this name already exists", "file");
            } else if (existingFile == null && addMode == AddMode.Replace) {
                throw new ArgumentException("No such file exists", "file");
            }

            if (existingFile != null) {
                this.files.Remove(existingFile);
            }
            this.files.Add(file);
        }

        public enum AddMode
        {
            /// <summary>
            /// Add a new item. An item with this name must not exist yet.
            /// </summary>
            New,
            /// <summary>
            /// Replace an existing item. An item with this name and type must already exist.
            /// </summary>
            Replace,
            /// <summary>
            /// If no item with this name and type does not exist yet, add the item. Else replace the existing item.
            /// </summary>
            NewOrReplace,
        }

        public async System.Threading.Tasks.Task<Folder> GetFolderByMerging(Folder overwriteFolder, System.Threading.CancellationToken ct)
        {
            Folder result = new Folder(this.Name);

            foreach (var overwriteSubFolder in overwriteFolder.Folders) {
                ct.ThrowIfCancellationRequested();

                var baseSubFolder = result.Folders.FirstOrDefault((f) => f.Name == overwriteSubFolder.Name);
                if (baseSubFolder == null) {
                    baseSubFolder = new Folder(overwriteSubFolder.Name);
                }

                Folder newSubFolder = await baseSubFolder.GetFolderByMerging(overwriteSubFolder, ct);
                result.Add(newSubFolder, AddMode.New);
            }

            foreach (var overwriteFile in overwriteFolder.Files) {
                ct.ThrowIfCancellationRequested();

                result.Add(overwriteFile.DeepClone());
            }

            return result;
        }

        public async System.Threading.Tasks.Task OverwriteWith(Folder overwriteFolder, System.Threading.CancellationToken ct)
        {
            foreach (var overwriteSubFolder in overwriteFolder.Folders) {
                ct.ThrowIfCancellationRequested();

                var baseSubFolder = this.Folders.FirstOrDefault((f) => f.Name == overwriteSubFolder.Name);
                if (baseSubFolder == null) {
                    baseSubFolder = new Folder(overwriteSubFolder.Name);
                    this.Add(baseSubFolder);
                }

                await baseSubFolder.OverwriteWith(overwriteSubFolder, ct);
            }

            foreach (var overwriteFile in overwriteFolder.Files) {
                ct.ThrowIfCancellationRequested();

                this.Add(overwriteFile.DeepClone());
            }
        }
    }

    public class File : IFileSystemItem
    {
        public string Name { get; set; }

        public IEnumerable<IFileSystemItem> Children { get { return Enumerable.Empty<IFileSystemItem>(); } }
        public int ChildCount { get { return 0; } }

        public long ModificationTimestamp { get; set; }
        public DateTime ModificationDate {
            get {
                DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dateTime = dateTime.AddSeconds(this.ModificationTimestamp);
                return dateTime;
            }
            set {
                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                TimeSpan diff = value.ToUniversalTime() - origin;
                this.ModificationTimestamp = (long)Math.Floor(diff.TotalSeconds);
            }
        }

        public FileContentsSource ContentsSource { get; set; }

        public File(string name)
        {
            this.Name = name;
        }

        public File DeepClone()
        {
            return new File(this.Name) {
                ModificationTimestamp = this.ModificationTimestamp,
                ContentsSource = this.ContentsSource != null ? this.ContentsSource.DeepClone() : null,
            };
        }
    }

    public class FileContentsSource
    {
        public BlockContentsSource BlockContentsSource { get; }

        public long PositionInBlock { get; }

        public long GlobalPosition {
            get {
                if (this.BlockContentsSource.Flags.IsMemoryResident && (this.BlockContentsSource.Flags.IsCompressed || this.BlockContentsSource.Flags.IsEncrypted)) {
                    throw new NotSupportedException("GlobalPosition on files in memory-resident blocks which are also compressed or encrypted is not meaningful");
                }
                return this.BlockContentsSource.Position + this.PositionInBlock;
            }
        }

        public long CompressedSize { get; }
        public long UncompressedSize { get; }

        public FileContentsSource(BlockContentsSource blockContentsSource, long positionInBlock, long compressedSize, long uncompressedSize)
        {
            if (blockContentsSource == null) {
                throw new ArgumentNullException("blockContentsSource");
            }
            if (positionInBlock < 0) {
                throw new ArgumentOutOfRangeException("positionInBlock cannot be negative.", "positionInBlock");
            }
            if (compressedSize < 0) {
                throw new ArgumentOutOfRangeException("compressedSize cannot be negative.", "compressedSize");
            }
            if (uncompressedSize < 0) {
                throw new ArgumentOutOfRangeException("uncompressedSize cannot be negative.", "uncompressedSize");
            }

            this.BlockContentsSource = blockContentsSource;
            this.PositionInBlock = positionInBlock;
            this.CompressedSize = compressedSize;
            this.UncompressedSize = uncompressedSize;
        }

        public Stream GetReadStream()
        {
            Stream stream = null;
            try {
                stream = this.BlockContentsSource.GetReadStream();
                stream = new SubStream(stream, this.PositionInBlock, this.CompressedSize);
                if (!this.BlockContentsSource.Flags.IsMemoryResident) {
                    if (this.BlockContentsSource.Flags.IsEncrypted) {
                        stream = new EncryptionStream(stream, StreamAccessMode.Read);
                    }
                    if (this.BlockContentsSource.Flags.IsCompressed) {
                        stream = new ZlibStream(stream, System.IO.Compression.CompressionMode.Decompress);
                    }
                }
                return stream;
            } catch {
                if (stream != null) {
                    stream.Dispose();
                }
                throw;
            }
        }

        public FileContentsSource DeepClone()
        {
            return new FileContentsSource(
                this.BlockContentsSource,
                this.PositionInBlock,
                this.CompressedSize,
                this.UncompressedSize
            );
        }
    }
    public class BlockContentsSource
    {
        public string ArchiveFilePath { get; }

        public struct BlockFlags
        {
            public int Value { get; }

            public bool IsCompressed {
                get {
                    return (this.Value & 0x1) != 0;
                }
            }
            public bool IsEncrypted {
                get {
                    return (this.Value & 0x2) != 0;
                }
            }
            public bool IsMemoryResident {
                get {
                    return (this.Value & 0x4) != 0;
                }
            }
            public bool IsDeleted {
                get {
                    return (this.Value & 0x8) != 0;
                }
            }

            public BlockFlags(int value)
            {
                this.Value = value;
            }
            public BlockFlags(bool isCompressed, bool isEncrypted, bool isMemoryResident, bool isDeleted)
            {
                this.Value = (isCompressed ? 0x1 : 0) | (isEncrypted ? 0x2 : 0) | (isMemoryResident ? 0x4 : 0) | (isDeleted ? 0x8 : 0);
            }
        }
        public BlockFlags Flags { get; }

        public long Position { get; }

        public long CompressedSize { get; }
        public long UncompressedSize { get; }

        public BlockContentsSource(string archiveFilePath, BlockFlags flags, long position, long compressedSize, long uncompressedSize)
        {
            if (archiveFilePath == null) {
                throw new ArgumentNullException("archiveFilePath");
            }
            if (position < 0) {
                throw new ArgumentOutOfRangeException("position cannot be negative.", "position");
            }
            if (compressedSize < 0) {
                throw new ArgumentOutOfRangeException("compressedSize cannot be negative.", "compressedSize");
            }
            if (uncompressedSize < 0) {
                throw new ArgumentOutOfRangeException("uncompressedSize cannot be negative.", "uncompressedSize");
            }

            this.ArchiveFilePath = archiveFilePath;
            this.Flags = flags;
            this.Position = position;
            this.CompressedSize = compressedSize;
            this.UncompressedSize = uncompressedSize;
        }
        public BlockContentsSource(string archiveFilePath, BlockFlags flags)
        {
            if (archiveFilePath == null) {
                throw new ArgumentNullException("archiveFilePath");
            }
            if (flags.IsMemoryResident) {
                throw new ArgumentOutOfRangeException("must provide position, compressedSize and uncompressedSize when memory resident", "flags.IsMemoryResident");
            }
            
            this.ArchiveFilePath = archiveFilePath;
            this.Flags = flags;
            this.Position = 0;
            this.CompressedSize = 0;
            this.UncompressedSize = 0;
        }

        public Stream GetRawReadStream()
        {
            Stream stream = null;
            try {
                stream = new FileStream(this.ArchiveFilePath, FileMode.Open, FileAccess.Read);
                if (this.Flags.IsMemoryResident) {
                    stream = new SubStream(stream, this.Position, this.CompressedSize);
                    using (var tempReader = new BinaryReader(stream)) {
                        stream = new MemoryStream(tempReader.ReadBytes((int)this.UncompressedSize));
                    }
                }
                return stream;
            } catch {
                if (stream != null) {
                    stream.Dispose();
                }
                throw;
            }
        }
        public Stream GetReadStream()
        {
            Stream stream = null;
            try {
                stream = new FileStream(this.ArchiveFilePath, FileMode.Open, FileAccess.Read);
                if (this.Flags.IsMemoryResident) {
                    stream = new SubStream(stream, this.Position, this.CompressedSize);
                    if (this.Flags.IsEncrypted) {
                        stream = new EncryptionStream(stream, StreamAccessMode.Read);
                    }
                    if (this.Flags.IsCompressed) {
                        stream = new ZlibStream(stream, System.IO.Compression.CompressionMode.Decompress);
                    }
                    using (var tempReader = new BinaryReader(stream)) {
                        stream = new MemoryStream(tempReader.ReadBytes((int)this.UncompressedSize));
                    }
                }
                return stream;
            } catch {
                if (stream != null) {
                    stream.Dispose();
                }
                throw;
            }
        }

        public BlockContentsSource DeepClone()
        {
            return new BlockContentsSource(
                this.ArchiveFilePath,
                this.Flags,
                this.Position,
                this.CompressedSize,
                this.UncompressedSize
            );
        }
    }
}
