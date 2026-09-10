using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CallCenterWinUI.Controls;

public sealed partial class StatusPillView : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(StatusPillView), new PropertyMetadata(string.Empty, OnTextChanged));

    public StatusPillView()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (StatusPillView)d;
        var text = (string)e.NewValue ?? string.Empty;
        view.Label.Text = text;
        var converter = new Converters.StatusToPillBrushConverter();
        view.Root.Background = (Microsoft.UI.Xaml.Media.Brush)converter.Convert(text, typeof(string), null!, string.Empty);
    }
}
