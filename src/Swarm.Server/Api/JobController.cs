using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swarm.Basic.Entity;
using Swarm.Core;
using Swarm.Core.Common;
using Swarm.Core.Controllers;
using Swarm.Server.Models;
using Swarm.Server.Models.Dto;

namespace Swarm.Server.Api
{
    public class JobController : AbstractApiControllerBase
    {
        private readonly SwarmDbContext _dbContext;

        public JobController(SwarmDbContext dbContext, IOptions<SwarmOptions> options) : base(options)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        [Route("swarm/v1.0/log")]
        public IActionResult QueryLog([FromQuery] LogPaginationQueryInput input)
        {
            var output = _dbContext.Log.PageList<Log, int, int>(input, l => l.JobId == input.JobId, d => d.Id);
            return new JsonResult(new ApiResult(ApiResult.SuccessCode, null, output));
        }

        [HttpGet]
        [Route("swarm/v1.0/jobProcess")]
        public IActionResult QueryProcess([FromQuery] JobProcessPaginationQueryInput input)
        {
            var id = input.JobId;

            Expression<Func<ClientProcess, bool>> where = j => j.JobId == id;
            if (input.State != null)
            {
                where = t => t.State == input.State;
            }
            //TODO: 更多的条件

            var output = _dbContext.ClientProcess.PageList<ClientProcess, int, int>(input, where, d => d.Id);
            var results = new List<ClientProcessDto>();
            foreach (ClientProcess clientProcess in output.Result)
            {
                results.Add(new ClientProcessDto
                {
                    Name = clientProcess.Name,
                    Group = clientProcess.Group,
                    TraceId = Guid.Parse(clientProcess.TraceId).ToInt64(),
                    Sharding = clientProcess.Sharding,
                    ProcessId = clientProcess.ProcessId,
                    State = clientProcess.State,
                    App = clientProcess.App,
                    Arguments = clientProcess.AppArguments,
                    Msg = clientProcess.Msg,
                    LastModificationTime = clientProcess.LastModificationTime ?? clientProcess.CreationTime
                });
            }

            output.Result = results;

            return new JsonResult(new ApiResult(ApiResult.SuccessCode, null, output));
        }

        [HttpGet]
        [Route("swarm/v1.0/job")]
        public IActionResult QueryJob([FromQuery] JobPaginationQueryInput input)
        {
            var keyword = input.Keyword;


            Expression<Func<Job, bool>> where = j => j.Trigger == input.Trigger;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                where = where.AndAlso(t => t.Name.Contains(keyword));
            }
            //TODO: 更多的条件

            var output = _dbContext.Job.PageList<Job, string, DateTimeOffset>(input, where, d => d.CreationTime);
            return new JsonResult(new ApiResult(ApiResult.SuccessCode, null, output));
        }
    }
}