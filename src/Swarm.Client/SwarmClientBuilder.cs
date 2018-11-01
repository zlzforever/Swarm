using Microsoft.Extensions.DependencyInjection;

namespace Swarm.Client
{
    public class SwarmClientBuilder : ISwarmClientBuilder
    {
        public IServiceCollection Services { get; }

        public SwarmClientBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}