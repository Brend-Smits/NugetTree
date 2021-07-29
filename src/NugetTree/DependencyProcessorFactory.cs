using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NugetTree.Core;
using NugetTree.Implementation;

namespace NugetTree
{
    public class DependencyProcessorFactory
    {
        private const string PackageSource = "https://api.nuget.org/v3/index.json";

        public static async Task<IDependencyProcessor> CreateDependencyProcessorAsync(LogLevel logLevel)
        {
            IProjectReader reader = await CreateProjectReaderAsync(logLevel);
            IProjectWriter writer = CreateProjectWriter();

            return new DependencyProcessor(reader, writer);
        }

        private static async Task<IProjectReader> CreateProjectReaderAsync(LogLevel logLevel)
        {
            ProjectReader baseReader = new ProjectReader();

            IEnumerable<Lazy<INuGetResourceProvider>> v3Providers = Repository.Provider.GetCoreV3();
            PackageSource v3PackageSource = new PackageSource(PackageSource);

            SourceRepository v3Repository = new SourceRepository(v3PackageSource, v3Providers);

            PackageMetadataResource metadataResource = await v3Repository.GetResourceAsync<PackageMetadataResource>();
            DependencyInfoResource dependencyResource = await v3Repository.GetResourceAsync<DependencyInfoResource>();

            SourceCacheContext cacheContext = new SourceCacheContext();
            ConsoleLogger logger = new ConsoleLogger(logLevel);

            return new ProjectReaderNugetDependencyDecorator(
                baseReader,
                metadataResource,
                dependencyResource,
                cacheContext,
                logger);
        }

        private static IProjectWriter CreateProjectWriter()
        {
            return new ProjectWriter();
        }
    }
}