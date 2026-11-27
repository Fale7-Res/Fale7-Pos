using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Client.CallCenterWinUI.Services;
using Microsoft.UI.Xaml;

namespace Falah.Client.CallCenterWinUI.ViewModels;

/// <summary>Root view model for the Call Center station: decides Login vs the order-intake screen,
/// owns the persistent app header, and polls server health - same shape as CashierWinUI's own
/// MainViewModel, simplified: three screens (order desk, order history, delivery board) and no shift
/// of its own.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly DispatcherTimer _healthTimer;
    private bool _stopped;

    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private bool _isOnline = true;
    [ObservableProperty] private bool _isLoggedIn;
    [ObservableProperty] private bool _isCloseBlocked;

    /// <summary>Which screen of the station is shown: "desk" (taking an order), "orders" (order history) or
    /// "delivery" (the dispatch board). Owner request 2026-11-24: the Call Center manages its deliveries too.</summary>
    [ObservableProperty] private string _section = "desk";

    /// <summary>The board's actions are delivery_manage on the server; without it the tab is not offered.</summary>
    public bool CanSeeDelivery => _session.HasPermission("delivery_manage");

    [RelayCommand]
    private void Navigate(string section)
    {
        if (!IsLoggedIn) return;
        switch (section)
        {
            case "orders":
                var orders = _session.OrderHistory;
                orders.RefreshPermissions();
                Section = section;
                CurrentView = orders;
                _ = orders.LoadAsync();
                break;
            case "delivery" when CanSeeDelivery:
                var board = _session.DeliveryBoard;
                board.RefreshPermissions();
                Section = section;
                CurrentView = board;
                _ = board.LoadAsync();
                break;
            default:
                Section = "desk";
                CurrentView = _session.GetOrderDesk(Header);
                break;
        }
    }

    public HeaderViewModel Header { get; }

    public MainViewModel(AppSession? session = null)
    {
        _session = session ?? new AppSession();
        Header = new HeaderViewModel(_session);
        ShowLogin(RetainedRequestStorage.Current?.RecoveryError);

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

    /// <summary>Owner request 2026-11-27: the log says which screen the station was on when something failed.</summary>
    partial void OnCurrentViewChanged(object? value) =>
        Falah.Client.Diagnostics.ClientLog.Write("INFO", $"screen: {value?.GetType().Name.Replace("ViewModel", "") ?? "none"}");

    [RelayCommand]
    private void Lock()
    {
        if (!_session.HasUnresolvedOperation) ShowLogin();
    }

    private void ShowLogin(string? errorMessage = null)
    {
        IsLoggedIn = false;
        Section = "desk";
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
        if (RetainedRequestStorage.Current?.RecoveryError is { } recoveryError) { ShowLogin(recoveryError); return; }
        if (!_session.HasPermission("pos"))
        {
            ShowLogin("هذا الحساب لا يملك صلاحية تسجيل الطلبات، ولا يمكن استخدامه في محطة مركز الاتصال.");
            return;
        }

        Header.Refresh();
        // Built at every login, before anything else: a delivery command an earlier run left unanswered is read
        // back from disk here, and until it is retried the station cannot be locked.
        _ = _session.DeliveryBoard;
        OnPropertyChanged(nameof(CanSeeDelivery));
        Section = "desk";
        IsLoggedIn = true;
        if (_session.CurrentUser is { } user)
            Falah.Client.Diagnostics.ClientLog.Write("INFO", $"signed in: {user.Name} ({user.Id}, {user.Role})");
        var vm = _session.GetOrderDesk(Header);
        CurrentView = vm;
        // async void (an event handler): anything that escaped InitializeAsync used to close the whole
        // call-center app at login. The desk stays open and the operator is told (audit 2026-11-18).
        try { await vm.InitializeAsync(); }
        catch (Exception ex)
        {
            Falah.Client.Diagnostics.CrashLog.Write(System.IO.Path.Combine(Services.FileRetainedRequestStore.DataRoot(), "logs"),
                "callcenter", ex, "order desk start-up");
            Falah.Client.Diagnostics.ClientLog.WriteException("order desk start-up", ex);
            vm.ShiftError = "تعذر تحميل بيانات المحطة. تحقق من الاتصال ثم اضغط تحديث.";
        }
    }
}
