namespace LofiMixer.Models;

public sealed record class AmbientSoundModel
{
    public string Name { get; init; } = string.Empty;

    public string SoundFileName { get; init; } = string.Empty;
}