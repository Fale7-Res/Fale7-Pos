using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Controls;

public sealed partial class ModalHeaderView : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(ModalHeaderView), new PropertyMetadata(string.Empty, OnTitleChanged));

    public static readonly DependencyProperty CloseCommandProperty = DependencyProperty.Register(
        nameof(CloseCommand), typeof(ICommand), typeof(ModalHeaderView), new PropertyMetadata(null, OnCloseCommandChanged));

    public static readonly DependencyProperty CloseAutomationIdProperty = DependencyProperty.Register(
        nameof(CloseAutomationId), typeof(string), typeof(ModalHeaderView), new PropertyMetadata(string.Empty, OnCloseAutomationIdChanged));

    public ModalHeaderView()
    {
        InitializeComponent();
        TitleBlock.Text = Title;
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    /// <summary>Lets each dialog keep its own stable automation id on the close button, which the
    /// existing UI-automation checks target by name.</summary>
    public string CloseAutomationId
    {
        get => (string)GetValue(CloseAutomationIdProperty);
        set => SetValue(CloseAutomationIdProperty, value);
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ModalHeaderView)d).TitleBlock.Text = (string)e.NewValue;

    private static void OnCloseCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ModalHeaderView)d).CloseButton.Command = e.NewValue as ICommand;

    private static void OnCloseAutomationIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        AutomationProperties.SetAutomationId(((ModalHeaderView)d).CloseButton, (string)e.NewValue);
}
