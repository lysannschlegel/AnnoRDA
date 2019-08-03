using System;
using System.IO;
using AnnoRDA.FileEntities;
using System.Collections.Generic;
using System.Linq;
using AnnoRDA.IO.Compression;
using AnnoRDA.IO.Encryption;
using AnnoRDA.IO;

namespace AnnoRDA.Loader
{
    /// <summary>
    /// Loads an RDA container file (e.g. data1.rda).
    /// </summary>
    public class ContainerFileLoader
    {
        public class Context : IDisposable
        {
            public string ContainerFilePath { get; }
            public ContainerFileLoaderReader Reader { get; }
            public IFileHeaderTransformer FileHeaderTransformer { get; }
            public FileSystem FileSystem { get; }

            public Context(string containerFilePath, Stream stream, bool leaveOpen, IFileHeaderTransformer fileHeaderTransformer)
            {
                if (containerFilePath == null) {
                    throw new ArgumentNullException("containerFilePath");
                }
                if (fileHeaderTransformer == null) {
                    throw new ArgumentNullException("fileHeaderTransformer");
                }

                this.ContainerFilePath = containerFilePath;
                this.Reader = new ContainerFileLoaderReader(stream, leaveOpen);
                this.FileHeaderTransformer = fileHeaderTransformer;
                this.FileSystem = new FileSystem();
            }

            public void Dispose()
            {
                ((IDisposable)Reader).Dispose();
            }
        }

        private ContainerFileLoaderVerifier verifier;

        public ContainerFileLoader()
        {
            this.verifier = new ContainerFileLoaderVerifier();
        }

        public FileSystem Load(string path)
        {
            return this.Load(path, null, System.Threading.CancellationToken.None);
        }

        public FileSystem Load(string path, IProgress<string> progress, System.Threading.CancellationToken ct)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                using (var context = new Context(path, stream, true, new PassThroughFileHeaderTransformer())) {
                    return this.Load(context, progress, ct);
                }
            }
        }

        public FileSystem Load(Context context, IProgress<string> progress, System.Threading.CancellationToken ct)
        {
            context.Reader.ReadHeaderMagic();
            long nextBlockOffset = context.Reader.ReadFirstBlockOffset();

            int i = 0;
            while (nextBlockOffset < context.Reader.StreamLength) {
                if (progress != null) {
                    progress.Report("Block " + i);
                }

                FileEntities.BlockHeader block = context.Reader.ReadBlockHeader(nextBlockOffset);
                this.verifier.VerifyBlockHeader(block);

                this.LoadFileHeaders(context, block);

                nextBlockOffset = block.NextBlockOffset;
                i++;
            }

            return context.FileSystem;
        }

        public void LoadFileHeaders(Context context, BlockHeader block)
        {
            BlockContentsSource blockContentsSource = this.CreateBlockContentsSourceAndSeekToFileHeadersStart(context, block);

            BinaryReader fileHeaderReader = context.Reader.BaseReader;
            try {
                fileHeaderReader = this.GetReaderForPotentiallyEncryptedBlock(context, fileHeaderReader, block);
                fileHeaderReader = this.GetReaderForPotentiallyCompressedBlock(context, fileHeaderReader, block);
                using (var structureReader = new ContainerFileLoaderStructureReader(fileHeaderReader, leaveOpen: fileHeaderReader == context.Reader.BaseReader)) {
                    for (int fileIndex = 0; fileIndex < block.NumFiles; ++fileIndex) {
                        long? errorOffset = block.IsCompressed ? default(long?) : context.Reader.StreamPosition;
                        var fileHeader = structureReader.ReadFileHeader();
                        fileHeader = context.FileHeaderTransformer.Transform(fileHeader);
                        this.verifier.VerifyFileHeader(fileHeader, block.IsCompressed, errorOffset);

                        this.AddFileToFileSystem(context, fileHeader, blockContentsSource);
                    }
                }

            } finally {
                if (fileHeaderReader != context.Reader.BaseReader) {
                    fileHeaderReader.Dispose();
                }
            }
        }

        #region Add to FileSystem

        public AnnoRDA.File AddFileToFileSystem(Context context, FileHeader fileHeader, AnnoRDA.BlockContentsSource blockContentsSource)
        {
            string[] filePathComponents = fileHeader.Path.Split('/');
            AnnoRDA.File file = this.AddFileToFolder(context.FileSystem.Root, fileHeader, filePathComponents, blockContentsSource);

            this.AddContainedFilesToFileSystem(context, fileHeader, file, blockContentsSource);

            return file;
        }
        public AnnoRDA.File AddFileToFolder(AnnoRDA.Folder folder, FileHeader file, IEnumerable<string> filePathComponents, AnnoRDA.BlockContentsSource blockContentsSource)
        {
            if (!filePathComponents.Any()) {
                throw new ArgumentException("filePathComponents cannot be empty", "filePathComponents");
            }

            string currentName = filePathComponents.First();
            IEnumerable<string> filePathComponentsRemaining = filePathComponents.Skip(1);
            if (filePathComponentsRemaining.Any()) {
                AnnoRDA.Folder currentFolder = folder.Folders.FirstOrDefault((f) => f.Name == currentName);
                if (currentFolder == null) {
                    currentFolder = new Folder(currentName);
                    folder.Add(currentFolder);
                }
                return this.AddFileToFolder(currentFolder, file, filePathComponentsRemaining, blockContentsSource);

            } else {
                AnnoRDA.File rdaFile = new File(currentName) {
                    ModificationTimestamp = file.ModificationTimestamp,
                    ContentsSource = new AnnoRDA.FileContentsSource(blockContentsSource, file.DataOffset, file.CompressedFileSize, file.UncompressedFileSize),
                };
                folder.Add(rdaFile);
                return rdaFile;
            }
        }

        #endregion

        #region Container in File

        public static readonly string SUB_CONTAINER_SEPARATOR = "|";

        public void AddContainedFilesToFileSystem(Context context, FileHeader fileHeader, AnnoRDA.File file, AnnoRDA.BlockContentsSource blockContentsSource)
        {
            if (!IsContainerFile(file)) {
                return;
            }

            using (var subContainerStream = file.ContentsSource.GetReadStream()) {
                var transformer = new PrefixingFileHeaderTransformer(fileHeader.Path + SUB_CONTAINER_SEPARATOR, fileHeader.DataOffset);
                FileSystem subFileSystem;
                try {
                    using (var subContext = new Context(context.ContainerFilePath, subContainerStream, true, transformer)) {
                        subFileSystem = this.Load(subContext, null, System.Threading.CancellationToken.None);
                    }
                } catch (FormatException) {
                    // not a container file
                    return;
                }
                
                context.FileSystem.OverwriteWith(subFileSystem, null, System.Threading.CancellationToken.None);
            }
        }

        static readonly string[] CONTAINER_FILE_EXTENSIONS = { ".a6m", ".a7m", ".a7t" };
        public static bool IsContainerFile(AnnoRDA.File file)
        {
            if (file.Name == "rd3d.data") {
                return true;
            } else {
                string extension = GetExtension(file.Name);
                return CONTAINER_FILE_EXTENSIONS.Contains(extension);
            }
        }
        private static string GetExtension(string fileName)
        {
            // not using System.IO.Path.GetExtension since that will throw when there are '|' in the name
            const char EXTENSION_SEPARATOR_CHAR = '.';
            char[] chars = { EXTENSION_SEPARATOR_CHAR, Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            int lastIndex = fileName.LastIndexOfAny(chars);
            if (lastIndex > 0) {
                if (fileName[lastIndex] == EXTENSION_SEPARATOR_CHAR) {
                    return fileName.Substring(lastIndex);
                }
            }
            return string.Empty;
        }

        #endregion

        #region Util

        public BlockContentsSource CreateBlockContentsSourceAndSeekToFileHeadersStart(Context context, BlockHeader block)
        {
            BlockContentsSource.BlockFlags blockFlags = new BlockContentsSource.BlockFlags(block.IsCompressed, block.IsEncrypted, block.HasContiguousDataSection, block.IsDeleted);

            if (block.HasContiguousDataSection) {
                long contiguousDataInfoStart = block.Offset - 16;
                long fileHeadersStart = contiguousDataInfoStart - block.CompressedFileHeadersSize;
                
                var memResInfo = context.Reader.ReadMemoryResidentBlockInfo(contiguousDataInfoStart);
                long dataStart = fileHeadersStart - memResInfo.CompressedSize;
                var result = new BlockContentsSource(context.ContainerFilePath, blockFlags, dataStart, memResInfo.CompressedSize, memResInfo.UncompressedSize);

                context.Reader.StreamPosition = fileHeadersStart;

                return result;

            } else {
                long fileHeadersStart = block.Offset - block.CompressedFileHeadersSize;
                context.Reader.StreamPosition = fileHeadersStart;

                return new BlockContentsSource(context.ContainerFilePath, blockFlags);
            }
        }

        public BinaryReader GetReaderForPotentiallyEncryptedBlock(Context context, BinaryReader currentReader, BlockHeader block)
        {
            if (block.IsEncrypted) {
                Stream wrappedStream = null;
                try {
                    wrappedStream = new SubStream(currentReader.BaseStream, currentReader.BaseStream.Position, block.CompressedFileHeadersSize,
                                                  leaveOpen: currentReader == context.Reader.BaseReader);
                    wrappedStream = new EncryptionStream(wrappedStream, StreamAccessMode.Read);
                    return new BinaryReader(wrappedStream);
                } catch {
                    if (wrappedStream != null) {
                        wrappedStream.Dispose();
                    }
                    throw;
                }
            }
            return currentReader;
        }

        public BinaryReader GetReaderForPotentiallyCompressedBlock(Context context, BinaryReader currentReader, BlockHeader block)
        {
            if (block.IsCompressed) {
                ZlibStream zlibStream = null;
                try {
                    zlibStream = new ZlibStream(currentReader.BaseStream, System.IO.Compression.CompressionMode.Decompress,
                                                leaveOpen: currentReader == context.Reader.BaseReader);
                    return new BinaryReader(zlibStream);
                } catch {
                    if (zlibStream != null) {
                        zlibStream.Dispose();
                    }
                    throw;
                }
            }
            return currentReader;
        }

        #endregion
    }
}
