using System;
using System.IO;
using System.Text;

namespace AnnoRDA.Loader
{
    public class ContainerFileLoaderReader : IDisposable
    {
        public BinaryReader BaseReader { get { return this.reader.BaseReader; } }
        private ContainerFileLoaderStructureReader reader;

        public long StreamLength { get { return this.BaseReader.BaseStream.Length; } }
        public long StreamPosition {
            get {
                return this.BaseReader.BaseStream.Position;
            }
            set {
                this.BaseReader.BaseStream.Position = value;
            }
        }

        public ContainerFileLoaderReader(Stream stream, bool leaveOpen)
        {
            if (stream == null) {
                throw new ArgumentNullException("stream");
            }
            if (!stream.CanRead) {
                throw new ArgumentException("The stream must support reading.", "stream");
            }
            if (!stream.CanSeek) {
                throw new ArgumentException("The stream must support seeking.", "stream");
            }

            var baseReader = new BinaryReader(stream, Encoding.UTF8, leaveOpen);
            this.reader = new ContainerFileLoaderStructureReader(baseReader, false);
        }

        public void Dispose()
        {
            ((IDisposable)this.BaseReader).Dispose();
        }

        #region Structures

        public void ReadHeaderMagic()
        {
            long errorOffset = 0;
            this.StreamPosition = errorOffset;

            byte[] headerBytes = this.reader.ReadBytes(18, FileFormatException.EntityType.RDAHeader);

            string headerString = Encoding.UTF8.GetString(headerBytes);
            if (headerString != "Resource File V2.2") {
                throw new FileFormatException(FileFormatException.EntityType.RDAHeader, FileFormatException.Error.InvalidValue, errorOffset);
            }
        }

        public long ReadFirstBlockOffset()
        {
            this.StreamPosition = 784;
            return this.reader.ReadInt64(FileFormatException.EntityType.RDAHeader);
        }

        public FileEntities.BlockHeader ReadBlockHeader(long offset)
        {
            this.StreamPosition = offset;
            var result = this.reader.ReadBlockHeader();
            result.Offset = offset;
            return result;
        }

        public FileEntities.MemoryResidentBlockInfo ReadMemoryResidentBlockInfo(long offset)
        {
            this.StreamPosition = offset;
            return this.reader.ReadMemoryResidentBlockInfo();
        }

        #endregion
    }

    public class ContainerFileLoaderStructureReader : IDisposable
    {
        public BinaryReader BaseReader { get; }
        private bool leaveOpen;

        public ContainerFileLoaderStructureReader(BinaryReader baseReader, bool leaveOpen)
        {
            if (baseReader == null) {
                throw new ArgumentNullException("baseReader");
            }

            this.BaseReader = baseReader;
            this.leaveOpen = leaveOpen;
        }

        public void Dispose()
        {
            if (!this.leaveOpen) {
                ((IDisposable)this.BaseReader).Dispose();
            }
        }

        #region Structures

        public FileEntities.BlockHeader ReadBlockHeader()
        {
            return new FileEntities.BlockHeader() {
                Flags = this.ReadInt32(FileFormatException.EntityType.BlockHeader),
                NumFiles = this.ReadUInt32(FileFormatException.EntityType.BlockHeader),
                CompressedFileHeadersSize = this.ReadInt64(FileFormatException.EntityType.BlockHeader),
                UncompressedFileHeadersSize = this.ReadInt64(FileFormatException.EntityType.BlockHeader),
                NextBlockOffset = this.ReadInt64(FileFormatException.EntityType.BlockHeader),
            };
        }

        public FileEntities.MemoryResidentBlockInfo ReadMemoryResidentBlockInfo()
        {
            return new FileEntities.MemoryResidentBlockInfo() {
                CompressedSize = this.ReadInt64(FileFormatException.EntityType.FileHeader),
                UncompressedSize = this.ReadInt64(FileFormatException.EntityType.FileHeader),
            };
        }

        public FileEntities.FileHeader ReadFileHeader()
        {
            // // For some reason the DeflateStream used by ZlibStream will sometimes hit EOF prematurely if we read only
            // // small parts at a time. But apparently we can work around this by reading the full file header first into
            // // a buffer and proceed by reading from the buffer.
            using (var memoryReader = new BinaryReader(new MemoryStream(this.BaseReader.ReadBytes(560)))) {
                using (var memoryStructReader = new ContainerFileLoaderStructureReader(memoryReader, true)) {
                    var result = new FileEntities.FileHeader() {
                        Path = memoryStructReader.ReadUTF16String(520, FileFormatException.EntityType.FileHeader),
                        DataOffset = memoryStructReader.ReadInt64(FileFormatException.EntityType.FileHeader),
                        CompressedFileSize = memoryStructReader.ReadInt64(FileFormatException.EntityType.FileHeader),
                        UncompressedFileSize = memoryStructReader.ReadInt64(FileFormatException.EntityType.FileHeader),
                        ModificationTimestamp = memoryStructReader.ReadInt64(FileFormatException.EntityType.FileHeader),
                    };
                    memoryStructReader.ReadInt64(FileFormatException.EntityType.FileHeader); // skipped
                    return result;
                }
            }
        }

        #endregion

        #region Primitives

        public byte[] ReadBytes(int count, FileFormatException.EntityType entityType)
        {
            byte[] result = this.BaseReader.ReadBytes(count);

            if (result.Length < count) {
                throw new FileFormatException(entityType, FileFormatException.Error.UnexpectedEndOfFile, this.CurrentPosition);
            }

            return result;
        }

        public string ReadUTF16String(int numBytes, FileFormatException.EntityType entityType)
        {
            long? errorOffset = this.CurrentPosition;
            byte[] bytes = this.ReadBytes(numBytes, entityType);
            string result;
            try {
                result = Encoding.Unicode.GetString(bytes);
            } catch (ArgumentException ex) {
                throw new FileFormatException(entityType, FileFormatException.Error.InvalidValue, errorOffset, null, ex);
            }
            result = result.TrimEnd('\0');
            return result;
        }

        public int ReadInt32(FileFormatException.EntityType entityType)
        {
            try {
                int result = this.BaseReader.ReadInt32();
                return result;

            } catch (EndOfStreamException) {
                throw new FileFormatException(entityType, FileFormatException.Error.UnexpectedEndOfFile, this.CurrentPosition);
            }
        }
        public uint ReadUInt32(FileFormatException.EntityType entityType)
        {
            try {
                uint result = this.BaseReader.ReadUInt32();
                return result;

            } catch (EndOfStreamException) {
                throw new FileFormatException(entityType, FileFormatException.Error.UnexpectedEndOfFile, this.CurrentPosition);
            }
        }

        public long ReadInt64(FileFormatException.EntityType entityType)
        {
            try {
                long result = this.BaseReader.ReadInt64();
                return result;

            } catch (EndOfStreamException) {
                throw new FileFormatException(entityType, FileFormatException.Error.UnexpectedEndOfFile, this.CurrentPosition);
            }
        }
        public ulong ReadUInt64(FileFormatException.EntityType entityType)
        {
            try {
                ulong result = this.BaseReader.ReadUInt64();
                return result;

            } catch (EndOfStreamException) {
                throw new FileFormatException(entityType, FileFormatException.Error.UnexpectedEndOfFile, this.CurrentPosition);
            }
        }

        #endregion

        #region Util

        public long? CurrentPosition
        {
            get {
                if (this.BaseReader.BaseStream.CanSeek) {
                    return this.BaseReader.BaseStream.Position;
                } else {
                    return null;
                }
            }
        }

        #endregion
    }
}
