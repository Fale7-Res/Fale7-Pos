using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Users;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Users and what each one is allowed to do. The permission list is fetched from the server's own
/// catalog (GET /api/permissions/catalog) rather than hardcoded here - that catalog is built from
/// every real authorization check in the codebase, so a hand-maintained copy in this screen could
/// only ever drift out of sync with what the backend actually enforces.
///
/// Two server rules are surfaced rather than hidden, because discovering them via a rejection is
/// worse than being told up front: the owner account is immune to administrative changes, and an
/// actor can never grant a permission they do not themselves hold.
/// </summary>
public partial class UsersViewModel : ObservableObject
{
    private readonly AppSession _session;

    public UsersViewModel(AppSession session) => _session = session;

    /// <summary>A catalog permission paired with whether the selected user currently holds it.</summary>
    public sealed partial class PermissionToggle : ObservableObject
    {
        public required string Key { get; init; }
        public required string Category { get; init; }
        public string Label => Key switch
        {
            "all" => "كل الصلاحيات", "pos" => "تسجيل الطلبات", "cash_drawer_sale" => "فتح درج النقد عند البيع",
            "orders_comp" => "اعتماد الضيافة", "orders_refund" => "استرداد الطلبات", "orders_discount" => "خصم الطلبات",
            "orders_view_all" => "عرض جميع الطلبات", "shifts_blind_close" => "إغلاق الوردية بالعد الفعلي",
            "shifts_manage" => "إدارة الورديات", "shifts_open_own" => "فتح ورديتي",
            "expenses_add_drawer" => "مصروفات درج النقد", "expenses_main_treasury" => "مصروفات الخزنة الرئيسية",
            "treasury_manage" => "إدارة الخزائن", "safe_drop" => "توريد النقد إلى الخزنة", "cash_drawer" => "فتح درج النقد",
            "business_day_open" => "فتح يوم العمل", "business_day_close" => "إغلاق يوم العمل",
            "business_day_reopen" => "إعادة فتح يوم العمل", "control_center" => "مراجعة مركز الرقابة", "cash_count" => "عد النقد",
            "purchasing_manage" => "إدارة المشتريات", "suppliers_manage" => "إدارة الموردين", "inventory_manage" => "إدارة المخزون",
            "menu_manage" => "إدارة القائمة", "offers_manage" => "إدارة العروض", "settings_manage" => "إدارة الإعدادات",
            "delivery_manage" => "إدارة التوصيل", "delivery_override" => "تجاوز قيود التوصيل",
            "delivery_settlement_approve" => "اعتماد تسوية عهدة السائق", "reports_view" => "عرض التقارير",
            "users_manage" => "إدارة المستخدمين", "audit_view" => "عرض سجل المراجعة", "employees_manage" => "إدارة الموظفين",
            "employees_ledger" => "حسابات الموظفين", "attendance_manage" => "إدارة الحضور", "payroll_approve" => "اعتماد الرواتب",
            "payroll_pay" => "صرف الرواتب", "backup_manage" => "إدارة النسخ الاحتياطية", _ => Key
        };
        public string CategoryLabel => Category switch
        {
            "system" => "النظام", "orders" => "الطلبات", "shifts" => "الورديات", "expenses" => "المصروفات",
            "treasury" => "الخزائن", "business_day" => "يوم العمل", "purchasing" => "المشتريات", "inventory" => "المخزون",
            "menu" => "القائمة", "settings" => "الإعدادات", "delivery" => "التوصيل", "reports" => "التقارير",
            "users" => "المستخدمون", "audit" => "المراجعة", "employees" => "الموظفون", "backup" => "النسخ الاحتياطية", _ => Category
        };
        [ObservableProperty] private bool _isGranted;

        /// <summary>False when the acting user does not hold this permission themselves - the server
        /// refuses that grant (AssertNoPrivilegeEscalation), so it is shown disabled rather than
        /// offered and then rejected.</summary>
        public required bool CanActorGrant { get; init; }
    }

    public ObservableCollection<UserDetailsDto> Users { get; } = new();
    public ObservableCollection<PermissionToggle> Permissions { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private string? _actionError;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private UserDetailsDto? _selectedUser;

    private IReadOnlyList<Falah.Domain.Authorization.PermissionDefinition> _catalog = Array.Empty<Falah.Domain.Authorization.PermissionDefinition>();

    public bool HasNoUsers => !IsLoading && LoadError is null && Users.Count == 0;

    /// <summary>The owner account is unconditionally immune to deactivation, PIN reset and
    /// permission changes from the outside - enforced server-side by
    /// UserManagementRules.AssertTargetIsNotOwner.</summary>
    public bool IsOwnerSelected => SelectedUser?.Role == "owner";
    public bool CanEditSelected => CanEditAction && !IsUserDialogOpen && SelectedUser is not null && !IsOwnerSelected && !IsLoading && LoadError is null && _session.HasPermission("users_manage");
    partial void OnIsBusyChanged(bool value) { RefreshActions(); _session.SetManagementOperationPending(this, !CanEditAction); }
    partial void OnIsLoadingChanged(bool value) => RefreshActions();
    partial void OnLoadErrorChanged(string? value) => RefreshActions();

    partial void OnSelectedUserChanged(UserDetailsDto? value)
    {
        OnPropertyChanged(nameof(IsOwnerSelected));
        RefreshActions();
        RebuildToggles();
    }

    private void RebuildToggles()
    {
        Permissions.Clear();
        if (SelectedUser is null) return;
        foreach (var def in _catalog.OrderBy(d => d.Category).ThenBy(d => d.Key))
            Permissions.Add(new PermissionToggle
            {
                Key = def.Key,
                Category = def.Category,
                IsGranted = SelectedUser.Permissions.Contains(def.Key),
                CanActorGrant = SelectedUser.Permissions.Contains(def.Key) || _session.HasPermission(def.Key),
            });
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!CanEditAction || IsUserDialogOpen) return;
        await ReloadAsync();
    }

    private async Task ReloadAsync(string? selectedId = null)
    {
        if (IsLoading) return;
        selectedId ??= SelectedUser?.Id;
        IsLoading = true;
        LoadError = null;
        try
        {
            var actor = _session.CurrentUser!.Id;
            var users = await _session.Api.GetAllUsersAsync(actor);
            if (!users.Success || users.Value is null)
            {
                LoadError = users.Error ?? "تعذر تحميل المستخدمين.";
                return;
            }

            var catalog = await _session.Api.GetPermissionCatalogAsync();
            if (!catalog.Success || catalog.Value is null) { LoadError = catalog.Error ?? "تعذر تحميل دليل الصلاحيات."; return; }
            _catalog = catalog.Value;

            Users.Clear();
            foreach (var u in users.Value) Users.Add(u);
            SelectedUser = Users.FirstOrDefault(u => u.Id == selectedId) ?? Users.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoUsers));
        }
    }

    [RelayCommand]
    private void SelectUser(UserDetailsDto user) { if (CanEditAction && !IsUserDialogOpen && !IsLoading) SelectedUser = user; }

    [RelayCommand]
    private async Task SavePermissionsAsync()
    {
        if (IsBusy || (!_pendingPermissions.HasPending && !CanEditSelected)) return;
        if (_pendingPermissions.HasPending) { await SendPermissionsAsync(_pendingPermissions.Value!); return; }
        if (SelectedUser is null) return;

        var grant = Permissions.Where(p => p.IsGranted && !SelectedUser.Permissions.Contains(p.Key)).Select(p => p.Key).ToList();
        var revoke = Permissions.Where(p => !p.IsGranted && SelectedUser.Permissions.Contains(p.Key)).Select(p => p.Key).ToList();
        if (grant.Count == 0 && revoke.Count == 0)
        {
            ActionError = "لا توجد تغييرات لحفظها.";
            return;
        }

        await SendPermissionsAsync(new UpdateUserPermissionsRequest(SelectedUser!.Id, grant.ToArray(), revoke.ToArray(), _session.CurrentUser!.Id));
    }

    private async Task SendPermissionsAsync(UpdateUserPermissionsRequest request)
    {
        IsBusy = true;
        ActionError = null;
        try
        {
            var result = await _pendingPermissions.SendAsync(request, r => _session.Api.UpdateUserPermissionsAsync(r));
            if (!result.Success)
            {
                ActionError = _pendingPermissions.FailureMessage(result.Error);
                return;
            }
            ActionNote = "تم تحديث الصلاحيات.";
            await ReloadAsync(request.UserId);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (IsBusy || (!_pendingActive.HasPending && !CanEditSelected)) return;
        var request = _pendingActive.Value ?? new SetUserActiveRequest(SelectedUser!.Id, !SelectedUser.Active, null, _session.CurrentUser!.Id);
        IsBusy = true;
        ActionError = null;
        try
        {
            var result = await _pendingActive.SendAsync(request, r => _session.Api.SetUserActiveAsync(r));
            if (!result.Success)
            {
                ActionError = _pendingActive.FailureMessage(result.Error);
                return;
            }
            ActionNote = "تم تحديث حالة المستخدم.";
            await ReloadAsync(request.UserId);
        }
        finally { IsBusy = false; }
    }
}
