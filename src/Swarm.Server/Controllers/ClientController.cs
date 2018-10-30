using System;
using Microsoft.AspNetCore.Mvc;
using Swarm.Basic.Entity;
using Swarm.Core.Common;
using Swarm.Server.Models;

namespace Swarm.Server.Controllers
{
    public class ClientController : Controller
    {
        private readonly SwarmDbContext _dbContext;

        public ClientController(SwarmDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("swarm/v1.0/client")]
        public IActionResult Query([FromQuery] PaginationQueryInput input)
        {
            var output = _dbContext.Client.PageList<Client, int, DateTimeOffset>(input, null, d => d.CreationTime);
            return new JsonResult(new ApiResult(ApiResult.SuccessCode, null, output));
        }
    }
}