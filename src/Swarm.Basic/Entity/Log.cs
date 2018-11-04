using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    /// <summary>
    /// 日志
    /// </summary>
    public class Log : EntityBase<int>
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
        /// 日志信息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTimeOffset CreationTime { get; set; }
    }
}