using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Controls;

public sealed partial class EmptyStateView : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(EmptyStateView), new PropertyMetadata("لا توجد بيانات", OnTitleChanged));

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(EmptyStateView), new PropertyMetadata(string.Empty, OnMessageChanged));

    public EmptyStateView()
    {
        InitializeComponent();
        TitleBlock.Text = Title;
        MessageBlock.Text = Message;
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((EmptyStateView)d).TitleBlock.Text = (string)e.NewValue;

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((EmptyStateView)d).MessageBlock.Text = (string)e.NewValue;
}
