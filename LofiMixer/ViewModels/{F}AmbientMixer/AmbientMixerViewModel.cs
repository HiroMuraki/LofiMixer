using CommunityToolkit.Mvvm.ComponentModel;
using HM.AppComponents.AppDataSerializer;
using LofiMixer.Models;
using System.Collections.ObjectModel;

namespace LofiMixer.ViewModels;

public sealed class AmbientMixerViewModel : ObservableObject
{
    public IEnumerable<AmbientSoundViewModel> AmbientSounds => _ambientSounds;

    internal async Task ReloadAmbientSoundsAsync()
    {
        var appDataSerializer = AppDataJsonSerializer.Create(
            App.Current.AppPaths[App.PathNames.AmbientSoundDirectory].GetSubPath("Items.json"));

        _ambientSounds.Clear();
        await appDataSerializer.LoadAsync<AmbientSoundModel[]>(data =>
        {
            if (data is null)
            {
                return;
            }

            foreach (AmbientSoundModel model in data)
            {
                var musicUri = new Uri(App.Current.AppPaths[App.PathNames.AmbientSoundDirectory].GetSubPath(model.SoundFileName), UriKind.Absolute);
                var ambientSound = new AmbientSoundViewModel(musicUri)
                {
                    Name = model.Name,
                };
                _ambientSounds.Add(ambientSound);
            }
        });
        App.Current.Signals.AmbientSoundsReloaded.Emit(new()
        {
            AmbientSoundFiles = _ambientSounds.Select(x => x.SoundUri).ToList()
        });
    }

    #region NonPublic
    private readonly ObservableCollection<AmbientSoundViewModel> _ambientSounds = [];
    #endregion
}