using LofiMixer.Models;
using System.Globalization;
using System.Windows.Data;

namespace LofiMixer.Wpf.ValueConverters;

[ValueConversion(typeof(object), typeof(object))]
public class MusicLoopStateToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var loopMode = (MusicLoopMode)value;
        switch (loopMode)
        {
            case MusicLoopMode.Order:
                return "顺序";
            case MusicLoopMode.Single:
                return "单曲";
            case MusicLoopMode.Shuffle:
                return "随机";
            default:
                return "";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

