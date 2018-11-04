using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    /// <summary>
    /// 客户端
    /// </summary>
    public class Client : EntityBase<int>
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
        [StringLength(120)]
        public string Group { get; set; }

        /// <summary>
        /// 连接标识
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ConnectionId { get; set; }

        /// <summary>
        /// IP 地址
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Ip { get; set; }

        /// <summary>
        /// 操作系统
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Os { get; set; }
        
        /// <summary>
        /// CPU 核心数
        /// </summary>
        [Required]
        public int CoreCount { get; set; }
        
        /// <summary>
        /// 内存
        /// </summary>
        [Required]
        public int Memory { get; set; }
        
        /// <summary>
        /// 用户编号
        /// </summary>
        [Required]
        public int UserId { get; set; }
        
        /// <summary>
        /// 是否连接
        /// </summary>
        [Required]
        public bool IsConnected { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTimeOffset? LastModificationTime { get; set; }

        public override string ToString()
        {
            return $"[{ConnectionId}, {Name}, {Group}, {Ip}]";
        }
    }
}