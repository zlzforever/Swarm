using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.Basic.Entity;
using Swarm.Core.Common;

namespace Swarm.Core.Impl
{
    public class NodeService : INodeService
    {
        private readonly ILogger _logger;
        private readonly ISwarmStore _store;


        public NodeService(ILoggerFactory loggerFactory, ISwarmStore store)
        {
            _logger = loggerFactory.CreateLogger<NodeService>();
            _store = store;
        }

        public async Task<ApiResult> GetNodes()
        {
            return new ApiResult(ApiResult.SuccessCode, null, await _store.GetNodes());
        }

        public async Task<ApiResult> GetNodeStatistics()
        {
            return new ApiResult(ApiResult.SuccessCode, null, await _store.GetNodeStatistics());
        }
    }
}