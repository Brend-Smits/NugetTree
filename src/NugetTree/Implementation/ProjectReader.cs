using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NugetTree.Core;
using NugetTree.Models;

namespace NugetTree.Implementation
{
    public class ProjectReader : IProjectReader
    {
        private const string Include = "Include";
        private const string ItemGroup = "ItemGroup";
        private const string PackageReference = "PackageReference";
        private const string PropertyGroup = "PropertyGroup";
        private const string TargetFramework = "TargetFramework";
        private const string TargetFrameworkVersion = "TargetFrameworkVersion";
        private const string Version = "Version";

        public Task<ProjectDependencyInfo> ReadProjectFile(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException(nameof(filename));

            if (!File.Exists(filename))
                throw new ArgumentException($"Project does not exist: {filename}", nameof(filename));

            string projectName = filename.Substring(filename.LastIndexOf("/", StringComparison.Ordinal) + 1)
                .Replace(".csproj", "");

            string fileContents = File.ReadAllText(filename);
            XElement xel = XElement.Parse(fileContents);

            // <Project>
            //   <ItemGroup>
            //     <PackageReference />
            //   </ItemGroup>
            // </Project>
            IEnumerable<XElement> referenceElements = xel.Elements()
                .Where(el => el.Name.LocalName == ItemGroup)
                .SelectMany(g => g.Elements().Where(gel => gel.Name.LocalName == PackageReference));

            // <Project>
            //   <PropertyGroup>
            //     <TargetFramework />        ==> netstandard, netcoreapp
            //     <TargetFrameworkVersion /> ==> v4.5, v4.7.2, ...
            //   </PropertyGroup>
            // </Project>
            XElement frameworkEl = xel.Elements()
                .Where(el => el.Name.LocalName == PropertyGroup)
                .SelectMany(el => el.Elements().Where(pel => pel.Name.LocalName == TargetFramework)
                    .Concat(el.Elements().Where(cel => cel.Name.LocalName == TargetFrameworkVersion)))
                .FirstOrDefault();

            // <Project>
            //   <PropertyGroup>
            //     <Version />        ==> 1.0.0.1
            //   </PropertyGroup>
            // </Project>
            XElement versionElement = xel.Elements()
                .Where(el => el.Name.LocalName == PropertyGroup)
                .SelectMany(el => el.Elements().Where(pel => pel.Name.LocalName == Version))
                .DefaultIfEmpty(new XElement(Version, "1.0.0")).Single();

            string frameworkName = frameworkEl?.Value;
            string projectVersion = versionElement.Value;

            IEnumerable<Dependency> dependencies = referenceElements.Select(element =>
            {
                // <PackageReference Include="Microsoft.Extensions.Logging.Abstractions">
                //     <Version>3.0.1</Version>
                // </PackageReference>
                // or:
                // <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="3.0.1" />
                string packageId = element.Attributes().FirstOrDefault(a => a.Name.LocalName == Include)?.Value;
                string versionId = element.Attributes().FirstOrDefault(a => a.Name.LocalName == Version)?.Value
                                   ?? element.Elements().FirstOrDefault(el => el.Name.LocalName == Version)?.Value
                                   ?? string.Empty;

                return new Dependency
                {
                    Name = packageId,
                    Version = versionId,
                    Framework = frameworkName,
                    Project = projectName
                };
            });

            ProjectDependencyInfo project = new ProjectDependencyInfo
            {
                FrameworkVersion = frameworkName ?? "Unknown",
                ProjectName = projectName,
                ProjectVersion = projectVersion,
                Dependencies = dependencies.ToList()
            };

            return Task.FromResult(project);
        }
    }
}