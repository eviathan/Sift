namespace Sift.Services
{
    public interface IKeyEventService<TKey> where TKey : struct, IConvertible
    {
        Dictionary<TKey, Action> KeyEvents { get; set; }

        void OnKeyDown(TKey key = default);

        void OnKeyUp(TKey key = default);

        void RegisterKeyEvent(TKey key, Action action);
    }
}