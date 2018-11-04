using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using Swarm.Basic;
using Swarm.Basic.Common;
using Swarm.Basic.Entity;
using Swarm.Core.Common;
using Swarm.Core.Impl;
using Swarm.Core.SignalR;

namespace Swarm.Core.Controllers
{
    [Route("swarm/v1.0/job")]
    public class JobController : AbstractApiControllerBase
    {
        private readonly ISharding _sharding;
        private readonly ILogger _logger;
        private readonly ISwarmStore _store;
        private readonly IHubContext<ClientHub> _hubContext;
        private readonly ISchedulerCache _schedulerCache;

        public JobController(ISharding sharding, ISchedulerCache schedulerCache, ILoggerFactory loggerFactory,
            ISwarmStore store,
            IHubContext<ClientHub> hubContext,
            IOptions<SwarmOptions> options) : base(options)
        {
            _sharding = sharding;
            _logger = loggerFactory.CreateLogger<JobController>();
            _store = store;
            _hubContext = hubContext;
            _schedulerCache = schedulerCache;
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
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return new JsonResult(new ApiResult(ApiResult.ModelNotValid, "The id is required"));
            }

            var job = await _store.GetJob(jobId);
            if (job == null)
            {
                return new JsonResult(new ApiResult(ApiResult.ModelNotValid, $"Job {jobId} is not exists"));
            }

            var result = job.ToPropertyArray();
            return new JsonResult(new ApiResult(ApiResult.SuccessCode, null, result));
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
                var apiResult = ValidateProperties(value);
                if (apiResult != null)
                {
                    return apiResult;
                }

                if (await _store.IsJobExists(value.Name, value.Group))
                {
                    return new JsonResult(new ApiResult(ApiResult.Error, $"Job [{value.Name}, {value.Group}] exists"));
                }

                var node = await _sharding.GetShardingNode();
                if (node == null)
                {
                    return new JsonResult(new ApiResult(ApiResult.Error, $"Swarm cluster has no available node"));
                }

                var qzJob = value.ToQuartzJob();
                var trigger = TriggerFactory.Create(value.Trigger, value.Id, value.Properties);
                var sched = _schedulerCache.Create(node.SchedName, node.NodeId, node.Provider, node.ConnectionString);
                await sched.ScheduleJob(qzJob, trigger);
                value.NodeId = node.NodeId;
                // TODO: 以后实现用户
                value.UserId = 0;
                await _store.AddJob(value);
                if (string.IsNullOrWhiteSpace(value.Id))
                {
                    await sched.DeleteJob(qzJob.Key);
                    await sched.UnscheduleJob(trigger.Key);
                    return new JsonResult(new ApiResult(ApiResult.DbError, "Save job failed"));
                }

                _logger.LogInformation(
                    $"Create job {JsonConvert.SerializeObject(value)} success");
                return new JsonResult(new ApiResult(ApiResult.SuccessCode, null, value.Id));
            }

            return new JsonResult(new ApiResult(ApiResult.ModelNotValid, GetModelStateErrorMsg()));
        }

        [HttpPut("{jobId}")]
        public async Task<IActionResult> Update(string jobId, [FromBody] Job value)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(jobId))
                {
                    return new JsonResult(new ApiResult(ApiResult.Error, "Id is empty/null"));
                }

                if (!await _store.CheckJobExists(jobId))
                {
                    return new JsonResult(new ApiResult(ApiResult.Error, $"Job {jobId} not exists"));
                }

                var qzJob = value.ToQuartzJob();
                var trigger = TriggerFactory.Create(value.Trigger, jobId, value.Properties);

                // TODO: need test if need delete qzJob firstly?
                value.Id = jobId;
                await _store.UpdateJob(value);
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

            // Remove from quartz firstly, then remove from swarm
            await sched.DeleteJob(new JobKey(jobId));
            await sched.UnscheduleJob(new TriggerKey(jobId));
            await _store.DeleteJob(jobId);
            return new JsonResult(new ApiResult(ApiResult.SuccessCode, "success"));
        }

        [HttpPost("{jobId}")]
        public async Task<IActionResult> Exit(string jobId)
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

            ApiResult result;
            switch (job.Performer)
            {
                case Performer.SignalR:
                {
                    await _hubContext.Clients.All.SendAsync("Kill", jobId);
                    result = new ApiResult(ApiResult.SuccessCode, "success");
                    break;
                }
                default:
                {
                    result = new ApiResult(ApiResult.Error, $"Performer {job.Performer} is not support to exit");
                    break;
                }
            }

            return new JsonResult(result);
        }

        private IActionResult ValidateProperties(Job value)
        {
            //TODO: VALID PROPERTIES
            switch (value.Executor)
            {
                case Executor.Process:
                {
                    if (string.IsNullOrWhiteSpace(value.Properties.GetValue(SwarmConts.ApplicationProperty)))
                    {
                        return new JsonResult(new ApiResult(ApiResult.ModelNotValid,
                            "The Application field is required"));
                    }

                    break;
                }
                case Executor.Reflection:
                {
                    if (string.IsNullOrWhiteSpace(value.Properties.GetValue(SwarmConts.ClassProperty)))
                    {
                        return new JsonResult(
                            new ApiResult(ApiResult.ModelNotValid, "The ClassName field is required"));
                    }

                    break;
                }
            }

            // TODO: value.Node = string.IsNullOrWhiteSpace(Options.Name) ? SwarmConts.DefaultGroup : Options.Name;
            return null;
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