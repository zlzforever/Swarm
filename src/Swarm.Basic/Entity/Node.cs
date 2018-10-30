using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{ 
    /// <summary>
    /// Swarm 节点
    /// </summary>
    [Table("SWARM_NODES")]
    public class Node : EntityBase<int>
    {
        /// <summary>
        /// 节点地址
        /// </summary>
        [StringLength(250)]
        [Required]
        [Column("HOST")]
        public string Host { get; set; }
        
        /// <summary>
        /// 节点名称
        /// </summary>
        [StringLength(250)]
        [Required]
        [Column("NAME")]
        public string Name { get; set; }
        
        /// <summary>
        /// 节点分组
        /// </summary>
        [StringLength(250)]
        [Required]
        [Column("GROUP")]
        public string Group { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATION_TIME")]
        public DateTimeOffset CreationTime { get; set; }
    }
}