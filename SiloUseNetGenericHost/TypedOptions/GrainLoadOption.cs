using System.Collections.Generic;

namespace SiloUseNetGenericHost.TypedOptions
{
    public class GrainLoadOption
    {
        public List<string> LoadPaths { get; set; }

        public List<string> ExcludeDLLs { get; set; }
    }
}