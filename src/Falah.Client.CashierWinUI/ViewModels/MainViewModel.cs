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
        _session.OperationStateChanged += RefreshRecoveredNotice;

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

    /// <summary>Where the operator finds the operation - the screen, never the storage key
    /// ("treasury.action"), which said nothing to the person reading it.</summary>
    private static string ScreenOf(string ownerKey) => ownerKey.Split('.')[0] switch
    {
        "cashier" => "نقطة البيع",
        "treasury" => "الخزينة",
        "delivery" => "التوصيل والسائقون",
        "employees" => "الموظفون",
        "payroll" => "الرواتب",
        "purchasing" => "المشتريات",
        "inventory" => "المخزون",
        "attendance" => "الحضور والانصراف",
        "backup" => "النسخ الاحتياطي",
        "business-day" or "controlcenter" => "مركز التحكم بيوم العمل",
        "menu" => "القائمة والتسعير",
        "users" => "المستخدمون والصلاحيات",
        "shifts" => "الشيفتات",
        _ => "إحدى شاشات الإدارة",
    };

    private static string? DescribeRecoveredOperations()
    {
        if (RetainedRequestStorage.Current?.RecoveryError is { } protectionError) return protectionError;
        var records = RetainedRequestStorage.Current?.List();
        if (records is null || records.Count == 0) return null;
        var oldest = records.Min(r => r.SavedAtUtc).ToLocalTime();
        var names = string.Join("، ", records.Select(r => ScreenOf(r.OwnerKey)).Distinct());
        return records.Count == 1
            ? $"عملية واحدة لم تُحسم نتيجتها قبل إغلاق البرنامج ({names}، منذ {oldest:yyyy/MM/dd HH:mm}). افتح شاشتها واضغط زر التحقق لمعرفة ما إذا كانت قد نُفِّذت."
            : $"{records.Count} عمليات لم تُحسم نتيجتها قبل إغلاق البرنامج ({names}، أقدمها {oldest:yyyy/MM/dd HH:mm}). افتح شاشة كل منها واضغط زر التحقق لمعرفة ما إذا كانت قد نُفِّذت.";
    }

    [RelayCommand]
    private void DismissRecoveredNotice() => RecoveredOperationsNotice = null;

    /// <summary>The notice described the store at startup. Once a screen resolves a recovered operation
    /// its record leaves the store, and a notice still claiming it is unresolved would send the operator
    /// looking for nothing. Only ever narrows or clears the notice - a dismissed one stays dismissed.</summary>
    private void RefreshRecoveredNotice()
    {
        if (RecoveredOperationsNotice is not null) RecoveredOperationsNotice = DescribeRecoveredOperations();
    }

    /// <summary>One check at a time (audit 2026-11-18): the timer fires every 5 s and a ping can wait the
    /// full 10 s request timeout, so with the server unreachable the checks used to pile up.</summary>
    private bool _checkingHealth;

    private async Task CheckHealthNowAsync()
    {
        if (_checkingHealth) return;
        _checkingHealth = true;
        try
        {
            var online = await _session.Api.PingHealthAsync();
            if (!_stopped) IsOnline = online;
        }
        finally { _checkingHealth = false; }
    }

    public bool TryClose()
    {
        IsCloseBlocked = _session.HasUnresolvedOperation;
        return !IsCloseBlocked;
    }

    public void Stop()
    {
        _stopped = true;
        _session.OperationStateChanged -= RefreshRecoveredNotice;
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
        if (RetainedRequestStorage.Current?.RecoveryError is { } recoveryError)
        {
            ShowLogin();
            if (CurrentView is LoginViewModel login) login.ErrorMessage = recoveryError;
            return;
        }
        Header.Refresh();
        IsLoggedIn = true;
        CurrentView = HasAnyManagementPermission() || !_session.HasPermission("pos")
            ? new ShellViewModel(_session, Header)
            : CreateCashier();
        _ = LoadCurrentShiftAsync();
    }

    /// <summary>The header's shift pill must be true on every screen, not only after the POS screen
    /// has loaded: a manager landing on the dashboard was told "مغلق" while a shift was open. A
    /// failed read leaves what the session already knew rather than claiming the shift is closed.</summary>
    private async Task LoadCurrentShiftAsync()
    {
        try
        {
            var shift = await _session.Api.GetCurrentShiftAsync();
            if (!shift.Success && shift.StatusCode != 404) return;
            _session.ShiftId = shift.Value?.ShiftId;
            _session.ShiftName = shift.Value?.ShiftName;
            Header.Refresh();
        }
        catch { /* the POS screen and the next login read it again */ }
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
