using AnnoRDA.FileEntities;

namespace AnnoRDA.Loader
{
    public interface IFileHeaderTransformer
    {
        FileHeader Transform(FileHeader orig);
    }

    public class PassThroughFileHeaderTransformer : IFileHeaderTransformer
    {
        public FileHeader Transform(FileHeader orig)
        {
            return orig;
        }
    }
    
    public class PrefixingFileHeaderTransformer : IFileHeaderTransformer
    {
        private string pathPrefix;
        private long dataOffsetPrefix;

        public PrefixingFileHeaderTransformer(string pathPrefix, long dataOffsetPrefix)
        {
            this.pathPrefix = pathPrefix;
            this.dataOffsetPrefix = dataOffsetPrefix;
        }

        public FileHeader Transform(FileHeader orig)
        {
            orig.Path = this.pathPrefix + orig.Path;
            orig.DataOffset = this.dataOffsetPrefix + orig.DataOffset;
            return orig;
        }
    }
}
