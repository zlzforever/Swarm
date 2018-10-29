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
    public class JobController : Controller
    {
        private readonly IScheduler _scheduler;
        private readonly SwarmOptions _options;
        private readonly ILogger _logger;
        private readonly IStore _store;
        private readonly IHubContext<ClientHub> _hubContext;

        public JobController(IScheduler scheduler, ILoggerFactory loggerFactory, IStore store,
            IHubContext<ClientHub> hubContext,
            IOptions<SwarmOptions> options)
        {
            _options = options.Value;
            _scheduler = scheduler;
            _logger = loggerFactory.CreateLogger<JobController>();
            _store = store;
            _hubContext = hubContext;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.IsAccess(_options))
            {
                throw new SwarmException("Auth dined.");
            }

            base.OnActionExecuting(context);
        }

        [HttpGet("{jobId}")]
        public async Task<IActionResult> GetJobInfo(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return new JsonResult(new ApiResult
                    {Code = ApiResult.ModelNotValid, Msg = "The id is required."});
            }

            var job = await _store.GetJob(jobId);
            if (job == null)
            {
                return new JsonResult(new ApiResult
                    {Code = ApiResult.ModelNotValid, Msg = $"Job {jobId} is not exists."});
            }

            var properties = await _store.GetJobProperties(jobId);
            var result = new List<object[]>();
            foreach (var property in properties)
            {
                result.Add(new object[] {property.Name, property.Value});
            }

            result.Add(new object[] {"id", job.Id});
            result.Add(new object[] {"Description", job.Description});
            result.Add(new object[] {"Executor", job.Executor});
            result.Add(new object[] {"Group", job.Group});
            result.Add(new object[] {"Name", job.Name});
            result.Add(new object[] {"Owner", job.Owner});
            result.Add(new object[] {"Performer", job.Performer});
            result.Add(new object[] {"Sharding", job.Sharding});
            result.Add(new object[] {"State", job.State});
            result.Add(new object[] {"Trigger", job.Trigger});
            result.Add(new object[] {"CreationTime", job.CreationTime});
            result.Add(new object[] {"RetryCount", job.RetryCount});
            result.Add(new object[] {"ShardingParameters", job.ShardingParameters});
            result.Add(new object[] {"ConcurrentExecutionDisallowed", job.ConcurrentExecutionDisallowed});
            result.Add(new object[] {"LastModificationTime", job.LastModificationTime});

            return new JsonResult(new ApiResult {Code = ApiResult.SuccessCode, Data = result});
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
                //TODO: VALID PROPERTIES

                switch (value.Executor)
                {
                    case Executor.Process:
                    {
                        if (string.IsNullOrWhiteSpace(properties.GetValue(SwarmConts.ApplicationProperty)))
                        {
                            return new JsonResult(new ApiResult
                                {Code = ApiResult.ModelNotValid, Msg = "The Application field is required."});
                        }

                        break;
                    }
                    case Executor.Reflection:
                    {
                        if (string.IsNullOrWhiteSpace(properties.GetValue(SwarmConts.ClassProperty)))
                        {
                            return new JsonResult(new ApiResult
                                {Code = ApiResult.ModelNotValid, Msg = "The ClassName field is required."});
                        }

                        break;
                    }
                }

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

            return new JsonResult(new ApiResult {Code = ApiResult.ModelNotValid, Msg = GetModelStateErrorMsg()});
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

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Job value, [FromQuery] Dictionary<string, string> properties)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(value.Id))
                {
                    return new JsonResult(new ApiResult {Code = ApiResult.Error, Msg = "Id is empty/null."});
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

            return new JsonResult(new ApiResult {Code = ApiResult.ModelNotValid, Msg = GetModelStateErrorMsg()});
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new JsonResult(new ApiResult {Code = ApiResult.Error, Msg = "Id is empty/null."});
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
                return new JsonResult(new ApiResult {Code = ApiResult.Error, Msg = "Id is empty/null."});
            }

            if (!await _store.CheckJobExists(id))
            {
                return new JsonResult(new ApiResult {Code = ApiResult.Error, Msg = $"Job {id} not exists."});
            }

            await _scheduler.TriggerJob(new JobKey(id));
            return new JsonResult(new ApiResult {Code = ApiResult.SuccessCode, Msg = "success"});
        }

        [HttpDelete()]
        public async Task<IActionResult> Exit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new JsonResult(new ApiResult {Code = ApiResult.Error, Msg = "Id is empty/null."});
            }

            var job = await _store.GetJob(id);
            if (job == null)
            {
                return new JsonResult(new ApiResult {Code = ApiResult.Error, Msg = $"Job {id} not exists."});
            }

            ApiResult result;
            switch (job.Performer)
            {
                case Performer.SignalR:
                {
                    await _hubContext.Clients.All.SendAsync("Kill", id);
                    result = new ApiResult {Code = ApiResult.SuccessCode, Msg = "success"};
                    break;
                }
                default:
                {
                    result = new ApiResult
                        {Code = ApiResult.Error, Msg = $"{job.Performer} is not support exit."};
                    break;
                }
            }

            await _store.ChangeJobState(id, State.Exit);
            return new JsonResult(result);
        }
    }
}