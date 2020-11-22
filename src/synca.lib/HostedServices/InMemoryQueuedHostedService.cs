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

namespace synca.lib.Hosted.Service
{
    [Obsolete("This will be entirely depricated in very near future")]
    public sealed class InMemoryQueuedHostedService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;
        public InMemoryQueuedHostedService(IBackgroundTaskQueue taskQueue,
            ILoggerFactory loggerFactory,
            IMemoryCache memoryCache)
        {
            TaskQueue = taskQueue;
            _logger = loggerFactory.CreateLogger<InMemoryQueuedHostedService>();
            _cache = memoryCache;
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
                    string alreadyCachedValue;
                    if(_cache.TryGetValue(cacheKey, out alreadyCachedValue))
                    {
                        string statusCodeString = alreadyCachedValue.Split(Convert.ToChar(31))[0];
                        string serializedResonse = alreadyCachedValue.Split(Convert.ToChar(31))[1];
                        if(int.Parse(statusCodeString) == (int)HttpStatusCode.Accepted)
                        {
                            _cache.Remove(cacheKey);
                            var cacheEntryOptions = new MemoryCacheEntryOptions()
                                .SetSlidingExpiration(TimeSpan.FromDays(1));
                            _cache.Set(cacheKey, cacheValue, cacheEntryOptions);
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