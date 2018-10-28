using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using Quartz.Xml.JobSchedulingData20;
using Swarm.Basic;
using Swarm.Basic.Common;
using Swarm.Basic.Entity;
using Swarm.Core.Common;
using Swarm.Core.Impl;

namespace Swarm.Core.Controllers
{
    [Route("swarm/v1.0/job")]
    public class JobController : Controller
    {
        private readonly IScheduler _scheduler;
        private readonly SwarmOptions _options;
        private readonly ILogger _logger;
        private readonly IStore _store;

        public JobController(IScheduler scheduler, ILoggerFactory loggerFactory, IStore store,
            IOptions<SwarmOptions> options)
        {
            _options = options.Value;
            _scheduler = scheduler;
            _logger = loggerFactory.CreateLogger(GetType());
            _store = store;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.IsAccess(_options))
            {
                throw new SwarmException("Auth dined.");
            }

            base.OnActionExecuting(context);
        }


        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="value">任务</param>
        /// <param name="properties"></param>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Job value, [FromQuery] Dictionary<string, string> properties)
        {
            if (ModelState.IsValid)
            {
                if (await _store.CheckJobExists(value.Name, value.Group))
                {
                    return new JsonResult(new ApiResult
                        {Code = ApiResult.Error, Msg = $"Job [{value.Name}, {value.Group}] exists."});
                }

                // default state is exit
                value.State = State.Exit;
                value.RetryCount = value.RetryCount <= 0 ? 1 : value.RetryCount;
                await _store.AddJob(value, properties);
                if (string.IsNullOrWhiteSpace(value.Id))
                {
                    return new JsonResult(new ApiResult {Code = ApiResult.DbError, Msg = "Save job failed."});
                }

                var qzJob = value.ToQuartzJob();
                var trigger = TriggerFactory.Create(value.Trigger, value.Id, properties);

                await _scheduler.ScheduleJob(qzJob, trigger);

                _logger.LogInformation(
                    $"Create job: {JsonConvert.SerializeObject(value)}, {JsonConvert.SerializeObject(properties)}.");
                return new JsonResult(new ApiResult {Code = ApiResult.SuccessCode, Msg = value.Id});
            }

            return new JsonResult(new ApiResult {Code = ApiResult.ModelNotValid});
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Job value, [FromQuery] Dictionary<string, string> properties)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(value.Id))
                {
                    return new JsonResult(new ApiResult {Code = ApiResult.Error, Msg = $"Id is empty/null."});
                }

                if (!await _store.CheckJobExists(value.Id))
                {
                    return new JsonResult(new ApiResult {Code = ApiResult.Error, Msg = $"Job {value.Id} not exists."});
                }

                var qzJob = value.ToQuartzJob();
                var trigger = TriggerFactory.Create(value.Trigger, value.Id, properties);

                // TODO: need test if need delete qzJob firstly?
                await _store.UpdateJob(value, properties);
                await _scheduler.UnscheduleJob(new TriggerKey(value.Id));
                await _scheduler.ScheduleJob(qzJob, trigger);

                return new JsonResult(new ApiResult {Code = ApiResult.SuccessCode, Msg = "success"});
            }

            return new JsonResult(new ApiResult {Code = ApiResult.ModelNotValid});
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new JsonResult(new ApiResult {Code = ApiResult.Error, Msg = $"Id is empty/null."});
            }

            if (!await _store.CheckJobExists(id))
            {
                return new JsonResult(new ApiResult {Code = ApiResult.Error, Msg = $"Job {id} not exists."});
            }

            // Remove from quartz firstly, then remove from swarm
            await _scheduler.DeleteJob(new JobKey(id));
            await _scheduler.UnscheduleJob(new TriggerKey(id));
            await _store.DeleteJob(id);
            return new JsonResult(new ApiResult {Code = ApiResult.SuccessCode, Msg = "success"});
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> Trigger(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new JsonResult(new ApiResult {Code = ApiResult.Error, Msg = $"Id is empty/null."});
            }

            if (!await _store.CheckJobExists(id))
            {
                return new JsonResult(new ApiResult {Code = ApiResult.Error, Msg = $"Job {id} not exists."});
            }

            await _scheduler.TriggerJob(new JobKey(id));
            return new JsonResult(new ApiResult {Code = ApiResult.SuccessCode, Msg = "success"});
        }
    }
}