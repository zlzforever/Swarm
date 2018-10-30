using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    /// <summary>
    /// 任务状态
    /// </summary>
    [Table("SWARM_JOB_STATE")]
    public class JobState : EntityBase<int>
    {
        /// <summary>
        /// 任务编号
        /// </summary>
        [Column("JOB_ID")]
        [StringLength(32)]
        public string JobId { get; set; }

        /// <summary>
        /// 跟踪编号
        /// </summary>
        [Column("TRACE_ID")]
        [StringLength(32)]
        public string TraceId { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [Column("STATE")]
        [Required]
        public State State { get; set; } = State.Exit;

        /// <summary>
        /// 客户端编号
        /// </summary>
        [Column("CLIENT")]
        [StringLength(120)]
        public string Client { get; set; }

        /// <summary>
        /// 信息
        /// </summary>
        [Column("MSG")]
        [StringLength(500)]
        public string Msg { get; set; }

        /// <summary>
        /// 分片
        /// </summary>
        [Column("SHARDING")]
        [Required]
        public int Sharding { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATION_TIME")]
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [Column("LAST_MODIFICATION_TIME")]
        public DateTimeOffset? LastModificationTime { get; set; }
    }
}