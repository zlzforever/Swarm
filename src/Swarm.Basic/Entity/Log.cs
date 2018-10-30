using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    /// <summary>
    /// 日志
    /// </summary>
    [Table("SWARM_LOGS")]
    public class Log : EntityBase<int>
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
        /// 日志信息
        /// </summary>
        [Column("MSG")]
        public string Msg { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATION_TIME")]
        public DateTimeOffset CreationTime { get; set; }
    }
}