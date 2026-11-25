using Falah.Client.CashierWinUI.Services;
namespace Falah.Client.CashierWinUI.ViewModels;

public partial class AttendanceViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "تصحيح الحضور";
    public bool IsRecoveryBusy => IsBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _correction };
    public void RefreshRecoveryState() { OnIsBusyChanged(IsBusy); }
}

public partial class BackupViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "استعادة النسخة الاحتياطية";
    public bool IsRecoveryBusy => IsBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _restore };
    public void RefreshRecoveryState() { OnIsBusyChanged(IsBusy); }
}

public partial class CashCountViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "جرد نقدية يوم العمل";
    public bool IsRecoveryBusy => IsBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _write };
    public void RefreshRecoveryState() { OnIsBusyChanged(IsBusy); }
}

public partial class DeliveryDirectoryViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "بيانات التوصيل";
    public bool IsRecoveryBusy => IsBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _write };
    public void RefreshRecoveryState() { OnIsBusyChanged(IsBusy); }
}

public partial class DeliveryViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "التوصيل وعهدة السائق";
    public bool IsRecoveryBusy => IsBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _assignRequest, _outcomeRequest, _settleRequest, _reassignRequest };
    public void RefreshRecoveryState() { OnIsBusyChanged(IsBusy); }
}

public partial class EmployeeEditorViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "بيانات الموظف";
    public bool IsRecoveryBusy => IsBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _write };
    public void RefreshRecoveryState() { OnIsBusyChanged(IsBusy); }
}

public partial class EmployeesViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "حساب الموظف وصرف المستحقات";
    public bool IsRecoveryBusy => IsBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _cashOutRequest, _dailyWageRequest, _overtimeRequest, _extraShiftRequest, _payRequest };
    public void RefreshRecoveryState() { OnIsBusyChanged(IsBusy); }
}

public partial class InventoryViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "حركة المخزون";
    public bool IsRecoveryBusy => IsBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _countRequest, _wasteRequest };
    public void RefreshRecoveryState() { OnIsBusyChanged(IsBusy); }
}

public partial class MenuEditorViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "تعديل المنيو والأسعار";
    public bool IsRecoveryBusy => IsBusy || IsImageBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _write };
    public void RefreshRecoveryState() { OnIsBusyChanged(IsBusy); }
}

public partial class PayrollViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "كشف الرواتب";
    public bool IsRecoveryBusy => IsBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _createRequest, _transitionRequest, _payRequest };
    public void RefreshRecoveryState() { OnIsBusyChanged(IsBusy); }
}

public partial class PurchasingViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "الموردون والمشتريات";
    public bool IsRecoveryBusy => IsBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _supplierRequest, _receiveRequest, _payRequest, _creditRequest };
    public void RefreshRecoveryState() { OnIsBusyChanged(IsBusy); }
}

public partial class UsersViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "إدارة المستخدمين";
    public bool IsRecoveryBusy => IsBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _pendingCreate, _pendingPin, _pendingActive, _pendingPermissions };
    public void RefreshRecoveryState() { OnIsBusyChanged(IsBusy); }
}

public partial class ControlCenterViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "يوم العمل والإيداع الآمن";
    public bool IsRecoveryBusy => IsBusy;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _safeDropRequest, ManualDayRecovery };
    public void RefreshRecoveryState() { _safeDropRequestId = Guid.NewGuid().ToString("N"); OnIsBusyChanged(IsBusy); }
    private IPendingRequest? _manualDayRecovery;
    private IPendingRequest ManualDayRecovery => _manualDayRecovery ??= new DelegatePendingRequest(
        () => _pendingOperation is not null, () => IsBusy,
        () => { _pendingOperation = null; _outcomeUnknown = false; DayAction = null; ExceptionTarget = null; });
}

public partial class TreasuryViewModel : IManagementRequestOwner
{
    public string RecoveryLabel => "حركة الخزينة";
    public bool IsRecoveryBusy => IsBusy;
    // UI-0001: the pending movement is the durable one (survives a restart, keeps its request id).
    // A correction kept from a lost answer is listed too, or it would block every later correction unseen.
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _write, _correction };
    public void RefreshRecoveryState()
    {
        // Discarded from the recovery list after the owner reviewed the server record.
        if (!_write.HasPending) { _pendingCommit = null; _outcomeUnknown = false; _pendingAmount = 0; ActionError = null; OpenAction = null; }
        RefreshOperationState();
    }
}
