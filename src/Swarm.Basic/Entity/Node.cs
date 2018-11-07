using System;
using System.ComponentModel.DataAnnotations;

namespace Swarm.Basic.Entity
{
    /// <summary>
    /// Swarm 节点
    /// </summary>
    public class Node : EntityBase<int>
    {
        /// <summary>
        /// Sched 名称
        /// </summary>
        [StringLength(250)]
        [Required]
        public string SchedName { get; set; }

        /// <summary>
        /// Sched 实例标识
        /// </summary>
        [StringLength(32)]
        [Required]
        public string SchedInstanceId { get; set; }

        /// <summary>
        /// 数据库
        /// </summary>
        [StringLength(250)]
        [Required]
        public string Provider { get; set; }

        /// <summary>
        /// 节点地址
        /// </summary>
        [StringLength(250)]
        [Required]
        public string ConnectionString { get; set; }

        /// <summary>
        /// 是否连接
        /// </summary>
        [Required]
        public bool IsConnected { get; set; }

        /// <summary>
        /// 历史总触发次数
        /// </summary>
        public long TriggerTimes { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTimeOffset? LastModificationTime { get; set; }

        public bool IsAvailable()
        {
            return IsConnected && (DateTime.Now - (LastModificationTime ?? CreationTime)).Seconds <
                   SwarmConsts.NodeOfflineInterval;
        }
    }
}