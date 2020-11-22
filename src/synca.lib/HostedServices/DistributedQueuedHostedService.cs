using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using synca.lib.Background;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace synca.lib.Hosted.Service
{
    public sealed class DistributedQueuedHostedService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IDistributedCache _cache;

        public DistributedQueuedHostedService(IBackgroundTaskQueue taskQueue,
            ILoggerFactory loggerFactory,
            IDistributedCache cache)
        {
            TaskQueue = taskQueue;
            _logger = loggerFactory.CreateLogger<DistributedQueuedHostedService>();
            _cache = cache;
        } 

        public IBackgroundTaskQueue TaskQueue { get; }

        protected async override Task ExecuteAsync(
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Queued Hosted Service is starting.");

            while (!cancellationToken.IsCancellationRequested)
            {
                var workItem = await TaskQueue.DequeueAsync(cancellationToken);

                var cacheKey = workItem(cancellationToken).Item1;   

                // cacheValue comes in the following format
                // {statusCodeString}ascii(31){serializedResonse}
                // eg: 200ascii(31){["one","two","three]}
                // why ascii(31) - It is Unit Separator control character
                string cacheValue = string.Empty;              

                switch (await workItem(cancellationToken).Item2)
                {
                    case ObjectResult o:
                        cacheValue = $"{((int)o.StatusCode).ToString()}{Convert.ToChar(31)}{JsonConvert.SerializeObject(o.Value)}";
                        break;
                    case StatusCodeResult s:
                        cacheValue = $"{((int)s.StatusCode).ToString()}{Convert.ToChar(31)}{string.Empty}";
                        break;
                    default:
                        break;
                }          

                try
                {
                    // alreadyCachedValue comes in the following format
                    // {statusCodeString}^{serializedResonse}
                    // eg: 200^{["one","two","three]}
                    string alreadyCachedValue = _cache.GetString(cacheKey);
                    if(!string.IsNullOrEmpty(alreadyCachedValue))
                    {
                        string statusCodeString = alreadyCachedValue.Split(Convert.ToChar(31))[0];
                        string serializedResonse = alreadyCachedValue.Split(Convert.ToChar(31))[1];
                        if(int.Parse(statusCodeString) == (int)HttpStatusCode.Accepted)
                        {
                            _cache.Remove(cacheKey);
                            var cacheEntryOptions = new DistributedCacheEntryOptions()
                                .SetSlidingExpiration(TimeSpan.FromDays(1));
                            _cache.SetString(cacheKey, cacheValue, cacheEntryOptions);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                       $"Error occurred executing {nameof(workItem)}.");
                }
            }

            _logger.LogInformation("Queued Hosted Service is stopping.");
        }
    }
}