using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{ 
    [Table("SWARM_NODES")]
    public class Node : IEntity<int>
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key] 
        [Column("ID")]
        public int Id { get; set; }
        
        [StringLength(250)]
        [Required]
        [Column("HOST")]
        public string Host { get; set; }
        
        [StringLength(250)]
        [Required]
        [Column("NAME")]
        public string Name { get; set; }
        
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