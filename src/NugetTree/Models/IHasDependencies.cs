using System.Collections.Generic;

namespace NugetTree.Models
{
    public interface IHasDependencies
    {
        IEnumerable<Dependency> Dependencies { get; }

        public IEnumerable<Dependency> RecurseDependencies()
        {
            foreach (Dependency dependency in Dependencies)
            {
                yield return dependency;
                foreach (Dependency item in ((IHasDependencies) dependency).RecurseDependencies()) yield return item;
            }
        }
    }
}