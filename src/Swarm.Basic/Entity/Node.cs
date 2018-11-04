using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    /// <summary>
    /// Swarm 节点
    /// </summary>
    public class Node : EntityBase<int>
    {
        /// <summary>
        /// 节点地址
        /// </summary>
        [StringLength(250)]
        [Required]
        public string ConnectionString { get; set; }

        /// <summary>
        /// 数据库
        /// </summary>
        [StringLength(250)]
        [Required]
        public string Provider { get; set; }

        /// <summary>
        /// 节点名称
        /// </summary>
        [StringLength(250)]
        [Required]
        public string SchedName { get; set; }

        /// <summary>
        /// 历史总触发次数
        /// </summary>
        public long TriggerTimes { get; set; }

        /// <summary>
        /// 节点分组
        /// </summary>
        [StringLength(32)]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTimeOffset? LastModificationTime { get; set; }
    }
}