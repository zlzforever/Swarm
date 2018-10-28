using Microsoft.Extensions.DependencyInjection;

namespace Swarm.Core
{
    public class SwarmBuilder : ISwarmBuilder
    {
        public SwarmBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}