namespace LofiMixer.ViewModels;

[Serializable]
public sealed class ComponentNotFoundException : Exception
{
    public ComponentNotFoundException(Type componentType, Exception inner) : base(componentType.FullName ?? string.Empty, inner) { }
}
