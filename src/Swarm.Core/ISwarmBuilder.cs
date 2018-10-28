using Microsoft.Extensions.DependencyInjection;

namespace Swarm.Core
{
    public interface ISwarmBuilder
    {
        IServiceCollection Services { get; }  
     }
 }