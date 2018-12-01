using System.Collections.Generic;
using System.Linq;

using AnnoRDA.FileDB.Structs;

namespace AnnoRDA.FileDB.Writer
{
    public class FileSystemWriter : System.IDisposable
    {
        private readonly DBWriter writer;
        private readonly Tags tags = CreateTags();
        private readonly IComparer<string> fileNameComparer = new AnnoRDA.Util.InvariantIndividualCharacterStringComparer();

        public FileSystemWriter(DBWriter writer)
        {
            this.writer = writer;
        }
        public FileSystemWriter(System.IO.Stream stream, bool leaveOpen)
        : this(new DBWriter(stream, leaveOpen))
        { }
        public FileSystemWriter(System.IO.Stream stream)
        : this(stream, false)
        { }

        private static Tags CreateTags()
        {
            Tags tags = new Tags();
            tags.AddEntries(FileSystemTags.Structures.AllEntries);
            tags.AddEntries(FileSystemTags.Attributes.AllEntries);
            return tags;
        }

        public void Dispose()
        {
            this.writer.Dispose();
        }

        public void WriteFileSystem(AnnoRDA.FileSystem fileSystem, ArchiveFileMap archiveFiles)
        {
            this.WriteArchiveMap(fileSystem, archiveFiles);
            this.writer.FinalizeFile(this.tags);
        }

        private void WriteArchiveMap(AnnoRDA.FileSystem fileSystem, ArchiveFileMap archiveFiles)
        {
            this.WriteStructureStart(FileSystemTags.Structures.Names.ArchiveMap);

            IList<AnnoRDA.BlockContentsSource> residentBuffers = new List<AnnoRDA.BlockContentsSource>();
            this.WriteFileTree(fileSystem, archiveFiles, residentBuffers);
            this.WriteArchiveFiles(archiveFiles);
            this.WriteResidentBuffers(residentBuffers);

            this.WriteStructureEnd();
        }
        private void WriteFileTree(AnnoRDA.FileSystem fileSystem, ArchiveFileMap archiveFiles, IList<AnnoRDA.BlockContentsSource> residentBuffers)
        {
            this.WriteStructureStart(FileSystemTags.Structures.Names.FileTree);
            if (fileSystem.Root.Folders.Any()) {
                this.WritePathMap(fileSystem.Root.Folders, "", archiveFiles, residentBuffers);
            }
            if (fileSystem.Root.Files.Any()) {
                this.WriteFileMap(fileSystem.Root.Files, "", archiveFiles, residentBuffers);
            }
            this.WriteStructureEnd();
        }
        private void WritePathMap(IEnumerable<AnnoRDA.Folder> folders, string fullPathSoFar, ArchiveFileMap archiveFiles, IList<AnnoRDA.BlockContentsSource> residentBuffers)
        {
            this.WriteStructureStart(FileSystemTags.Structures.Names.PathMap);
            foreach (var folder in folders.OrderBy(folder => folder.Name, this.fileNameComparer)) {
                this.WriteGenericStringAttribute(folder.Name);
                this.WriteFolderContents(folder, fullPathSoFar + folder.Name + "/", archiveFiles, residentBuffers);
            }
            this.WriteStructureEnd();
        }
        private void WriteFolderContents(AnnoRDA.Folder folder, string fullPathSoFar, ArchiveFileMap archiveFiles, IList<AnnoRDA.BlockContentsSource> residentBuffers)
        {
            this.WriteListStart();
            if (folder.Folders.Any()) {
                this.WritePathMap(folder.Folders, fullPathSoFar, archiveFiles, residentBuffers);
            }
            if (folder.Files.Any()) {
                this.WriteFileMap(folder.Files, fullPathSoFar, archiveFiles, residentBuffers);
            }
            this.WriteListEnd();
        }
        private void WriteFileMap(IEnumerable<AnnoRDA.File> files, string fullPathSoFar, ArchiveFileMap archiveFiles, IList<AnnoRDA.BlockContentsSource> residentBuffers)
        {
            this.WriteStructureStart(FileSystemTags.Structures.Names.FileMap);
            foreach (var file in files.OrderBy(file => file.Name, this.fileNameComparer)) {
                this.WriteGenericStringAttribute(file.Name);
                this.WriteFileContents(file, fullPathSoFar + file.Name, archiveFiles, residentBuffers);
            }
            this.WriteStructureEnd();
        }
        private void WriteFileContents(AnnoRDA.File file, string fullFilePath, ArchiveFileMap archiveFiles, IList<AnnoRDA.BlockContentsSource> residentBuffers)
        {
            this.WriteListStart();

            this.WriteAttribute(FileSystemTags.Attributes.Names.FileName, (string)fullFilePath);

            int archiveFileIndex = archiveFiles.GetIndexForLoadPath(file.ContentsSource.BlockContentsSource.ArchiveFilePath);
            if (archiveFileIndex < 0) {
                throw new System.ArgumentException("archive file path not present in archive file map");
            }
            if (archiveFileIndex != 0) {
                this.WriteAttribute(FileSystemTags.Attributes.Names.ArchiveFileIndex, (int)archiveFileIndex);
            }

            if (file.ContentsSource.PositionInBlock != 0) {
                this.WriteAttribute(FileSystemTags.Attributes.Names.Position, (long)file.ContentsSource.PositionInBlock);
            }
            if (file.ContentsSource.CompressedSize != 0) {
                this.WriteAttribute(FileSystemTags.Attributes.Names.CompressedSize, (long)file.ContentsSource.CompressedSize);
            }
            if (file.ContentsSource.UncompressedSize != 0) {
                this.WriteAttribute(FileSystemTags.Attributes.Names.UncompressedSize, (long)file.ContentsSource.UncompressedSize);
            }
            if (file.ModificationTimestamp != 0) {
                this.WriteAttribute(FileSystemTags.Attributes.Names.ModificationTime, (long)file.ModificationTimestamp);
            }
            if (file.ContentsSource.BlockContentsSource.Flags.Value != 0) {
                this.WriteAttribute(FileSystemTags.Attributes.Names.Flags, (int)file.ContentsSource.BlockContentsSource.Flags.Value);
            }

            if (file.ContentsSource.BlockContentsSource.Flags.IsMemoryResident) {
                int residentBufferIndex = this.GetIndexInListAddIfMissing(residentBuffers, file.ContentsSource.BlockContentsSource);
                if (residentBufferIndex != 0) {
                    this.WriteAttribute(FileSystemTags.Attributes.Names.ResidentBufferIndex, (int)residentBufferIndex);
                }
            }

            this.WriteListEnd();
        }
        private int GetIndexInListAddIfMissing<T>(IList<T> list, T value)
        {
            int result = list.IndexOf(value);
            if (result == -1) {
                result = list.Count;
                list.Add(value);
            }
            return result;
        }
        private void WriteArchiveFiles(ArchiveFileMap archiveFiles)
        {
            this.WriteStructureStart(FileSystemTags.Structures.Names.ArchiveFiles);

            foreach (string archiveFile in archiveFiles.GetNames()) {
                this.WriteListStart();
                this.WriteGenericStringAttribute(archiveFile);
                this.WriteGenericStringAttribute("");
                this.WriteListEnd();
            }

            this.WriteStructureEnd();

            this.WriteAttribute(FileSystemTags.Attributes.Names.LastArchiveFile, (string)archiveFiles.GetLastName());
        }
        private void WriteResidentBuffers(IList<AnnoRDA.BlockContentsSource> residentBuffers)
        {
            this.WriteStructureStart(FileSystemTags.Structures.Names.ResidentBuffers);

            foreach (AnnoRDA.BlockContentsSource residentBuffer in residentBuffers) {
                this.WriteListStart();

                this.WriteAttribute(FileSystemTags.Attributes.Names.Size, (int)residentBuffer.CompressedSize);

                using (var stream = residentBuffer.GetRawReadStream()) {
                    using (var reader = new System.IO.BinaryReader(stream)) {
                        if (stream.Length != residentBuffer.CompressedSize) {
                            throw new System.ArgumentException("resident buffer stream length does not match CompresseSize", "residentBuffers");
                        }
                        byte[] bytes = reader.ReadBytes((int)residentBuffer.CompressedSize);
                        this.WriteAttribute(FileSystemTags.Attributes.Names.Buffer, (byte[])bytes);
                    }
                }

                this.WriteListEnd();
            }

            this.WriteStructureEnd();
        }

        #region Redirect to DBWriter

        private void WriteListStart()
        {
            this.writer.WriteTag(Tags.BuiltinTags.IDs.List);
        }
        private void WriteListEnd()
        {
            this.writer.WriteTag(Tags.BuiltinTags.IDs.StructureEnd);
        }
        private void WriteStructureStart(string tagName)
        {
            this.writer.WriteTag(this.GetStructureStartTagId(tagName));
        }
        private void WriteStructureEnd()
        {
            this.writer.WriteTag(Tags.BuiltinTags.IDs.StructureEnd);
        }

        private void WriteGenericStringAttribute(string value)
        {
            this.writer.WriteAttribute(Tags.BuiltinTags.IDs.String, value);
        }
        private void WriteAttribute(string tagName, string value)
        {
            this.writer.WriteAttribute(this.GetAttributeTagId(tagName), value);
        }
        private void WriteAttribute(string tagName, int value)
        {
            this.writer.WriteAttribute(this.GetAttributeTagId(tagName), value);
        }
        private void WriteAttribute(string tagName, long value)
        {
            this.writer.WriteAttribute(this.GetAttributeTagId(tagName), value);
        }
        private void WriteAttribute(string tagName, byte[] value)
        {
            this.writer.WriteAttribute(this.GetAttributeTagId(tagName), value);
        }

        private Tag GetTag(string tagName)
        {
            Tag tag;
            if (this.tags.TryGetTagByName(tagName, out tag))
            {
                return tag;
            }
            else
            {
                throw new System.ArgumentException("tagName");
            }
        }
        private ushort GetStructureStartTagId(string tagName)
        {
            Tag tag = GetTag(tagName);
            if (tag.Type != Tag.TagType.StructureStart)
            {
                throw new System.ArgumentException("tagName does not refer to a structure start");
            }
            return tag.ID;
        }
        private ushort GetAttributeTagId(string tagName)
        {
            Tag tag = GetTag(tagName);
            if (tag.Type != Tag.TagType.Attribute)
            {
                throw new System.ArgumentException("tagName does not refer to an attribute");
            }
            return tag.ID;
        }

        #endregion
    }
}
