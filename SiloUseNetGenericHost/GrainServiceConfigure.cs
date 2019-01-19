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
                var ret = new List<Action<IServiceCollection>>
                {
                    ServerServiceConfigurationAction
                };
                ret.AddRange(GetAllNeedServiceConfigure(dllPaths));
                return ret;
            }
        }

        private static Action<IServiceCollection> ServerServiceConfigurationAction { get; } = services =>
        {
            services
                .AddLogging(loggingBuilder => loggingBuilder.AddSerilog());
        };

        private IEnumerable<Action<IServiceCollection>> GetAllNeedServiceConfigure(List<string> pathsList)
        {
            var ret = new List<Action<IServiceCollection>>();
            foreach (var path in pathsList)
            {
                var dllFileInfo = new FileInfo(path);
                var assemblyDll = Assembly.LoadFile(dllFileInfo.FullName);
                var types = assemblyDll.GetTypes();
                var needServiceConfigureClasses = types.Where(x =>
                        typeof(IGrainServiceConfigDelegate).IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface)
                    .ToList();
                foreach (var serviceConfigureClass in needServiceConfigureClasses)
                {

                    var obj = Activator.CreateInstance(serviceConfigureClass) as IGrainServiceConfigDelegate;

                    if (obj == null)
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