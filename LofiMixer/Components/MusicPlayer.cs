using HM.AppComponents;
using HM.AppComponents.AppService;
using HM.AppComponents.AppService.Services;
using HM.Common;
using LofiMixer.Models;
using System.Collections.Immutable;

namespace LofiMixer.Components;

public sealed class MusicPlayer :
    IAppComponent,
    ISignalReceiver<PlayMusicRequestedArgs>,
    ISignalReceiver<MusicPlayListReloadedArgs>,
    ISignalReceiver<MusicPlayerSettingsChangedArgs>
{
    public MusicPlayer()
    {
        App.Current.Signals.PlayMusicRequested.Register(this);
        App.Current.Signals.MusicPlayListReloaded.Register(this);
        App.Current.Signals.MusicPlayerSettingsChanged.Register(this);
    }

    public void Dispose()
    {
        Reset();
    }

    #region NonPublic
    private IAudioPlayer? _player;
    private int _currentMusicIndex;
    private MusicLoopMode _loopMode;
    private IImmutableList<Uri> _musicList = [];
    private void Reset()
    {
        _player?.Dispose();
        _player = null;
        _currentMusicIndex = 0;
        _musicList = [];
    }
    private void Play(Uri musicFile)
    {
        int index = _musicList.IndexOf(musicFile);
        if (index == -1)
        {
            App.Current.ServiceProvider.GetServiceThen<IErrorNotifier>(errorNotifier =>
            {
                errorNotifier.NotifyError(new InvalidOperationException($"Provided music was not found in current music list"));
            });
            return;
        }

        if (_currentMusicIndex == index)
        {
            return;
        }

        _currentMusicIndex = index;
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
        Uri musicFile = _musicList[_currentMusicIndex];

        if (_player is not null)
        {
            _player.Open(musicFile);
            _player.Play();
            App.Current.Signals.MusicPlayed.Emit(new MusicPlayedSignalArgs
            {
                PlayingMusicFile = musicFile
            });
        }
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
        Play(signalArg.MusicFile);
    }
    void ISignalReceiver<MusicPlayListReloadedArgs>.Receive(MusicPlayListReloadedArgs signalArg)
    {
        Reset();

        _musicList = signalArg.MusicFiles.ToImmutableList();
        App.Current.ServiceProvider.GetServiceThen<IAudioPlayerFactory>(audioPlayerFactory =>
        {
            _player = audioPlayerFactory.CreatePlayer();
            _player.Volume = 1;
            _player.PlaybackStateChanged += HandlePlaybackStateChanged;
        });
    }
    void ISignalReceiver<MusicPlayerSettingsChangedArgs>.Receive(MusicPlayerSettingsChangedArgs signalArg)
    {
        if (_player is not null && signalArg.Volume.HasValue)
        {
            _player.Volume = ValueClamper.Clamp(signalArg.Volume.Value, 0, 1);
        }

        _loopMode = signalArg?.MusicLoopMode ?? _loopMode;
    }
    #endregion
}