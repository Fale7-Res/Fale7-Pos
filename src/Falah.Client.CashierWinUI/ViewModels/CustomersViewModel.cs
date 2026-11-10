using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Customers;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Customer lookup by phone.
///
/// This is deliberately a search, not a browsable directory: phone is the customer's identity in
/// this system (CustomerQueryService looks up by phone and never by name), and there is no
/// list-all-customers read on the backend. Rather than invent one - which would mean a new endpoint
/// over a table that grows unboundedly, for a browsing use case nobody has evidenced - the screen
/// matches how the data is actually keyed and how the reference app itself found customers.
/// </summary>
public partial class CustomersViewModel : ObservableObject
{
    private readonly AppSession _session;

    public CustomersViewModel(AppSession session) => _session = session;

    public ObservableCollection<CustomerAddressDto> Addresses { get; } = new();

    [ObservableProperty] private string _phoneQuery = "";
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private string? _searchError;
    [ObservableProperty] private CustomerDto? _customer;
    [ObservableProperty] private bool _hasSearched;

    public bool HasCustomer => Customer is not null;
    public bool NotFound => HasSearched && !IsSearching && Customer is null && SearchError is null;

    /// <summary>Nothing has been searched for yet. The screen opened on a blank page with a search
    /// box and no statement of what it wanted, which reads as "empty" rather than as "ask me
    /// something" - so this state now says what to type, instead of leaving the operator to infer it.</summary>
    public bool AwaitingFirstSearch => !HasSearched && !IsSearching && Customer is null && SearchError is null;

    partial void OnCustomerChanged(CustomerDto? value)
    {
        OnPropertyChanged(nameof(HasCustomer));
        OnPropertyChanged(nameof(NotFound));
        OnPropertyChanged(nameof(AwaitingFirstSearch));
    }

    partial void OnHasSearchedChanged(bool value) => OnPropertyChanged(nameof(AwaitingFirstSearch));
    partial void OnIsSearchingChanged(bool value) => OnPropertyChanged(nameof(AwaitingFirstSearch));
    partial void OnSearchErrorChanged(string? value) => OnPropertyChanged(nameof(AwaitingFirstSearch));

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (IsSearching) return;
        if (string.IsNullOrWhiteSpace(PhoneQuery))
        {
            SearchError = "أدخل رقم الهاتف للبحث.";
            return;
        }

        IsSearching = true;
        SearchError = null;
        Customer = null;
        Addresses.Clear();
        try
        {
            var result = await _session.Api.LookupCustomerAsync(PhoneQuery.Trim());
            HasSearched = true;

            if (!result.Success || result.Value is null)
            {
                // A 404 is the ordinary "no such customer yet" outcome, not a failure to report.
                if (result.StatusCode != 404) SearchError = result.Error ?? "تعذر البحث عن العميل.";
                return;
            }
            Customer = result.Value;
            foreach (var a in result.Value.Addresses.OrderByDescending(a => a.LastUsedAtUtc)) Addresses.Add(a);
        }
        finally
        {
            IsSearching = false;
            OnPropertyChanged(nameof(NotFound));
        }
    }
}
