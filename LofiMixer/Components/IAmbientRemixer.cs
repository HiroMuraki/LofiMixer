using LofiMixer.ViewModels;

namespace LofiMixer.Components;

public interface IAmbientRemixer
{
    void Reset();

    void Remix(AmbientSoundViewModel ambientSound);
}
