using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using synca.lib.Background;
using synca.lib.Hosted.Service;

namespace synca.lib.Services
{
    public static class IServiceCollectionExtension
    {
        [Obsolete("This will be entirely depricated in very near future")]
        public static IServiceCollection AddSynca(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<InMemoryQueuedHostedService>();
            return services;
        }

        [Obsolete("This will be entirely depricated in very near future")]
        public static IServiceCollection AddSynca(this IServiceCollection services, Action<MemoryCacheOptions> setupAction)
        {
            services.AddMemoryCache(setupAction);
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<InMemoryQueuedHostedService>();
            return services;
        }

        public static IServiceCollection AddSyncaDistributed(this IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<DistributedQueuedHostedService>();
            return services;
        }

        public static IServiceCollection AddSyncaDistributed(this IServiceCollection services, Action<MemoryCacheOptions> setupAction)
        {
            services.AddDistributedMemoryCache(setupAction);
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<DistributedQueuedHostedService>();
            return services;
        }

        public static IServiceCollection AddSyncaDistributedSql(this IServiceCollection services, Action<SqlServerCacheOptions> setupAction)
        {
            services.AddDistributedSqlServerCache(setupAction);
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<DistributedQueuedHostedService>();
            return services;
        }
    }
}