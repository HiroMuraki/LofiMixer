using HM.Common;
using LofiMixer.Components;
using LofiMixer.ViewModels;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Windows.Media;

namespace LofiMixer.Wpf.Components;

internal class AppMusicPlayer : IMusicPlayer
{
    public double Volume
    {
        get => _player.GetMemberValueOr(p => p.Volume, -1);
        set
        {
            _player.GetThen(p =>
            {
                if (value < 0)
                {
                    value = 0;
                }
                else if (value > 1)
                {
                    value = 1;
                }

                p.Volume = value;
            });
        }
    }

    public MusicLoopMode LoopMode { get; set; }

    public void Reset()
    {
        _player.GetThen(p =>
        {
            p.MediaEnded -= PlayNextMusic;
            p.Stop();
            p.Close();
            _player = null;
        });
    }

    public void Play(MusicViewModel music)
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

    public void SetPlayList(IEnumerable<MusicViewModel> musicList)
    {
        Reset();

        _musicList = musicList.ToImmutableList();
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

    #region NonPublic
    private Option<MediaPlayer> _player;
    private int _currentMusicIndex;
    private IImmutableList<MusicViewModel> _musicList = [];
    private void PlayFirstMusic()
    {
        if (_musicList.Count <= 0)
        {
            return;
        }

        switch (LoopMode)
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

        Debug.WriteLine(LoopMode);
        switch (LoopMode)
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
    #endregion
}