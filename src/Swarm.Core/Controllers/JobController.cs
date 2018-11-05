using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Swarm.Basic.Entity;
using Swarm.Core.Common;

namespace Swarm.Core.Controllers
{
    [Route("swarm/v1.0/jo")]
    public class JobController : AbstractApiControllerBase
    {
        private readonly IJobService _jobService;

        public JobController(IJobService jobService, IOptions<SwarmOptions> options) : base(options)
        {
            _jobService = jobService;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.IsAccess(Options))
            {
                throw new SwarmException("Auth dined");
            }

            base.OnActionExecuting(context);
        }

        [HttpGet("{jobId}")]
        public async Task<IActionResult> GetJobInfo(string jobId)
        {
            return new JsonResult(await _jobService.Get(jobId));
        }

        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="value">任务</param>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Job value)
        {
            if (ModelState.IsValid)
            {
                return new JsonResult(await _jobService.Create(value));
            }

            return new JsonResult(new ApiResult(ApiResult.ModelNotValid, GetModelStateErrorMsg()));
        }

        [HttpPut("{jobId}")]
        public async Task<IActionResult> Update(string jobId, [FromBody] Job value)
        {
            if (ModelState.IsValid)
            {
//                if (string.IsNullOrWhiteSpace(jobId))
//                {
//                    return new JsonResult(new ApiResult(ApiResult.Error, "Id is empty/null"));
//                }
//
//                if (!await _store.CheckJobExists(jobId))
//                {
//                    return new JsonResult(new ApiResult(ApiResult.Error, $"Job {jobId} not exists"));
//                }
//
//                var qzJob = value.ToQuartzJob();
//                var trigger = TriggerFactory.Create(value.Trigger, jobId, value.Properties);
//
//                // TODO: need test if need delete qzJob firstly?
//                value.Id = jobId;
//                await _store.UpdateJob(value);
                // TODO: new implement
                //await _scheduler.UnscheduleJob(new TriggerKey(jobId));
                //await _scheduler.ScheduleJob(qzJob, trigger);

                return new JsonResult(new ApiResult(ApiResult.SuccessCode, "success"));
            }

            return new JsonResult(new ApiResult(ApiResult.ModelNotValid, GetModelStateErrorMsg()));
        }

        [HttpDelete("{jobId}")]
        public async Task<IActionResult> Delete(string jobId)
        {
            return new JsonResult(await _jobService.Delete(jobId));
        }

        [HttpPost("{jobId}")]
        public async Task<IActionResult> Exit(string jobId)
        {
            return new JsonResult(await _jobService.Exit(jobId));
        }

        private string GetModelStateErrorMsg()
        {
            var errors = new List<string>();
            foreach (var state in ModelState)
            {
                var error = state.Value.Errors.FirstOrDefault();
                if (error != null)
                {
                    errors.Add(error.ErrorMessage);
                }
            }

            return string.Join(",", errors);
        }
    }
}