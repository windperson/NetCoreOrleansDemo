using System;
using Microsoft.Extensions.DependencyInjection;

namespace ShareInterface
{
    public interface IServiceConfigDelegate
    {
        Action<IServiceCollection> ServiceConfigurationAction { get; }
    }

    public interface IGrainServiceConfigDelegate : IServiceConfigDelegate
    {
        
    }
}