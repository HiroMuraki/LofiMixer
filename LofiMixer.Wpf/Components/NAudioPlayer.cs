using HM.Common;
using LofiMixer.Components;
using NAudio.Wave;
using PlaybackState = LofiMixer.Components.PlaybackState;

namespace LofiMixer.Wpf.Components;

public sealed class NAudioPlayer : IAudioPlayer
{
    public class Factory : IAudioPlayerFactory
    {
        public IAudioPlayer CreatePlayer() => new NAudioPlayer();
    }

    public event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;

    public Option<Uri> SourceAudioFile { get; private set; }

    public TimeSpan TotalTime => _waveStream.GetMemberValueOr(x => TotalTime, TimeSpan.Zero);

    public TimeSpan CurrentTime
    {
        get => _waveStream.GetMemberValueOr(x => x.CurrentTime, TimeSpan.Zero);
        set
        {
            _waveStream.GetThen(x => x.CurrentTime = value);
        }
    }

    public float Volume
    {
        get => _wavePlayer.GetMemberValueOr(x => x.Volume, 0);
        set
        {
            _wavePlayer.GetThen(x =>
            {
                if (value < 0)
                {
                    value = 0;
                }
                else if (value > 1)
                {
                    value = 1;
                }

                x.Volume = value;
            });
        }
    }

    public bool LoopPlay
    {
        get => _loopPlay;
        set
        {
            _loopPlay = value;

            Option.AllThen(_wavePlayer, _waveStream, (wavePlayer, waveStream) =>
            {
                if (_loopPlay)
                {
                    wavePlayer.PlaybackStopped += Replay;
                }
                else
                {
                    wavePlayer.PlaybackStopped -= Replay;
                }
            });
        }
    }

    public void Open(Uri filePath)
    {
        Close();

        SourceAudioFile = filePath;
        var wavePlayer = new WaveOutEvent();
        var waveStream = new AudioFileReader(filePath.LocalPath);
        wavePlayer.PlaybackStopped += (_, _) =>
        {
            OnPlaybackStateChanged(PlaybackState.Stopped);
        };
        wavePlayer.Init(waveStream);

        _wavePlayer = wavePlayer;
        _waveStream = waveStream;
    }

    public void Play()
    {
        _wavePlayer.GetThen(x =>
        {
            if (x.PlaybackState == NAudio.Wave.PlaybackState.Playing)
            {
                return;
            }
            x.Play();
            OnPlaybackStateChanged(PlaybackState.Playing);
        });
    }

    public void Pause()
    {
        _wavePlayer.GetThen(x =>
        {
            if (x.PlaybackState == NAudio.Wave.PlaybackState.Playing)
            {
                x.Pause();
                OnPlaybackStateChanged(PlaybackState.Paused);
            }
        });
    }

    public void Stop()
    {
        _wavePlayer.GetThen(x =>
        {
            x.Stop();
            OnPlaybackStateChanged(PlaybackState.Stopped);
        });
    }

    public void Close()
    {
        SourceAudioFile = null;
        _wavePlayer.GetThen(x =>
        {
            x.Stop();
            x.Dispose();
            _wavePlayer = null;
        });
        _waveStream.GetThen(x =>
        {
            x.Close();
            x.Dispose();
            _waveStream = null;
        });
    }

    public void Dispose()
    {
        Close();
    }

    #region NonPublic
    private bool _loopPlay;
    private Option<IWavePlayer> _wavePlayer;
    private Option<WaveStream> _waveStream;
    private void Replay(object? sender, StoppedEventArgs e)
    {
        Option.AllThen(_wavePlayer, _waveStream, (wavePlayer, waveStream) =>
        {
            waveStream.Position = 0;
            wavePlayer.Play();
        });
    }
    private void OnPlaybackStateChanged(PlaybackState state)
    {
        PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(state));
    }
    #endregion
}