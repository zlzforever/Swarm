using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Swarm.Core.Common;

namespace Swarm.Core.Controllers
{
    [Route("swarm/v1.0/trigger")]
    public class TriggerController : AbstractApiControllerBase
    {
        private readonly IScheduler _scheduler;
        private readonly ILogger _logger;
        private readonly ISwarmStore _store;

        public TriggerController(IScheduler scheduler, ILoggerFactory loggerFactory, ISwarmStore store,
            IOptions<SwarmOptions> options) : base(options)
        {
            _scheduler = scheduler;
            _logger = loggerFactory.CreateLogger<JobController>();
            _store = store;
        }

        [HttpPost("{jobId}")]
        public async Task<IActionResult> Trigger(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return new JsonResult(new ApiResult(ApiResult.Error, "Id is empty/null"));
            }

            if (!await _store.CheckJobExists(jobId))
            {
                return new JsonResult(new ApiResult(ApiResult.Error, $"Job {jobId} not exists"));
            }

            await _scheduler.TriggerJob(new JobKey(jobId));
            _logger.LogInformation($"Trigger job {jobId} success");
            return new JsonResult(new ApiResult(ApiResult.SuccessCode, "success"));
        }
    }
}