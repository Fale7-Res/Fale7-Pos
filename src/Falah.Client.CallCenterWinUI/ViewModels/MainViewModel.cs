using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Client.CallCenterWinUI.Services;
using Microsoft.UI.Xaml;

namespace Falah.Client.CallCenterWinUI.ViewModels;

/// <summary>Root view model for the Call Center station: decides Login vs the order-intake screen,
/// owns the persistent app header, and polls server health - same shape as CashierWinUI's own
/// MainViewModel, deliberately simplified since this station has no management shell to route into
/// (it does exactly one job) and never opens a shift of its own.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly DispatcherTimer _healthTimer;
    private bool _stopped;

    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private bool _isOnline = true;
    [ObservableProperty] private bool _isLoggedIn;
    [ObservableProperty] private bool _isCloseBlocked;

    public HeaderViewModel Header { get; }

    public MainViewModel(AppSession? session = null)
    {
        _session = session ?? new AppSession();
        Header = new HeaderViewModel(_session);
        ShowLogin();

        _healthTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _healthTimer.Tick += async (_, _) => await CheckHealthNowAsync();
        _healthTimer.Start();
        _ = CheckHealthNowAsync();
    }

    private async Task CheckHealthNowAsync()
    {
        var online = await _session.Api.PingHealthAsync();
        if (!_stopped) IsOnline = online;
    }

    public bool TryClose()
    {
        IsCloseBlocked = _session.HasUnresolvedOperation;
        return !IsCloseBlocked;
    }

    public void Stop()
    {
        _stopped = true;
        _healthTimer.Stop();
        Header.Stop();
    }

    [RelayCommand]
    private void Lock()
    {
        if (!_session.HasUnresolvedOperation) ShowLogin();
    }

    private void ShowLogin(string? errorMessage = null)
    {
        IsLoggedIn = false;
        var vm = new LoginViewModel(_session) { ErrorMessage = errorMessage };
        vm.LoggedIn += OnLoggedIn;
        CurrentView = vm;
    }

    /// <summary>Checkout requires the CashierId user (here, whoever is staffing this station) to
    /// hold "pos" - see CheckoutOrderHandler's own unconditional gate. Gating station access on the
    /// same permission means anyone let past this login can actually complete the orders they take;
    /// no separate "call_center" permission is invented for a check the backend already makes.</summary>
    private async void OnLoggedIn()
    {
        if (!_session.HasPermission("pos"))
        {
            ShowLogin("هذا الحساب لا يملك صلاحية تسجيل الطلبات، ولا يمكن استخدامه في محطة مركز الاتصال.");
            return;
        }

        Header.Refresh();
        IsLoggedIn = true;
        var vm = _session.GetOrderDesk(Header);
        CurrentView = vm;
        await vm.InitializeAsync();
    }
}
