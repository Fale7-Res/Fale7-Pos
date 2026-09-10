using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CallCenterWinUI.Controls;

public sealed partial class ErrorStateView : UserControl
{
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(ErrorStateView), new PropertyMetadata(string.Empty, OnMessageChanged));

    public static readonly DependencyProperty RetryCommandVisibleProperty = DependencyProperty.Register(
        nameof(RetryCommandVisible), typeof(bool), typeof(ErrorStateView), new PropertyMetadata(false, OnRetryVisibleChanged));

    public event EventHandler? RetryRequested;

    public ErrorStateView()
    {
        InitializeComponent();
        MessageBlock.Text = Message;
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool RetryCommandVisible
    {
        get => (bool)GetValue(RetryCommandVisibleProperty);
        set => SetValue(RetryCommandVisibleProperty, value);
    }

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ErrorStateView)d).MessageBlock.Text = (string)e.NewValue;

    private static void OnRetryVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ErrorStateView)d).RetryButton.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;

    private void OnRetryClick(object sender, RoutedEventArgs e) => RetryRequested?.Invoke(this, EventArgs.Empty);
}
