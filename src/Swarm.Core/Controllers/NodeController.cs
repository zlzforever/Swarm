using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swarm.Basic.Entity;

namespace Swarm.Core.Controllers
{
    [Route("swarm/v1.0/node")]
    public class NodeController : AbstractApiControllerBase
    {
        private readonly INodeService _nodeService;

        public NodeController(IOptions<SwarmOptions> options, INodeService nodeService) : base(options)
        {
            _nodeService = nodeService;
        }

        public async Task<IActionResult> GetNodes()
        {
            var result = await _nodeService.GetNodes();
            foreach (Node node in result.Data)
            {
                node.IsConnected = node.IsAvailable();
            }

            return new JsonResult(result);
        }
    }
}