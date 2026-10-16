using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Users;
using Falah.Client.CashierWinUI.Services;
using Falah.Domain.Authorization;
using Falah.Domain.Common;

namespace Falah.Client.CashierWinUI.ViewModels;

public partial class UsersViewModel
{
    private readonly RetainedRequest<CreateUserRequest> _pendingCreate = new("users.create");
    private readonly RetainedRequest<ChangePinRequest> _pendingPin = new("users.change-pin");
    private readonly RetainedRequest<SetUserActiveRequest> _pendingActive = new("users.set-active");
    private readonly RetainedRequest<UpdateUserPermissionsRequest> _pendingPermissions = new("users.permissions");
    public bool HasPendingAction => _pendingCreate.HasPending || _pendingPin.HasPending || _pendingActive.HasPending || _pendingPermissions.HasPending;
    public bool CanEditAction => !IsBusy && !HasPendingAction;
    public bool IsUserDialogOpen => IsCreateUserOpen || PinTarget is not null;
    public bool CanCreateUser => CanEditAction && !IsUserDialogOpen && !IsLoading && LoadError is null &&
        _catalog.Count > 0 && _session.HasPermission("users_manage");
    public bool CanChangeSelectedPin => CanEditAction && !IsUserDialogOpen && !IsLoading && LoadError is null && SelectedUser is not null &&
        (SelectedUser.Id == _session.CurrentUser?.Id || (!IsOwnerSelected && _session.HasPermission("users_manage")));
    public bool IsPinChangeOpen => PinTarget is not null;
    public bool IsSelfPinChange => PinTarget?.Id == _session.CurrentUser?.Id;
    public string PinChangeTitle => IsSelfPinChange ? "تغيير رمزي السري" : "إعادة تعيين الرمز السري";
    public string CreateUserLabel => IsBusy ? "جارٍ التحقق..." : _pendingCreate.HasPending ? "التحقق وإعادة المحاولة" : "إنشاء المستخدم";
    public string ChangePinLabel => IsBusy ? "جارٍ التحقق..." : _pendingPin.HasPending ? "التحقق وإعادة المحاولة" : "تأكيد التغيير";
    public ObservableCollection<PermissionToggle> NewUserPermissions { get; } = new();

    [ObservableProperty] private bool _isCreateUserOpen;
    [ObservableProperty] private string _newUserName = "";
    [ObservableProperty] private string _newUserRole = "";

    /// <summary>The three roles this system actually recognises, with the exact keys the server
    /// compares against. Typed free-hand, "Owner" or "مالك" silently produced an ordinary account
    /// that merely looked like an owner - the owner-immutability rule matches the literal "owner"
    /// and nothing else - so the role is picked, not spelled.</summary>
    public sealed record RoleOption(string Key, string Label);

    public IReadOnlyList<RoleOption> RoleOptions { get; } = new[]
    {
        new RoleOption("cashier", "كاشير"),
        new RoleOption("manager", "مدير"),
        new RoleOption(UserManagementRules.OwnerRole, "مالك (حساب الطوارئ المحمي)"),
    };

    [ObservableProperty] private RoleOption? _selectedNewUserRole;

    partial void OnSelectedNewUserRoleChanged(RoleOption? value)
    {
        NewUserRole = value?.Key ?? "";
        OnPropertyChanged(nameof(RoleHint));
    }

    /// <summary>Shown under the role picker so whoever creates the owner knows what they are
    /// creating: it is the one account no administrator can deactivate or strip, and the system
    /// allows exactly one.</summary>
    public string RoleHint => SelectedNewUserRole?.Key == UserManagementRules.OwnerRole
        ? "حساب المالك لا يمكن تعطيله ولا تعديل صلاحياته - وهو طريق الرجوع الوحيد لو ضاع باقي المديرين. يُنشأ مرة واحدة فقط."
        : "";
    [ObservableProperty] private string _newUserPin = "";
    [ObservableProperty] private string _confirmUserPin = "";
    [ObservableProperty] private UserDetailsDto? _pinTarget;
    [ObservableProperty] private string _currentPin = "";
    [ObservableProperty] private string _newPin = "";
    [ObservableProperty] private string _confirmPin = "";

    partial void OnIsCreateUserOpenChanged(bool value) => RefreshActions();
    partial void OnPinTargetChanged(UserDetailsDto? value) => RefreshActions();

    private void RefreshActions()
    {
        foreach (var property in new[] { nameof(CanEditAction), nameof(HasPendingAction), nameof(CanEditSelected),
            nameof(CanCreateUser), nameof(CanChangeSelectedPin), nameof(IsUserDialogOpen), nameof(IsPinChangeOpen),
            nameof(IsSelfPinChange), nameof(PinChangeTitle), nameof(CreateUserLabel), nameof(ChangePinLabel) }) OnPropertyChanged(property);
    }

    [RelayCommand]
    private void OpenCreateUser()
    {
        if (!CanCreateUser) return;
        NewUserName = NewUserRole = NewUserPin = ConfirmUserPin = "";
        SelectedNewUserRole = null;
        ActionError = ActionNote = null;
        NewUserPermissions.Clear();
        foreach (var definition in _catalog.OrderBy(d => d.Category).ThenBy(d => d.Key))
            NewUserPermissions.Add(new PermissionToggle { Key = definition.Key, Category = definition.Category,
                CanActorGrant = _session.HasPermission(definition.Key) });
        IsCreateUserOpen = true;
    }

    [RelayCommand]
    private void CloseUserDialog()
    {
        if (!CanEditAction) return;
        IsCreateUserOpen = false; PinTarget = null;
        NewUserPin = ConfirmUserPin = CurrentPin = NewPin = ConfirmPin = "";
        ActionError = null;
    }

    private bool ValidatePin(string pin, string confirmation)
    {
        if (pin != confirmation) { ActionError = "الرمزان غير متطابقين."; return false; }
        try { PinHasher.AssertValidPin(pin); return true; }
        catch (DomainException ex) { ActionError = ex.Message; return false; }
    }

    [RelayCommand]
    private async Task CreateUserAsync()
    {
        if (IsBusy || (!_pendingCreate.HasPending && (!CanEditAction || !IsCreateUserOpen || !_session.HasPermission("users_manage")))) return;
        var request = _pendingCreate.Value;
        if (request is null)
        {
            if (string.IsNullOrWhiteSpace(NewUserName) || string.IsNullOrWhiteSpace(NewUserRole))
            { ActionError = "اسم المستخدم ودوره مطلوبان."; return; }
            // The server decides whether an owner may be created (only when the installation has
            // none yet) - the client used to refuse outright, which left a till with no owner no
            // way to ever get one.
            if (!ValidatePin(NewUserPin, ConfirmUserPin)) return;
            var permissions = NewUserPermissions.Where(p => p.IsGranted).Select(p => p.Key).ToArray();
            if (permissions.Any(p => !_session.HasPermission(p))) { ActionError = "لا يمكنك منح صلاحية لا تملكها."; return; }
            request = new CreateUserRequest($"create-user-{Guid.NewGuid():N}", NewUserName.Trim(), NewUserRole.Trim(),
                NewUserPin, permissions, _session.CurrentUser!.Id);
        }
        IsBusy = true; ActionError = null;
        try
        {
            var result = await _pendingCreate.SendAsync(request, r => _session.Api.CreateUserAsync(r));
            if (!result.Success || result.Value is null) { ActionError = _pendingCreate.FailureMessage(result.Error); return; }
            IsCreateUserOpen = false; NewUserPin = ConfirmUserPin = "";
            ActionNote = "تم إنشاء المستخدم.";
            await ReloadAsync(result.Value.UserId);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenPinChange()
    {
        if (!CanChangeSelectedPin) return;
        CurrentPin = NewPin = ConfirmPin = ""; ActionError = ActionNote = null;
        PinTarget = SelectedUser;
    }

    [RelayCommand]
    private async Task ChangePinAsync()
    {
        if (IsBusy || (!_pendingPin.HasPending && (!CanEditAction || PinTarget is null))) return;
        var wasPending = _pendingPin.HasPending;
        var request = _pendingPin.Value;
        if (request is null)
        {
            if (IsSelfPinChange && string.IsNullOrEmpty(CurrentPin)) { ActionError = "أدخل الرمز الحالي."; return; }
            if (!IsSelfPinChange && (PinTarget!.Role == UserManagementRules.OwnerRole || !_session.HasPermission("users_manage"))) return;
            if (!ValidatePin(NewPin, ConfirmPin)) return;
            request = new ChangePinRequest(PinTarget!.Id, IsSelfPinChange ? CurrentPin : null, NewPin, _session.CurrentUser!.Id);
        }
        IsBusy = true; ActionError = null;
        try
        {
            var result = await _pendingPin.SendAsync(request, async original =>
            {
                // The self-service endpoint proves the OLD PIN, so replay after a committed
                // change may reject it. Confirm the intended new credential belongs to this
                // exact user before issuing another write. Never change the logged-in session.
                if (wasPending && original.UserId == original.ActorUserId)
                {
                    var proof = await _session.Api.LoginByPinAsync(original.NewPin);
                    if (proof.Success && proof.Value?.Id == original.UserId) return new ApiClient.ApiResult<bool>(true, true, null);
                }
                return await _session.Api.ChangePinAsync(original);
            });
            if (!result.Success) { ActionError = _pendingPin.FailureMessage(result.Error); return; }
            PinTarget = null; CurrentPin = NewPin = ConfirmPin = "";
            ActionNote = "تم تأكيد تغيير الرمز السري.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RetryUserActionAsync()
    {
        if (IsBusy) return;
        if (_pendingCreate.HasPending) await CreateUserAsync();
        else if (_pendingPin.HasPending) await ChangePinAsync();
        else if (_pendingPermissions.HasPending) await SavePermissionsAsync();
        else if (_pendingActive.HasPending) await ToggleActiveAsync();
    }
}
