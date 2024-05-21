using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HM.AppComponents;

namespace LofiMixer.ViewModels;

public sealed partial class MusicViewModel :
    ObservableObject,
    IMutexSelectable
{
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
        }
    }

    bool IMutexSelectable.IsMutexSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value, nameof(IsSelected));
    }

    [RelayCommand]
    public void Play()
    {
        App.Current.Signals.PlayMusicRequested.Emit(new()
        {
            MusicFile = MusicUri
        });
    }

    #region NonPublic
    private readonly MutexSelector? _mutexSelector;
    private Uri _musicUri;
    private bool _isSelected;
    #endregion
}
