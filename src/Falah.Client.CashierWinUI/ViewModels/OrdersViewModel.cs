using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Orders;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Order history. Scope is decided server-side, not here: a user without `orders_view_all` gets
/// only the currently-open shift's orders, and that filtering happens in the endpoint rather than
/// as a client-side slice of a fuller list this screen was never allowed to fetch.
/// </summary>
public partial class OrdersViewModel : ObservableObject
{
    private readonly AppSession _session;

    public OrdersViewModel(AppSession session) => _session = session;

    public ObservableCollection<OrderDetailsDto> Orders { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string _searchQuery = "";
    [ObservableProperty] private OrderDetailsDto? _selectedOrder;

    private readonly List<OrderDetailsDto> _all = new();

    public bool HasNoOrders => !IsLoading && LoadError is null && Orders.Count == 0;
    public bool IsDetailOpen => SelectedOrder is not null;
    partial void OnSelectedOrderChanged(OrderDetailsDto? value) => OnPropertyChanged(nameof(IsDetailOpen));

    /// <summary>Matches on order number, customer name or phone - the three things someone actually
    /// has in hand when a customer calls back about an order.</summary>
    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Orders.Clear();
        IEnumerable<OrderDetailsDto> source = _all;
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.Trim();
            source = source.Where(o =>
                (o.OrderNumber?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (o.CustomerName?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (o.CustomerPhone?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false));
        }
        foreach (var o in source) Orders.Add(o);
        OnPropertyChanged(nameof(HasNoOrders));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            var result = await _session.Api.GetOrdersAsync(_session.CurrentUser!.Id);
            if (!result.Success || result.Value is null)
            {
                LoadError = result.Error ?? "تعذر تحميل الطلبات.";
                return;
            }
            _all.Clear();
            _all.AddRange(result.Value);
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoOrders));
        }
    }

    [RelayCommand]
    private void OpenOrder(OrderDetailsDto order) => SelectedOrder = order;

    [RelayCommand]
    private void CloseOrder() => SelectedOrder = null;
}
