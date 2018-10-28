using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    [Table("SWARM_CLIENTS")]
    public class Client
    {        
        [Key]
        [Required]
        [StringLength(120)]
        [Column("NAME")]
        public string Name { get; set; }
        
        [StringLength(120)]
        [Column("GROUP")]
        public string Group { get; set; }
        
        /// <summary>
        /// 需要唯一索引
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("CONNECTION_ID")]
        public string ConnectionId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Column("IP")]
        public string Ip { get; set; }
        
        [Required]
        [Column("CREATION_TIME")]
        public DateTimeOffset CreationTIme { get; set; }
    }
}