using HM.AppComponents;
using HM.AppComponents.AppService;
using HM.Common;
using LofiMixer.Models;
using LofiMixer.ViewModels;
using System.Collections.Immutable;

namespace LofiMixer.Components;

public sealed class MusicPlayer :
    IAppComponent,
    ISignalReceiver<PlayMusicRequestedArgs>,
    ISignalReceiver<MusicPlayListReloadedArgs>,
    ISignalReceiver<StatesChanged<MusicPlayListViewModel>>
{
    public MusicPlayer()
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
    private Option<IAudioPlayer> _player;
    private int _currentMusicIndex;
    private MusicLoopMode _loopMode;
    private IImmutableList<MusicViewModel> _musicList = [];
    private void Reset()
    {
        _player.GetThen(p =>
        {
            p.Dispose();
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
    private void PlayNextMusic()
    {
        if (_musicList.Count <= 0)
        {
            return;
        }

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
            p.Play();
        });
    }
    private void HandlePlaybackStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        switch (e.State)
        {
            case PlaybackState.Finished:
                PlayNextMusic();
                break;
            case PlaybackState.Playing:
            case PlaybackState.Paused:
            case PlaybackState.Stopped:
                break;
        }
    }
    void ISignalReceiver<PlayMusicRequestedArgs>.Receive(PlayMusicRequestedArgs signalArg)
    {
        Play(signalArg.Music);
    }
    void ISignalReceiver<MusicPlayListReloadedArgs>.Receive(MusicPlayListReloadedArgs signalArg)
    {
        Reset();

        _musicList = signalArg.MusicList.ToImmutableList();
        App.Current.ServiceProvider.GetServiceThen<IAudioPlayerFactory>(audioPlayerFactory =>
        {
            IAudioPlayer player = audioPlayerFactory.CreatePlayer();
            player.Volume = 1;
            player.PlaybackStateChanged += HandlePlaybackStateChanged;
            _player = new Option<IAudioPlayer>(player);
            PlayFirstMusic();
        });
    }
    void ISignalReceiver<StatesChanged<MusicPlayListViewModel>>.Receive(StatesChanged<MusicPlayListViewModel> signalArg)
    {
        _player.GetThen(p =>
        {
            float musicVolume = signalArg.Sender.MusicVolume;
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