using HM.Common;

namespace LofiMixer.Components;

public enum PlaybackState
{
    Playing,
    Paused,
    Stopped,
    Finished,
}

public sealed class PlaybackStateChangedEventArgs : EventArgs
{
    public PlaybackStateChangedEventArgs(PlaybackState state)
    {
        State = state;
    }

    public PlaybackState State { get; }
}

public interface IAudioPlayer : IDisposable
{
    event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;

    Uri? SourceAudioFile { get; }

    TimeSpan CurrentTime { get; set; }

    TimeSpan TotalTime { get; }

    bool LoopPlay { get; set; }

    float Volume { get; set; }

    void Open(Uri filePath);

    void Pause();

    void Play();

    void Stop();
}