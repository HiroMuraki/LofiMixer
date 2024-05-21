using LofiMixer.Components;
using NAudio.Wave;
using PlaybackState = LofiMixer.Components.PlaybackState;

namespace LofiMixer.Wpf.Services;

public class NAudioPlayerFactory : IAudioPlayerFactory
{
    public IAudioPlayer CreatePlayer() => new NAudioPlayer();

    #region NonPublic
    private sealed class NAudioPlayer : IAudioPlayer
    {
        public event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;

        public Uri? SourceAudioFile { get; private set; }

        public TimeSpan TotalTime => _waveStream?.TotalTime ?? TimeSpan.Zero;

        public TimeSpan CurrentTime
        {
            get => _waveStream?.CurrentTime ?? TimeSpan.Zero;
            set
            {
                if (_waveStream is not null)
                {
                    _waveStream.CurrentTime = value;
                }
            }
        }

        public float Volume
        {
            get => _wavePlayer?.Volume ?? 0;
            set
            {
                if (_wavePlayer is not null)
                {
                    if (value < 0)
                    {
                        value = 0;
                    }
                    else if (value > 1)
                    {
                        value = 1;
                    }

                    _wavePlayer.Volume = value;
                }
            }
        }

        public bool LoopPlay
        {
            get => _loopPlay;
            set
            {
                _loopPlay = value;

                if (_wavePlayer is not null)
                {
                    if (_loopPlay)
                    {
                        _wavePlayer.PlaybackStopped += Replay;
                    }
                    else
                    {
                        _wavePlayer.PlaybackStopped -= Replay;
                    }
                }
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
                OnPlaybackStateChanged(PlaybackState.Finished);
            };
            wavePlayer.Init(waveStream);

            _wavePlayer = wavePlayer;
            _waveStream = waveStream;
        }

        public void Play()
        {
            if (_wavePlayer is not null)
            {
                if (_wavePlayer.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                {
                    return;
                }
                _wavePlayer.Play();
                OnPlaybackStateChanged(PlaybackState.Playing);
            }
        }

        public void Pause()
        {
            if (_wavePlayer?.PlaybackState == NAudio.Wave.PlaybackState.Playing)
            {
                _wavePlayer.Pause();
                OnPlaybackStateChanged(PlaybackState.Paused);
            }
        }

        public void Stop()
        {
            if (_wavePlayer is not null)
            {
                _wavePlayer.Stop();
                OnPlaybackStateChanged(PlaybackState.Stopped);
            }
        }

        public void Close()
        {
            SourceAudioFile = null;

            _wavePlayer?.Stop();
            _wavePlayer?.Dispose();
            _wavePlayer = null;

            _waveStream?.Close();
            _waveStream?.Dispose();
            _waveStream = null;
        }

        public void Dispose()
        {
            Close();
        }

        #region NonPublic
        private bool _loopPlay;
        private IWavePlayer? _wavePlayer;
        private WaveStream? _waveStream;
        private void Replay(object? sender, StoppedEventArgs e)
        {
            if (_waveStream is not null)
            {
                _waveStream.Position = 0;
            }
            _wavePlayer?.Play();
        }
        private void OnPlaybackStateChanged(PlaybackState state)
        {
            PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(state));
        }
        #endregion
    }
    #endregion
}
