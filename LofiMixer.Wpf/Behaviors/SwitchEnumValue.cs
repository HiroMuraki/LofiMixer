using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace LofiMixer.Wpf.Behaviors;

internal class SwitchEnumValue : Behavior<ButtonBase>
{
    public static readonly DependencyProperty EnumValueProperty = DependencyProperty.Register(
        nameof(EnumValue),
        typeof(Enum),
        typeof(SwitchEnumValue),
        new FrameworkPropertyMetadata(default, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
    );

    public Enum EnumValue
    {
        get => (Enum)GetValue(EnumValueProperty);
        set => SetValue(EnumValueProperty, value);
    }

    protected override void OnAttached()
    {
        AssociatedObject.Click += Handler;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Click -= Handler;
    }

    #region NonPublic
    private void Handler(object sender, RoutedEventArgs e)
    {
        Array values = Enum.GetValues(EnumValue.GetType());
        for (int i = 0; i < values.Length; i++)
        {
            if (EnumValue.Equals((Enum)values.GetValue(i)!))
            {
                int nextIndex = (i + 1) % values.Length;
                EnumValue = (Enum)values.GetValue(nextIndex)!;
                break;
            }
        }
    }
    #endregion
}
