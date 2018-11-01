using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Swarm.Basic;

namespace Swarm.Client.Impl
{
    public class ExecutorFactory : IExecutorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public static readonly Dictionary<Executor, Type> Executors = new Dictionary<Executor, Type>();

        static ExecutorFactory()
        {
            Executors.Add(Executor.Reflection, typeof(ReflectionExecutor));
            Executors.Add(Executor.Process, typeof(ProcessExecutor));
        }

        public ExecutorFactory(IServiceProvider provider)
        {
            _serviceProvider = provider;
        }

        public IExecutor Create(Executor executor)
        {
            if (!Executors.ContainsKey(executor))
            {
                throw new SwarmClientException($"Unsupported executor: {executor}.");
            }

            return (IExecutor) _serviceProvider.GetRequiredService(Executors[executor]);
        }
    }
}