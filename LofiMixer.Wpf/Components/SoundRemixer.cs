using HM.AppComponents;
using HM.AppComponents.AppService;
using HM.AppComponents.AppService.Services;
using HM.Common;
using LofiMixer.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Immutable;
using System.Diagnostics;

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
            _musicFiles = musicFiles.ToImmutableArray();
        }

        public MusicLoopMode LoopMode { get; set; }

        public float Volume
        {
            get => _currentAudioFile?.Volume ?? -1;
            set
            {
                value = ValueClamper.Clamp(value, 0, 1);
                if (_currentAudioFile is not null)
                {
                    _currentAudioFile.Volume = value;
                }
            }
        }

        public void Play(Uri uri)
        {
            int index = _musicFiles.IndexOf(uri);
            if (index == -1)
            {
                LofiMixer.App.Current.ServiceProvider.GetServiceThen<IErrorNotifier>(errorNotifier =>
                {
                    errorNotifier.NotifyError(new InvalidOperationException($"Uri {uri} was not found"));
                });
            }
            _currentMusicIndex = index;
            Play();
        }

        public void Dispose()
        {
            ResetPlayer();
            _musicFiles.Clear();
            _musicFiles = null!;
        }

        #region NonPublic
        private int _currentMusicIndex;
        private IWavePlayer? _wavePlayer;
        private AudioFileReader? _currentAudioFile;
        private IImmutableList<Uri> _musicFiles;
        private int NextMusicIndex()
        {
            switch (LoopMode)
            {
                case MusicLoopMode.Order:
                    _currentMusicIndex++;
                    break;
                case MusicLoopMode.Single:
                    break;
                case MusicLoopMode.Shuffle:
                    _currentMusicIndex = Random.Shared.Next(0, _musicFiles.Count);
                    break;
            }

            _currentMusicIndex %= _musicFiles.Count;
            return _currentMusicIndex;
        }
        private void ResetPlayer()
        {
            _wavePlayer?.Stop();
            _wavePlayer?.Dispose();
            _wavePlayer = null!;

            _currentAudioFile?.Close();
            _currentAudioFile?.Dispose();
            _currentAudioFile = null!;
        }
        private void Play()
        {
            ResetPlayer();

            _wavePlayer = new WaveOutEvent();
            _wavePlayer.PlaybackStopped += (_, e) =>
            {
                if (_currentAudioFile?.CurrentTime >= _currentAudioFile?.TotalTime)
                {
                    NextMusicIndex();
                    Play();
                }
            };
            _currentAudioFile = new AudioFileReader(_musicFiles[_currentMusicIndex].LocalPath);
            _wavePlayer.Init(_currentAudioFile);
            _wavePlayer.Play();

            LofiMixer.App.Current.Signals.MusicPlayed.Emit(new()
            {
                PlayingMusicFile = _musicFiles[_currentMusicIndex],
            });
        }
        #endregion
    }

    private sealed class AmbientSoundMixer : IDisposable
    {
        public AmbientSoundMixer(IEnumerable<Uri> ambientSoundFiles)
        {
            var normalizedWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            AmbientSoundSampleProvider[] sampleProviders = ambientSoundFiles
                .Select(x =>
                {
                    using var audioFile = new AudioFileReader(x.LocalPath);
                    var resampler = new MediaFoundationResampler(audioFile, normalizedWaveFormat);
                    return new AmbientSoundSampleProvider(x, resampler.ToSampleProvider());
                }).ToArray();
            _mixingSampleProvider = new MixingSampleProvider(sampleProviders);
            _mixingSampleProvider.MixerInputEnded += (s, e) =>
            {
                throw new NotImplementedException("TODO");
                //((AudioFileReader)e.SampleProvider!).Position = 0;
                //_mixingSampleProvider.AddMixerInput(e.SampleProvider);
            };
            _mixedAmbientSoundPlayer = new WaveOutEvent();
            _mixedAmbientSoundPlayer.Init(_mixingSampleProvider);
            _mixedAmbientSoundPlayer.Play();
        }

        public void SetChannelVolume(Uri musicFile, float? volume)
        {
            AmbientSoundSampleProvider? targetAudio = _mixingSampleProvider?.MixerInputs
                .Cast<AmbientSoundSampleProvider>()
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
        private class AmbientSoundSampleProvider : VolumeSampleProvider
        {
            public AmbientSoundSampleProvider(Uri sourceUri, ISampleProvider source) : base(source)
            {
                SourceUri = sourceUri;
            }

            public Uri SourceUri { get; }
        }
        #endregion
    }
    #endregion
}
