using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NugetTree.Core;
using NugetTree.Models;

namespace NugetTree.Implementation
{
    public class ProjectReaderNugetDependencyDecorator : IProjectReader
    {
        private const bool IncludePrerelease = false;
        private const string PackagesConfigFilename = "packages.config";

        private readonly IProjectReader _base;
        private readonly SourceCacheContext _cacheContext;
        private readonly CancellationToken _cancellationToken;
        private readonly DependencyInfoResource _dependencyResolver;
        private readonly ILogger _logger;
        private readonly PackageMetadataResource _metadataProvider;

        public ProjectReaderNugetDependencyDecorator(IProjectReader @base,
            PackageMetadataResource metadataProvider,
            DependencyInfoResource dependencyResolver,
            SourceCacheContext cacheContext,
            ILogger logger)
        {
            _base = @base ?? throw new ArgumentNullException(nameof(@base));
            _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
            _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
            _cacheContext = cacheContext ?? throw new ArgumentNullException(nameof(cacheContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _cancellationToken = CancellationToken.None;
        }

        public async Task<ProjectDependencyInfo> ReadProjectFile(string filename)
        {
            ProjectDependencyInfo project = await _base.ReadProjectFile(filename);
            if (project == null)
                return null;

            string packagesConfigPath = filename.Replace(project.ProjectName, PackagesConfigFilename);

            PopulateDependenciesFromPackagesConfig(project, packagesConfigPath);

            foreach (Dependency dependency in project.Dependencies) await PopulateDependencies(dependency);

            return project;
        }

        private async Task PopulateDependencies(Dependency dependency)
        {
            List<Dependency> dependencies = new List<Dependency>();

            NuGetVersion.TryParse(dependency.Version, out NuGetVersion version);
            PackageIdentity package = new PackageIdentity(dependency.Name, version);
            NuGetFramework framework = string.IsNullOrEmpty(dependency.Framework)
                ? NuGetFramework.AnyFramework
                : NuGetFramework.ParseFrameworkName(dependency.Framework, new DefaultFrameworkNameProvider());

            SourcePackageDependencyInfo dependencyInfo =
                await _dependencyResolver.ResolvePackage(package, framework, _cacheContext, _logger,
                    _cancellationToken);

            if (dependencyInfo == null)
            {
                _logger.LogWarning(
                    $"Package not available in source(s): {dependency.Name} {dependency.Version}. Required by {dependency.Project}");
                return;
            }

            dependency.FoundInSources = true;

            if (dependencyInfo?.Dependencies == null)
                return;

            foreach (PackageDependency info in dependencyInfo.Dependencies)
            {
                IEnumerable<IPackageSearchMetadata> searchResults = await _metadataProvider.GetMetadataAsync(info.Id,
                    IncludePrerelease, false, _cacheContext, _logger, _cancellationToken);

                IPackageSearchMetadata metadata = searchResults.OrderBy(s => s.Identity.Version)
                    .LastOrDefault(r => info.VersionRange.Satisfies(r.Identity.Version));

                if (metadata == null)
                {
                    _logger.LogWarning(
                        $"Package not available in source(s): {info.Id} {info.VersionRange}.  Required by {dependency.Name} from {dependency.Project}");
                    continue;
                }

                SourcePackageDependencyInfo subpackage = await _dependencyResolver.ResolvePackage(metadata.Identity,
                    framework, _cacheContext, _logger, _cancellationToken);
                Dependency childDependency = new Dependency
                {
                    Framework = dependency.Framework,
                    Name = subpackage.Id,
                    Version = subpackage.Version?.Version?.ToString(),
                    Project = dependency.Project,
                    VersionLimited = info.VersionRange.MaxVersion == null
                        ? null
                        : $"{info.VersionRange.MaxVersion.Version} by {dependency.Name} ({dependency.Version})"
                };

                dependencies.Add(childDependency);

                await PopulateDependencies(childDependency);
            }

            dependency.Dependencies = dependencies;
        }

        private void PopulateDependenciesFromPackagesConfig(ProjectDependencyInfo project, string packagesConfigPath)
        {
            if (!File.Exists(packagesConfigPath))
                return;

            string fileContent = File.ReadAllText(packagesConfigPath);

            XElement xel = XElement.Parse(fileContent);
            IEnumerable<Dependency> packages = xel.Elements()
                .Where(l => l.Name.LocalName == "package")
                .Select(el =>
                {
                    string packageId = el.Attributes().FirstOrDefault(a => a.Name.LocalName == "id")?.Value;
                    string packageVersion = el.Attributes().FirstOrDefault(a => a.Name.LocalName == "version")?.Value;

                    return new Dependency
                    {
                        Framework = project.FrameworkVersion,
                        Name = packageId,
                        Project = project.ProjectName,
                        Version = packageVersion
                    };
                });

            project.Dependencies = project.Dependencies.Concat(packages);
        }
    }
}