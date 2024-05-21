//using HM.AppComponents;
//using HM.AppComponents.AppService;
//using LofiMixer.ViewModels;

//namespace LofiMixer.Components;

//public sealed class AmbientRemixer :
//    IAppComponent,
//    ISignalReceiver<AmbientSoundSettingChangedSignalArgs>,
//    ISignalReceiver<AmbientSoundsReloadedSignalArgs>
//{
//    public AmbientRemixer()
//    {
//        App.Current.Signals.AmbientSoundStateChanged.Register(this);
//    }

//    public void Dispose()
//    {
//        Reset();
//    }

//    #region NonPublic
//    private readonly List<IAudioPlayer> _remixPlayers = [];
//    private void Reset()
//    {
//        foreach (IAudioPlayer player in _remixPlayers)
//        {
//            player.Dispose();
//        }
//        _remixPlayers.Clear();
//    }
//    private void Remix(Uri soundUri, float? volume)
//    {
//        IAudioPlayer? player = _remixPlayers
//            .FirstOrDefault(x => x.SourceAudioFile?.AbsolutePath == soundUri.AbsolutePath);

//        if (player is not null && volume.HasValue)
//        {
//            player.Volume = volume.Value;
//        }
//        else
//        {
//            if (soundUri.IsFile && !File.Exists(soundUri.LocalPath))
//            {
//                return;
//            }

//            App.Current.ServiceProvider.GetServiceThen<IAudioPlayerFactory>(audioPlayerFactory =>
//            {
//                IAudioPlayer player = audioPlayerFactory.CreatePlayer();
//                player.Open(soundUri);
//                player.Volume = volume ?? 1;
//                player.LoopPlay = true;
//                player.Play();
//                _remixPlayers.Add(player);
//            });
//        }
//    }
//    void ISignalReceiver<AmbientSoundSettingChangedSignalArgs>.Receive(AmbientSoundSettingChangedSignalArgs signalArg)
//    {
//        Remix(signalArg.SoundUri, signalArg.Volume);
//    }
//    void ISignalReceiver<AmbientSoundsReloadedSignalArgs>.Receive(AmbientSoundsReloadedSignalArgs signalArg)
//    {
//        Reset();
//    }
//    #endregion
//}
