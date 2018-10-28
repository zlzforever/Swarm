using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Swarm.Basic
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Trigger
    {
        Cron,
        Simple,
        DailyTimeInterval,
        CalendarInterval
    }
}