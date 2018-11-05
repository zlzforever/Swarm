using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swarm.Client.Impl;
using Swarm.Client.Listener;

namespace Swarm.Client
{
    public static class ServiceCollectionExtensions
    {
        public static IMvcBuilder AddSwarm(this IMvcBuilder builder, IConfigurationSection configuration)
        {
            builder.Services.AddSwarmClient(configuration);
            return builder;
        }

        public static IServiceCollection AddSwarmClient(this IServiceCollection services,
            IConfigurationSection configuration, Action<ISwarmClientBuilder> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.Configure<SwarmClientOptions>(configuration);
            services.AddSingleton<IProcessStore>(p => FileStore.Instance);
            services.AddSingleton<IExecutorFactory, ExecutorFactory>();
            services.AddTransient<ProcessExecutor>();
            services.AddTransient<ReflectionExecutor>();
            services.AddSingleton<KillAllListener>();
            services.AddSingleton<KillListener>();
            services.AddSingleton<TriggerListener>();
            services.AddSingleton<ExitListener>();

            var builder = new SwarmClientBuilder(services);
            configure?.Invoke(builder);

            services.AddSingleton<ISwarmClient, SwarmClient>();
            return services;
        }
    }
}