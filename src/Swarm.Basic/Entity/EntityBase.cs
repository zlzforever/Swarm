using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swarm.Basic.Entity
{
    public abstract class EntityBase<T> : IEntity<T>
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        public T Id { get; set; }
    }
}