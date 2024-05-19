﻿using HM.AppComponents.AppService.Services;
using HM.Common;
using LofiMixer.Components;
using LofiMixer.ViewModels;
using LofiMixer.Wpf.Components;
using System.Diagnostics;
using System.Windows;

namespace LofiMixer.Wpf;

public partial class App : Application
{
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        LofiMixer.App.Current.ServiceProvider.RegisterService<IAmbientRemixer>(new AppAmbientRemixer());
        LofiMixer.App.Current.ServiceProvider.RegisterService<IMusicPlayer>(new AppMusicPlayer());
        LofiMixer.App.Current.ServiceProvider.RegisterService<IErrorNotifier>(ErrorNotifier.Create(e =>
        {
            Debug.WriteLine(e);
        }));

        var mainWindowViewModel = new MainWindowViewModel();
        LofiMixer.App.Current.AppPaths.EnsureAppPathsCreated();
        await mainWindowViewModel.LoadStatesAsync();

        var mainWindow = new MainWindow()
        {
            DataContext = mainWindowViewModel
        };
        _mainWindowViewModel = mainWindowViewModel;

        mainWindow.Show();
    }

    private async void Application_Exit(object sender, ExitEventArgs e)
    {
        await _mainWindowViewModel.GetThenAsync(x => x.SaveStatesAsync());
    }

    #region NonPublic
    private Option<MainWindowViewModel> _mainWindowViewModel;
    #endregion
}

