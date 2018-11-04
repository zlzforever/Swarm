using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Swarm.Basic;
using Swarm.Basic.Entity;

namespace Swarm.Core.Common
{
    public static class HubCallerContextExtensions
    {
        public static HttpRequest GetHttpRequest(this HubCallerContext context)
        {
            return context.GetHttpContext().Request;
        }

        public static Client GetClient(this HubCallerContext context)
        {
            var name = context.GetHttpRequest().Query["name"].FirstOrDefault();
            var group = context.GetHttpRequest().Query["group"].FirstOrDefault();
            var ip = context.GetHttpRequest().Query["ip"].FirstOrDefault();
            group = string.IsNullOrWhiteSpace(group) ? SwarmConts.DefaultGroup : group;
            var userId = int.Parse(context.GetHttpRequest().Query["userId"].First());
            var os = context.GetHttpRequest().Query["os"].FirstOrDefault();
            var coreCount = int.Parse(context.GetHttpRequest().Query["coreCount"].First());
            var memory = int.Parse(context.GetHttpRequest().Query["memory"].First());

            //TODO: 数据验证

            return new Client
            {
                Name = name, Group = group, Ip = ip, ConnectionId = context.ConnectionId, Memory = memory, Os = os,
                CoreCount = coreCount, UserId = userId
            };
        }
    }
}