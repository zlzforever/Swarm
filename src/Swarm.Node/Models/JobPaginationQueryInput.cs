using Swarm.Basic;

namespace Swarm.Node.Models
{
    public class JobPaginationQueryInput : PaginationQueryInput
    {
        public string Keyword { get; set; }
        public Trigger Trigger { get; set; }
    }
}