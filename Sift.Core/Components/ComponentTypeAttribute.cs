namespace Sift.Core.Components
{
    // NOTE: The name is the component's "type" in saved sequences, so don't change it once released
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ComponentTypeAttribute : Attribute
    {
        public string Name { get; }

        public ComponentTypeAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
