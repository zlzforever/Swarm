using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Swarm.Basic
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum State
    {
        Performing,
        Performed,
        Running,
        Paused,
        Exiting,
        Exit
    }
}