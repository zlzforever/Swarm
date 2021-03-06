using System;
using System.Collections.Generic;
using Quartz;
using Swarm.Basic;
using Swarm.Basic.Common;

namespace Swarm.Core.Impl
{
    public class SimpleTriggerBuilder : ITriggerBuilder
    {
        public ITrigger Build(string id, Dictionary<string, string> properties)
        {
            var builder = TriggerBuilder.Create().WithIdentity(id);
            var startAt = properties.GetValue(SwarmConsts.StartAtProperty);
            if (DateTimeOffset.TryParse(startAt, out DateTimeOffset sa))
            {
                builder.StartAt(sa);
            }

            var endAt = properties.GetValue(SwarmConsts.EndAtProperty);
            if (DateTimeOffset.TryParse(endAt, out DateTimeOffset ea))
            {
                builder.EndAt(ea);
            }

            builder.WithSimpleSchedule(bd =>
            {
                // 设置为 -1 则为永远重复
                var rcStr = properties.GetValue(SwarmConsts.RepeatCountProperty);
                if (!string.IsNullOrWhiteSpace(rcStr) && int.TryParse(rcStr, out var rc))
                {
                    bd.WithRepeatCount(rc);
                }

                var isStr = properties.GetValue(SwarmConsts.IntervalProperty);
                if (!string.IsNullOrWhiteSpace(rcStr) && int.TryParse(isStr, out var isd))
                {
                    bd.WithIntervalInSeconds(isd);
                }

                // TODO: 做更多的设置
            });
            return builder.Build();
        }
    }
}