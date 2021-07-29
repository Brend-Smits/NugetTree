using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using NugetTree.Core;
using NugetTree.Models;

namespace NugetTree.Implementation
{
    public sealed class DependencyProcessor : IDependencyProcessor
    {
        private static readonly Regex ProjectPathRegex = new Regex(@"""([^""]+\.csproj)""", RegexOptions.Compiled);

        private readonly IProjectReader _projectReader;
        private readonly IProjectWriter _projectWriter;

        public DependencyProcessor(IProjectReader projectReader, IProjectWriter projectWriter)
        {
            _projectReader = projectReader ?? throw new ArgumentNullException(nameof(projectReader));
            _projectWriter = projectWriter ?? throw new ArgumentNullException(nameof(projectWriter));
        }

        public async Task<IEnumerable<ProjectDependencyInfo>> ProcessSolution(string solutionFileName,
            params OutputType[] outputTypes)
        {
            if (string.IsNullOrEmpty(solutionFileName))
                throw new ArgumentNullException(nameof(solutionFileName));

            if (!File.Exists(solutionFileName))
                throw new ArgumentException($"Solution does not exist: {solutionFileName}", nameof(solutionFileName));

            string solutionContent = File.ReadAllText(solutionFileName);
            MatchCollection matches = ProjectPathRegex.Matches(solutionContent);

            string solutionName =
                solutionFileName.Substring(solutionFileName.LastIndexOf("/", StringComparison.Ordinal) + 1);
            List<ProjectDependencyInfo> projects = new List<ProjectDependencyInfo>();

            foreach (Match match in matches)
            {
                Group group = match.Groups[1];
                string groupValue = group.Value.Replace('\\', '/');
                string projectFilename = solutionFileName.Replace(solutionName, groupValue);

                try
                {
                    ProjectDependencyInfo project = await _projectReader.ReadProjectFile(projectFilename);
                    projects.Add(project);
                }
                catch (XmlException e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            projects = projects.OrderBy(p => p.ProjectName).ToList();

            foreach (OutputType outputType in outputTypes) _projectWriter.WriteProjects(projects, outputType);

            return projects;
        }
    }
}