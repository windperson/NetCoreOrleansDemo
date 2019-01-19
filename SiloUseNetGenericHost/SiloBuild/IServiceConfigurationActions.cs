using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace SiloUseNetGenericHost.SiloBuild
{
    public interface IServiceConfigurationActions
    {
        List<Action<IServiceCollection>> GrainServiceConfigurationActions { get; }
    }
}