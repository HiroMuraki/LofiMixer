using HM.AppComponents.AppService;
using HM.AppComponents.AppService.Services;
using LofiMixer.ViewModels;

namespace LofiMixer;

internal static class ServiceHelper
{
    public static void ActWith<T>(Action<T> action)
        where T : class
    {
        App.Current.ServiceProvider.GetServiceThen<T>(x =>
        {
            action(x);
        }).Else(() =>
        {
            App.Current.ServiceProvider.GetServiceThen<IErrorNotifier>(x =>
            {
                x.NotifyError(new ComponentNotFoundException(typeof(T), new AppServiceNotFoundException(typeof(T))));
            });
        });
    }
}
