using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swarm.Basic.Entity;
using Swarm.Core;
using Swarm.Core.Common;
using Swarm.Core.Controllers;
using Swarm.Node.Models;

namespace Swarm.Node.Api
{
    public class ClientController : AbstractApiControllerBase
    {
        private readonly SwarmDbContext _dbContext;

        public ClientController(SwarmDbContext dbContext, IOptions<SwarmOptions> options) : base(options)
        {
            _dbContext = dbContext;
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