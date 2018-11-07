using Swarm.Basic;

namespace Swarm.Server.Models
{
    public class JobPaginationQueryInput : PaginationQueryInput
    {
        public string Keyword { get; set; }
        public Trigger Trigger { get; set; }
    }
}