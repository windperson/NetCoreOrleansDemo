using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Serilog;
using ShareInterface;
using WebClient.Models;
using WebClient.TypedOptions;
using WebClient.Util;

namespace WebClient.Controllers
{
    public class CallGrainController : Controller
    {
        private readonly ClusterInfoOption _clusterInfo;
        private readonly OrleansProviderOption _provider;

        private int attempt = 0;
        const int initializeAttemptsBeforeFailing = 5;
        const double retryWaitSeconds = 4.0;

        public CallGrainController(IOptionsMonitor<ClusterInfoOption> clusterInfoMonitor, IOptionsMonitor<OrleansProviderOption> providerOptionsMonitor)
        {
            _clusterInfo = clusterInfoMonitor.CurrentValue;
            _provider = providerOptionsMonitor.CurrentValue;
        }

        public IActionResult Index(NUlid.Ulid? runSessionId)
        {
            ViewData["signalrHubUrl"] = @"/running_status";
            ViewData["sessionId"] = runSessionId ?? null;

            var viewModel = TempData.Get<CallResultViewModel>("CallResult");
            if (viewModel != null)
            {
                return View(viewModel);
            }

            return View();
        }

        public async Task<IActionResult> CallGrainAlarm()
        {
            using (var client = OrleansClientBuilder.Create(_clusterInfo, _provider, new[] { typeof(IValueTaskDemo) }))
            {
                await client.Connect(RetryFilter);

                var runSessionId = NUlid.Ulid.NewUlid();
                var grainGuid = runSessionId.ToGuid();

                var demoGrain = client.GetGrain<IValueTaskDemo>(grainGuid);
                var callResult = await demoGrain.Alarm();
                await client.Close();

                TempData.Put("CallResult", new CallResultViewModel { Result = callResult, RunSessionId = runSessionId });

                return RedirectToAction(nameof(Index), new { runSessionId });
            }
        }

        #region Orleans Connect Retry Filter
        private async Task<bool> RetryFilter(Exception exception)
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
        #endregion
    }
}