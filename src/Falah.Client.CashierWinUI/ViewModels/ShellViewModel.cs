using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>Root of the role-adaptive management shell (Reports, Delivery, Inventory, Purchasing,
/// Employees, Users/Audit, Menu, Settings, Backup, Control Center, plus a "الكاشير" entry back into
/// the fast POS workflow). MainViewModel routes here for any logged-in user who holds at least one
/// permission beyond plain "pos" - a pure cashier never sees this shell at all and goes straight to
/// CashierViewModel, keeping the touch-terminal workflow exactly as fast as before this shell
/// existed. Every nav entry is filtered by AppSession.HasPermission before it is ever added, so
/// "do not expose screens users do not have permission to access" holds by construction rather than
/// by a visibility binding a future edit could forget.</summary>
public partial class ShellViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly HeaderViewModel _header;

    [ObservableProperty] private object? _currentPage;
    [ObservableProperty] private ShellNavItem? _selectedItem;

    public ObservableCollection<ShellNavItem> NavItems { get; } = new();

    /// <summary>The same entries arranged for a top bar. Built after filtering, so a group with no
    /// permitted children is never created.</summary>
    public ObservableCollection<ShellNavGroup> NavGroups { get; } = new();

    /// <summary>Exposes the same header view model the persistent app-bar already uses (restaurant
    /// identity, clock, shift status) so the pane's own branding block shows the identical, real
    /// identity rather than a second hardcoded copy.</summary>
    public HeaderViewModel Header => _header;

    public ShellViewModel(AppSession session, HeaderViewModel header)
    {
        _session = session;
        _header = header;

        foreach (var candidate in AllCandidates())
        {
            if (candidate.Permission is null || _session.HasPermission(candidate.Permission) ||
                candidate.Key == "payroll" && (_session.HasPermission("payroll_approve") || _session.HasPermission("payroll_pay")) ||
                candidate.Key == "shifts" && _session.HasPermission("reports_view") ||
                candidate.Key == "treasury" && _session.HasPermission("treasury_manage"))
                NavItems.Add(candidate);
        }

        BuildGroups();

        if (NavItems.Count > 0)
            Navigate(NavItems[0]);
    }

    /// <summary>Related screens travel together so twenty-two of them fit one row. The order is the
    /// order of a working day - what happened, then what is sold, then what it cost, then who did
    /// it - not alphabetical and not the order the modules were built in.</summary>
    private static readonly (string Key, string Label, string[] Members)[] GroupPlan =
    {
        ("dashboard",  "لوحة المتابعة",     new[] { "dashboard" }),
        ("cashier",    "نقطة البيع",        new[] { "cashier" }),
        ("sales",      "الطلبات والتوصيل",  new[] { "orders", "delivery", "customers" }),
        ("menu",       "القائمة والتسعير",  new[] { "menu", "recipes" }),
        ("stock",      "المخزون",           new[] { "inventory", "waste" }),
        ("buying",     "المشتريات",         new[] { "purchasing", "suppliers" }),
        ("money",      "الخزينة",           new[] { "treasury", "shifts" }),
        ("people",     "الموظفون",          new[] { "employees", "attendance", "payroll" }),
        ("insight",    "التقارير",          new[] { "reports", "daily_statement", "daily_summary", "audit" }),
        ("more",       "المزيد",            new[] { "controlcenter", "printing", "users", "backup", "settings" }),
    };

    private void BuildGroups()
    {
        NavGroups.Clear();
        var placed = new HashSet<string>();
        foreach (var (key, label, members) in GroupPlan)
        {
            var items = members
                .Select(m => NavItems.FirstOrDefault(i => i.Key == m))
                .Where(i => i is not null)
                .Select(i => i!)
                .ToList();
            if (items.Count == 0) continue;
            foreach (var item in items) placed.Add(item.Key);
            NavGroups.Add(new ShellNavGroup(key, items.Count == 1 ? items[0].Label : label, items));
        }

        // Anything the plan does not name still has to be reachable - a screen added later must not
        // silently vanish from the bar because nobody remembered to group it.
        var unplaced = NavItems.Where(i => !placed.Contains(i.Key)).ToList();
        if (unplaced.Count == 0) return;
        var more = NavGroups.FirstOrDefault(g => g.Key == "more");
        if (more is null) NavGroups.Add(new ShellNavGroup("more", "المزيد", unplaced));
        else NavGroups[NavGroups.IndexOf(more)] = more with { Items = more.Items.Concat(unplaced).ToList() };
    }

    /// <summary>The complete module catalog this build's shell knows about - one entry per module
    /// named in the final-product spec. Permission keys are the real PermissionCatalog strings (see
    /// Falah.Domain.Authorization.PermissionCatalog), not invented ones, so gating here matches
    /// exactly what the backend itself would authorize. Nav icons are resolved separately in the
    /// View from Key via NavKeyToGlyphConverter - glyph choice is presentation, not nav state.</summary>
    private static IEnumerable<ShellNavItem> AllCandidates() => new List<ShellNavItem>
    {
        new("dashboard", "لوحة المتابعة", "reports_view"),
        new("cashier", "الكاشير", "pos"),
        // Filtered by an extra OR clause above (same pattern as "shifts"/"payroll"): reports_view
        // covers an owner reading balances/movements, treasury_manage covers a manager who can post
        // deposits/withdrawals/transfers but may not hold reports_view. This entry used to be
        // missing from AllCandidates entirely - the group plan and Navigate() both referenced
        // "treasury", but nothing ever added it to NavItems, so no role could ever open the screen.
        new("treasury", "الخزينة", "reports_view"),
        new("orders", "الطلبات", "orders_view_all"),
        new("delivery", "التوصيل والسائقون", "delivery_manage"),
        new("customers", "العملاء", "delivery_manage"),
        new("reports", "التقارير", "reports_view"),
        // The fuller owner statement - keeps daily_summary's own compact sheet unchanged and
        // reachable at its existing nav key (Section 13's option B: a new page alongside it, not a
        // rewrite of it - safer given daily_summary's fields are already relied on elsewhere).
        new("daily_statement", "كشف اليوم", "reports_view"),
        new("daily_summary", "ملخص اليوم", "reports_view"),
        // Filtered by an extra OR clause below (same pattern "payroll" already uses): shifts_manage
        // covers a supervisor who can open/close/approve any shift but holds no other management
        // permission, reports_view covers an owner/manager reading the history table's sales
        // figures. Matches the reference's own two separate sub-gates (canOpenShift/canSeeShiftTotals)
        // collapsed into one nav-level OR - a viewer with neither could see nothing useful here.
        new("shifts", "الشيفتات", "shifts_manage"),
        new("inventory", "المخزون", "inventory_manage"),
        new("recipes", "الوصفات والتكلفة", "menu_manage"),
        new("waste", "الهدر والفاقد", "inventory_manage"),
        new("purchasing", "المشتريات", "purchasing_manage"),
        new("suppliers", "الموردون", "suppliers_manage"),
        new("employees", "الموظفون", "employees_manage"),
        new("attendance", "الحضور والانصراف", "attendance_manage"),
        new("payroll", "الرواتب والأجور اليومية", "employees_ledger"),
        new("menu", "القائمة والتسعير", "menu_manage"),
        new("users", "المستخدمون والصلاحيات", "users_manage"),
        new("audit", "سجل المراجعة", "audit_view"),
        new("controlcenter", "مركز التحكم بيوم العمل", "control_center"),
        new("backup", "النسخ الاحتياطي والاستعادة", "backup_manage"),
        new("settings", "الإعدادات", "settings_manage"),
        new("printing", "الطباعة والأجهزة", "settings_manage"),
    };

    [RelayCommand]
    private void Navigate(ShellNavItem item)
    {
        if (_session.HasUnresolvedOperation || !NavItems.Contains(item)) return;
        SelectedItem = item;
        CurrentPage = item.Key switch
        {
            "dashboard" => CreateOwnerDashboard(),
            "cashier" => CreateCashier(),
            "delivery" => CreateDelivery(),
            "reports" => CreateReports(),
            "daily_statement" => CreateDailyStatement(),
            "daily_summary" => CreateDailySummary(),
            "inventory" => CreateInventory(),
            "treasury" => CreateTreasury(),
            "shifts" => CreateShifts(),
            "purchasing" or "suppliers" => CreatePurchasing(),
            "recipes" => CreateRecipes(),
            "waste" => CreateWaste(),
            "employees" => CreateEmployees(),
            "attendance" => CreateAttendance(),
            "payroll" => CreatePayroll(),
            "users" => CreateUsers(),
            "audit" => CreateAudit(),
            "menu" => CreateMenu(),
            "settings" => CreateSettings(),
            "backup" => CreateBackup(),
            "controlcenter" => CreateControlCenter(),
            "printing" => CreatePrinting(),
            "orders" => CreateOrders(),
            "customers" => CreateCustomers(),
            _ => new ShellPlaceholderViewModel(item.Label),
        };
    }

    private OrdersViewModel CreateOrders()
    {
        var vm = new OrdersViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private CustomersViewModel CreateCustomers() => new(_session);

    private MenuViewModel CreateMenu()
    {
        var vm = new MenuViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private SettingsViewModel CreateSettings()
    {
        var vm = new SettingsViewModel(_session, _header);
        _ = vm.LoadAsync();
        return vm;
    }

    private BackupViewModel CreateBackup()
    {
        var vm = new BackupViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private ControlCenterViewModel CreateControlCenter()
    {
        var vm = new ControlCenterViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private PrintingViewModel CreatePrinting()
    {
        var vm = new PrintingViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private UsersViewModel CreateUsers()
    {
        var vm = new UsersViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private AuditViewModel CreateAudit()
    {
        var vm = new AuditViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private EmployeesViewModel CreateEmployees()
    {
        var vm = new EmployeesViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private AttendanceViewModel CreateAttendance()
    {
        var vm = new AttendanceViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private PayrollViewModel CreatePayroll()
    {
        var vm = new PayrollViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private RecipesViewModel CreateRecipes()
    {
        var vm = new RecipesViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private WasteViewModel CreateWaste()
    {
        var vm = new WasteViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private PurchasingViewModel CreatePurchasing()
    {
        var vm = new PurchasingViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private TreasuryViewModel CreateTreasury()
    {
        var vm = new TreasuryViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private ShiftsViewModel CreateShifts()
    {
        var vm = new ShiftsViewModel(_session, _header);
        _ = vm.LoadAsync();
        return vm;
    }

    private InventoryViewModel CreateInventory()
    {
        var vm = new InventoryViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private OwnerDashboardViewModel CreateOwnerDashboard()
    {
        // An attention row opens the screen that resolves it, using the same nav keys and the same
        // permission filter as the pane - a row for a screen this user cannot open never appears,
        // because that screen is not in NavItems at all.
        var vm = new OwnerDashboardViewModel(_session, NavigateByKey);
        _ = vm.LoadAsync();
        return vm;
    }

    private void NavigateByKey(string key)
    {
        if (NavItems.FirstOrDefault(i => i.Key == key) is { } target) Navigate(target);
    }

    private ReportsViewModel CreateReports()
    {
        var vm = new ReportsViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private DailySummaryViewModel CreateDailySummary()
    {
        var vm = new DailySummaryViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private DailyStatementViewModel CreateDailyStatement()
    {
        var vm = new DailyStatementViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private DeliveryViewModel CreateDelivery()
    {
        var vm = new DeliveryViewModel(_session);
        _ = vm.LoadAsync();
        return vm;
    }

    private CashierViewModel CreateCashier()
    {
        var vm = _session.GetCashier(_header);
        _ = vm.InitializeAsync();
        return vm;
    }
}
