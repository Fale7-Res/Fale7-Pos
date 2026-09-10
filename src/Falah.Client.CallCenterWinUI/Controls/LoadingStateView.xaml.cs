using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CallCenterWinUI.Controls;

public sealed partial class LoadingStateView : UserControl
{
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(LoadingStateView), new PropertyMetadata("جارٍ التحميل...", OnMessageChanged));

    public LoadingStateView()
    {
        InitializeComponent();
        MessageBlock.Text = Message;
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((LoadingStateView)d).MessageBlock.Text = (string)e.NewValue;
}
