using CommunityToolkit.Mvvm.ComponentModel;
using HM.AppComponents;
using HM.AppComponents.AppDataSerializer;
using LofiMixer.Models;
using System.Collections.ObjectModel;

namespace LofiMixer.ViewModels;

public sealed class AmbientMixerResetArgs
{
    public AmbientMixerResetArgs(IReadOnlyCollection<AmbientSoundViewModel> ambientSounds)
    {
        AmbientSounds = ambientSounds;
    }

    public IReadOnlyCollection<AmbientSoundViewModel> AmbientSounds { get; }
}

public sealed class AmbientMixerViewModel : ObservableObject
{
    public static Signal<AmbientMixerResetArgs> AmbientMixerReset { get; } = new();

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
        AmbientMixerReset.Emit(new AmbientMixerResetArgs(_ambientSounds));
    }

    #region NonPublic
    private readonly ObservableCollection<AmbientSoundViewModel> _ambientSounds = [];
    #endregion
}