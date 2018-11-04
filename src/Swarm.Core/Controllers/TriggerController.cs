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
        private readonly ILogger _logger;
        private readonly ISwarmStore _store;
        private readonly ISchedulerCache _schedulerCache;

        public TriggerController(ISchedulerCache schedulerCache, ILoggerFactory loggerFactory,
            ISwarmStore store,
            IOptions<SwarmOptions> options) : base(options)
        {
            _logger = loggerFactory.CreateLogger<JobController>();
            _store = store;
            _schedulerCache = schedulerCache;
        }

        [HttpPost("{jobId}")]
        public async Task<IActionResult> Trigger(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return new JsonResult(new ApiResult(ApiResult.Error, "Id is empty/null"));
            }

            var job = await _store.GetJob(jobId);
            if (job == null)
            {
                return new JsonResult(new ApiResult(ApiResult.Error, $"Job {jobId} not exists"));
            }

            var node = await _store.GetNode(job.NodeId);
            var sched = _schedulerCache.Create(node.SchedName, node.NodeId, node.Provider, node.ConnectionString);

            await sched.TriggerJob(new JobKey(jobId));
            _logger.LogInformation($"Trigger job {jobId} success");
            return new JsonResult(new ApiResult(ApiResult.SuccessCode, "success"));
        }
    }
}