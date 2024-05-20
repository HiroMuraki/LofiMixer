using HM.AppComponents;
using LofiMixer.ViewModels;
using System.Windows.Media;

namespace LofiMixer.Wpf.Components;

internal class AppAmbientRemixer :
    IAppComponent,
    ISignalReceiver<StatesChanged<AmbientSoundViewModel>>,
    ISignalReceiver<AmbientMixerResetArgs>
{
    public AppAmbientRemixer()
    {
        AmbientSoundViewModel.StateChanged.Register(this);
    }

    public void Dispose()
    {
        Reset();
    }

    #region NonPublic
    private readonly List<MediaPlayer> _remixPlayers = [];
    private void Reset()
    {
        foreach (MediaPlayer player in _remixPlayers)
        {
            player.MediaOpened -= ReplaySound;
            player.MediaEnded -= ReplaySound;
            player.Stop();
            player.Close();
        }
        _remixPlayers.Clear();
    }
    private void Remix(AmbientSoundViewModel ambientSound)
    {
        MediaPlayer? player = _remixPlayers.FirstOrDefault(x => x.Source.AbsolutePath == ambientSound.MusicUri.AbsolutePath);
        if (player is null)
        {
            if (ambientSound.MusicUri.IsFile && !File.Exists(ambientSound.MusicUri.LocalPath))
            {
                return;
            }
            player = new MediaPlayer();
            player.Open(ambientSound.MusicUri);
            player.Volume = ambientSound.Volume;
            player.IsMuted = ambientSound.Volume <= 0;
            player.MediaEnded += ReplaySound;
            player.MediaOpened += ReplaySound;
            _remixPlayers.Add(player);
        }
        else
        {
            player.Volume = ambientSound.Volume;
            player.IsMuted = ambientSound.Volume <= 0;
        }
    }
    private static void ReplaySound(object? sender, EventArgs e)
    {
        ((MediaPlayer)sender!).Position = TimeSpan.Zero;
        ((MediaPlayer)sender!).Play();
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
