using HM.AppComponents;
using HM.AppComponents.AppService;
using LofiMixer.ViewModels;

namespace LofiMixer.Components;

public sealed class AmbientRemixer :
    IAppComponent,
    ISignalReceiver<StatesChanged<AmbientSoundViewModel>>,
    ISignalReceiver<AmbientMixerResetArgs>
{
    public AmbientRemixer()
    {
        App.Current.Signals.StateChanged.Register(this);
    }

    public void Dispose()
    {
        Reset();
    }

    #region NonPublic
    private readonly List<IAudioPlayer> _remixPlayers = [];
    private void Reset()
    {
        foreach (IAudioPlayer player in _remixPlayers)
        {
            player.Dispose();
        }
        _remixPlayers.Clear();
    }
    private void Remix(AmbientSoundViewModel ambientSound)
    {
        IAudioPlayer? player = _remixPlayers
            .FirstOrDefault(x => x.SourceAudioFile?.AbsolutePath == ambientSound.MusicUri.AbsolutePath);

        if (player is not null)
        {
            player.Volume = ambientSound.Volume;
        }
        else
        {
            if (ambientSound.MusicUri.IsFile && !File.Exists(ambientSound.MusicUri.LocalPath))
            {
                return;
            }

            App.Current.ServiceProvider.GetServiceThen<IAudioPlayerFactory>(audioPlayerFactory =>
            {
                IAudioPlayer player = audioPlayerFactory.CreatePlayer();
                player.Open(ambientSound.MusicUri);
                player.Volume = ambientSound.Volume;
                player.LoopPlay = true;
                player.Play();
                _remixPlayers.Add(player);
            });
        }
    }
    void ISignalReceiver<StatesChanged<AmbientSoundViewModel>>.Receive(StatesChanged<AmbientSoundViewModel> signalArg)
    {
        Remix(signalArg.Sender);
    }
    void ISignalReceiver<AmbientMixerResetArgs>.Receive(AmbientMixerResetArgs signalArg)
    {
        Reset();
    }
    #endregion
}
