using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Client.CashierWinUI.Services;
using Microsoft.UI.Xaml;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>Root view model: decides Login vs Cashier, owns the persistent app header, and polls
/// the server's health so an Online/Offline indicator is always honest - if the Host's server
/// process is unreachable, this client must say so plainly rather than let the cashier discover it
/// only when a payment fails.</summary>
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

        RecoveredOperationsNotice = DescribeRecoveredOperations();

        _healthTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _healthTimer.Tick += async (_, _) => await CheckHealthNowAsync();
        _healthTimer.Start();
        _ = CheckHealthNowAsync();
    }

    /// <summary>Operations whose outcome was still unknown when this station last stopped. They are
    /// surfaced, never re-sent on their own: the screen that submitted one recovers the exact request
    /// and its own retry re-sends those same bytes, which is the only safe way to ask the server what
    /// actually happened.</summary>
    [ObservableProperty] private string? _recoveredOperationsNotice;

    private static string? DescribeRecoveredOperations()
    {
        var records = RetainedRequestStorage.Current?.List();
        if (records is null || records.Count == 0) return null;
        var oldest = records.Min(r => r.SavedAtUtc).ToLocalTime();
        var names = string.Join("، ", records.Select(r => r.OwnerKey).Distinct());
        return records.Count == 1
            ? $"عملية واحدة لم تُحسم نتيجتها قبل إغلاق البرنامج ({names}، منذ {oldest:yyyy/MM/dd HH:mm}). افتح شاشتها واضغط زر التحقق لمعرفة ما إذا كانت قد نُفِّذت."
            : $"{records.Count} عمليات لم تُحسم نتيجتها قبل إغلاق البرنامج ({names}، أقدمها {oldest:yyyy/MM/dd HH:mm}). افتح شاشة كل منها واضغط زر التحقق لمعرفة ما إذا كانت قد نُفِّذت.";
    }

    [RelayCommand]
    private void DismissRecoveredNotice() => RecoveredOperationsNotice = null;

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

    /// <summary>Matches the reference app's "قفل" button: returns to the PIN screen without
    /// ending the shift or clearing anything server-side - a different person can unlock with
    /// their own PIN, or the same person can resume.</summary>
    [RelayCommand]
    private void Lock() { if (!_session.HasUnresolvedOperation) ShowLogin(); }

    private void ShowLogin()
    {
        IsLoggedIn = false;
        var vm = new LoginViewModel(_session);
        vm.LoggedIn += ShowMain;
        CurrentView = vm;
    }

    /// <summary>Routes a freshly-logged-in user to the fast, chrome-free Cashier screen if "pos" is
    /// all they hold, or to the role-adaptive management shell if they hold at least one permission
    /// a management module gates on - a pure cashier's touch-terminal workflow must stay exactly as
    /// fast as it was before the shell existed, so it never even constructs a NavigationView for
    /// that user. The shell itself still exposes "الكاشير" as its first nav entry, so anyone who does
    /// see the shell can always reach the same POS screen from inside it.</summary>
    private void ShowMain()
    {
        Header.Refresh();
        IsLoggedIn = true;
        CurrentView = HasAnyManagementPermission() || !_session.HasPermission("pos")
            ? new ShellViewModel(_session, Header)
            : CreateCashier();
    }

    private bool HasAnyManagementPermission() =>
        ShellManagementPermissions.Any(_session.HasPermission);

    private static readonly string[] ShellManagementPermissions =
    {
        "orders_view_all", "delivery_manage", "reports_view", "treasury_manage", "inventory_manage",
        "purchasing_manage", "suppliers_manage", "employees_manage", "attendance_manage",
        "employees_ledger", "payroll_approve", "payroll_pay", "menu_manage", "users_manage", "audit_view", "control_center",
        "backup_manage", "settings_manage",
        // Added for the new Shifts management screen: a supervisor holding ONLY shifts_manage (no
        // other management permission) must still reach the shell to see it - without this they
        // would have a nav entry gated on a permission the shell itself would never let them in for.
        "shifts_manage", "all",
    };

    private CashierViewModel CreateCashier()
    {
        var vm = _session.GetCashier(Header);
        _ = vm.InitializeAsync();
        return vm;
    }
}
