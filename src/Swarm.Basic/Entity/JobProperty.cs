using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    /// <summary>
    /// 任务额外信息
    /// </summary>
    [Table("SWARM_JOB_PROPERTIES")]
    public class JobProperty : EntityBase<int>
    {
        /// <summary>
        /// 任务编号
        /// </summary>
        [StringLength(32)]
        [Column("JOB_ID")]
        public string JobId { get; set; }

        /// <summary>
        /// 属性名称
        /// </summary>
        [StringLength(32)]
        [Column("NAME")]
        public string Name { get; set; }

        /// <summary>
        /// 属性值
        /// </summary>
        [Column("VALUE")]
        [StringLength(250)]
        public string Value { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATION_TIME")]
        public DateTimeOffset CreationTime { get; set; }
    }
}