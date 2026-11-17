using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>One line of the closing statement as it prints: a heading/text line, a label-value row,
/// or a rule. Built from the very paper the printer receives, so the screen cannot say something the
/// paper does not.</summary>
public sealed record PaperLine(string Kind, string Text, string Label, string Value, bool Bold, bool Centered)
{
    public bool IsText => Kind == "text";
    public bool IsRow => Kind == "row";
    public bool IsRule => Kind == "rule";

    internal static PaperLine? From(TicketElementDto e)
    {
        var kind = e.Kind.ToLowerInvariant();
        if (kind == "rule") return new("rule", "", "", "", false, false);
        // A section title prints as a one-cell black band (paper redesign 2026-11-17); on screen it
        // stays the centred bold heading it always was.
        if (kind == "row" && e.Inverted && e.Cells is [var band])
            return new("text", band.Text, "", "", true, true);
        if (kind == "row" && e.Cells is { Count: > 0 } cells)
        {
            var label = cells[0].Text;
            var value = cells.Count > 1 ? cells[^1].Text : "";
            return new("row", "", label, value, cells.Any(c => c.Bold) || e.Bold, false);
        }
        if (kind == "text" && !string.IsNullOrWhiteSpace(e.Text))
            return new("text", e.Text!, "", "", e.Bold, e.Align is "center");
        return null;   // cut, qr, spacing: nothing to show on screen
    }
}

/// <summary>
/// UI-REQ-002: the closing and handover statement on screen, after a close (POS or Shifts screen) and
/// from a closed shift's history row. Shows exactly what prints, says whether the paper was printed,
/// is still waiting, or failed, and offers a reprint that is marked as a reprint and moves no money.
/// </summary>
public partial class ShiftStatementViewModel : ObservableObject
{
    private readonly AppSession _session;
    private string? _shiftId;

    public ShiftStatementViewModel(AppSession session) => _session = session;

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _printStatus;
    [ObservableProperty] private bool _printFailed;
    [ObservableProperty] private string? _reprintMessage;
    [ObservableProperty] private bool _isReprinting;
    [ObservableProperty] private string _title = "ورقة إغلاق الوردية";

    public ObservableCollection<PaperLine> Lines { get; } = new();

    public async Task ShowAsync(string shiftId)
    {
        _shiftId = shiftId;
        ReprintMessage = null;
        IsOpen = true;
        await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_shiftId is null) return;
        IsLoading = true;
        Error = null;
        try
        {
            var result = await _session.Api.GetShiftCloseStatementAsync(_shiftId);
            if (!result.Success || result.Value is null)
            {
                Error = result.Error ?? "تعذر تحميل ورقة الإغلاق.";
                return;
            }
            Lines.Clear();
            foreach (var line in result.Value.Paper.Lines.Select(PaperLine.From).OfType<PaperLine>()) Lines.Add(line);
            Title = $"ورقة إغلاق {result.Value.Statement.ShiftName} ({result.Value.Statement.ShiftReference})";
            (PrintStatus, PrintFailed) = DescribePrint(result.Value.PrintJobs);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Shown right after a successful close: whatever goes wrong fetching the paper must never
            // escape into the close itself, which has already committed and must not look failed.
            Error = "تعذر تحميل ورقة الإغلاق الآن. الإغلاق تم؛ افتحها لاحقًا من سجل الورديات.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>The original paper's state, then how many reprints were asked for. "Succeeded" means
    /// the agent reported the job sent to the printer; whether paper came out is not knowable here.</summary>
    internal static (string Text, bool Failed) DescribePrint(IReadOnlyList<ShiftClosePrintStatusDto> jobs)
    {
        var original = jobs.FirstOrDefault(j => !j.IsReprint);
        var reprints = jobs.Count(j => j.IsReprint);
        var suffix = reprints > 0 ? $" — أعيدت طباعتها {reprints} مرة" : "";
        return original?.Status switch
        {
            null => ("لم تُسجَّل ورقة إغلاق لهذه الوردية." + suffix, false),
            "Pending" => ("ورقة الإغلاق في انتظار الطابعة." + suffix, false),
            "Sending" => ("ورقة الإغلاق قيد الإرسال للطابعة." + suffix, false),
            "Succeeded" => ("أُرسلت ورقة الإغلاق للطابعة." + suffix, false),
            "Failed" => ($"فشلت طباعة ورقة الإغلاق{(string.IsNullOrWhiteSpace(original.LastError) ? "" : ": " + original.LastError)}. الإغلاق نفسه تم ولن يتكرر؛ يمكنك إعادة الطباعة." + suffix, true),
            var other => ($"حالة الطباعة: {other}" + suffix, false),
        };
    }

    /// <summary>A reprint is a new, separate paper marked "إعادة طباعة" on the paper itself and in the
    /// audit trail. It never re-closes the shift or moves money (the server only queues paper).</summary>
    [RelayCommand]
    private async Task ReprintAsync()
    {
        if (_shiftId is null || IsReprinting) return;
        IsReprinting = true;
        ReprintMessage = null;
        try
        {
            var result = await _session.Api.ReprintDocumentAsync(new ReprintDocumentRequest(
                $"rp-{Guid.NewGuid():N}", "ShiftClose", _shiftId, _session.CurrentUser!.Id, "إعادة طباعة ورقة إغلاق الوردية"));
            ReprintMessage = result.Success
                ? "أُرسلت نسخة مُعلَّمة «إعادة طباعة» للطابعة."
                : result.Error ?? "تعذر إرسال إعادة الطباعة.";
            if (result.Success) await LoadAsync();
        }
        finally
        {
            IsReprinting = false;
        }
    }

    [RelayCommand]
    private void Close() => IsOpen = false;
}
