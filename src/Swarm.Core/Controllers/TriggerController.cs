using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Swarm.Core.Controllers
{
    [Route("swarm/v1.0/trigger")]
    public class TriggerController : AbstractApiControllerBase
    {
        private readonly IJobService _jobService;

        public TriggerController(IJobService jobService,
            IOptions<SwarmOptions> options) : base(options)
        {
            _jobService = jobService;
        }

        [HttpPost("{jobId}")]
        public async Task<IActionResult> Trigger(string jobId)
        {
            return new JsonResult(await _jobService.Trigger(jobId));
        }
    }
}