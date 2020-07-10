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

namespace synca.lib.Hosted.Service
{
    public sealed class QueuedHostedService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;

        public QueuedHostedService(IBackgroundTaskQueue taskQueue,
            ILoggerFactory loggerFactory,
            IMemoryCache memoryCache)
        {
            TaskQueue = taskQueue;
            _logger = loggerFactory.CreateLogger<QueuedHostedService>();
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
                (int,string) cacheValue = (int.MinValue, string.Empty);              

                switch (await workItem(cancellationToken).Item2)
                {
                    case ObjectResult o:
                        cacheValue = ((int)o.StatusCode, JsonConvert.SerializeObject(o.Value));
                        break;
                    case StatusCodeResult s:
                        cacheValue = ((int)s.StatusCode, string.Empty);
                        break;
                    default:
                        break;
                }          

                try
                {
                    (int,string) alreadyCachedValue;
                    if(_cache.TryGetValue(cacheKey, out alreadyCachedValue))
                    {
                        if(alreadyCachedValue.Item1 == (int)HttpStatusCode.Accepted)
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