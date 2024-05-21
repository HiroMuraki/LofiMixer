using HM.AppComponents;
using HM.AppComponents.AppService.Services;
using LofiMixer.ViewModels;
using LofiMixer.Wpf.Components;
using System.Diagnostics;
using System.Windows;

namespace LofiMixer.Wpf;

public partial class App : Application
{
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        LofiMixer.App.Current.AppPaths.EnsureAppPathsCreated();
        LofiMixer.App.Current.Components.AddComponent(new SoundRemixer());
        LofiMixer.App.Current.ServiceProvider.RegisterService<IErrorNotifier>(ErrorNotifier.Create(e =>
        {
            Debug.WriteLine(e);
            MessageBox.Show(e.Message);
        }));

        _mainWindowViewModel = new MainWindowViewModel();
        await _mainWindowViewModel.LoadStatesAsync();

        var mainWindow = new MainWindow()
        {
            DataContext = _mainWindowViewModel
        };

        mainWindow.Show();
    }

    private async void Application_Exit(object sender, ExitEventArgs e)
    {
        await (_mainWindowViewModel?.SaveStatesAsync() ?? Task.CompletedTask);
        foreach (IAppComponent component in LofiMixer.App.Current.Components)
        {
            component.Dispose();
        }
    }

    #region NonPublic
    private MainWindowViewModel? _mainWindowViewModel;
    #endregion
}

