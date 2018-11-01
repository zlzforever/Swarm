using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Swarm.Basic;
using Swarm.Basic.Entity;
using Swarm.Client;

namespace Swarm.Sample
{
    public class MyJob : ISwarmJob
    {
        public Task Handle(JobContext context)
        {
            Console.WriteLine($"Complete reflection job: {context.JobId}.");
            return Task.CompletedTask;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //SwarmClient client = new SwarmClient("http://127.0.0.1:8000", "BBBBBBBB", "client001", null);
           // client.Start();
            Console.Read();
        }

        static void CreateReflectionJob()
        {
            SwarmApi api = new SwarmApi("http://127.0.0.1:8000", "BBBBBBBB");
            var job = new Job
            {
                Name = "test2",
                Group = "DEFAULT",
                RetryCount = 1,
                Performer = Performer.SignalR,
                Executor = Executor.Reflection,
                Description = "iam a test job",
                Load = 0,
                Owner = "tester",
                Sharding = 1,
                ShardingParameters = null,
                Properties = new Dictionary<string, string>
                {
                    {SwarmConts.CronProperty, "*/15 * * * * ?"},
                    {SwarmConts.ClassProperty, typeof(MyJob).AssemblyQualifiedName}
                }
            };
            api.Create(job).Wait();
        }

        static void CreateProcessJob()
        {
            SwarmApi api = new SwarmApi("http://127.0.0.1:8000", "BBBBBBBB");
            var job = new Job
            {
                Name = "process",
                Group = "DEFAULT",
                RetryCount = 1,
                Performer = Performer.SignalR,
                Executor = Executor.Process,
                Description = "iam a test job",
                Load = 0,
                Owner = "tester",
                Sharding = 1,
                ShardingParameters = null,
                Properties = new Dictionary<string, string>
                {
                    {SwarmConts.CronProperty, "*/15 * * * * ?"},
                    {SwarmConts.ApplicationProperty, "echo"},
                    {SwarmConts.LogPatternProperty, @"\[INF\]"},
                    {
                        SwarmConts.ArgumentsProperty,
                        "[INF]: %JobId% %TraceId% %Sharding% %Partition% %ShardingParameter%"
                    }
                }
            };
            api.Create(job).Wait();
        }
    }
}