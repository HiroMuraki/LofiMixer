using HM.AppComponents;
using HM.AppComponents.AppService;
using LofiMixer.Models;

namespace LofiMixer;

public sealed class MusicPlayedSignalArgs
{
    public required Uri PlayingMusicFile { get; init; }
}

public sealed class MusicPlayListReloadedSignalArgs
{
    public required IReadOnlyCollection<Uri> MusicFiles { get; init; }
}

public sealed class MusicPlayerSettingsChangedSignalArgs
{
    public MusicLoopMode? MusicLoopMode { get; init; }

    public float? Volume { get; init; }
}

public sealed class PlayMusicRequestedSignalArgs
{
    public required Uri MusicFile { get; init; }
}

public sealed class AmbientSoundsReloadedSignalArgs
{
    public required IReadOnlyCollection<Uri> AmbientSoundFiles { get; init; }
}

public sealed class AmbientSoundSettingChangedSignalArgs
{
    public required Uri SoundUri { get; init; }

    public float? Volume { get; init; }
}

public sealed class App
{
    public enum PathNames
    {
        MusicDirectory,
        AmbientSoundDirectory,
        ConfigFile,
    }

    public sealed class AppSignals
    {
        public Signal<PlayMusicRequestedSignalArgs> PlayMusicRequested { get; } = new();

        public Signal<MusicPlayerSettingsChangedSignalArgs> MusicPlayerSettingsChanged { get; } = new();

        public Signal<MusicPlayListReloadedSignalArgs> MusicPlayListReloaded { get; } = new();

        public Signal<MusicPlayedSignalArgs> MusicPlayed { get; } = new();

        public Signal<AmbientSoundsReloadedSignalArgs> AmbientSoundsReloaded { get; } = new();

        public Signal<AmbientSoundSettingChangedSignalArgs> AmbientSoundStateChanged { get; } = new();
    }

    public static App Current { get; } = new();

    public AppServiceProvider ServiceProvider { get; } = new();

    public AppComponents Components { get; } = [];

    public AppSignals Signals { get; } = new();

    public AppPaths<PathNames> AppPaths { get; } = new()
    {
        [PathNames.MusicDirectory] = new([Environment.CurrentDirectory, "CustomRes", "Music"], AppPathType.Directory),
        [PathNames.AmbientSoundDirectory] = new([Environment.CurrentDirectory, "CustomRes", "Ambient"], AppPathType.Directory),
        [PathNames.ConfigFile] = new([Environment.CurrentDirectory, "Config.json"], AppPathType.File),
    };
}