using System;
using Microsoft.Extensions.DependencyInjection;

namespace Swarm.Core.Common.Internal
{
    internal static class Ioc
    {
        internal static IServiceProvider ServiceProvider { get; set; }

        internal static TEntity GetRequiredService<TEntity>()
        {
            return ServiceProvider.GetRequiredService<TEntity>();
        }
    }
}