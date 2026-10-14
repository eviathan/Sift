namespace Sift.Core.Interfaces
{
    public interface IPoolItem<TPool>
    {
        TPool Pool { get; }
    }
}