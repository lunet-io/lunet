using System;
using System.IO;
using Zio;
using Zio.FileSystems;

namespace Lunet.Core
{
    public abstract class SiteOptions
    {
        public abstract IFileSystem BaseFileSystem { get; }

        public abstract IFileSystem TempFileSystem { get; }

        public abstract IFileSystem OutputFileSystem { get; }
    }

    public sealed class DefaultSiteOptions : SiteOptions
    {
        public const string DefaultTempFolder = ".lunet";
        public const string DefaultOutputFolder = DefaultTempFolder + "/www";

        public DefaultSiteOptions(string rootDirectory = null)
        {
            rootDirectory = Path.GetFullPath(rootDirectory ?? Directory.GetCurrentDirectory());

            if (!Directory.Exists(rootDirectory))
            {
                throw new DirectoryNotFoundException($"The directory `{rootDirectory}` was not found");
            }

            var basefs = new PhysicalFileSystem();
            BaseFileSystem = new SubFileSystem(basefs, basefs.ConvertPathFromInternal(rootDirectory));

            TempFileSystem = new SubFileSystem(BaseFileSystem, UPath.Root / DefaultTempFolder);

            OutputFileSystem = new SubFileSystem(BaseFileSystem, UPath.Root / DefaultOutputFolder);
        }

        public override IFileSystem BaseFileSystem { get; }

        public override IFileSystem TempFileSystem { get; }

        public override IFileSystem OutputFileSystem { get; }
    }

}