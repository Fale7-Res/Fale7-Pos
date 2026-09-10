using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Employees;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Roster authoring over the existing employee contracts. Creating a person and correcting their
/// record were the one part of the employee module with no client surface at all, so a new hire
/// could be paid, advanced against and rostered - but only if somebody had inserted the row by
/// other means.
///
/// The compensation fields are deliberately all present at once rather than switched by type: the
/// server stores daily wage, shift value and monthly salary independently of CompensationType, and
/// payroll reads whichever the employee's own arrangement calls for. Hiding the others would make
/// this screen quietly drop values the record already holds.
///
/// A save captures a complete immutable request; a retry after an ambiguous outcome re-sends the
/// exact bytes that were already in flight, never a re-read of the form.
/// </summary>
public partial class EmployeeEditorViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly RetainedRequest<object> _write = new("employees.record");
    private EmployeeDto? _editing;

    public EmployeeEditorViewModel(AppSession session) => _session = session;

    public event Action? Saved;

    public sealed record Choice(string Id, string Label);

    /// <summary>The domain's own enums - CompensationType, EmploymentArrangement and
    /// EmployeeLifecycleStatus - not an invented vocabulary. The server parses these names.</summary>
    public IReadOnlyList<Choice> CompensationTypes { get; } = new[]
    {
        new Choice("Daily", "يومي"),
        new Choice("Monthly", "شهري"),
        new Choice("Hourly", "بالساعة"),
    };

    public IReadOnlyList<Choice> Arrangements { get; } = new[]
    {
        new Choice("Daily", "دوام يومي"),
        new Choice("Monthly", "دوام شهري"),
        new Choice("PartTime", "دوام جزئي"),
        new Choice("Contract", "بعقد"),
    };

    public IReadOnlyList<Choice> LifecycleStatuses { get; } = new[]
    {
        new Choice("Active", "على رأس العمل"),
        new Choice("Inactive", "موقوف مؤقتًا"),
        new Choice("Terminated", "منتهية خدمته"),
        new Choice("Archived", "مؤرشف"),
    };

    public ObservableCollection<EmployeeDto> Roster { get; } = new();

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _hasForm;
    [ObservableProperty] private string? _actionError;
    [ObservableProperty] private string? _actionNote;

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _employeeNumber = "";
    [ObservableProperty] private string _jobTitle = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private DateTimeOffset _hireDate = DateTimeOffset.Now;
    [ObservableProperty] private Choice? _compensationType;
    [ObservableProperty] private Choice? _arrangement;
    [ObservableProperty] private Choice? _lifecycleStatus;
    [ObservableProperty] private string _dailyWageText = "";
    [ObservableProperty] private string _shiftValueText = "";
    [ObservableProperty] private string _monthlySalaryText = "";
    [ObservableProperty] private string _overtimeRateText = "";
    [ObservableProperty] private string _department = "";
    [ObservableProperty] private string _role = "";
    [ObservableProperty] private string _nationalId = "";
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private string _fingerprintId = "";
    [ObservableProperty] private string _notes = "";
    [ObservableProperty] private bool _canDeliver;
    [ObservableProperty] private bool _isActive = true;

    public bool CanManage => _session.HasPermission("employees_manage");
    public bool CanEdit => CanManage && !IsBusy;
    public bool IsExisting => _editing is not null;
    public string FormTitle => _editing is null ? "موظف جديد" : "تعديل بيانات الموظف";
    public string SaveLabel => IsBusy ? "جاري الحفظ والتحقق..."
        : _write.HasPending ? "التحقق من الحفظ السابق" : "حفظ البيانات";

    partial void OnIsBusyChanged(bool value) => NotifyState();

    private void NotifyState()
    {
        foreach (var name in new[] { nameof(CanEdit), nameof(SaveLabel), nameof(FormTitle), nameof(IsExisting) })
            OnPropertyChanged(name);
        _session.SetManagementOperationPending(this, IsBusy || _write.HasPending);
    }

    /// <summary>Opens the directory. The roster is passed in by the screen that already loaded it,
    /// so opening the editor never issues a second read of data the caller is holding.</summary>
    public void Open(IEnumerable<EmployeeDto> roster)
    {
        if (!CanManage || _session.HasUnresolvedOperation) return;
        Roster.Clear();
        foreach (var e in roster) Roster.Add(e);
        ActionError = ActionNote = null;
        HasForm = false;
        _editing = null;
        IsOpen = true;
        NotifyState();
    }

    [RelayCommand]
    private void Close()
    {
        if (IsBusy || _write.HasPending) return;
        IsOpen = false;
        HasForm = false;
    }

    [RelayCommand]
    private void New()
    {
        if (!CanEdit || _write.HasPending) return;
        ResetForm();
        _editing = null;
        HasForm = true;
        NotifyState();
    }

    [RelayCommand]
    private void Select(EmployeeDto employee)
    {
        if (!CanEdit || _write.HasPending || employee is null) return;
        ResetForm();
        _editing = employee;
        Name = employee.Name;
        EmployeeNumber = employee.EmployeeNumber;
        JobTitle = employee.JobTitle;
        Phone = employee.Phone;
        HireDate = new DateTimeOffset(employee.HireDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        CompensationType = CompensationTypes.FirstOrDefault(c => string.Equals(c.Id, employee.CompensationType, StringComparison.OrdinalIgnoreCase));
        Arrangement = Arrangements.FirstOrDefault(a => string.Equals(a.Id, employee.EmploymentArrangement, StringComparison.OrdinalIgnoreCase));
        LifecycleStatus = LifecycleStatuses.FirstOrDefault(s => string.Equals(s.Id, employee.LifecycleStatus, StringComparison.OrdinalIgnoreCase));
        DailyWageText = CashInput.Format(employee.DailyWagePiastres);
        ShiftValueText = CashInput.Format(employee.ShiftValuePiastres);
        MonthlySalaryText = employee.MonthlySalaryPiastres is { } salary ? CashInput.Format(salary) : "";
        OvertimeRateText = employee.OvertimeRatePiastres is { } overtime ? CashInput.Format(overtime) : "";
        Department = employee.Department ?? "";
        Role = employee.Role;
        NationalId = employee.NationalId ?? "";
        Address = employee.Address ?? "";
        FingerprintId = employee.FingerprintId ?? "";
        Notes = employee.Notes ?? "";
        CanDeliver = employee.CanDeliver;
        IsActive = employee.IsActive;
        HasForm = true;
        NotifyState();
    }

    private void ResetForm()
    {
        Name = EmployeeNumber = JobTitle = Phone = "";
        Department = Role = NationalId = Address = FingerprintId = Notes = "";
        DailyWageText = ShiftValueText = "0";
        MonthlySalaryText = OvertimeRateText = "";
        HireDate = DateTimeOffset.Now;
        CompensationType = CompensationTypes[0];
        Arrangement = null;
        LifecycleStatus = LifecycleStatuses[0];
        CanDeliver = false;
        IsActive = true;
        ActionError = ActionNote = null;
    }

    /// <summary>Optional money fields are genuinely absent when blank - a monthly salary that was
    /// never set must stay null rather than becoming a zero the payroll would treat as a real
    /// figure.</summary>
    private bool TryOptionalMoney(string text, string label, out long? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!CashInput.TryParse(text, out var parsed) || parsed < 0)
        {
            ActionError = $"أدخل {label} بشكل صحيح، أو اتركه فارغًا.";
            return false;
        }
        value = parsed;
        return true;
    }

    private object? CreateRequest()
    {
        if (!CanEdit || _session.CurrentUser is null) return null;
        if (string.IsNullOrWhiteSpace(Name)) { ActionError = "اسم الموظف مطلوب."; return null; }
        if (string.IsNullOrWhiteSpace(EmployeeNumber)) { ActionError = "الرقم الوظيفي مطلوب."; return null; }
        if (string.IsNullOrWhiteSpace(JobTitle)) { ActionError = "المسمى الوظيفي مطلوب."; return null; }
        if (string.IsNullOrWhiteSpace(Phone)) { ActionError = "رقم هاتف الموظف مطلوب."; return null; }
        if (CompensationType is null) { ActionError = "اختر نوع الأجر."; return null; }
        if (!CashInput.TryParse(DailyWageText, out var daily) || daily < 0) { ActionError = "أدخل الأجر اليومي بشكل صحيح."; return null; }
        if (!CashInput.TryParse(ShiftValueText, out var shift) || shift < 0) { ActionError = "أدخل قيمة الوردية بشكل صحيح."; return null; }
        if (!TryOptionalMoney(MonthlySalaryText, "الراتب الشهري", out var monthly)) return null;
        if (!TryOptionalMoney(OvertimeRateText, "قيمة ساعة الإضافي", out var overtime)) return null;

        var actor = _session.CurrentUser.Id;
        var hireDate = DateOnly.FromDateTime(HireDate.Date);
        var role = string.IsNullOrWhiteSpace(Role) ? JobTitle.Trim() : Role.Trim();
        string? Optional(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        return _editing is { } existing
            ? new UpdateEmployeeRequest(existing.Id, Name.Trim(), EmployeeNumber.Trim(), JobTitle.Trim(), Phone.Trim(),
                hireDate, CompensationType.Id, daily, shift, monthly, CanDeliver, role, IsActive, Optional(Notes),
                Optional(Address), Optional(NationalId), Optional(Department), Arrangement?.Id, Optional(FingerprintId),
                LifecycleStatus?.Id, overtime, actor)
            : new CreateEmployeeRequest($"emp-{Guid.NewGuid():N}", Name.Trim(), EmployeeNumber.Trim(), JobTitle.Trim(),
                Phone.Trim(), hireDate, CompensationType.Id, daily, shift, monthly, CanDeliver, role, Optional(Notes),
                Optional(Address), Optional(NationalId), Optional(Department), Arrangement?.Id, Optional(FingerprintId),
                LifecycleStatus?.Id, overtime, actor);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;
        ActionError = null;
        var request = _write.Value ?? CreateRequest();
        if (request is null) return;

        IsBusy = true;
        ActionNote = null;
        try
        {
            var result = await _write.SendAsync(request, SendWriteAsync);
            if (!result.Success) { ActionError = _write.FailureMessage(result.Error); return; }
            var saved = result.Value;
            if (saved is not null)
            {
                var index = Roster.ToList().FindIndex(e => e.Id == saved.Id);
                if (index >= 0) Roster[index] = saved; else Roster.Add(saved);
            }
            HasForm = false;
            _editing = null;
            ActionNote = "تم حفظ بيانات الموظف.";
            Saved?.Invoke();
        }
        finally { IsBusy = false; }
    }

    private async Task<ApiClient.ApiResult<EmployeeDto>> SendWriteAsync(object request) => request switch
    {
        CreateEmployeeRequest r => await _session.Api.CreateEmployeeAsync(r),
        UpdateEmployeeRequest r => await _session.Api.UpdateEmployeeAsync(r),
        _ => throw new InvalidOperationException("Unsupported employee command."),
    };

    public string HireDateDisplay => HireDate.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
}
