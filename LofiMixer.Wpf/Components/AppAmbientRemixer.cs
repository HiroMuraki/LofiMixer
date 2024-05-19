using LofiMixer.Components;
using LofiMixer.ViewModels;
using System.Windows.Media;

namespace LofiMixer.Wpf.Components;

internal class AppAmbientRemixer : IAmbientRemixer
{
    public void Reset()
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

    public void Remix(AmbientSoundViewModel ambientSound)
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

    #region NonPublic
    private readonly List<MediaPlayer> _remixPlayers = [];
    private static void ReplaySound(object? sender, EventArgs e)
    {
        ((MediaPlayer)sender!).Position = TimeSpan.Zero;
        ((MediaPlayer)sender!).Play();
    }
    #endregion
}
