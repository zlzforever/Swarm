using System;

namespace Swarm.Core.Common
{
    public static class GuidExtensions
    {
        public static Int64 ToInt64(this Guid guid)
        {
            byte[] buffer = guid.ToByteArray();
            return BitConverter.ToInt64(buffer, 0);
        }
    }
}