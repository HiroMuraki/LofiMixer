using HM.AppComponents;
using HM.AppComponents.AppService;
using HM.AppComponents.AppService.Services;
using HM.Common;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Immutable;

namespace LofiMixer.Wpf.Components;

public sealed class SoundRemixer :
    IAppComponent,
    ISignalReceiver<PlayMusicRequestedSignalArgs>,
    ISignalReceiver<MusicPlayListReloadedSignalArgs>,
    ISignalReceiver<MusicPlayerSettingsChangedSignalArgs>,
    ISignalReceiver<AmbientSoundSettingChangedSignalArgs>,
    ISignalReceiver<AmbientSoundsReloadedSignalArgs>
{
    public SoundRemixer()
    {
        LofiMixer.App.Current.Signals.AmbientSoundsReloaded.Register(this);
        LofiMixer.App.Current.Signals.AmbientSoundStateChanged.Register(this);
        LofiMixer.App.Current.Signals.PlayMusicRequested.Register(this);
        LofiMixer.App.Current.Signals.MusicPlayListReloaded.Register(this);
        LofiMixer.App.Current.Signals.MusicPlayerSettingsChanged.Register(this);
    }

    public void Dispose()
    {
        _musicPlayer?.Dispose();
        _ambientSoundMixer?.Dispose();
    }

    #region NonPublic
    private MusicPlayer? _musicPlayer;
    private AmbientSoundMixer? _ambientSoundMixer;
    void ISignalReceiver<PlayMusicRequestedSignalArgs>.Receive(PlayMusicRequestedSignalArgs signalArg)
    {
        _musicPlayer?.Play(signalArg.MusicFile);
    }
    void ISignalReceiver<MusicPlayListReloadedSignalArgs>.Receive(MusicPlayListReloadedSignalArgs signalArg)
    {
        _musicPlayer?.Dispose();
        _musicPlayer = new MusicPlayer(signalArg.MusicFiles);
    }
    void ISignalReceiver<MusicPlayerSettingsChangedSignalArgs>.Receive(MusicPlayerSettingsChangedSignalArgs signalArg)
    {
        if (_musicPlayer is not null && signalArg.Volume.HasValue)
        {
            _musicPlayer.Volume = signalArg.Volume.Value;
        }
    }
    void ISignalReceiver<AmbientSoundSettingChangedSignalArgs>.Receive(AmbientSoundSettingChangedSignalArgs signalArg)
    {
        _ambientSoundMixer?.SetChannelVolume(signalArg.SoundUri, signalArg.Volume);
    }
    void ISignalReceiver<AmbientSoundsReloadedSignalArgs>.Receive(AmbientSoundsReloadedSignalArgs signalArg)
    {
        _ambientSoundMixer?.Dispose();
        _ambientSoundMixer = new AmbientSoundMixer(signalArg.AmbientSoundFiles);
    }
    private sealed class MusicPlayer : IDisposable
    {
        public MusicPlayer(IEnumerable<Uri> musicFiles)
        {
            Play(musicFiles);
        }

        public float Volume
        {
            get => _cycledSampleProvider?.Volume ?? -1;
            set
            {
                value = ValueClamper.Clamp(value, 0, 1);
                if (_cycledSampleProvider is not null)
                {
                    _cycledSampleProvider.Volume = value;
                }
            }
        }

        public void Play(Uri uri)
        {
            if (_cycledSampleProvider is null)
            {
                return;
            }

            var musicQueue = new Queue<Uri>(_cycledSampleProvider.MusicList);
            for (int i = 0; i < musicQueue.Count; i++)
            {
                if (musicQueue.Peek().AbsolutePath != uri.AbsolutePath)
                {
                    musicQueue.Enqueue(musicQueue.Dequeue());
                }
            }
            if (musicQueue.Peek().AbsolutePath == uri.AbsolutePath)
            {
                Reset();
                Play(musicQueue);
                LofiMixer.App.Current.Signals.MusicPlayed.Emit(new()
                {
                    PlayingMusicFile = musicQueue.Peek(),
                });
            }
            else
            {
                LofiMixer.App.Current.ServiceProvider.GetServiceThen<IErrorNotifier>(errorNotifier =>
                {
                    errorNotifier.NotifyError(new InvalidOperationException($"Uri {uri} was not found"));
                });
            }
        }

        public void Dispose()
        {
            Reset();
        }

        #region NonPublic
        private CycledSampleProvider? _cycledSampleProvider;
        private IWavePlayer? _wavePlayer;
        private void Reset()
        {
            _wavePlayer?.Stop();
            _wavePlayer?.Dispose();
            _wavePlayer = null;

            _cycledSampleProvider?.Dispose();
            _cycledSampleProvider = null;
        }
        private void Play(IEnumerable<Uri> musicFiles)
        {
            _cycledSampleProvider = new CycledSampleProvider(musicFiles, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            _wavePlayer = new WaveOutEvent();
            _wavePlayer.Init(_cycledSampleProvider);
            _wavePlayer.Play();
        }
        #endregion
    }
    private sealed class AmbientSoundMixer : IDisposable
    {
        public AmbientSoundMixer(IEnumerable<Uri> ambientSoundFiles)
        {
            var normalizedWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            LoopedSampleProvider[] sampleProviders = ambientSoundFiles
                .Select(x => new LoopedSampleProvider(x, normalizedWaveFormat))
                .ToArray();
            _mixingSampleProvider = new MixingSampleProvider(sampleProviders);
            _mixedAmbientSoundPlayer = new WaveOutEvent();
            _mixedAmbientSoundPlayer.Init(_mixingSampleProvider);
            _mixedAmbientSoundPlayer.Play();
        }

        public void SetChannelVolume(Uri musicFile, float? volume)
        {
            LoopedSampleProvider? targetAudio = _mixingSampleProvider?.MixerInputs
                .Cast<LoopedSampleProvider>()
                .FirstOrDefault(x => x.SourceUri == musicFile);

            if (targetAudio is not null && volume.HasValue)
            {
                targetAudio.Volume = volume.Value;
            }
        }

        public void Play()
        {
            _mixedAmbientSoundPlayer?.Play();
        }

        public void Dispose()
        {
            Reset();
        }

        #region NonPublic
        private IWavePlayer? _mixedAmbientSoundPlayer;
        private MixingSampleProvider? _mixingSampleProvider;
        private void Reset()
        {
            _mixingSampleProvider?.RemoveAllMixerInputs();
            _mixingSampleProvider = null;

            _mixedAmbientSoundPlayer?.Stop();
            _mixedAmbientSoundPlayer?.Dispose();
            _mixedAmbientSoundPlayer = null;

        }
        #endregion
    }
    private sealed class LoopedSampleProvider :
        ISampleProvider,
        IDisposable
    {
        public LoopedSampleProvider(Uri sourceUri, WaveFormat waveFormat)
        {
            SourceUri = sourceUri;
            _audioFileReader = new AudioFileReader(sourceUri.LocalPath);
            _outputSampleProvider = new MediaFoundationResampler(_audioFileReader, waveFormat)
            {
                ResamplerQuality = 60
            }.ToSampleProvider();
            WaveFormat = waveFormat;
        }

        public Uri SourceUri { get; }

        public float Volume
        {
            get => _audioFileReader.Volume;
            set
            {
                value = ValueClamper.Clamp(value, 0, 1);
                _audioFileReader.Volume = value;
            }
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int readCount = _audioFileReader.Read(buffer, offset, count);
            int padding = count - readCount;
            while (padding > 0)
            {
                _audioFileReader.Position = 0;
                readCount += _audioFileReader.Read(buffer, readCount, padding);
                padding = count - readCount;
            }

            return count;
        }

        public void Dispose()
        {
            _outputSampleProvider = null!;
            _audioFileReader?.Close();
            _audioFileReader?.Dispose();
            _audioFileReader = null!;
        }

        #region NonPublic
        private ISampleProvider _outputSampleProvider;
        private AudioFileReader _audioFileReader;
        #endregion
    }
    private sealed class CycledSampleProvider :
        ISampleProvider,
        IDisposable
    {
        public enum CycleMode
        {
            Order,
            Single,
            Random,
            Shuffle,
        }

        public CycledSampleProvider(IEnumerable<Uri> audioFiles, WaveFormat waveFormat)
        {
            _audioFiles = audioFiles.ToImmutableArray();
            WaveFormat = waveFormat;
            _currentAudioIndex = -1;
            ReadNextAudio();
        }

        public IEnumerable<Uri> MusicList => _audioFiles;

        public float Volume
        {
            get => _volume;
            set
            {
                value = ValueClamper.Clamp(value, 0, 1);
                _volume = value;
                if (_currentAudioFileReader is not null)
                {
                    _currentAudioFileReader.Volume = value;
                }
            }
        }

        public CycleMode Mode { get; set; } = CycleMode.Order;

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            if (_outputSampleProvider is null)
            {
                return 0;
            }

            int readCount = _outputSampleProvider.Read(buffer, offset, count);
            int padding = count - readCount;
            while (padding > 0)
            {
                ReadNextAudio();
                readCount += _outputSampleProvider.Read(buffer, readCount, padding);
                padding = count - readCount;
            }

            return count;
        }

        public void Dispose()
        {
            _outputSampleProvider = null;
            _currentAudioFileReader?.Close();
            _currentAudioFileReader?.Dispose();
            _currentAudioFileReader = null;
            _currentAudioIndex = -1;
            _audioFiles = [];
        }

        #region NonPublic
        private float _volume = 1f;
        private ISampleProvider? _outputSampleProvider;
        private AudioFileReader? _currentAudioFileReader;
        private int _currentAudioIndex;
        private ImmutableArray<Uri> _audioFiles;
        private void ReadNextAudio()
        {
            if (_audioFiles.Length == 0)
            {
                return;
            }

            _currentAudioFileReader?.Close();
            _currentAudioFileReader?.Dispose();

            _currentAudioIndex = Mode switch
            {
                CycleMode.Single => _currentAudioIndex == -1 ? 0 : _currentAudioIndex,
                CycleMode.Order => _currentAudioIndex + 1,
                CycleMode.Random => Random.Shared.Next(0, _audioFiles.Length),
                CycleMode.Shuffle => Random.Shared.Next(0, _audioFiles.Length),
                _ => _currentAudioIndex,
            };
            _currentAudioIndex %= _audioFiles.Length;

            Uri music = _audioFiles[_currentAudioIndex];
            _currentAudioFileReader = new AudioFileReader(music.LocalPath)
            {
                Volume = _volume,
            };
            _outputSampleProvider = new MediaFoundationResampler(_currentAudioFileReader, WaveFormat)
            {
                ResamplerQuality = 60
            }.ToSampleProvider();
        }
        #endregion
    }
    #endregion
}
