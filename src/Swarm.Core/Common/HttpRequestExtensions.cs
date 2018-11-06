using System;
using Microsoft.AspNetCore.Http;
using Swarm.Basic;

namespace Swarm.Core.Common
{
    public static class HttpRequestExtensions
    {
        public static bool IsAccess(this HttpRequest request, SwarmOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (options.AccessTokens == null || options.AccessTokens.Count == 0)
            {
                return true;
            }

            if (request.Headers.ContainsKey(SwarmConsts.AccessTokenHeader))
            {
                var token = request.Headers[SwarmConsts.AccessTokenHeader].ToString();
                return options.AccessTokens.Contains(token);
            }

            return false;
        }
    }
}