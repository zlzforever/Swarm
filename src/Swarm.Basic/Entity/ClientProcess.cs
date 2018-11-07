using System;
using System.ComponentModel.DataAnnotations;

namespace Swarm.Basic.Entity
{
    public class ClientProcess : EntityBase<int>
    {
        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        [StringLength(120)]
        public string Name { get; set; }

        /// <summary>
        /// 分组
        /// </summary>
        [Required]
        [StringLength(120)]
        public string Group { get; set; }

        /// <summary>
        /// 任务编号
        /// </summary>
        [Required]
        [StringLength(120)]
        public string JobId { get; set; }

        /// <summary>
        /// 跟踪编号
        /// </summary>
        [StringLength(32)]
        public string TraceId { get; set; }       

        /// <summary>
        /// 分片
        /// </summary>
        [Required]
        public int Sharding { get; set; }
        
        /// <summary>
        /// 进程编号
        /// </summary>
        [Required]
        public int ProcessId { get; set; }
        
        /// <summary>
        /// 跟踪编号
        /// </summary>
        [StringLength(32)]
        public State State { get; set; }
        
        /// <summary>
        /// 应用程序
        /// </summary>
        [Required]
        [StringLength(120)]
        public string App { get; set; }

        /// <summary>
        /// 执行参数
        /// </summary>
        [Required]
        public string AppArguments { get; set; }
        
        /// <summary>
        /// 信息
        /// </summary>
        [StringLength(500)]
        public string Msg { get; set; }

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