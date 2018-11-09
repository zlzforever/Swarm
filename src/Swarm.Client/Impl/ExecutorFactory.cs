using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Swarm.Basic;

namespace Swarm.Client.Impl
{
    public class ExecutorFactory : IExecutorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<Executor, IExecutor> _cache = new ConcurrentDictionary<Executor, IExecutor>();

        public Dictionary<Executor, Type> Executors { get; }

        public ExecutorFactory(IServiceProvider provider)
        {
            _serviceProvider = provider;
            Executors = new Dictionary<Executor, Type>
            {
                {Executor.Reflection, typeof(ReflectionExecutor)}, {Executor.Process, typeof(ProcessExecutor)}
            };
        }

        public IExecutor Create(Executor executor)
        {
            if (_cache.ContainsKey(executor))
            {
                return _cache[executor];
            }

            if (!Executors.ContainsKey(executor))
            {
                throw new SwarmClientException($"Unsupported executor: {executor}");
            }

            var executorInstance= (IExecutor) _serviceProvider.GetRequiredService(Executors[executor]);
            _cache.TryAdd(executor, executorInstance);
            return executorInstance;
        }

        public void Dispose()
        {
            foreach (var executor in _cache)
            {
                executor.Value.Dispose();
            }
        }
    }
}