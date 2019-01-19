using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ShareInterface;

namespace MyReminderGrain
{
    // ReSharper disable once UnusedMember.Global
    public class MyReminderGrainServiceConfigure : IGrainServiceConfigDelegate
    {
        public Action<IServiceCollection> ServiceConfigurationAction =>
            services =>
            {
                services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());
                services.AddTransient<IOutputMsg, OutputMsg>();
            };
    }
}