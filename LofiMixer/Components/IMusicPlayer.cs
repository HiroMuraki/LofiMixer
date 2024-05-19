using LofiMixer.ViewModels;

namespace LofiMixer.Components;

public interface IMusicPlayer
{
    MusicLoopMode LoopMode { get; set; }

    double Volume { get; set; }

    void Reset();

    void Play(MusicViewModel music);

    void SetPlayList(IEnumerable<MusicViewModel> musicList);
}
