using System;
using System.ComponentModel.DataAnnotations;

namespace Swarm.Basic.Entity
{
    /// <summary>
    /// 任务状态
    /// </summary>
    public class JobState : EntityBase<int>
    {
        /// <summary>
        /// 任务编号
        /// </summary>
        [StringLength(32)]
        public string JobId { get; set; }

        /// <summary>
        /// 跟踪编号
        /// </summary>
        [StringLength(32)]
        public string TraceId { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [Required]
        public State State { get; set; } = State.Exit;

        /// <summary>
        /// 客户端编号
        /// </summary>
        [StringLength(120)]
        public string Client { get; set; }

        /// <summary>
        /// 信息
        /// </summary>
        [StringLength(500)]
        public string Msg { get; set; }

        /// <summary>
        /// 分片
        /// </summary>
        [Required]
        public int Sharding { get; set; }

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