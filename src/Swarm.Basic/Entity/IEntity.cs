namespace Swarm.Basic.Entity
{
    public interface IEntity<T>
    {
        T Id { get; set; }
    }
}