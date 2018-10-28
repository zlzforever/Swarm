namespace Swarm.Client
{
    public class ApiResult
    {
        public const int SuccessCode = 200;
        
        public int Code { get; set; }

        public string Msg { get; set; }
    }
}