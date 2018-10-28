using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    [Table("SWARM_JOB_STATE")]
    public class JobState
    {
        [Column("JOB_ID")]
        [StringLength(32)]
        public string JobId { get; set; }
        
        [Column("TRACE_ID")]
        [StringLength(32)]
        public string TraceId { get; set; }

        [Column("STATE")] [Required] public State State { get; set; } = State.Exit;
        
        [Column("CLIENT")]
        [StringLength(120)]
        public string Client { get; set; }
        
        [Column("MSG")]
        [StringLength(500)]
        public string Msg { get; set; }
        
        [Required]
        [Column("CREATION_TIME")]
        public DateTimeOffset CreationTime { get; set; }
        
        [Column("LAST_MODIFICATION_TIME")]
        public DateTimeOffset? LastModificationTime { get; set; }
    }
}