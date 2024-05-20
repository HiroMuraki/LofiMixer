using HM.AppComponents;
using HM.Common;
using LofiMixer.Components;
using LofiMixer.ViewModels;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Windows.Media;

namespace LofiMixer.Wpf.Components;

internal class AppMusicPlayer :
    IAppComponent,
    ISignalReceiver<PlayMusicRequestedArgs>,
    ISignalReceiver<MusicPlayListReloadedArgs>,
    ISignalReceiver<StatesChanged<MusicPlayListViewModel>>
{
    public AppMusicPlayer()
    {
        MusicViewModel.PlayMusicRequested.Register(this);
        MusicPlayListViewModel.MusicPlayListReloaded.Register(this);
        MusicPlayListViewModel.StatesChanged.Register(this);
    }

    public void Dispose()
    {
        Reset();
    }

    #region NonPublic
    private Option<MediaPlayer> _player;
    private int _currentMusicIndex;
    private MusicLoopMode _loopMode;
    private IImmutableList<MusicViewModel> _musicList = [];
    private void Reset()
    {
        _player.GetThen(p =>
        {
            p.MediaEnded -= PlayNextMusic;
            p.Stop();
            p.Close();
            _player = null;
        });
    }
    private void Play(MusicViewModel music)
    {
        int index = _musicList.IndexOf(music);
        if (index == -1)
        {
            throw new InvalidOperationException($"Provided music was not found in current music list");
        }

        if (_currentMusicIndex == index)
        {
            return;
        }
        _currentMusicIndex = index;
        UpdatePlayer();
    }
    private void PlayFirstMusic()
    {
        if (_musicList.Count <= 0)
        {
            return;
        }

        switch (_loopMode)
        {
            case MusicLoopMode.Order:
            case MusicLoopMode.Single:
                _currentMusicIndex = 0;
                break;
            case MusicLoopMode.Shuffle:
                _currentMusicIndex = Random.Shared.Next(0, _musicList.Count);
                break;
        }

        UpdatePlayer();
    }
    private void PlayNextMusic(object? sender, EventArgs e)
    {
        if (_musicList.Count <= 0)
        {
            return;
        }

        Debug.WriteLine(_loopMode);
        switch (_loopMode)
        {
            case MusicLoopMode.Order:
                _currentMusicIndex++;
                break;
            case MusicLoopMode.Single:
                break;
            case MusicLoopMode.Shuffle:
                _currentMusicIndex = Random.Shared.Next(0, _musicList.Count);
                break;
        }

        UpdatePlayer();
    }
    private void UpdatePlayer()
    {
        _currentMusicIndex %= _musicList.Count;
        _player.GetThen(p =>
        {
            MusicViewModel music = _musicList[_currentMusicIndex];
            p.Open(music.MusicUri);
            music.IsSelected = true;
        });
    }
    void ISignalReceiver<PlayMusicRequestedArgs>.Receive(PlayMusicRequestedArgs signalArg)
    {
        Play(signalArg.Music);
    }
    void ISignalReceiver<MusicPlayListReloadedArgs>.Receive(MusicPlayListReloadedArgs signalArg)
    {
        Reset();

        _musicList = signalArg.MusicList.ToImmutableList();
        _player.WithValue(new MediaPlayer(), p =>
        {
            p.Volume = 1;
            p.MediaEnded += PlayNextMusic;
            p.MediaOpened += (s, _) =>
            {
                ((MediaPlayer)s!).Play();
            };
            PlayFirstMusic();
        });
    }
    void ISignalReceiver<StatesChanged<MusicPlayListViewModel>>.Receive(StatesChanged<MusicPlayListViewModel> signalArg)
    {
        _player.GetThen(p =>
        {
            double musicVolume = signalArg.Sender.MusicVolume;
            if (musicVolume < 0)
            {
                musicVolume = 0;
            }
            else if (musicVolume > 1)
            {
                musicVolume = 1;
            }

            p.Volume = musicVolume;
        });

        _loopMode = signalArg.Sender.MusicLoopMode;
    }
    #endregion
}