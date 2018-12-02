using System;
using System.Collections.Generic;
using System.Linq;
using AnnoRDA.FileDB.Structs;

namespace AnnoRDA.FileDB.Reader
{
    public class FileSystemReader : System.IDisposable, IContentReaderDelegate
    {
        private readonly DBReader reader;

        private AnnoRDA.FileSystem fileSystem;
        private Writer.ArchiveFileMap archiveFiles;

        public FileSystemReader(DBReader reader)
        {
            this.reader = reader;
        }
        public FileSystemReader(System.IO.Stream stream, bool leaveOpen)
        : this(new DBReader(stream, leaveOpen))
        {}
        public FileSystemReader(System.IO.Stream stream)
        : this(stream, false)
        {}

        public void Dispose()
        {
            this.reader.Dispose();
        }

        public struct Result
        {
            public AnnoRDA.FileSystem FileSystem;
            public Writer.ArchiveFileMap ArchiveFiles; // TODO move from Writer namespace?
        }
        public Result ReadFileSystem()
        {
            this.fileSystem = new FileSystem();
            this.archiveFiles = new Writer.ArchiveFileMap();

            this.stateStack = new Stack<IState>();
            this.stateStack.Push(new RootState(this.fileSystem, this.archiveFiles));

            this.reader.ReadFile(this);

            return new Result { FileSystem = this.fileSystem, ArchiveFiles = this.archiveFiles };
        }

        void IContentReaderDelegate.OnStructureStart(Tag tag)
        {
            IState nextState = this.stateStack.Peek().OnStructureStart(tag.Name);
            if (nextState != null) {
                this.stateStack.Push(nextState);
            } else {
                throw new FormatException("Unexpected tag '" + tag.Name + "'");
            }
        }
        void IContentReaderDelegate.OnStructureEnd(Tag tag)
        {
            this.stateStack.Pop();
        }

        void IContentReaderDelegate.OnAttribute(Tag tag, AttributeValue value)
        {
            if ( !this.stateStack.Peek().OnAttribute(tag.Name, value) ) {
                throw new FormatException("Unexpected tag '" + tag.Name + "'");
            }
        }

        private enum State
        {
            Root,
            ArchiveMap,
            FileTree,
            PathMap,
            PathMapContents,
            FileMap,
            FileContents,
            ArchiveFiles,
            ArchiveFileContents,
            ResidentBuffers,
            ResidentBufferContents,
        }

        private Stack<IState> stateStack;

        private interface IState
        {
            IState OnStructureStart(string tagName);
            void OnStructureEnd(IState state);
            bool OnAttribute(string name, AttributeValue value);
        }

        private class RootState : IState
        {
            private AnnoRDA.FileSystem fileSystem;
            private Writer.ArchiveFileMap archiveFiles;

            public RootState(AnnoRDA.FileSystem fileSystem, Writer.ArchiveFileMap archiveFiles)
            {
                this.fileSystem = fileSystem;
                this.archiveFiles = archiveFiles;
            }

            public IState OnStructureStart(string tagName)
            {
                switch (tagName) {
                    case FileSystemTags.Structures.Names.ArchiveMap: {
                        return new ArchiveMapState(this.fileSystem, this.archiveFiles);
                    }
                    default: return null;
                }
            }
            public void OnStructureEnd(IState state)
            {}
            public bool OnAttribute(string name, AttributeValue value)
            {
                return false;
            }
        }

        private class ArchiveMapState : IState
        {
            private AnnoRDA.FileSystem fileSystem;
            private Writer.ArchiveFileMap archiveFiles;

            public ArchiveMapState(AnnoRDA.FileSystem fileSystem, Writer.ArchiveFileMap archiveFiles)
            {
                this.fileSystem = fileSystem;
                this.archiveFiles = archiveFiles;
            }

            public IState OnStructureStart(string tagName)
            {
                switch (tagName) {
                    case FileSystemTags.Structures.Names.FileTree: {
                        return new FileTreeState(this.fileSystem, this.archiveFiles);
                    }
                    case FileSystemTags.Structures.Names.ArchiveFiles: {
                        return new ArchiveFilesState(this.archiveFiles);
                    }
                    case FileSystemTags.Structures.Names.ResidentBuffers: {
                        return new ResidentBuffersState();
                    }
                    default: return null;
                }
            }
            public void OnStructureEnd(IState state)
            {}
            public bool OnAttribute(string name, AttributeValue value)
            {
                switch (name) {
                    case FileSystemTags.Attributes.Names.LastArchiveFile: {
                        string lastArchiveFileValue = value.GetUnicodeString();
                        string actualLastArchiveFile = this.archiveFiles.GetLastName();
                        if (lastArchiveFileValue != actualLastArchiveFile) {
                            throw new FormatException("LastArchiveFile '" + lastArchiveFileValue + "' does not match '" + actualLastArchiveFile + "'");
                        }
                        return true;
                    }
                    default: return false;
                }
            }
        }

        private class FileTreeState : IState
        {
            private AnnoRDA.FileSystem fileSystem;
            private Writer.ArchiveFileMap archiveFiles;

            public FileTreeState(AnnoRDA.FileSystem fileSystem, Writer.ArchiveFileMap archiveFiles)
            {
                this.fileSystem = fileSystem;
                this.archiveFiles = archiveFiles;
            }

            public IState OnStructureStart(string tagName)
            {
                switch (tagName) {
                    case FileSystemTags.Structures.Names.PathMap: {
                        return new PathMapState(this.fileSystem.Root, this.archiveFiles);
                    }
                    case FileSystemTags.Structures.Names.FileMap: {
                        return new FileMapState(this.fileSystem.Root, this.archiveFiles);
                    }
                    default: return null;
                }
            }
            public void OnStructureEnd(IState state)
            {}
            public bool OnAttribute(string name, AttributeValue value)
            {
                return false;
            }
        }

        private class PathMapState : IState
        {
            private AnnoRDA.Folder parentFolder;
            private Writer.ArchiveFileMap archiveFiles;

            private AnnoRDA.Folder currentFolder;

            public PathMapState(AnnoRDA.Folder parentFolder, Writer.ArchiveFileMap archiveFiles)
            {
                this.parentFolder = parentFolder;
                this.archiveFiles = archiveFiles;
            }

            public IState OnStructureStart(string tagName)
            {
                switch (tagName) {
                    case Tags.BuiltinTags.Names.List: {
                        return new PathMapContentsState(this.currentFolder, this.archiveFiles);
                    }
                    default: return null;
                }
            }
            public void OnStructureEnd(IState state)
            {
                this.currentFolder = null;
            }
            public bool OnAttribute(string name, AttributeValue value)
            {
                switch (name) {
                    case Tags.BuiltinTags.Names.String: {
                        this.currentFolder = new Folder(value.GetUnicodeString());
                        this.parentFolder.Add(this.currentFolder);
                        return true;
                    }
                    default: return false;
                }
            }
        }

        private class PathMapContentsState : IState
        {
            private AnnoRDA.Folder folder;
            private Writer.ArchiveFileMap archiveFiles;

            public PathMapContentsState(AnnoRDA.Folder folder, Writer.ArchiveFileMap archiveFiles)
            {
                this.folder = folder;
                this.archiveFiles = archiveFiles;
            }

            public IState OnStructureStart(string tagName)
            {
                switch (tagName) {
                    case FileSystemTags.Structures.Names.PathMap: {
                        return new PathMapState(this.folder, this.archiveFiles);
                    }
                    case FileSystemTags.Structures.Names.FileMap: {
                        return new FileMapState(this.folder, this.archiveFiles);
                    }
                    default: return null;
                }
            }
            public void OnStructureEnd(IState state)
            {}
            public bool OnAttribute(string name, AttributeValue value)
            {
                return false;
            }
        }

        private class FileMapState : IState
        {
            private AnnoRDA.Folder parentFolder;
            private Writer.ArchiveFileMap archiveFiles;

            private AnnoRDA.File currentFile;

            public FileMapState(AnnoRDA.Folder parentFolder, Writer.ArchiveFileMap archiveFiles)
            {
                this.parentFolder = parentFolder;
                this.archiveFiles = archiveFiles;
            }

            public IState OnStructureStart(string tagName)
            {
                switch (tagName) {
                    case Tags.BuiltinTags.Names.List: {
                        return new FileMapContentsState(this.currentFile, this.archiveFiles);
                    }
                    default: return null;
                }
            }
            public void OnStructureEnd(IState state)
            {
                this.currentFile = null;
            }
            public bool OnAttribute(string name, AttributeValue value)
            {
                switch (name) {
                    case Tags.BuiltinTags.Names.String: {
                        this.currentFile = new File(value.GetUnicodeString());
                        this.parentFolder.Add(this.currentFile);
                        return true;
                    }
                    default: return false;
                }
            }
        }

        private class FileMapContentsState : IState
        {
            private AnnoRDA.File file;
            private Writer.ArchiveFileMap archiveFiles;

            public FileMapContentsState(AnnoRDA.File file, Writer.ArchiveFileMap archiveFiles)
            {
                this.file = file;
                this.archiveFiles = archiveFiles;
            }

            public IState OnStructureStart(string tagName)
            {
                return null;
            }
            public void OnStructureEnd(IState state)
            {}
            public bool OnAttribute(string name, AttributeValue value)
            {
                switch (name) {
                    case FileSystemTags.Attributes.Names.FileName: {
                        // could check of the path matches?
                        return true;
                    }
                    case FileSystemTags.Attributes.Names.ArchiveFileIndex: {
                        // TODO
                        return true;
                    }
                    case FileSystemTags.Attributes.Names.Position: {
                        // TODO
                        return true;
                    }
                    case FileSystemTags.Attributes.Names.CompressedSize: {
                        // TODO
                        return true;
                    }
                    case FileSystemTags.Attributes.Names.UncompressedSize: {
                        // TODO
                        return true;
                    }
                    case FileSystemTags.Attributes.Names.ModificationTime: {
                        // TODO
                        return true;
                    }
                    case FileSystemTags.Attributes.Names.Flags: {
                        // TODO
                        return true;
                    }
                    case FileSystemTags.Attributes.Names.ResidentBufferIndex: {
                        // TODO
                        return true;
                    }
                    default: return false;
                }
            }
        }

        private class ArchiveFilesState : IState
        {
            private Writer.ArchiveFileMap archiveFiles;

            public ArchiveFilesState(Writer.ArchiveFileMap archiveFiles)
            {
                this.archiveFiles = archiveFiles;
            }

            public IState OnStructureStart(string tagName)
            {
                switch (tagName) {
                    case Tags.BuiltinTags.Names.List: {
                        return new ArchiveFileContentsState(this.archiveFiles);
                    }
                    default: return null;
                }
            }
            public void OnStructureEnd(IState state)
            {}
            public bool OnAttribute(string name, AttributeValue value)
            {
                return false;
            }
        }

        private class ArchiveFileContentsState : IState
        {
            private Writer.ArchiveFileMap archiveFiles;
            bool readPath = false;

            public ArchiveFileContentsState(Writer.ArchiveFileMap archiveFiles)
            {
                this.archiveFiles = archiveFiles;
            }

            public IState OnStructureStart(string tagName)
            {
                return null;
            }
            public void OnStructureEnd(IState state)
            {}
            public bool OnAttribute(string name, AttributeValue value)
            {
                switch (name) {
                    case Tags.BuiltinTags.Names.String: {
                        if (!this.readPath) {
                            string rdaFileName = value.GetUnicodeString();
                            this.archiveFiles.Add(rdaFileName, rdaFileName);
                            this.readPath = true;
                        }
                        return true;
                    }
                    default: return false;
                }
            }
        }

        private class ResidentBuffersState : IState
        {
            public ResidentBuffersState()
            {}

            public IState OnStructureStart(string tagName)
            {
                switch (tagName) {
                    case Tags.BuiltinTags.Names.List: {
                        return new ResidentBufferContentsState();
                    }
                    default: return null;
                }
            }
            public void OnStructureEnd(IState state)
            {}
            public bool OnAttribute(string name, AttributeValue value)
            {
                return false;
            }
        }

        private class ResidentBufferContentsState : IState
        {
            public ResidentBufferContentsState()
            {}

            public IState OnStructureStart(string tagName)
            {
                return null;
            }
            public void OnStructureEnd(IState state)
            {}
            public bool OnAttribute(string name, AttributeValue value)
            {
                switch (name) {
                    case FileSystemTags.Attributes.Names.Size: {
                        // TODO
                        return true;
                    }
                    case FileSystemTags.Attributes.Names.Buffer: {
                        // TODO
                        return true;
                    }
                    default: return false;
                }
            }
        }
    }
}
