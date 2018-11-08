using Swarm.Basic;

namespace Swarm.Node.Models
{
    public class JobProcessPaginationQueryInput: PaginationQueryInput
    {
        public State? State { get; set; }
        public string JobId { get; set; }
    }
}