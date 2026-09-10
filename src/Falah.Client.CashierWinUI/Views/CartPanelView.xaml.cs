using System.Collections.Specialized;
using Falah.Client.CashierWinUI.Models;
using Falah.Client.CashierWinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Views;

public sealed partial class CartPanelView : UserControl
{
    private CashierViewModel? _subscribed;
    private CashierViewModel? ViewModel => DataContext as CashierViewModel;

    public CartPanelView()
    {
        InitializeComponent();
        CartSurface.Translation = new System.Numerics.Vector3(0, 0, 8);
        SizeChanged += (_, e) => ApplyFooterDensity(e.NewSize.Height);
        DataContextChanged += (_, _) =>
        {
            Subscribe();
            UpdateEmptyCartVisibility();
        };
        Loaded += (_, _) => { Subscribe(); UpdateEmptyCartVisibility(); };
        Unloaded += (_, _) => { if (_subscribed is not null) _subscribed.Cart.CollectionChanged -= OnCartChanged; _subscribed = null; };
    }

    private void ApplyFooterDensity(double availableHeight)
    {
        // Reflow controls into explicit cells on short, scaled POS displays. Financial rows
        // remain separate; the cart's star row retains space instead of being squeezed to zero.
        var compact = availableHeight < 520;
        OrderTypeSegments.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        CompactOrderTypeButton.Visibility = compact ? Visibility.Visible : Visibility.Collapsed;
        foreach (var child in FooterGrid.Children.OfType<TextBlock>())
            if (Grid.GetRow(child) < 5) child.MinHeight = compact ? 24 : 26;
        FooterTotalLabel.MinHeight = FooterTotalAmount.MinHeight = compact ? 44 : 52;
        FooterTotalAmount.FontSize = compact ? 24 : 26;
        Grid.SetColumn(FooterCheckoutButton, compact ? 1 : 0);
        Grid.SetColumnSpan(FooterCheckoutButton, compact ? 1 : 2);
        FooterCheckoutButton.MinHeight = compact ? 48 : 56;
        FooterCheckoutButton.Content = compact ? "الدفع ←" : "متابعة إلى الدفع  ←";
        Grid.SetRow(FooterCancelButton, compact ? 10 : 11);
        Grid.SetColumnSpan(FooterCancelButton, compact ? 1 : 2);
        // Compact puts cancel beside checkout, so the checkout button no longer spans the footer -
        // the reason line still does, and stays centred over both.
        FooterCancelButton.Margin = new Thickness(0, compact ? 12 : 0, 0, 0);
    }

    private void Subscribe()
    {
        if (_subscribed is not null) _subscribed.Cart.CollectionChanged -= OnCartChanged;
        _subscribed = ViewModel;
        if (_subscribed is not null) _subscribed.Cart.CollectionChanged += OnCartChanged;
    }

    private void OnCartChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateEmptyCartVisibility();

    private void UpdateEmptyCartVisibility()
    {
        var isEmpty = ViewModel is null || ViewModel.Cart.Count == 0;
        EmptyCartHint.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        CartScrollViewer.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>Quick modifier chips ("+ بدون بصل" etc.) - the button's DataContext is the CartLine
    /// (from the cart ItemTemplate) and its Tag carries the fixed note text.</summary>
    private void OnNoteChipClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CartLine line, Tag: string note } && ViewModel is { } vm)
            vm.AppendNoteText(line, note);
    }
}
