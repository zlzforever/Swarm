using System;
using System.ComponentModel.DataAnnotations;

namespace Swarm.Basic.Entity
{
    /// <summary>
    /// 日志
    /// </summary>
    public class Log : EntityBase<int>
    {
        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        [StringLength(120)]
        public string ClientName { get; set; }

        /// <summary>
        /// 分组
        /// </summary>
        [Required]
        [StringLength(120)]
        public string ClientGroup { get; set; }

        /// <summary>
        /// 任务编号
        /// </summary>
        [StringLength(32)]
        [Required]
        public string JobId { get; set; }

        /// <summary>
        /// 跟踪编号
        /// </summary>
        [StringLength(32)]
        [Required]
        public string TraceId { get; set; }

        /// <summary>
        /// 分片
        /// </summary>
        [Required]
        public int Sharding { get; set; }

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