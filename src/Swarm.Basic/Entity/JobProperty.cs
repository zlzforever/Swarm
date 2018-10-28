using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    [Table("SWARM_JOB_PROPERTIES")]
    public class JobProperty: IEntity<int>
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }
        
        [StringLength(32)]
        [Column("JOB_ID")]
        public string JobId { get; set; }
        
        [StringLength(32)]
        [Column("NAME")]
        public string Name { get; set; }
        
        [Column("VALUE")]
        [StringLength(250)]
        public  string Value { get; set; }
        
        [Required]
        [Column("CREATION_TIME")]
        public DateTimeOffset CreationTime { get; set; }
    }
}