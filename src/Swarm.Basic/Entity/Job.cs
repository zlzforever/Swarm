using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    [Table("SWARM_JOBS")]
    public class Job
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        [Column("ID")]
        [StringLength(32)]
        public string Id { get; set; }

        [Column("STATE")]
        public State State { get; set; }
        
        [Required]
        [Column("TRIGGER")]
        public Trigger Trigger { get; set; }
        
        /// <summary>
        /// 回调类型: Http, WebSocket, Mq
        /// </summary>
        [Required]
        [Column("PERFORMER")]
        public Performer Performer { get; set; }

        [Column("EXECUTER")]
        [Required] 
        public Executor Executor { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        [StringLength(120)]
        [Required]
        [Column("NAME")]
        public string Name { get; set; }

        /// <summary>
        /// 分组
        /// </summary>
        [StringLength(120)]
        [Required]
        [Column("GROUP")]
        public string Group { get; set; }

        /// <summary>
        /// 任务负载
        /// </summary>
        [Required]
        [Column("LOAD")]
        public int Load { get; set; } = 0;

        /// <summary>
        /// 分片数
        /// </summary>
        [Required]
        [Column("SHARDING")]
        public int Sharding { get; set; } = 1;

        /// <summary>
        /// 分片参数
        /// </summary>
        [StringLength(500)]
        [Column("SHARDING_PARAMETERS")]
        public string ShardingParameters { get; set; }

        /// <summary>
        /// 任务描述
        /// </summary>
        [StringLength(500)]
        [Column("DESCRIPTION")]
        public string Description { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        [Required]
        [Column("RETRY_COUNT")]
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// 
        /// </summary>
        [StringLength(120)]
        [Column("OWNER")]
        public string Owner { get; set; }

        /// <summary>
        /// 是否并行执行
        /// </summary>
        [Column("CONCURRENT_EXECUTION_DISALLOWED")]
        public bool ConcurrentExecutionDisallowed { get; set; }
        
        [Required]
        [Column("CREATION_TIME")]
        public DateTimeOffset CreationTime { get; set; }
        
        [Column("LAST_MODIFICATION_TIME")]
        public DateTimeOffset? LastModificationTime { get; set; }
        
    }
}