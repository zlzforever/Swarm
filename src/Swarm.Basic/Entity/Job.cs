using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Swarm.Basic.Entity
{
    /// <summary>
    /// 任务
    /// </summary>
    public class Job : EntityBase<string>
    {
        private static readonly List<PropertyInfo> PropertyInfos;

        static Job()
        {
            PropertyInfos = typeof(Job).GetProperties().Where(p => p.Name != "Properties" && p.CanWrite).ToList();
        }

        #region Node FK

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

        #endregion             

        /// <summary>
        /// 任务名称
        /// </summary>
        [StringLength(120)]
        [Required]
        [MinLength(4)]
        public string Name { get; set; }

        /// <summary>
        /// 分组
        /// </summary>
        [StringLength(120)]
        [Required]
        [MinLength(4)]
        public string Group { get; set; }

        /// <summary>
        /// 触发器类型: Cron, Simple, etc
        /// </summary>
        [Required]
        public Trigger Trigger { get; set; }

        /// <summary>
        /// 回调类型: Http, WebSocket, Mq
        /// </summary>
        [Required]
        public Performer Performer { get; set; }

        /// <summary>
        /// 任务的执行器: 进程, 反射
        /// </summary>
        [Required]
        public Executor Executor { get; set; } 
        
        /// <summary>
        /// 任务负载
        /// </summary>
        [Required]
        public int Load { get; set; } = 1;
        
        /// <summary>
        /// 分片数
        /// </summary>
        [Required]
        public int Sharding { get; set; } = 1;

        /// <summary>
        /// 分片参数
        /// </summary>
        [StringLength(500)]
        public string ShardingParameters { get; set; }
        
        /// <summary>
        /// 是否允许并行执行
        /// </summary>
        public bool AllowConcurrent { get; set; }

        /// <summary>
        /// 任务描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 任务负责人
        /// </summary>
        [StringLength(120)]
        public string Owner { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTimeOffset? LastModificationTime { get; set; }

        /// <summary>
        /// 任务额外信息
        /// </summary>
        [NotMapped]
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

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