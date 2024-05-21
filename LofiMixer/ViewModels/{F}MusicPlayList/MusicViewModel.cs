using CommunityToolkit.Mvvm.ComponentModel;
using HM.AppComponents;

namespace LofiMixer.ViewModels;

public sealed class PlayMusicRequestedArgs
{
    public PlayMusicRequestedArgs(MusicViewModel music)
    {
        Music = music;
    }

    public MusicViewModel Music { get; }
}

public sealed class MusicViewModel : ObservableObject, IMutexSelectable
{
    public static Signal<PlayMusicRequestedArgs> PlayMusicRequested { get; } = new();

    public MusicViewModel(Uri musicFile, MutexSelector? mutexSelector)
    {
        _musicUri = musicFile;
        _mutexSelector = mutexSelector;
    }

    public string MusicName => Path.GetFileNameWithoutExtension(MusicUri.LocalPath ?? string.Empty);

    public Uri MusicUri
    {
        get => _musicUri;
        set => SetProperty(ref _musicUri, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_mutexSelector is not null)
            {
                if (value)
                {
                    this.MutexSelect(_mutexSelector);
                }
                else
                {
                    this.MutexUnselect(_mutexSelector);
                }
            }
            else
            {
                SetProperty(ref _isSelected, value);
            }

            if (_isSelected)
            {
                Play();
            }
        }
    }

    bool IMutexSelectable.IsMutexSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value, nameof(IsSelected));
    }

    public void Play()
    {
        PlayMusicRequested.Emit(new PlayMusicRequestedArgs(this));
    }

    #region NonPublic
    private readonly MutexSelector? _mutexSelector;
    private Uri _musicUri;
    private bool _isSelected;
    #endregion
}
