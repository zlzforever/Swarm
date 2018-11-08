using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic.Entity;
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

            services.AddSingleton<ISchedCache, SchedCache>();
            services.AddSingleton<ISwarmCluster, SwarmCluster>();
            services.AddSingleton<IJobService, JobService>();
            services.AddSingleton<INodeService, NodeService>();

            var builder = new SwarmBuilder(services);
            configure?.Invoke(builder);

            return services;
        }


        public static ISwarmBuilder UseSqlServerLogStore(this ISwarmBuilder builder)
        {
            builder.Services.AddSingleton<ILogStore, SqlServerSwarmStore>();
            return builder;
        }

        public static ISwarmBuilder UseSqlServerClientStore(this ISwarmBuilder builder)
        {
            builder.Services.AddSingleton<IClientStore, SqlServerSwarmStore>();
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
            var cancellationToken =
                app.ApplicationServices.GetRequiredService<IApplicationLifetime>().ApplicationStopping;

            var options = app.ApplicationServices.GetRequiredService<IOptions<SwarmOptions>>().Value;
            if (options == null)
            {
                throw new SwarmException("Can't get SwarmOption, please make sure your configuration file is correct");
            }

            if (string.IsNullOrWhiteSpace(options.SchedName))
            {
                throw new SwarmException("Name in SwarmOption is empty");
            }

            if (string.IsNullOrWhiteSpace(options.SchedInstanceId))
            {
                throw new SwarmException("NodeId in SwarmOption is empty");
            }

            var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
            logger.LogInformation($"Version: {typeof(ServiceCollectionExtensions).Assembly.GetName().Version}");
            logger.LogInformation($"BaseDirectory: {AppContext.BaseDirectory}");
            logger.LogInformation(
                $"SSN-DB: {options.Provider}, {options.ConnectionString}; SSN-NAME: {options.SchedName}; SSN-ID: {options.SchedInstanceId}; {options.Provider}; SSN-QUARTZ-DB: {options.QuartzConnectionString};");

            // Start quartz instance
            var sched = app.ApplicationServices.GetRequiredService<ISchedCache>().GetOrCreate(options.SchedName,
                options.SchedInstanceId, options.Provider,
                options.QuartzConnectionString);
            sched.Start(cancellationToken).ConfigureAwait(true);

            var token = new CancellationToken();
            cancellationToken.Register(async () => { await sched.Shutdown(token); });

            // Start swarm sharding node
            var cluster = app.ApplicationServices.GetRequiredService<ISwarmCluster>();
            cluster.Start(cancellationToken).ConfigureAwait(true);
            cancellationToken.Register(async () => { await cluster.Shutdown(); });

            app.UseSignalR(routes => { routes.MapHub<ClientHub>("/clienthub"); });

            return app;
        }
    }
}