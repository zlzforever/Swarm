namespace Swarm.Client
{
    public class ProcessKey
    {
        public string JobId { get; }
        public string TraceId { get; }
        public int Sharding { get; }

        public ProcessKey(string jobId, string traceId, int sharding)
        {
            JobId = jobId;
            TraceId = traceId;
            Sharding = sharding;
        }

        public override int GetHashCode()
        {
            return $"{JobId}_{TraceId}_{Sharding}".GetHashCode();
        }

        public override string ToString()
        {
            return $"{JobId}_{TraceId}_{Sharding}";
        }
    }
}