using CommunityToolkit.Mvvm.ComponentModel;
using HM.AppComponents;
using HM.AppComponents.AppService;
using HM.AppComponents.AppService.Services;
using HM.Common;
using LofiMixer.Models;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace LofiMixer.ViewModels;

public sealed class MusicPlayListViewModel :
    ObservableObject,
    ISignalReceiver<MusicPlayedSignalArgs>
{
    public MusicPlayListViewModel()
    {
        App.Current.Signals.MusicPlayed.Register(this);
    }

    public IEnumerable<MusicViewModel> MusicList => _musicList;

    public MusicLoopMode MusicLoopMode
    {
        get => _musicLoopMode;
        set
        {
            SetProperty(ref _musicLoopMode, value);
            App.Current.Signals.MusicPlayerSettingsChanged.Emit(new MusicPlayerSettingsChangedArgs
            {
                MusicLoopMode = MusicLoopMode,
            });
        }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            value = ValueClamper.Clamp(value, 0, 1);

            SetProperty(ref _musicVolume, value);
            App.Current.Signals.MusicPlayerSettingsChanged.Emit(new MusicPlayerSettingsChangedArgs
            {
                Volume = MusicVolume,
            });
        }
    }

    internal async Task ReloadMusicListAsync()
    {
        string[] musicFiles = App.Current.AppPaths[App.PathNames.MusicDirectory]
            .EnumerateFiles()
            .Where(x => _supportedMusicFormats.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase))
            .ToArray();

        _musicList.Clear();
        var mutexSelector = new MutexSelector();
        foreach (string musicFile in musicFiles)
        {
            var musicUri = new Uri(App.Current.AppPaths[App.PathNames.MusicDirectory].GetSubPath(musicFile), UriKind.Absolute);
            var music = new MusicViewModel(musicUri, mutexSelector);
            _musicList.Add(music);
            await Task.Delay(1);
        }

        App.Current.Signals.MusicPlayerSettingsChanged.Emit(new MusicPlayerSettingsChangedArgs
        {
            Volume = MusicVolume,
            MusicLoopMode = MusicLoopMode,
        });
        App.Current.Signals.MusicPlayListReloaded.Emit(new MusicPlayListReloadedArgs
        {
            MusicFiles = _musicList.Select(x => x.MusicUri).ToImmutableArray(),
        });
    }

    #region NonPublic
    private static readonly HashSet<string> _supportedMusicFormats =
    [
        ".mp3", ".ogg",
    ];
    private readonly ObservableCollection<MusicViewModel> _musicList = [];
    private float _musicVolume = 1;
    private MusicLoopMode _musicLoopMode = MusicLoopMode.Order;
    void ISignalReceiver<MusicPlayedSignalArgs>.Receive(MusicPlayedSignalArgs signalArg)
    {
        MusicViewModel? item = _musicList.FirstOrDefault(x => x.MusicUri.AbsolutePath == signalArg.PlayingMusicFile.AbsolutePath);
        if (item is not null)
        {
            item.IsSelected = true;
        }
        else
        {
            App.Current.ServiceProvider.GetServiceThen<IErrorNotifier>(errorNotifier =>
            {
                errorNotifier.NotifyError(new InvalidOperationException($"Music {signalArg.PlayingMusicFile.AbsoluteUri} was not found in current play list"));
            });
        }
    }
    #endregion
}
