namespace Swarm.Server.Models
{
    public class PaginationQueryOutput
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
        public dynamic Result { get; set; }
    }
}