using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Swarm.Basic.Entity;
using Swarm.Core.Common;
using Swarm.Server.Models;

namespace Swarm.Server.Controllers
{
    public class JobController : Controller
    {
        private readonly SwarmDbContext _dbContext;

        public JobController(SwarmDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult CronProc()
        {
            return View();
        }

        public IActionResult State()
        {
            return View();
        }

        public IActionResult Log()
        {
            return View();
        }
        
        public IActionResult Detail(string id)
        {
            ViewData["JobID"] = id;
            return View();
        }
        
        [HttpGet]
        [Route("swarm/v1.0/log")]
        public IActionResult QueryLog([FromQuery] LogPaginationQueryInput input)
        {
            var output = _dbContext.Log.PageList<Log, int, int>(input, l=>l.JobId==input.JobId, d => d.Id);
            return new JsonResult(new ApiResult {Code = ApiResult.SuccessCode, Data = output});
        }
        
        [HttpGet]
        [Route("swarm/v1.0/jobState")]
        public IActionResult QueryState([FromQuery] JobStatePaginationQueryInput input)
        {
            var id = input.JobId;

            Expression<Func<JobState, bool>> where = j => j.JobId == id;
            if (input.State != null)
            {
                where = t => t.State == input.State;
            }
            //TODO: 更多的条件

            var output = _dbContext.JobState.PageList<JobState, int, int>(input, where, d => d.Id);
            return new JsonResult(new ApiResult {Code = ApiResult.SuccessCode, Data = output});
        }

        [HttpGet]
        [Route("swarm/v1.0/job")]
        public IActionResult Query([FromQuery] JobPaginationQueryInput input)
        {
            var keyword = input.Keyword;

            Expression<Func<Job, bool>> where = null;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                where = t => t.Name.Contains(keyword);
            }
            //TODO: 更多的条件

            var output = _dbContext.Job.PageList<Job, string, DateTimeOffset>(input, where, d => d.CreationTime);
            return new JsonResult(new ApiResult {Code = ApiResult.SuccessCode, Data = output});
        }
    }
}