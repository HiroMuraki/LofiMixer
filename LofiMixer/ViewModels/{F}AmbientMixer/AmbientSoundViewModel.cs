using CommunityToolkit.Mvvm.ComponentModel;
using HM.Common;

namespace LofiMixer.ViewModels;

public sealed class AmbientSoundViewModel : ObservableObject
{
    public AmbientSoundViewModel(Uri musicUri)
    {
        _soundUri = musicUri;
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public Uri SoundUri
    {
        get => _soundUri;
        set => SetProperty(ref _soundUri, value);
    }

    public float Volume
    {
        get => _volume;
        set
        {
            value = ValueClamper.Clamp(value, 0, 1);
            SetProperty(ref _volume, value);
            App.Current.Signals.AmbientSoundStateChanged.Emit(new()
            {
                SoundUri = SoundUri,
                Volume = Volume
            });
        }
    }

    #region NonPublic
    private float _volume;
    private Uri _soundUri;
    private string _name = string.Empty;
    #endregion
}