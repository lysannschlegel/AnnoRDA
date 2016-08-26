using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AnnoRDA.Loader
{
    /// <summary>
    /// Loads a collection of RDA container files into one file system.
    /// </summary>
    public class ContainerDirectoryLoader
    {
        private ContainerFileLoader fileLoader = new ContainerFileLoader();

        public struct Result
        {
            public FileSystem FileSystem { get; }
            public IEnumerable<string> ContainerPaths { get; }

            public Result(FileSystem fileSystem, IEnumerable<string> containerPaths)
            {
                this.FileSystem = fileSystem;
                this.ContainerPaths = containerPaths;
            }
        }

        public async System.Threading.Tasks.Task<Result> Load(string path)
        {
            return await Load(path, System.Threading.CancellationToken.None);
        }

        public async System.Threading.Tasks.Task<Result> Load(string path, System.Threading.CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            IEnumerable<string> containerPaths = Directory.GetFiles(path, "data*.rda");
            containerPaths = SortContainerPaths(containerPaths);

            FileSystem fileSystem = new FileSystem();

            foreach (string containerPath in containerPaths) {
                ct.ThrowIfCancellationRequested();

                FileSystem containerFileSystem = await LoadContainerFileSystem(containerPath, ct);
                fileSystem = await fileSystem.GetFileSystemByMerging(containerFileSystem, ct);
            }

            return new Result(fileSystem, containerPaths);
        }

        public static IEnumerable<string> SortContainerPaths(IEnumerable<string> paths)
        {
            return paths.OrderBy((p) => p, new Util.NaturalFilenameStringComparer());
        }

        private async System.Threading.Tasks.Task<FileSystem> LoadContainerFileSystem(string containerPath, System.Threading.CancellationToken ct)
        {
            return await this.fileLoader.Load(containerPath, ct);
        }
    }
}
