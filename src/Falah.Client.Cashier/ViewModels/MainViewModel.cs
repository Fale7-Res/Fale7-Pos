using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Client.Cashier.Services;

namespace Falah.Client.Cashier.ViewModels;

/// <summary>Root view model: decides Login vs Cashier, owns the persistent app header, and polls
/// the server's health so an Online/Offline indicator is always honest - if the Host's server
/// process is unreachable, this client must say so plainly rather than let the cashier discover it
/// only when a payment fails.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly DispatcherTimer _healthTimer;

    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private bool _isOnline = true;
    [ObservableProperty] private bool _isLoggedIn;

    public HeaderViewModel Header { get; }

    public MainViewModel()
    {
        _session = new AppSession();
        Header = new HeaderViewModel(_session);
        ShowLogin();

        _healthTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _healthTimer.Tick += async (_, _) => IsOnline = await _session.Api.PingHealthAsync();
        _healthTimer.Start();
        _ = CheckHealthNowAsync();
    }

    private async Task CheckHealthNowAsync() => IsOnline = await _session.Api.PingHealthAsync();

    /// <summary>Matches the reference app's "قفل" button: returns to the PIN screen without
    /// ending the shift or clearing anything server-side - a different person can unlock with
    /// their own PIN, or the same person can resume.</summary>
    [RelayCommand]
    private void Lock() => ShowLogin();

    private void ShowLogin()
    {
        IsLoggedIn = false;
        var vm = new LoginViewModel(_session);
        vm.LoggedIn += ShowCashier;
        CurrentView = vm;
    }

    private void ShowCashier()
    {
        Header.Refresh();
        IsLoggedIn = true;
        var vm = new CashierViewModel(_session, Header);
        CurrentView = vm;
        if (vm.IsShiftOpen) _ = vm.LoadMenuAsync();
    }
}
