using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Swarm.Basic.Entity
{
    [Table("SWARM_JOBS")]
    public class Job : IEntity<string>
    {
        private static readonly List<PropertyInfo> PropertyInfos;

        static Job()
        {
            PropertyInfos = typeof(Job).GetProperties().Where(p => p.Name != "Properties" && p.CanWrite).ToList();
        }

        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        [Column("ID")]
        [StringLength(32)]
        public string Id { get; set; }

        /// <summary>
        /// 任务的状态
        /// </summary>
        [Column("STATE")]
        public State State { get; set; }

        /// <summary>
        /// 触发器类型: Cron, Simple, etc
        /// </summary>
        [Required]
        [Column("TRIGGER")]
        public Trigger Trigger { get; set; }

        /// <summary>
        /// 回调类型: Http, WebSocket, Mq
        /// </summary>
        [Required]
        [Column("PERFORMER")]
        public Performer Performer { get; set; }

        /// <summary>
        /// 任务的执行器: 进程, 反射
        /// </summary>
        [Column("EXECUTOR")]
        [Required]
        public Executor Executor { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        [StringLength(120)]
        [Required]
        [MinLength(4)]
        [Column("NAME")]
        public string Name { get; set; }

        /// <summary>
        /// 分组
        /// </summary>
        [StringLength(120)]
        [Required]
        [Column("GROUP")]
        [MinLength(4)]
        public string Group { get; set; }

        /// <summary>
        /// Swarm 节点
        /// </summary>
        [StringLength(120)]
        [Column("NODE")]
        [MinLength(4)]
        public string Node { get; set; }

        /// <summary>
        /// 任务负载
        /// </summary>
        [Required]
        [Column("LOAD")]
        public int Load { get; set; } = 1;

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

        [NotMapped] public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        internal List<object[]> ToPropertyArray()
        {
            var result = new List<object[]>();
            foreach (var property in Properties)
            {
                result.Add(new object[] {property.Key, property.Value});
            }

            foreach (var propertyInfo in PropertyInfos)
            {
                var value = propertyInfo.GetValue(this);
                if (value != null)
                {
                    result.Add(new[] {propertyInfo.Name, value});
                }
            }

            return result;
        }
    }
}