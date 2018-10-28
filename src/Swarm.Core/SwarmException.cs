using System;

namespace Swarm.Core
{
    public class SwarmException : Exception
    {
        public SwarmException(string msg) : base(msg)
        {
        }
    }
}