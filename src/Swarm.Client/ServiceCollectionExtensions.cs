using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            IConfigurationSection configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.Configure<SwarmClientOptions>(configuration);
            services.AddSingleton<ISwarmClient,SwarmClient>();
            return services;
        }
    }
}