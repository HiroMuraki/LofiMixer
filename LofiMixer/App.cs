using HM.AppComponents;
using HM.AppComponents.AppService;

namespace LofiMixer;

public sealed class App
{
    public enum PathNames
    {
        MusicDirectory,
        AmbientSoundDirectory,
        ConfigFile,
    }

    public static App Current { get; } = new();

    public AppServiceProvider ServiceProvider { get; } = new();

    public AppPaths<PathNames> AppPaths { get; } = new()
    {
        [PathNames.MusicDirectory] = new([Environment.CurrentDirectory, "CustomRes", "Music"], AppPathType.Directory),
        [PathNames.AmbientSoundDirectory] = new([Environment.CurrentDirectory, "CustomRes", "Ambient"], AppPathType.Directory),
        [PathNames.ConfigFile] = new([Environment.CurrentDirectory, "Config.json"], AppPathType.File),
    };

    public AppComponents Components { get; } = [];
}