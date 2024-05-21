using CommunityToolkit.Mvvm.ComponentModel;
using HM.AppComponents;
using HM.Common;

namespace LofiMixer.ViewModels;

public sealed class AmbientSoundViewModel : ObservableObject
{
    public AmbientSoundViewModel(Uri musicUri)
    {
        _musicUri = musicUri;
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public Uri MusicUri
    {
        get => _musicUri;
        set => SetProperty(ref _musicUri, value);
    }

    public float Volume
    {
        get => _volume;
        set
        {
            value = ValueClamper.Clamp(value, 0, 1);
            SetProperty(ref _volume, value);
            App.Current.Signals.StateChanged.Emit(new StatesChanged<AmbientSoundViewModel>(this));
        }
    }

    #region NonPublic
    private float _volume;
    private Uri _musicUri;
    private string _name = string.Empty;
    #endregion
}