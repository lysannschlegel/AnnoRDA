using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace AnnoRDA.Loader
{
    /// <summary>
    /// Loads a collection of RDA container files into one file system.
    /// </summary>
    public class ContainerDirectoryLoader
    {
        private ContainerFileLoader fileLoader = new ContainerFileLoader();

        public FileSystem Load(string path)
        {
            return Load(path, CancellationToken.None);
        }

        public FileSystem Load(string path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            IEnumerable<string> containerPaths = Directory.GetFiles(path, "data*.rda");
            containerPaths = SortContainerPaths(containerPaths);

            FileSystem result = new FileSystem();

            foreach (string containerPath in containerPaths) {
                ct.ThrowIfCancellationRequested();

                FileSystem containerFileSystem = LoadContainerFileSystem(containerPath, ct);
                result.Merge(containerFileSystem, ct);
            }

            return result;
        }

        public static IEnumerable<string> SortContainerPaths(IEnumerable<string> paths)
        {
            return paths.OrderBy((p) => p, new Util.NaturalFilenameStringComparer());
        }

        private FileSystem LoadContainerFileSystem(string containerPath, CancellationToken ct)
        {
            return this.fileLoader.Load(containerPath, ct);
        }
    }
}
