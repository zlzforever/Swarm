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

        [HttpGet]
        [Route("swarm/v1.0/[controller]")]
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