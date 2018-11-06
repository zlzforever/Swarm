using System;
using System.Collections.Generic;
using Quartz;
using Swarm.Basic;
using Swarm.Basic.Common;

namespace Swarm.Core.Impl
{
    public class CronTriggerBuilder : ITriggerBuilder
    {
        public ITrigger Build(string id, Dictionary<string, string> properties)
        {
            return TriggerBuilder.Create().WithCronSchedule(properties.GetValue(SwarmConsts.CronProperty),
                    bd =>
                    {
                        var tzpStr = properties.GetValue(SwarmConsts.TimeZoneProperty);
                        if (string.IsNullOrWhiteSpace(tzpStr)) return;
                        try
                        {
                            var tzp = TimeZoneInfo.FromSerializedString(tzpStr);
                            bd.InTimeZone(tzp);
                        }
                        catch
                        {
                            throw new SwarmException("TimeZone string is uncorrected");
                        }
                    })
                .WithIdentity(id).Build();
        }
    }
}