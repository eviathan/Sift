namespace Sift.Sequencer.Interfaces
{
    public interface IPoolItem<TPool>
    {
        TPool Pool { get; }
    }
}