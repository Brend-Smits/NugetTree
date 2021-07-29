using System;
using System.Collections.Generic;
using System.Linq;
using NugetTree.Core;
using NugetTree.Models;

namespace NugetTree.Implementation
{
    public sealed class ProjectWriter : IProjectWriter
    {
        public void WriteProjects(IEnumerable<ProjectDependencyInfo> projects, OutputType outputType)
        {
            Console.WriteLine();
            switch (outputType)
            {
                case OutputType.Conflicts:
                    WriteConflicts(projects);
                    break;

                case OutputType.Flat:
                    WriteFlat(projects);
                    break;

                case OutputType.Frameworks:
                    WriteFrameworks(projects);
                    break;

                case OutputType.TopLevel:
                    WriteTopLevel(projects);
                    break;

                case OutputType.Tree:
                    WriteTree(projects);
                    break;

                case OutputType.None:
                default:
                    break;
            }
        }

        private static void WriteConflicts(IEnumerable<ProjectDependencyInfo> projects)
        {
            IEnumerable<IGrouping<string, Dependency>> groupedDeps = projects.SelectMany(p => p.FlattenedDependencies)
                .Distinct(new DependencyComparer()).GroupBy(d => d.Name);
            foreach (IGrouping<string, Dependency> dep in groupedDeps.Where(d => d.Count() > 1))
            {
                Console.WriteLine(dep.Key);
                foreach (Dependency depversion in dep)
                {
                    string message = $"   {depversion.Version} ==> {depversion.Project}";
                    if (depversion.VersionLimited != null)
                        message += $" (limited: {depversion.VersionLimited})";
                    Console.WriteLine(message);
                }
            }
        }

        private static void WriteDependency(Dependency dependency, int level = 1, bool flat = false)
        {
            Console.WriteLine(
                $"{new string('-', level * 3)}{dependency.Name} {dependency.VersionLimited ?? dependency.Version}");
            if (flat)
                return;

            foreach (Dependency dep in dependency.Dependencies) WriteDependency(dep, level + 1);
        }

        private static void WriteFlat(IEnumerable<ProjectDependencyInfo> projects)
        {
            foreach (ProjectDependencyInfo project in projects)
            {
                Console.WriteLine($"{project.ProjectName} {project.FrameworkVersion}");
                foreach (Dependency dep in project.FlattenedDependencies) WriteDependency(dep, 1, true);
            }
        }

        private static void WriteFrameworks(IEnumerable<ProjectDependencyInfo> projects)
        {
            foreach (IGrouping<string, ProjectDependencyInfo> projectGroup in projects.GroupBy(p => p.FrameworkVersion))
            {
                Console.WriteLine(projectGroup.Key);
                foreach (ProjectDependencyInfo project in projectGroup.OrderBy(p => p.ProjectName))
                    Console.WriteLine($"   {project.ProjectName}");
            }
        }

        private static void WriteTopLevel(IEnumerable<ProjectDependencyInfo> projects)
        {
            foreach (ProjectDependencyInfo project in projects)
            {
                Console.WriteLine($"{project.ProjectName} {project.FrameworkVersion}");
                foreach (Dependency dep in project.Dependencies) WriteDependency(dep, 1, true);
            }
        }

        private static void WriteTree(IEnumerable<ProjectDependencyInfo> projects)
        {
            foreach (ProjectDependencyInfo project in projects)
            {
                Console.WriteLine($"{project.ProjectName} {project.ProjectVersion}");
                foreach (Dependency dep in project.Dependencies) WriteDependency(dep);
            }
        }
    }
}