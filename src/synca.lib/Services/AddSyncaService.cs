using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using synca.lib.Background;
using synca.lib.Hosted.Service;

namespace synca.lib.Services
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddSynca(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedHostedService>();
            return services;
        }

        public static IServiceCollection AddSynca(this IServiceCollection services, Action<MemoryCacheOptions> setupAction)
        {
            services.AddMemoryCache(setupAction);
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedHostedService>();
            return services;
        }

        public static IServiceCollection AddSyncaDistributed(this IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedHostedService>();
            return services;
        }

        public static IServiceCollection AddSyncaDistributed(this IServiceCollection services, Action<MemoryCacheOptions> setupAction)
        {
            services.AddDistributedMemoryCache(setupAction);
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedHostedService>();
            return services;
        }
    }
}