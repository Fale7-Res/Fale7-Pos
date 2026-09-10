using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Controls;

public sealed partial class SummaryCardView : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(SummaryCardView), new PropertyMetadata(string.Empty, OnLabelChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(SummaryCardView), new PropertyMetadata(string.Empty, OnValueChanged));

    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
        nameof(Caption), typeof(string), typeof(SummaryCardView), new PropertyMetadata(string.Empty, OnCaptionChanged));

    public SummaryCardView()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((SummaryCardView)d).LabelBlock.Text = (string)e.NewValue;

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((SummaryCardView)d).ValueBlock.Text = (string)e.NewValue;

    private static void OnCaptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (SummaryCardView)d;
        var caption = (string)e.NewValue;
        view.CaptionBlock.Text = caption;
        view.CaptionBlock.Visibility = string.IsNullOrEmpty(caption) ? Visibility.Collapsed : Visibility.Visible;
    }
}
