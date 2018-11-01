using Microsoft.Extensions.DependencyInjection;

namespace Swarm.Client
{
    public interface ISwarmClientBuilder
    {
        IServiceCollection Services { get; }  
    }
}