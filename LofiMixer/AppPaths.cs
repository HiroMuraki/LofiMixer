using HM.AppComponents;

namespace LofiMixer.ViewModels;

public class AppPaths
{
    public AppPath MusicDirectory { get; set; } = new([Environment.CurrentDirectory, "CustomRes", "Music"]);

    public AppPath AmbientSoundDirectory { get; set; } = new([Environment.CurrentDirectory, "CustomRes", "Ambient"]);

    public AppPath ConfigFile { get; set; } = new([Environment.CurrentDirectory, "Config.json"]);

    public void EnsureAppPathsCreated()
    {
        MusicDirectory.EnsureCreated();
        AmbientSoundDirectory.EnsureCreated();
    }
}
