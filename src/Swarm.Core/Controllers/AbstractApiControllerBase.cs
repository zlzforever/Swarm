using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Swarm.Core.Common;

namespace Swarm.Core.Controllers
{
    public class AbstractApiControllerBase : Controller
    {
        protected readonly SwarmOptions Options;

        protected AbstractApiControllerBase(IOptions<SwarmOptions> options)
        {
            Options = options.Value;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.IsAccess(Options))
            {
                throw new SwarmException("Auth dined");
            }

            base.OnActionExecuting(context);
        }
    }
}