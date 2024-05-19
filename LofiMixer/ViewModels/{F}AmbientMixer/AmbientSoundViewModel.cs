using CommunityToolkit.Mvvm.ComponentModel;
using LofiMixer.Components;

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

    public double Volume
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
            ServiceHelper.ActWith<IAmbientRemixer>(x =>
            {
                x.Remix(this);
            });
        }
    }

    #region NonPublic
    private double _volume;
    private Uri _musicUri;
    private string _name = string.Empty;
    #endregion
}