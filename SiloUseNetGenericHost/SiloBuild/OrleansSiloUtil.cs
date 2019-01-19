﻿using System;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using SiloUseNetGenericHost.TypedOptions;
using Serilog;
using ShareInterface;

namespace SiloUseNetGenericHost.SiloBuild
{
    internal class OrleansSiloUtil
    {
        public static ISiloHost CreateSiloHost(SiloConfigOption siloOptions,
            OrleansProviderOption providerOptions,
            OrleansDashboardOption dashboardOptions,
            Microsoft.Extensions.Logging.ILogger logger, IServiceConfigurationActions serviceConfigurationDelegates)
        {
            var builder = new SiloHostBuilder();

            if (dashboardOptions.Enable)
            {
                builder.UseDashboard(options =>
                {
                    options.Port = dashboardOptions.Port;
                });
            }

            builder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = siloOptions.ClusterId;
                options.ServiceId = siloOptions.ServiceId;
            });

            if (IpAddressNotSpecified(siloOptions.AdvertisedIp))
            {
                builder.ConfigureEndpoints(siloOptions.SiloPort, siloOptions.GatewayPort,
                    listenOnAnyHostAddress: siloOptions.ListenOnAnyHostAddress);
            }
            else
            {
                var advertisedIp = IPAddress.Parse(siloOptions.AdvertisedIp.Trim());
                builder.ConfigureEndpoints(advertisedIp, siloOptions.SiloPort, siloOptions.GatewayPort,
                    siloOptions.ListenOnAnyHostAddress);
            }

            if (providerOptions.DefaultProvider == "MongoDB")
            {
                var mongoDbOption = providerOptions.MongoDB;
                builder.UseMongoDBClustering(options =>
                {
                    var clusterOption = mongoDbOption.Cluster;

                    options.ConnectionString = clusterOption.DbConn;
                    options.DatabaseName = clusterOption.DbName;

                    // see:https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/54
                    options.CollectionPrefix = clusterOption.CollectionPrefix;
                })
                .UseMongoDBReminders(options =>
                {
                    var reminderOption = mongoDbOption.Reminder;

                    options.ConnectionString = reminderOption.DbConn;
                    options.DatabaseName = reminderOption.DbName;

                    if (!string.IsNullOrEmpty(reminderOption.CollectionPrefix))
                    {
                        options.CollectionPrefix = reminderOption.CollectionPrefix;
                    }

                })
                .AddMongoDBGrainStorageAsDefault(options =>
                {
                    var storageOption = mongoDbOption.Storage;

                    options.ConnectionString = storageOption.DbConn;
                    options.DatabaseName = storageOption.DbName;

                    if (!string.IsNullOrEmpty(storageOption.CollectionPrefix))
                    {
                        options.CollectionPrefix = storageOption.CollectionPrefix;
                    }
                });
            }

            foreach (var configurationAction in serviceConfigurationDelegates.GrainServiceConfigurationActions)
            {
                builder.ConfigureServices(configurationAction);
            }
            
            builder
                .ConfigureApplicationParts(parts =>
                {
                    parts.AddFromApplicationBaseDirectory().WithReferences();
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddSerilog(dispose: true);
                });

            try
            {
                return builder.Build();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Create Silo failed");
                throw;
            }
        }

        private static bool IpAddressNotSpecified(string ipString)
        {
            return string.IsNullOrEmpty(ipString.Trim()) || "*".Equals(ipString.Trim());
        }
    }
}