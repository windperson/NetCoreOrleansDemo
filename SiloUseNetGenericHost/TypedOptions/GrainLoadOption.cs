using System.Collections.Generic;

namespace SiloUseNetGenericHost.TypedOptions
{
    public class GrainLoadOption
    {
        public List<string> LoadPaths { get; } = new List<string>();

        public List<string> ExcludedTypeFullNames { get; } = new List<string>();
    }
}