using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Swarm.Core.Common;

namespace Swarm.Core
{
    public class HttpGlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<HttpGlobalExceptionFilter> _logger;

        public HttpGlobalExceptionFilter(ILogger<HttpGlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            context.HttpContext.Response.StatusCode = 201;
            var info = context.Exception is SwarmException
                ? new ApiResult(ApiResult.SwarmError, context.Exception.Message)
                : new ApiResult(ApiResult.InternalError, "Internal Error");

            _logger.LogError(context.Exception.ToString());
            context.Result=new JsonResult(info);
        }
    }
}