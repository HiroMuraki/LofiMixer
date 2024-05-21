using CommunityToolkit.Mvvm.ComponentModel;
using HM.AppComponents.AppDataSerializer;
using LofiMixer.Models;

namespace LofiMixer.ViewModels;

public sealed class MainWindowViewModel :
    ObservableObject
{
    public MusicPlayListViewModel MusicPlayList { get; } = new();

    public AmbientMixerViewModel AmbientMixer { get; } = new();

    public async Task SaveStatesAsync()
    {
        var appDataSerializer = AppDataJsonSerializer.Create(App.Current.AppPaths[App.PathNames.ConfigFile].Path);

        var appData = new AppDataModel()
        {
            MusicFileName = MusicPlayList.MusicList.FirstOrDefault(x => x.IsSelected)?.MusicName ?? string.Empty,
            MusicVolume = MusicPlayList.MusicVolume,
            MusicLoopMode = MusicPlayList.MusicLoopMode,
            AmbientSoundVolumes = AmbientMixer.AmbientSounds.ToDictionary(k => k.Name, v => v.Volume),
        };

        await appDataSerializer.SaveAsync(appData);
    }

    public async Task LoadStatesAsync()
    {
        var appDataSerializer = AppDataJsonSerializer.Create(App.Current.AppPaths[App.PathNames.ConfigFile].Path);

        await AmbientMixer.ReloadAmbientSoundsAsync();
        await MusicPlayList.ReloadMusicListAsync();
        await appDataSerializer.LoadAsync<AppDataModel>(data =>
        {
            if (data is null)
            {
                return;
            }

            MusicPlayList.MusicVolume = data.MusicVolume;
            MusicPlayList.MusicLoopMode = data.MusicLoopMode;
            var ambientSoundNameMap = AmbientMixer.AmbientSounds.ToDictionary(k => k.Name, v => v);
            foreach ((string name, float volume) in data.AmbientSoundVolumes)
            {
                AmbientSoundViewModel? ambientSound = ambientSoundNameMap.GetValueOrDefault(name);
                if (ambientSound is not null)
                {
                    ambientSound.Volume = volume;
                }
            }

            (MusicPlayList.MusicList.FirstOrDefault(m => m.MusicName == data.MusicFileName)
                ?? MusicPlayList.MusicList.FirstOrDefault())?.Play();
        });
    }
}
