namespace LofiMixer.Models;

public sealed record class AppDataModel
{
    public string MusicFileName { get; init; } = string.Empty;

    public float MusicVolume { get; init; }

    public MusicLoopMode MusicLoopMode { get; init; }

    public Dictionary<string, float> AmbientSoundVolumes { get; init; } = [];
}
