using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.AdoJobStore;
using Quartz.Simpl;
using Quartz.Spi;
using Swarm.Core.Common.Internal;
using Swarm.Core.Impl;
using Swarm.Core.SignalR;

namespace Swarm.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IMvcBuilder AddSwarm(this IMvcBuilder builder, IConfigurationSection configuration,
            Action<ISwarmBuilder> configure = null)
        {
            builder.AddMvcOptions(o => o.Filters.Add<HttpGlobalExceptionFilter>());
            builder.Services.AddSwarm(configuration, configure);
            return builder;
        }

        public static IServiceCollection AddSwarm(this IServiceCollection services,
            IConfigurationSection configuration, Action<ISwarmBuilder> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.Configure<SwarmOptions>(configuration);
            services.AddSignalR().AddMessagePackProtocol();

            services.AddSingleton<ISchedulerCache, SchedulerCache>();
            services.AddSingleton<ISwarmCluster, SwarmCluster>();

            var builder = new SwarmBuilder(services);
            configure?.Invoke(builder);

            return services;
        }


        public static ISwarmBuilder UseSqlServerLogStore(this ISwarmBuilder builder)
        {
            builder.Services.AddSingleton<ILogStore, SqlServerSwarmStore>();
            return builder;
        }

        public static ISwarmBuilder UseSqlServer(this ISwarmBuilder builder)
        {
            builder.Services.AddSingleton<ISwarmStore, SqlServerSwarmStore>();
            return builder;
        }

        public static ISwarmBuilder UseDefaultSharding(this ISwarmBuilder builder)
        {
            builder.Services.AddSingleton<ISharding, DefaultSharding>();
            return builder;
        }

        public static IApplicationBuilder UseSwarm(this IApplicationBuilder app)
        {
            Ioc.ServiceProvider = app.ApplicationServices;

            var options = app.ApplicationServices.GetRequiredService<IOptions<SwarmOptions>>().Value;
            if (options == null)
            {
                throw new SwarmException("SwarmOption is empty");
            }

            if (string.IsNullOrWhiteSpace(options.Name))
            {
                throw new SwarmException("Name in SwarmOption is empty");
            }

            var schedCache = app.ApplicationServices.GetRequiredService<ISchedulerCache>();
            var sched = schedCache.Create(options.Name, options.NodeId, options.Provider,
                options.QuartzConnectionString);
            sched.Start().ConfigureAwait(false);

            var cluster = app.ApplicationServices.GetRequiredService<ISwarmCluster>();
            cluster.Start().ConfigureAwait(true);
            var store = app.ApplicationServices.GetRequiredService<ISwarmStore>();
            store.DisconnectAllClients().Wait();

            app.UseSignalR(routes => { routes.MapHub<ClientHub>("/clienthub"); });

            return app;
        }
    }
}