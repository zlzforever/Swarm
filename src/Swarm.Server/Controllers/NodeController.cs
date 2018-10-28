using System;
using Microsoft.AspNetCore.Mvc;
using Swarm.Basic.Entity;
using Swarm.Core.Common;
using Swarm.Server.Models;

namespace Swarm.Server.Controllers
{
    public class NodeController : Controller
    {
        private readonly SwarmDbContext _dbContext;

        public NodeController(SwarmDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("swarm/v1.0/[controller]")]
        public IActionResult Query([FromQuery] PaginationQueryInput input)
        {
            var output = _dbContext.Client.PageList<Client, int, DateTimeOffset>(input, null, d => d.CreationTIme);
            return new JsonResult(new ApiResult {Code =  ApiResult.SuccessCode, Data = output});
        }
    }
}