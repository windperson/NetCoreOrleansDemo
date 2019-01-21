using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ConsoleClient.TypedOptions;
using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Serilog;
using Serilog.Events;
using ShareInterface;

namespace ConsoleClient
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Trace()
                .WriteTo.Debug();
            Log.Logger = logConfig.CreateLogger();

            var (clusterInfo, providerInfo) = ReadJsonFileSettings(args);

            return await RunMainAsync(clusterInfo, providerInfo);
        }

        private static async Task<int> RunMainAsync(ClusterInfoOption clusterInfo,
            OrleansProviderOption providerOption)
        {
            try
            {
                Console.WriteLine("Press Enter to begin connect");
                Console.ReadLine();
                using (var client = await StartClientWithRetries(clusterInfo, providerOption, new[] { typeof(IValueTaskDemo) }))
                {
                    var demoGrain = client.GetGrain<IValueTaskDemo>(Guid.NewGuid());
                    var result = await demoGrain.Alarm();

                    Log.Information($"result={result}");

                    Console.WriteLine("Press any key to stop Client.");
                    Console.ReadKey();
                    await client.Close();
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Run Error");
                return 1;
            }
        }

        private static (ClusterInfoOption, OrleansProviderOption) ReadJsonFileSettings(string[] args)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables(prefix: "ORLEANS_CLIENT_APP_")
                .AddCommandLine(args);

            var config = builder.Build().GetSection("Orleans");

            var clusterInfo = new ClusterInfoOption();
            config.GetSection("Cluster").Bind(clusterInfo);

            var providerInfo = new OrleansProviderOption();
            config.GetSection("Provider").Bind(providerInfo);

            return (clusterInfo, providerInfo);
        }

        private static async Task<IClusterClient> StartClientWithRetries(ClusterInfoOption clusterInfo,
            OrleansProviderOption providerOption, IEnumerable<Type> applicationPartTypes)
        {
            var clientBuilder = new ClientBuilder();

            clientBuilder.Configure<ClientMessagingOptions>(options =>
                {
                    options.ResponseTimeout = TimeSpan.FromSeconds(20);
                    options.ResponseTimeoutWithDebugger = TimeSpan.FromMinutes(60);
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = clusterInfo.ClusterId;
                    options.ServiceId = clusterInfo.ServiceId;
                });

            if (providerOption.DefaultProvider == "MongoDB")
            {
                clientBuilder.UseMongoDBClustering(options =>
                {
                    var mongoSetting = providerOption.MongoDB.Cluster;

                    options.ConnectionString = mongoSetting.DbConn;
                    options.DatabaseName = mongoSetting.DbName;
                    // see:https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/54
                    options.CollectionPrefix = mongoSetting.CollectionPrefix;
                });
            }

            clientBuilder.ConfigureApplicationParts(manager =>
            {
                foreach (var applicationPartType in applicationPartTypes)
                {
                    manager.AddApplicationPart(applicationPartType.Assembly).WithReferences();
                }
            }).ConfigureLogging(builder => { builder.AddSerilog(dispose: true); });

            try
            {
                var attempt = 0;
                const int initializeAttemptsBeforeFailing = 5;
                const double retryWaitSeconds = 4.0;

                var client = clientBuilder.Build();
                await client.Connect(RetryFilter);

                return client;

                async Task<bool> RetryFilter(Exception exception)
                {
                    if (exception.GetType() != typeof(SiloUnavailableException))
                    {
                        Log.Error($"Cluster client failed to connect to cluster with unexpected error.  Exception: {exception}");
                        return false;
                    }

                    attempt++;

                    Log.Information($"Cluster client attempt {attempt} of {initializeAttemptsBeforeFailing} failed to connect to cluster.  Exception: {exception}");

                    if (attempt > initializeAttemptsBeforeFailing)
                    {
                        return false;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(retryWaitSeconds));
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "init client error");
                throw;
            }
        }
    }
}
