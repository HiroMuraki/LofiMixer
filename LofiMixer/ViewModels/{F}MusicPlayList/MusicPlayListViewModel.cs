using CommunityToolkit.Mvvm.ComponentModel;
using HM.AppComponents;
using LofiMixer.Models;
using System.Collections.ObjectModel;

namespace LofiMixer.ViewModels;

public sealed class MusicPlayListReloadedArgs
{
    public MusicPlayListReloadedArgs(IReadOnlyCollection<MusicViewModel> musicList)
    {
        MusicList = musicList.ToArray();
    }

    public IReadOnlyCollection<MusicViewModel> MusicList { get; }
}

public sealed class MusicPlayListViewModel : ObservableObject
{
    public static Signal<StatesChanged<MusicPlayListViewModel>> StatesChanged { get; } = new();

    public static Signal<MusicPlayListReloadedArgs> MusicPlayListReloaded { get; } = new();

    public IEnumerable<MusicViewModel> MusicList => _musicList;

    public MusicLoopMode MusicLoopMode
    {
        get => _musicLoopMode;
        set
        {
            SetProperty(ref _musicLoopMode, value);
            StatesChanged.Emit(new StatesChanged<MusicPlayListViewModel>(this));
        }
    }

    public double MusicVolume
    {
        get => _musicVolume;
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

            SetProperty(ref _musicVolume, value);
            StatesChanged.Emit(new StatesChanged<MusicPlayListViewModel>(this));
        }
    }

    internal async Task ReloadMusicListAsync()
    {
        string[] musicFiles = App.Current.AppPaths
            .MusicDirectory
            .EnumerateFiles()
            .Where(x => _supportedMusicFormats.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase))
            .ToArray();

        _musicList.Clear();
        var mutexSelector = new MutexSelector();
        foreach (string musicFile in musicFiles)
        {
            var musicUri = new Uri(App.Current.AppPaths.MusicDirectory.GetSubPath(musicFile), UriKind.Absolute);
            var music = new MusicViewModel(musicUri, mutexSelector);
            _musicList.Add(music);
            await Task.Delay(1);
        }

        StatesChanged.Emit(new StatesChanged<MusicPlayListViewModel>(this));
        MusicPlayListReloaded.Emit(new MusicPlayListReloadedArgs(_musicList));
    }

    #region NonPublic
    private static readonly HashSet<string> _supportedMusicFormats =
    [
        ".mp3", ".ogg",
    ];
    private readonly ObservableCollection<MusicViewModel> _musicList = [];
    private double _musicVolume = 1;
    private MusicLoopMode _musicLoopMode = MusicLoopMode.Order;
    #endregion
}
