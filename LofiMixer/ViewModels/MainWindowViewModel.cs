using CommunityToolkit.Mvvm.ComponentModel;
using HM.AppComponents.AppDataSerializer;
using HM.Common;
using LofiMixer.Models;

namespace LofiMixer.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    public MusicPlayListViewModel MusicPlayList { get; } = new();

    public AmbientMixerViewModel AmbientMixer { get; } = new();

    public async Task SaveStatesAsync()
    {
        var appDataSerializer = AppDataJsonSerializer.Create(App.Current.AppPaths.ConfigFile.Path);

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
        var appDataSerializer = AppDataJsonSerializer.Create(App.Current.AppPaths.ConfigFile.Path);

        await AmbientMixer.ReloadAmbientSoundsAsync();
        await MusicPlayList.ReloadMusicListAsync();
        await appDataSerializer.LoadAsync<AppDataModel>(data =>
        {
            data.GetThen(d =>
            {
                MusicPlayList.MusicVolume = d.MusicVolume;
                MusicPlayList.MusicLoopMode = d.MusicLoopMode;
                var ambientSoundNameMap = AmbientMixer.AmbientSounds.ToDictionary(k => k.Name, v => v);
                foreach ((string name, double volume) in d.AmbientSoundVolumes)
                {
                    Option<AmbientSoundViewModel> ambientSound = ambientSoundNameMap.GetValueOrDefault(name);
                    ambientSound.GetThen(a =>
                    {
                        a.Volume = volume;
                    });
                }
                MusicPlayList.MusicList.FirstOrDefault(m => m.MusicName == d.MusicFileName).AsOption().GetThen(x =>
                {
                    x.Play();
                });
            });
        });
    }
}
