using CommunityToolkit.Mvvm.ComponentModel;
using HM.AppComponents.AppDataSerializer;
using HM.AppComponents.AppService;
using LofiMixer.Components;
using LofiMixer.Models;
using System.Collections.ObjectModel;

namespace LofiMixer.ViewModels;

public sealed class AmbientMixerViewModel : ObservableObject
{
    public IEnumerable<AmbientSoundViewModel> AmbientSounds => _ambientSounds;

    internal async Task ReloadAmbientSoundsAsync()
    {
        var appDataSerializer = AppDataJsonSerializer.Create(
            App.Current.AppPaths.AmbientSoundDirectory.GetSubPath("Items.json"));

        App.Current.ServiceProvider.GetServiceThen<IAmbientRemixer>(x =>
        {
            x.Reset();
        });

        _ambientSounds.Clear();
        await appDataSerializer.LoadAsync<AmbientSoundModel[]>(data =>
        {
            foreach (AmbientSoundModel model in data.GetOr(() => []))
            {
                var musicUri = new Uri(App.Current.AppPaths.AmbientSoundDirectory.GetSubPath(model.SoundFileName), UriKind.Absolute);
                var ambientSound = new AmbientSoundViewModel(musicUri)
                {
                    Name = model.Name,
                };
                _ambientSounds.Add(ambientSound);
            }
        });
    }

    #region NonPublic
    private readonly ObservableCollection<AmbientSoundViewModel> _ambientSounds = [];
    #endregion
}