using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swarm.Core.Common;

namespace Swarm.Core.Controllers
{
    [Route("swarm/v1.0/dashboard")]
    public class DashboardController : AbstractApiControllerBase
    {
        private readonly INodeService _nodeService;
        private readonly IClientStore _clientStore;
        private readonly ISwarmStore _store;

        public DashboardController(IOptions<SwarmOptions> options,
            INodeService nodeService, IClientStore clientStore, ISwarmStore store) : base(options)
        {
            _nodeService = nodeService;
            _clientStore = clientStore;
            _store = store;
        }

        public async Task<IActionResult> GetDashboard()
        {
            var nodeStatistics = await _nodeService.GetNodeStatistics();
            var clientCount = await _clientStore.GetClientCount();
            var jobCount = await _store.GetJobCount();
            return new JsonResult(new ApiResult(200, null, new
            {
                clientCount,
                nodeStatistics.Data.NodeCount,
                offlineCount = nodeStatistics.Data.OfflineCount,
                nodeStatistics.Data.TriggerTimes,
                jobCount
            }));
        }
    }
}