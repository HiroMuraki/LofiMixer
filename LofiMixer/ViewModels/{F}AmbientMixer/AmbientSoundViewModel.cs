using CommunityToolkit.Mvvm.ComponentModel;
using HM.AppComponents;

namespace LofiMixer.ViewModels;

public sealed class AmbientSoundViewModel : ObservableObject
{
    public static Signal<StatesChanged<AmbientSoundViewModel>> StateChanged { get; } = new();

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
            if (value < 0)
            {
                value = 0;
            }
            else if (value > 1)
            {
                value = 1;
            }

            SetProperty(ref _volume, value);
            StateChanged.Emit(new StatesChanged<AmbientSoundViewModel>(this));
        }
    }

    #region NonPublic
    private float _volume;
    private Uri _musicUri;
    private string _name = string.Empty;
    #endregion
}