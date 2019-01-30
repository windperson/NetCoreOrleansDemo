﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Serilog;
using ShareInterface;
using WebClient.TypedOptions;
using WebClient.Util;

namespace WebClient.Hubs
{
    public class LongRunningStatusHub : Hub
    {
        private readonly ILogger<LongRunningStatusHub> _logger;
        private readonly ClusterInfoOption _clusterInfo;
        private readonly OrleansProviderOption _provider;

        public LongRunningStatusHub(
            IOptions<TypedOptions.ClusterInfoOption> clusterInfoOptions,
            IOptions<TypedOptions.OrleansProviderOption> providerOptions,
            ILogger<LongRunningStatusHub> logger)
        {
            _clusterInfo = clusterInfoOptions.Value;
            _provider = providerOptions.Value;
            _logger = logger;
        }

        // ReSharper disable once UnusedMember.Global
        public ChannelReader<string> CheckJobStatus(string grainId)
        {
            if (string.IsNullOrEmpty(grainId) || Guid.TryParse(grainId, out var grainGuid))
            {
                throw new Exception($"grainId is not valid!, input={{{grainId}}}");
            }

            var channel = Channel.CreateUnbounded<string>();
            _ = DetectLongRunningStatus(channel.Writer, grainGuid, 60, new CancellationToken());

            return channel.Reader;
        }

        private async Task DetectLongRunningStatus(ChannelWriter<string> writer, Guid guid, int delay, CancellationToken cancellationToken)
        {
            var attempt = 0;
            const int initializeAttemptsBeforeFailing = 5;
            const double retryWaitSeconds = 4.0;

            try
            {
                using (var client =
                    OrleansClientBuilder.Create(_clusterInfo, _provider, new[] { typeof(IValueTaskDemo) }))
                {
                    await client.Connect(exception => RetryFilter(exception, cancellationToken));

                    var status = "stopped";

                    do
                    {
                        // Check the cancellation token regularly so that the server will stop
                        // producing items if the client disconnects.
                        cancellationToken.ThrowIfCancellationRequested();

                        var demoGrain = client.GetGrain<IValueTaskDemo>(guid);
                        status = await demoGrain.GetCurrentStatus();

                        await writer.WriteAsync(status, cancellationToken);
                        await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

                    } while (status == "running");

                    await client.Close();
                }

                #region Orleans Connect Retry Filter
                async Task<bool> RetryFilter(Exception exception, CancellationToken cancelToken)
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

                    await Task.Delay(TimeSpan.FromSeconds(retryWaitSeconds), cancelToken);
                    return true;
                }
                #endregion
            }
            catch (Exception ex)
            {
                writer.TryComplete(ex);
            }

            writer.TryComplete();
        }
    }
}