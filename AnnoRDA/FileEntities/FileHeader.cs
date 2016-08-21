namespace AnnoRDA.FileEntities
{
    public struct FileHeader
    {
        public string Path { get; set; }

        public long DataOffset { get; set; }
        public long CompressedFileSize { get; set; }
        public long UncompressedFileSize { get; set; }

        public long ModificationTimestamp { get; set; }
    }
}
