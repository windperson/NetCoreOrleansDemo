using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Hosting;
using ShareInterface;
using SiloUseNetGenericHost.SiloBuild;
using SiloUseNetGenericHost.TypedOptions;

namespace SiloUseNetGenericHost.HostedServices
{
    public class OrleansSiloHostedService : IHostedService
    {
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly SiloConfigOption _siloOptions;
        private readonly OrleansProviderOption _providerOptions;
        private readonly GrainLoadOption _grainLoadOptions;

        private readonly OrleansDashboardOption _dashboardOptions;
        private readonly ILogger<OrleansSiloHostedService> _logger;

        private ISiloHost _siloHost;
        private readonly IServiceConfigurationActions _serviceConfigDelegate;

        public OrleansSiloHostedService(IApplicationLifetime applicationLifetime,
            IOptions<SiloConfigOption> siloOptions,
            IOptions<OrleansProviderOption> providerOptions,
            IOptions<GrainLoadOption> grainLoadOptions,
            IOptions<OrleansDashboardOption> dashboardOptions,
            IServiceConfigurationActions serviceConfigDelegate,
            ILogger<OrleansSiloHostedService> logger)
        {
            _applicationLifetime = applicationLifetime;
            _siloOptions = siloOptions.Value;
            _providerOptions = providerOptions.Value;
            _grainLoadOptions = grainLoadOptions.Value;
            _dashboardOptions = dashboardOptions.Value;
            _serviceConfigDelegate = serviceConfigDelegate;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Register .NET Generic Host life time");
            _applicationLifetime.ApplicationStarted.Register(OnApplicationStartedAsync);
            _applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //do nothing
            return Task.CompletedTask;
        }

        private async void OnApplicationStartedAsync()
        {
            _logger.LogInformation("initialize Orleans silo host...");

            _siloHost = OrleansSiloBuildUtil.CreateSiloHost(_siloOptions, _providerOptions, _grainLoadOptions, _dashboardOptions, _logger, _serviceConfigDelegate);
            try
            {
                await _siloHost.StartAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Start up Silo Host failed");
                throw;
            }
        }

        private async void OnApplicationStopping()
        {
            _logger.LogInformation("stopping Orleans silo host...");

            try
            {
                await _siloHost.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when shutdown Silo Host");
                throw;
            }
        }
    }
}