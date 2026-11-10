using System.Collections.Specialized;
using Falah.Client.CallCenterWinUI.Models;
using Falah.Client.CallCenterWinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CallCenterWinUI.Views;

public sealed partial class CallCenterOrderPanelView : UserControl
{
    private CallCenterViewModel? ViewModel => DataContext as CallCenterViewModel;
    private CallCenterViewModel? _subscribed;
    private bool _isLoaded;

    public CallCenterOrderPanelView()
    {
        InitializeComponent();
        Loaded += (_, _) => { _isLoaded = true; UpdateSubscription(); };
        Unloaded += (_, _) => { _isLoaded = false; UpdateSubscription(); };
        DataContextChanged += (_, _) => UpdateSubscription();
    }

    private void UpdateSubscription()
    {
        if (_subscribed is not null) _subscribed.Cart.CollectionChanged -= OnCartChanged;
        _subscribed = _isLoaded ? ViewModel : null;
        if (_subscribed is not null) _subscribed.Cart.CollectionChanged += OnCartChanged;
        UpdateEmptyCartVisibility();
    }

    private void OnCartChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateEmptyCartVisibility();

    private void UpdateEmptyCartVisibility()
    {
        var isEmpty = ViewModel is null || ViewModel.Cart.Count == 0;
        EmptyCartHint.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        CartScrollViewer.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnNoteChipClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CartLine line, Tag: string note } && ViewModel is { } vm)
            vm.AppendNoteText(line, note);
    }
}
