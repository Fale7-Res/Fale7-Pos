using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Employees;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Attendance for one date, read from the real records.
///
/// This screen deliberately does NOT offer check-in/check-out buttons. Those endpoints exist to be
/// driven by the biometric device (BiometricCheckInRequest carries a BiometricEventId), and a
/// check-in posts a daily wage automatically. Letting a manager press "check in" from a desktop
/// screen would manufacture attendance the device never saw and post real wage money for it. What a
/// manager legitimately does here is CORRECT a record - which is a separate, permissioned, audited
/// command that reconciles the financial delta rather than rewriting history.
/// </summary>
public partial class AttendanceViewModel : ObservableObject
{
    private readonly AppSession _session;

    public AttendanceViewModel(AppSession session)
    {
        _session = session;
        var cairo = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        _date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, cairo).DateTime);
    }

    public ObservableCollection<AttendanceRecordDto> Records { get; } = new();

    [ObservableProperty] private DateOnly _date;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;

    public string DateLabel => Date.ToString("yyyy-MM-dd");
    partial void OnDateChanged(DateOnly value) => OnPropertyChanged(nameof(DateLabel));

    public bool HasNoRecords => !IsLoading && LoadError is null && Records.Count == 0;
    public bool CanCorrect => _session.HasPermission("attendance_manage");
    public bool CanBrowse => !IsBusy && !IsCorrectionOpen;
    public bool HasRecords => !IsLoading && LoadError is null && Records.Count > 0;
    private bool _reloadRequested;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!CanBrowse) return;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (IsLoading) { _reloadRequested = true; return; }
        IsLoading = true;
        NotifyReadState();
        try
        {
            do
            {
                _reloadRequested = false;
                LoadError = null;
                Records.Clear();
                var date = Date;
                var records = await _session.Api.GetAttendanceAsync(date);
                if (date != Date) { _reloadRequested = true; continue; }
                if (!records.Success || records.Value is null)
                    LoadError = records.Error ?? "تعذر تحميل سجل الحضور.";
                else
                    foreach (var r in records.Value) Records.Add(r);
            }
            while (_reloadRequested);
        }
        finally
        {
            IsLoading = false;
            NotifyReadState();
        }
    }

    [RelayCommand]
    private void PreviousDay() { if (CanBrowse) { Date = Date.AddDays(-1); _ = LoadAsync(); } }

    [RelayCommand]
    private void NextDay() { if (CanBrowse) { Date = Date.AddDays(1); _ = LoadAsync(); } }

    private void NotifyReadState()
    {
        OnPropertyChanged(nameof(HasNoRecords));
        OnPropertyChanged(nameof(HasRecords));
    }

    public sealed record StatusChoice(string Key, string Label);
    public IReadOnlyList<StatusChoice> Statuses { get; } = new[]
    {
        new StatusChoice("Present", "حاضر"), new StatusChoice("Late", "متأخر"),
        new StatusChoice("Absent", "غائب"), new StatusChoice("Leave", "إجازة"),
        new StatusChoice("MissingFingerprint", "بصمة ناقصة")
    };
    [ObservableProperty] private AttendanceRecordDto? _correctionRecord;
    [ObservableProperty] private StatusChoice? _correctionStatus;
    [ObservableProperty] private string _lateMinutesText = "";
    [ObservableProperty] private string _workHoursText = "";
    [ObservableProperty] private string _overtimeHoursText = "";
    [ObservableProperty] private string _tatbiqCountText = "";
    [ObservableProperty] private string _correctionReason = "";
    [ObservableProperty] private string? _correctionError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private bool _isBusy;
    private readonly RetainedRequest<CorrectAttendanceRequest> _correction = new("attendance.correction");
    public bool IsCorrectionOpen => CorrectionRecord is not null;
    public bool CanEditCorrection => !IsBusy && !_correction.HasPending;
    public string SaveCorrectionLabel => IsBusy ? "جاري التحقق..." : _correction.HasPending ? "التحقق من التصحيح السابق" : "حفظ التصحيح";
    partial void OnCorrectionRecordChanged(AttendanceRecordDto? value)
    {
        OnPropertyChanged(nameof(IsCorrectionOpen));
        OnPropertyChanged(nameof(CanBrowse));
    }
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditCorrection));
        OnPropertyChanged(nameof(SaveCorrectionLabel));
        OnPropertyChanged(nameof(CanBrowse));
        _session.SetManagementOperationPending(this, IsBusy || _correction.HasPending);
    }

    [RelayCommand]
    private void OpenCorrection(AttendanceRecordDto record)
    {
        if (!CanCorrect || !CanBrowse || IsLoading || LoadError is not null) return;
        CorrectionRecord = record;
        CorrectionStatus = Statuses.FirstOrDefault(s => s.Key.Equals(record.Status, StringComparison.OrdinalIgnoreCase));
        LateMinutesText = record.LateMinutes.ToString(CultureInfo.InvariantCulture);
        WorkHoursText = record.WorkHours.ToString(CultureInfo.InvariantCulture);
        OvertimeHoursText = record.OvertimeHours.ToString(CultureInfo.InvariantCulture);
        TatbiqCountText = record.TatbiqCount.ToString(CultureInfo.InvariantCulture);
        CorrectionReason = "";
        CorrectionError = ActionNote = null;
    }

    [RelayCommand]
    private void CloseCorrection() { if (CanEditCorrection) CorrectionRecord = null; }

    // Accept the same Arabic digit families as monetary inputs, without limiting hours to cents.
    private static string NormalizeNumber(string text) => string.Concat(text.Trim().Select(c =>
        c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') :
        c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') : c == '٫' ? '.' : c));
    private static bool TryHours(string text, out double value) =>
        double.TryParse(NormalizeNumber(text), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value)
        && double.IsFinite(value) && value >= 0;
    private static bool TryCount(string text, out int value) =>
        int.TryParse(NormalizeNumber(text), NumberStyles.None, CultureInfo.InvariantCulture, out value) && value >= 0;

    [RelayCommand]
    private async Task SaveCorrectionAsync()
    {
        if (IsBusy) return;
        var request = _correction.Value;
        if (request is null)
        {
            if (!CanCorrect || CorrectionRecord is null || _session.CurrentUser is null) return;
            if (CorrectionStatus is null || !Statuses.Contains(CorrectionStatus)
                || !TryCount(LateMinutesText, out var late) || !TryCount(TatbiqCountText, out var count)
                || !TryHours(WorkHoursText, out var work) || !TryHours(OvertimeHoursText, out var overtime))
            { CorrectionError = "اختر الحالة وأدخل مددًا وأعدادًا صحيحة غير سالبة."; return; }
            if (string.IsNullOrWhiteSpace(CorrectionReason))
            { CorrectionError = "سبب التصحيح إلزامي."; return; }
            request = new($"attendance-{Guid.NewGuid():N}", CorrectionRecord.Id, CorrectionStatus.Key,
                late, work, overtime, count, CorrectionReason.Trim(), _session.CurrentUser.Id, null, null);
        }
        IsBusy = true;
        CorrectionError = null;
        try
        {
            var result = await _correction.SendAsync(request, r => _session.Api.CorrectAttendanceAsync(r));
            if (!result.Success || result.Value is null)
            { CorrectionError = _correction.FailureMessage(result.Error); return; }
            CorrectionRecord = null;
            CorrectionReason = "";
            ActionNote = "تم تأكيد التصحيح وتسجيل أثره المالي. تُعرض أحدث السجلات بعد التحديث.";
            // The retry may confirm an older snapshot; reload the current record rather than
            // overwriting a newer correction made by another authorized operator.
            await ReloadAsync();
        }
        finally { IsBusy = false; }
    }
}
