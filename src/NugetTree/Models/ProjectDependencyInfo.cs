using System.Collections.Generic;
using System.Linq;

namespace NugetTree.Models
{
    public class ProjectDependencyInfo : IHasDependencies
    {
        private List<Dependency> _dependencies;

        public IEnumerable<Dependency> FlattenedDependencies
        {
            get
            {
                return ((IHasDependencies) this).RecurseDependencies().Distinct(new DependencyComparer())
                    .OrderBy(d => d.Name);
            }
        }

        public string FrameworkVersion { get; set; }

        public string ProjectName { get; set; }

        public string ProjectVersion { get; set; }

        public IEnumerable<Dependency> Dependencies
        {
            get => _dependencies ??= new List<Dependency>();
            set => _dependencies = value?.ToList();
        }
    }
}