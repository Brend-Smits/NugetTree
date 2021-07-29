using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NugetTree.Models
{
    public class Dependency : IHasDependencies
    {
        private static readonly Regex ThreePartVersion = new Regex(@"^\d+\.\d+\.\d+$", RegexOptions.Compiled);

        private List<Dependency> _dependencies;
        private string _version;

        public bool FoundInSources { get; set; }

        public string Framework { get; set; }

        public string Name { get; set; }

        public string Project { get; set; }

        public string Version
        {
            get => _version;
            set
            {
                string val = value ?? string.Empty;
                if (ThreePartVersion.IsMatch(val)) val += ".0";
                _version = val;
            }
        }

        public string VersionLimited { get; set; }

        public IEnumerable<Dependency> Dependencies
        {
            get { return _dependencies ??= new List<Dependency>(); }
            set => _dependencies = value?.ToList();
        }
    }
}