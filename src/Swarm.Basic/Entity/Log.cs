using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    [Table("SWARM_LOGS")]
    public class Log : IEntity<int>
    {
        [Key] [Column("ID")] public int Id { get; set; }

        [Column("JOB_ID")] [StringLength(32)] public string JobId { get; set; }

        [Column("TRACE_ID")]
        [StringLength(32)]
        public string TraceId { get; set; }

        [Column("MSG")] public string Msg { get; set; }

        [Required] [Column("CREATION_TIME")] public DateTimeOffset CreationTime { get; set; }
    }
}