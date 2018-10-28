using System;

namespace Swarm.Client
{
    public class SwarmClientException : Exception
    {
        public SwarmClientException(string msg) : base(msg)
        {
        }
    }
}