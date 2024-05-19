using HM.AppComponents.AppService;
using LofiMixer.ViewModels;

namespace LofiMixer;

public sealed class App
{
    public static App Current { get; } = new();

    public AppServiceProvider ServiceProvider { get; } = new();

    public AppPaths AppPaths { get; } = new();
}