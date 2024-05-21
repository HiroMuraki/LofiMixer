using HM.AppComponents;
using HM.AppComponents.AppService;
using LofiMixer.Models;
using LofiMixer.ViewModels;

namespace LofiMixer;

public sealed class MusicPlayedSignalArgs
{
    public required Uri PlayingMusicFile { get; init; }
}

public sealed class MusicPlayListReloadedArgs
{
    public required IReadOnlyCollection<Uri> MusicFiles { get; init; }
}

public sealed class MusicPlayerSettingsChangedArgs
{
    public MusicLoopMode? MusicLoopMode { get; init; }

    public float? Volume { get; init; }
}

public sealed class PlayMusicRequestedArgs
{
    public required Uri MusicFile { get; init; }
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
        public Signal<PlayMusicRequestedArgs> PlayMusicRequested { get; } = new();

        public Signal<MusicPlayerSettingsChangedArgs> MusicPlayerSettingsChanged { get; } = new();

        public Signal<MusicPlayListReloadedArgs> MusicPlayListReloaded { get; } = new();

        public Signal<MusicPlayedSignalArgs> MusicPlayed { get; } = new();

        public Signal<StatesChanged<AmbientSoundViewModel>> StateChanged { get; } = new();
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