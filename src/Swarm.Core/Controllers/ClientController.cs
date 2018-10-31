using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Core.Common;
using Swarm.Core.SignalR;

namespace Swarm.Core.Controllers
{
    [Route("swarm/v1.0/client")]
    public class ClientController : AbstractApiControllerBase
    {
        private readonly ILogger _logger;
        private readonly IHubContext<ClientHub> _hubContext;
        private readonly ISwarmStore _store;

        public ClientController(ILoggerFactory loggerFactory, ISwarmStore store, IHubContext<ClientHub> hubContext,
            IOptions<SwarmOptions> options) : base(options)
        {
            _logger = loggerFactory.CreateLogger<ClientController>();
            _hubContext = hubContext;
            _store = store;
        }

        [HttpDelete("{connectionId}")]
        public async Task<IActionResult> Exit(string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                return new JsonResult(new ApiResult(ApiResult.Error,
                    $"{nameof(connectionId)} is empty/null"));
            }

            _logger.LogInformation($"Try to exit connection {connectionId}");
            await _hubContext.Clients.Client(connectionId).SendAsync("Exit");
            return new JsonResult(new ApiResult());
        }

        [HttpDelete]
        public async Task<IActionResult> Remove(int clientId)
        {
            if (clientId == 0)
            {
                return new JsonResult(new ApiResult(ApiResult.Error, "Id should be larger than 0"));
            }

            await _store.RemoveClient(clientId);
            return new JsonResult(new ApiResult());
        }
    }
}