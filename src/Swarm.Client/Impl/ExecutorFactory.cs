using System.Collections.Generic;
using Swarm.Basic;

namespace Swarm.Client.Impl
{
    public static class ExecutorFactory
    {
        public static readonly Dictionary<Executor, IExecutor> Executors = new Dictionary<Executor, IExecutor>();

        static ExecutorFactory()
        {
            Executors.Add(Executor.Reflection,new ReflectionExecutor());
            Executors.Add(Executor.Process,new ProcessExecutor());
        }
        
        public static IExecutor Create(Executor executor)
        {
            if (!Executors.ContainsKey(executor))
            {
                throw new SwarmClientException($"Unsupported executor: {executor}.");
            }

            return Executors[executor];
        }
    }
}