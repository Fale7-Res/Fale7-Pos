using System.Collections.Specialized;
using Falah.Client.CashierWinUI.Models;
using Falah.Client.CashierWinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Views;

public sealed partial class CartPanelView : UserControl
{
    private CashierViewModel? ViewModel => DataContext as CashierViewModel;

    public CartPanelView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (ViewModel is { } vm) vm.Cart.CollectionChanged += OnCartChanged;
            UpdateEmptyCartVisibility();
        };
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
