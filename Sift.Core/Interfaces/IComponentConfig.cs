namespace Sift.Core.Interfaces
{
    // NOTE: Immutable, serialisable settings for a component. Runtime state belongs on the component itself
    public interface IComponentConfig
    {
        // NOTE: Throw ArgumentException for invalid settings, called whenever a component is (re)configured
        void Validate() { }
    }
}
