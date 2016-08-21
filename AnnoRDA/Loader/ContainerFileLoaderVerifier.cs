namespace AnnoRDA.Loader
{
    public class ContainerFileLoaderVerifier
    {
        public void VerifyBlockHeader(FileEntities.BlockHeader block)
        {
            if (block.UncompressedFileHeadersSize != block.NumFiles * 560) {
                throw new FileFormatException(FileFormatException.EntityType.BlockHeader, FileFormatException.Error.InvalidValue, block.Offset, "The file headers size does not match the number of files.");

            } else if (!block.IsCompressed && block.CompressedFileHeadersSize != block.UncompressedFileHeadersSize) {
                throw new FileFormatException(FileFormatException.EntityType.BlockHeader, FileFormatException.Error.InvalidValue, block.Offset, "The compressed file headers size should match the uncompressed size when compression is disabled.");

            } else if (block.Offset < 792 + block.CompressedFileHeadersSize) {
                throw new FileFormatException(FileFormatException.EntityType.BlockHeader, FileFormatException.Error.InvalidValue, block.Offset, "The file header offset must be after the end of the RDA header.");
            }
        }

        public void VerifyFileHeader(FileEntities.FileHeader fileHeader, bool blockIsCompressed, long? errorOffset)
        {
            if (!blockIsCompressed && fileHeader.CompressedFileSize != fileHeader.UncompressedFileSize) {
                throw new FileFormatException(FileFormatException.EntityType.FileHeader, FileFormatException.Error.InvalidValue, errorOffset, "The compressed file size should match the uncompressed size when compression is disabled.");
            }
        }
    }
}
