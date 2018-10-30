using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            var info = JsonConvert.SerializeObject(context.Exception is SwarmException
                ? new ApiResult(ApiResult.SwarmError, context.Exception.Message)
                : new ApiResult(ApiResult.InternalError, "Internal Error"));

            _logger.LogError(context.Exception.ToString());

            var bytes = Encoding.UTF8.GetBytes(info);
            context.ExceptionHandled = true;
            context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
            context.HttpContext.Response.Body.Write(bytes, 0, bytes.Length);
        }
    }
}