using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    /// <summary>
    /// 客户端
    /// </summary>
    [Table("SWARM_CLIENTS")]
    public class Client : EntityBase<int>
    {
        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        [StringLength(120)]
        [Column("NAME")]
        public string Name { get; set; }

        /// <summary>
        /// 分组
        /// </summary>
        [StringLength(120)]
        [Column("GROUP")]
        public string Group { get; set; }

        /// <summary>
        /// 连接标识
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("CONNECTION_ID")]
        public string ConnectionId { get; set; }

        /// <summary>
        /// IP 地址
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("IP")]
        public string Ip { get; set; }

        /// <summary>
        /// 是否连接
        /// </summary>
        [Column("IS_CONNECTED")]
        public bool IsConnected { get; set; }

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

        public override string ToString()
        {
            return $"[{ConnectionId}, {Name}, {Group}, {Ip}]";
        }
    }
}