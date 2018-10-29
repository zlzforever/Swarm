using Swarm.Basic;

namespace Swarm.Server.Models
{
    public class JobStatePaginationQueryInput: PaginationQueryInput
    {
        public State? State { get; set; }
        public string JobId { get; set; }
    }
}