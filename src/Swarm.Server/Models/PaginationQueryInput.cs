namespace Swarm.Server.Models
{
    public class PaginationQueryInput
    {
        private int _page = 1;
        private int _size = 30;

        public int? Page
        {
            get => _page;
            set
            {
                if (value == null || value <= 0)
                {
                    _page = 1;
                }
                else
                {
                    _page = value.Value;
                }
            }
        }

        public int? Size
        {
            get => _size;
            set
            {
                if (value == null || value <= 0)
                {
                    _size = 30;
                }
                else
                {
                    _size = value.Value;
                }
            }
        }

        public string Sort { get; set; }
        public bool SortByDesc { get; set; } = true;
    }
}