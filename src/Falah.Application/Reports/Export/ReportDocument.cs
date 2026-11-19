namespace Falah.Application.Reports.Export;

/// <summary>
/// The immutable snapshot every export format is rendered from. It is built ONCE, on the server, from the same
/// report query services the screens read (no calculation of its own); Excel, PDF, Word and the PNG summary are
/// all drawn from the same instance, so for one scope they cannot disagree. <see cref="Fingerprint"/> is a SHA-256
/// of the snapshot's content (everything but the fingerprint itself) and is written into every file, so any two
/// exports can be checked to come from the same figures.
///
/// Money is integer piastres; <c>null</c> means unavailable and is printed as "غير متاح", never as 0.
/// </summary>
public sealed record ReportDocument(
    string ReportKey,
    string Title,
    string RestaurantName,
    DateOnly From,
    DateOnly To,
    string ScopeLabel,
    string Basis,
    DateTimeOffset GeneratedAtUtc,
    string GeneratedBy,
    IReadOnlyList<ReportKpiGroup> Summary,
    IReadOnlyList<ReportTable> Tables,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> Warnings,
    string Fingerprint = "");

public enum ReportCellKind { Text, Money, Count, Date, DateTime }

/// <summary>One value. Money and Count use <see cref="Number"/> (Money in piastres); null Number on a Money or Count
/// cell is "unavailable". DateTime is a UTC instant shown in Cairo time.</summary>
public sealed record ReportCell(ReportCellKind Kind, string? Text = null, long? Number = null, DateOnly? Date = null, DateTimeOffset? Instant = null)
{
    public static ReportCell Of(string? text) => new(ReportCellKind.Text, Text: text ?? "");
    public static ReportCell Money(long? piastres) => new(ReportCellKind.Money, Number: piastres);
    public static ReportCell Count(long? count) => new(ReportCellKind.Count, Number: count);
    public static ReportCell Day(DateOnly? date) => new(ReportCellKind.Date, Date: date);
    public static ReportCell At(DateTimeOffset? instant) => new(ReportCellKind.DateTime, Instant: instant);
}

public sealed record ReportColumn(string Header, ReportCellKind Kind, int Weight = 1);

/// <summary>A detailed table. <see cref="TotalRow"/> is a row of the same width whose figures come from the source
/// report (or are the plain sum of the column the source itself sums); <see cref="Truncated"/> says rows were cut.</summary>
public sealed record ReportTable(
    string Key, string Title, string? Basis, IReadOnlyList<ReportColumn> Columns, IReadOnlyList<IReadOnlyList<ReportCell>> Rows,
    IReadOnlyList<ReportCell>? TotalRow = null, string? Note = null, bool Truncated = false);

/// <summary>Status: actual / estimate / partial / unavailable - the same vocabulary as the owner report.</summary>
public sealed record ReportKpi(string Label, ReportCell Value, string Status = "actual", string? Note = null);

public sealed record ReportKpiGroup(string Title, IReadOnlyList<ReportKpi> Kpis);

/// <summary>A report the server can export, and the permission its screen requires. An export never grants more
/// than the screen it comes from.</summary>
public sealed record ReportExportCatalogEntry(string Key, string Title, string Description, IReadOnlyList<string> Formats);

public static class ReportExportCatalog
{
    public static readonly IReadOnlyList<string> AllFormats = new[] { "xlsx", "pdf", "docx", "png" };

    /// <summary>Key, title, description, and the permissions any one of which grants it ("all" always does).</summary>
    public static readonly IReadOnlyList<(string Key, string Title, string Description, string[] AnyOf)> Reports = new[]
    {
        ("owner", "التقرير المالي للمالك", "المبيعات والتحصيل الفعلي والأرصدة والسائقين والمستحقات والالتزامات والفروق والربحية التقديرية، مع المطابقة. يومي أو أسبوعي أو شهري حسب الفترة.", new[] { "reports_view" }),
        ("sales", "تقرير المبيعات", "المبيعات وطرق الدفع ونوع الطلب، حسب الوردية وحسب يوم العمل.", new[] { "reports_view" }),
        ("cash", "مطابقة النقدية وحركة الخزينة", "رصيد أول المدة والوارد والمنصرف وآخر المدة لكل حساب، وكل حركة خزينة في الفترة.", new[] { "reports_view" }),
        ("expenses", "المصروفات", "كل مصروف وقيد تصحيح، والإجمالي حسب البند.", new[] { "reports_view" }),
        ("refunds", "المرتجعات والإلغاءات", "كل مرتجع وتاريخ بيعه الأصلي، وكل طلب ملغي ومصير طعامه.", new[] { "reports_view" }),
        ("delivery", "أداء التوصيل وتسويات السائقين", "نتائج التوصيل وفروق التحصيل وتسويات السائقين.", new[] { "reports_view" }),
        ("products", "أداء الأصناف", "الكميات والقيم المباعة لكل صنف وفئة.", new[] { "reports_view" }),
        ("exceptions", "الاستثناءات المالية", "فروق تحصيل غير محسومة، تسليمات نقدية غير مؤكدة، تحويلات لم تُؤكد، وفروق الورديات.", new[] { "reports_view" }),
        ("suppliers", "أرصدة الموردين والمشتريات", "المستحق لكل مورد وأعمار الفواتير، والمشتريات والسداد في الفترة.", Array.Empty<string>()),
    };

    public const int MaxDays = 366;
}
