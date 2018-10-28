using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Swarm.Basic;

namespace Swarm.Client.Impl
{
    public class ReflectionExecutor : IExecutor
    {
        private readonly ILogger _logger;
        
        public ReflectionExecutor()
        {
            if (_logger == null)
            {
                _logger = new ConsoleLogger("ProcessExecutor", (cat, lv) => lv > LogLevel.Debug, true);
            }
        }
        
        public async Task<int> Execute(JobContext context, Action<string, string, string> logger)
        {
            var className = context.Parameters[SwarmConts.ClassProperty];
            var type = Type.GetType(className);
            if (type != null)
            {
                if (Activator.CreateInstance(type) is ISwarmJob instance)
                {
                    try
                    {
 
                        await instance.Handle(context);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Execute job [{context.Name}, {context.Group}] failed: {ex}.");
                        return -1;
                    }
                }
                else
                {
                    throw new SwarmClientException($"{className} is not implement ISwarmJob.");
                }
            }

            throw new SwarmClientException($"{className} unfounded.");
        }
    }
}