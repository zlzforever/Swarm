using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    [Table("SWARM_CLIENTS")]
    public class Client : IEntity<int>
    {
        [Key] [Column("ID")] public int Id { get; set; }

        [Required]
        [StringLength(120)]
        [Column("NAME")]
        public string Name { get; set; }

        [StringLength(120)] [Column("GROUP")] public string Group { get; set; }

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

        [Column("IS_CONNECTED")] public bool IsConnected { get; set; }

        [Required] [Column("CREATION_TIME")] public DateTimeOffset CreationTime { get; set; }

        [Column("LAST_MODIFICATION_TIME")] public DateTimeOffset? LastModificationTime { get; set; }
    }
}