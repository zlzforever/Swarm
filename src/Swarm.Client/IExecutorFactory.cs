using System;
using System.Collections.Generic;
using Swarm.Basic;

namespace Swarm.Client
{
    public interface IExecutorFactory : IDisposable
    {
        IExecutor Create(Executor executor);

        Dictionary<Executor, Type> Executors { get; }
    }
}