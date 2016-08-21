namespace AnnoRDA.FileEntities
{
    public struct BlockHeader
    {
        public long Offset { get; set; }

        public int Flags { get; set; }
        public bool IsEncrypted {
            get {
                return (this.Flags & 2) != 0;
            }
            set {
                if (value) {
                    this.Flags |= 2;
                } else {
                    this.Flags &= ~2;
                }
            }
        }
        public bool IsCompressed {
            get {
                return (this.Flags & 1) != 0;
            }
            set {
                if (value) {
                    this.Flags |= 1;
                } else {
                    this.Flags &= ~1;
                }
            }
        }
        public bool HasContiguousDataSection {
            get {
                return (this.Flags & 4) != 0;
            }
            set {
                if (value) {
                    this.Flags |= 4;
                } else {
                    this.Flags &= ~4;
                }
            }
        }
        public bool IsDeleted {
            get {
                return (this.Flags & 8) != 0;
            }
            set {
                if (value) {
                    this.Flags |= 8;
                } else {
                    this.Flags &= ~8;
                }
            }
        }

        public uint NumFiles { get; set; }
        public long CompressedFileHeadersSize { get; set; }
        public long UncompressedFileHeadersSize { get; set; }

        public long NextBlockOffset { get; set; }
    }
}
