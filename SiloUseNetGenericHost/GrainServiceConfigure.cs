using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using ShareInterface;
using SiloUseNetGenericHost.SiloBuild;
using SiloUseNetGenericHost.TypedOptions;

namespace SiloUseNetGenericHost
{
    public class GrainServiceConfigure : IServiceConfigurationActions
    {
        private readonly GrainLoadOption _grainLoadOptions;

        public GrainServiceConfigure(IOptions<GrainLoadOption> grainLoadOptions)
        {
            _grainLoadOptions = grainLoadOptions.Value;
        }

        public List<Action<IServiceCollection>> GrainServiceConfigurationActions
        {
            get
            {
                var dllPaths = _grainLoadOptions.LoadPaths;
                var excludedTypeFullNames = _grainLoadOptions.ExcludedTypeFullNames;
                var ret = new List<Action<IServiceCollection>>
                {
                    ServerServiceConfigurationAction
                };
                ret.AddRange(GetAllNeedServiceConfigure(dllPaths, excludedTypeFullNames));
                return ret;
            }
        }

        private static Action<IServiceCollection> ServerServiceConfigurationAction { get; } = services =>
        {
            //services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());
        };

        private static IEnumerable<Action<IServiceCollection>> GetAllNeedServiceConfigure(IEnumerable<string> pathsList, ICollection<string> excludedTypeFullNames)
        {
            var ret = new List<Action<IServiceCollection>>();
            foreach (var path in pathsList)
            {
                var fullPath = Path.GetFullPath(path);
                var dllFileInfo = new FileInfo(fullPath);
                var assemblyDll = Assembly.LoadFile(dllFileInfo.FullName);
                var types = assemblyDll.GetTypes();
                var needServiceConfigureClasses = types.Where(x =>
                        typeof(IGrainServiceConfigDelegate).IsAssignableFrom(x) 
                        && !x.IsAbstract 
                        && !x.IsInterface 
                        && !excludedTypeFullNames.Contains(x.FullName))
                    .ToList();
                foreach (var serviceConfigureClass in needServiceConfigureClasses)
                {
                    if (!(Activator.CreateInstance(serviceConfigureClass) is IGrainServiceConfigDelegate obj))
                    {
                        throw new LoadGrainDllFailedException(serviceConfigureClass.FullName);
                    }

                    var loadAction = obj.ServiceConfigurationAction;
                    ret.Add(loadAction);
                }
            }

            return ret;
        }
    }

    public class LoadGrainDllFailedException : Exception
    {
        public string AssemblyName { get; }

        public LoadGrainDllFailedException(string assemblyName)
        {
            AssemblyName = assemblyName;
        }
    }
}