using System.Collections.Generic;

namespace Swarm.Basic.Common
{
    public static class DictionaryExtensions
    {
        public static TV GetValue<TK, TV>(this Dictionary<TK, TV> dic, TK key)
        {
            return dic.ContainsKey(key) ? dic[key] : default(TV);
        }
    }
}