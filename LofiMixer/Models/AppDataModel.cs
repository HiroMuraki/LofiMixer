using LofiMixer.Components;

namespace LofiMixer.Models;

public sealed record class AppDataModel
{
    public string MusicFileName { get; init; } = string.Empty;

    public double MusicVolume { get; init; } = double.MaxValue;

    public MusicLoopMode MusicLoopMode { get; init; }

    public Dictionary<string, double> AmbientSoundVolumes { get; init; } = [];
}
