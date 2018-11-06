using Swarm.Basic;

namespace Swarm.Server.Models
{
    public class JobProcessPaginationQueryInput: PaginationQueryInput
    {
        public State? State { get; set; }
        public string JobId { get; set; }
    }
}