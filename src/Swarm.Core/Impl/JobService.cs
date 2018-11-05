using System.Collections;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using Swarm.Basic;
using Swarm.Basic.Common;
using Swarm.Basic.Entity;
using Swarm.Core.Common;
using Swarm.Core.Controllers;
using Swarm.Core.SignalR;

namespace Swarm.Core.Impl
{
    public class JobService : IJobService
    {
        private readonly ISharding _sharding;
        private readonly ILogger _logger;
        private readonly ISwarmStore _store;
        private readonly IHubContext<ClientHub> _hubContext;
        private readonly ISchedulerCache _schedCache;
        private readonly SwarmOptions _options;

        public JobService(ISharding sharding, ISchedulerCache schedulerCache, ILoggerFactory loggerFactory,
            ISwarmStore store,
            IHubContext<ClientHub> hubContext,
            IOptions<SwarmOptions> options)
        {
            _sharding = sharding;
            _logger = loggerFactory.CreateLogger<JobController>();
            _store = store;
            _hubContext = hubContext;
            _schedCache = schedulerCache;
            _options = options.Value;
        }

        public async Task<ApiResult> Create(Job job)
        {
            // 参数验证
            var apiResult = ValidateProperties(job);
            if (apiResult != null)
            {
                return apiResult;
            }

            if (await _store.GetJob(job.Name, job.Group) != null)
            {
                return new ApiResult(ApiResult.Error, $"Job [{job.Name}, {job.Group}] exists");
            }

            var node = await _sharding.GetShardingNode();
            if (node == null)
            {
                return new ApiResult(ApiResult.Error, "Swarm cluster has no available node");
            }

            var sched = _schedCache.Create(node.SchedName, node.NodeId, node.Provider, node.ConnectionString);
            
            var qzJob = job.ToQuartzJob();
            var trigger = TriggerFactory.Create(job.Trigger, job.Id, job.Properties);           
            await sched.ScheduleJob(qzJob, trigger);
            
            job.NodeId = node.NodeId;
            // TODO: 以后实现用户
            job.UserId = 0;
            await _store.AddJob(job);
            
            if (string.IsNullOrWhiteSpace(job.Id))
            {
                await sched.DeleteJob(qzJob.Key);
                await sched.UnscheduleJob(trigger.Key);
                return new ApiResult(ApiResult.DbError, "Save job failed");
            }

            _logger.LogInformation(
                $"Create job {JsonConvert.SerializeObject(job)} success");
            return new ApiResult(ApiResult.SuccessCode, null, job.Id);
        }

        public async Task<ApiResult> Delete(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return new ApiResult(ApiResult.Error, "Id is empty/null");
            }

            var job = await _store.GetJob(jobId);
            if (job == null)
            {
                return new ApiResult(ApiResult.Error, $"Job {jobId} not exists");
            }

            var node = await _store.GetNode(job.NodeId);
            var sched = _schedCache.Create(node.SchedName, node.NodeId, node.Provider, node.ConnectionString);

            // Remove from quartz firstly, then remove from swarm
            await sched.DeleteJob(new JobKey(jobId));
            await sched.UnscheduleJob(new TriggerKey(jobId));
            await _store.DeleteJob(jobId);
            return new ApiResult(ApiResult.SuccessCode, "success");
        }

        public async Task<ApiResult> Exit(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return new ApiResult(ApiResult.Error, "Id is empty/null");
            }

            var job = await _store.GetJob(jobId);
            if (job == null)
            {
                return new ApiResult(ApiResult.Error, $"Job {jobId} not exists");
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

            return result;
        }

        public async Task<ApiResult> Get(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return new ApiResult(ApiResult.ModelNotValid, "The id is required");
            }

            var job = await _store.GetJob(jobId);
            if (job == null)
            {
                return new ApiResult(ApiResult.ModelNotValid, $"Job {jobId} is not exists");
            }

            var result = job.ToPropertyArray();
            return new ApiResult(ApiResult.SuccessCode, null, result);
        }

        private ApiResult ValidateProperties(Job value)
        {
            //TODO: VALID PROPERTIES
            switch (value.Executor)
            {
                case Executor.Process:
                {
                    if (string.IsNullOrWhiteSpace(value.Properties.GetValue(SwarmConts.ApplicationProperty)))
                    {
                        return new ApiResult(ApiResult.ModelNotValid,
                            "The Application field is required");
                    }

                    break;
                }
                case Executor.Reflection:
                {
                    if (string.IsNullOrWhiteSpace(value.Properties.GetValue(SwarmConts.ClassProperty)))
                    {
                        return new ApiResult(ApiResult.ModelNotValid, "The ClassName field is required");
                    }

                    break;
                }
            }

            // TODO: value.Node = string.IsNullOrWhiteSpace(Options.Name) ? SwarmConts.DefaultGroup : Options.Name;
            return null;
        }
    }
}