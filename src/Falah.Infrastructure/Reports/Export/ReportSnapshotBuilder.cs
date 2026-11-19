namespace Falah.Infrastructure.Reports.Export;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Falah.Application.Reports;
using Falah.Application.Reports.Export;
using Falah.Domain.Common;
using Falah.Domain.Inventory;
using Falah.Infrastructure.Delivery;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Purchasing;
using Falah.Infrastructure.Shifts;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Builds the export snapshot (<see cref="ReportDocument"/>) for one report and one scope, from the SAME services the
/// screens read: the owner financial report, the reporting queries, the payables aging, the shortfall and handover
/// lists. It computes nothing financial itself - every figure is copied from a report DTO or from a stored source row
/// (a movement, a settlement, an invoice), and a table's total row is either the source report's own total or the
/// plain sum of the rows the source itself lists. Tables are capped at <see cref="MaxRows"/> rows, with a warning.
/// </summary>
public sealed class ReportSnapshotBuilder
{
    public const int MaxRows = 5_000;

    private readonly FalahDbContext _db;
    private readonly ReportingQueryService _reports;
    private readonly OwnerFinancialReportQueryService _owner;
    private readonly PurchasingQueryService _purchasing;
    private readonly ClassifyCollectionShortfallHandler _shortfalls;
    private readonly AcknowledgeShiftHandoverHandler _handovers;

    public ReportSnapshotBuilder(FalahDbContext db, ReportingQueryService reports, OwnerFinancialReportQueryService owner,
        PurchasingQueryService purchasing, ClassifyCollectionShortfallHandler shortfalls, AcknowledgeShiftHandoverHandler handovers)
    {
        _db = db;
        _reports = reports;
        _owner = owner;
        _purchasing = purchasing;
        _shortfalls = shortfalls;
        _handovers = handovers;
    }

    public async Task<ReportDocument> BuildAsync(string reportKey, DateOnly from, DateOnly to, string generatedBy, CancellationToken ct = default)
    {
        var range = new BusinessDateRange(from, to);
        var restaurant = await _db.Settings.AsNoTracking().Select(s => s.RestaurantName).FirstOrDefaultAsync(ct);
        var meta = new Meta(string.IsNullOrWhiteSpace(restaurant) ? "مطعم فالح" : restaurant, from, to, generatedBy, DateTimeOffset.UtcNow);
        var document = reportKey switch
        {
            "owner" => await OwnerAsync(meta, range, ct),
            "sales" => await SalesAsync(meta, range, ct),
            "cash" => await CashAsync(meta, range, ct),
            "expenses" => await ExpensesAsync(meta, range, ct),
            "refunds" => await RefundsAsync(meta, range, ct),
            "delivery" => await DeliveryAsync(meta, range, ct),
            "products" => await ProductsAsync(meta, range, ct),
            "exceptions" => await ExceptionsAsync(meta, range, ct),
            "suppliers" => await SuppliersAsync(meta, range, ct),
            _ => throw new ArgumentException("نوع التقرير غير معروف.", nameof(reportKey)),
        };
        return WithFingerprint(document);
    }

    /// <summary>The fingerprint identifies the FIGURES: everything but when and by whom the export was made, so the same
    /// data exported twice, or in two formats, carries the same fingerprint, and a changed figure changes it.</summary>
    public static ReportDocument WithFingerprint(ReportDocument document)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(document with { Fingerprint = "", GeneratedAtUtc = default, GeneratedBy = "" });
        return document with { Fingerprint = Convert.ToHexString(SHA256.HashData(json)) };
    }

    private sealed record Meta(string Restaurant, DateOnly From, DateOnly To, string By, DateTimeOffset At);

    private static ReportDocument Doc(Meta m, string key, string title, string basis, IReadOnlyList<ReportKpiGroup> summary,
        IReadOnlyList<ReportTable> tables, IReadOnlyList<string>? notes = null, IReadOnlyList<string>? warnings = null)
    {
        var allWarnings = (warnings ?? Array.Empty<string>()).ToList();
        foreach (var t in tables.Where(t => t.Truncated))
            allWarnings.Add($"جدول «{t.Title}» اقتُطع عند {MaxRows:#,##0} صف؛ صغّر الفترة لعرضه كاملًا.");
        if (summary.SelectMany(g => g.Kpis).Any(k => k.Status is "estimate"))
            allWarnings.Add("يحتوي التقرير على أرقام تقديرية (موسومة «تقديري») - ليست أرقامًا فعلية.");
        if (summary.SelectMany(g => g.Kpis).Any(k => k.Status is "unavailable" or "partial"))
            allWarnings.Add("بعض الأرقام غير متاحة أو جزئية (موسومة) لأن بياناتها غير مكتملة - لم تُعرض كصفر.");
        return new ReportDocument(key, title, m.Restaurant, m.From, m.To,
            $"{ReportText.Period(m.From, m.To)} · أيام تقويمية بتوقيت القاهرة", basis, m.At, m.By, summary, tables,
            notes ?? Array.Empty<string>(), allWarnings.Distinct().ToList());
    }

    private static ReportTable Table(string key, string title, string? basis, ReportColumn[] columns,
        IEnumerable<ReportCell[]> rows, ReportCell[]? total = null, string? note = null)
    {
        var list = rows.Take(MaxRows + 1).ToList();
        var truncated = list.Count > MaxRows;
        if (truncated) list.RemoveAt(list.Count - 1);
        return new ReportTable(key, title, basis, columns, list, total, note, truncated);
    }

    private static ReportCell T(string? s) => ReportCell.Of(s);
    private static ReportCell M(long? p) => ReportCell.Money(p);
    private static ReportCell N(long? n) => ReportCell.Count(n);

    // ---------------------------------------------------------------- owner

    private async Task<ReportDocument> OwnerAsync(Meta m, BusinessDateRange range, CancellationToken ct)
    {
        var r = await _owner.GetAsync(range, ct);
        var summary = r.Sections.Select(s => new ReportKpiGroup(s.Title, s.Kpis.Select(k => new ReportKpi(
            k.Label, k.AmountPiastres is not null || k.Count is null ? M(k.AmountPiastres) : N(k.Count), k.Status,
            k.AmountPiastres is not null && k.Count is > 0 ? $"العدد: {k.Count}" + (k.Note is null ? "" : $" - {k.Note}") : k.Note)).ToList())).ToList();

        var kpiDefs = Table("definitions", "تعريفات المؤشرات ومصادرها", null,
            new[] { new ReportColumn("المؤشر", ReportCellKind.Text, 2), new ReportColumn("التعريف", ReportCellKind.Text, 4),
                    new ReportColumn("الأساس", ReportCellKind.Text), new ReportColumn("المصدر", ReportCellKind.Text, 3) },
            r.Sections.SelectMany(s => s.Kpis).Select(k => new[] { T(k.Label), T(k.Definition), T(k.Basis), T(k.Sources) }));

        var b = r.SalesBridge;
        var bridge = Table("bridge", "أين ذهبت أموال مبيعات الفترة (جسر المبيعات)", "بوقت البيع",
            new[] { new ReportColumn("البند", ReportCellKind.Text, 3), new ReportColumn("المبلغ", ReportCellKind.Money) },
            new[]
            {
                new[] { T("إجمالي مبيعات الفترة (قبل المرتجعات)"), M(b.GrossSalesPiastres) },
                new[] { T("دخل الخزينة عند البيع أو التأكيد"), M(b.ReceivedThroughTreasuryPiastres) },
                new[] { T("سوّاه السائقون"), M(b.CodSettledPiastres) },
                new[] { T("مع السائقين لم يُسوَّ"), M(b.CodHeldByDriversPiastres) },
                new[] { T("لم يُحصَّل بعد (مع العميل)"), M(b.CodNotYetCollectedPiastres) },
                new[] { T("تحويلات لم تُؤكد"), M(b.PendingElectronicPiastres) },
                new[] { T("فروق تحصيل غير محسومة"), M(b.ShortfallUnresolvedPiastres) },
                new[] { T("فروق تحصيل مستحقة (عميل/سائق)"), M(b.ShortfallReceivablePiastres) },
                new[] { T("فروق تحصيل معتمدة كخصم أو إعدام"), M(b.ShortfallRealisedPiastres) },
                new[] { T("فرق غير مفسَّر (يجب أن يكون صفرًا)"), M(b.ResidualPiastres) },
                new[] { T("مرتجعات دُفعت في الفترة"), M(b.RefundsMadePiastres) },
                new[] { T("صافي المبيعات"), M(b.NetSalesPiastres) },
            });

        var f = r.MoneyFlow;
        var flow = Table("money_flow", "حركة المال حسب نوعها", "بوقت حركة المال",
            new[] { new ReportColumn("النوع", ReportCellKind.Text, 3), new ReportColumn("وارد", ReportCellKind.Money), new ReportColumn("منصرف", ReportCellKind.Money),
                    new ReportColumn("داخلي (لا يُحسب)", ReportCellKind.Money), new ReportColumn("عدد الحركات", ReportCellKind.Count), new ReportColumn("تحصيل من العملاء؟", ReportCellKind.Text) },
            f.Lines.Select(l => new[] { T(l.Label), M(l.InflowPiastres), M(l.OutflowPiastres), M(l.MovedInternallyPiastres), N(l.MovementCount), T(l.CountsAsCustomerReceipt ? "نعم" : "لا") }),
            new[] { T("الإجمالي"), M(f.InflowPiastres), M(f.OutflowPiastres), M(f.InternalTransfersPiastres), N(f.Lines.Sum(l => l.MovementCount)), T("") });

        var reconciliation = new List<string>
        {
            b.ResidualPiastres == 0 && f.ResidualPiastres == 0 && f.UnclassifiedMovementCount == 0
                ? "المطابقة سليمة: كل مبيعات الفترة وكل حركات الخزينة مفسَّرة دون فرق."
                : $"فرق غير مفسَّر: الجسر {ReportText.Money(b.ResidualPiastres)}، الحركات {ReportText.Money(f.ResidualPiastres)}، حركات غير مصنفة {f.UnclassifiedMovementCount}.",
            $"أول المدة {ReportText.Money(f.OpeningBalancePiastres)} + وارد {ReportText.Money(f.InflowPiastres)} − منصرف {ReportText.Money(f.OutflowPiastres)} = آخر المدة {ReportText.Money(f.ClosingBalancePiastres)}.",
        };
        var warnings = new List<string>();
        if (b.ResidualPiastres != 0 || f.ResidualPiastres != 0 || f.UnclassifiedMovementCount != 0) warnings.Add(reconciliation[0]);

        return Doc(m, "owner", m.From == m.To ? "التقرير المالي اليومي للمالك" : (m.To.DayNumber - m.From.DayNumber) < 7 ? "التقرير المالي الأسبوعي للمالك" : "التقرير المالي للمالك",
            "المبيعات بوقت البيع، والتحصيل بوقت دخول المال، والأرصدة الآن", summary, new[] { bridge, flow, kpiDefs },
            reconciliation.Concat(r.TimingNotes).ToList(), warnings);
    }

    // ---------------------------------------------------------------- sales

    private async Task<ReportDocument> SalesAsync(Meta m, BusinessDateRange range, CancellationToken ct)
    {
        var s = await _reports.GetSalesReportAsync(range, null, ct);
        var tax = await _reports.GetTaxReportAsync(range, null, ct);
        var summary = new[]
        {
            new ReportKpiGroup("المبيعات", new[]
            {
                new ReportKpi("عدد الطلبات", N(s.OrderCount)),
                new ReportKpi("إجمالي المبيعات (قبل المرتجعات)", M(s.GrossSalesPiastres)),
                new ReportKpi("المرتجعات المدفوعة في الفترة", M(s.RefundsPiastres)),
                new ReportKpi("صافي المبيعات", M(s.NetSalesPiastres)),
                new ReportKpi("ضريبة القيمة المضافة (صافي)", M(tax.NetTaxPiastres)),
                new ReportKpi("متوسط قيمة الطلب", M(s.AverageOrderValuePiastres)),
            }),
            new ReportKpiGroup("ما لم يُحصَّل بعد", new[]
            {
                new ReportKpi("المبيعات المحصّلة (أساس المبيعات)", M(s.CollectedSalesPiastres), Note: "ليست النقدية الفعلية؛ انظر التقرير المالي للمالك"),
                new ReportKpi("تحويلات إلكترونية لم تُؤكد", M(s.PendingElectronicSalesPiastres)),
                new ReportKpi("نقدية مع السائقين", M(s.CashWithDriversPiastres)),
            }),
            new ReportKpiGroup("ما مُنح من القيمة", new[]
            {
                new ReportKpi("قيمة العروض", M(s.OffersValuePiastres)),
                new ReportKpi("الضيافة", M(s.CompsValuePiastres)),
                new ReportKpi("الخصومات اليدوية", M(s.ManualDiscountsPiastres)),
            }),
        };
        var money = new[] { new ReportColumn("البند", ReportCellKind.Text, 2), new ReportColumn("صافي المبلغ", ReportCellKind.Money) };
        var channels = Table("channels", "حسب طريقة الدفع", "بوقت البيع، ناقص مرتجعات الفترة عبر نفس الطريقة", money, new[]
        {
            new[] { T("نقدي"), M(s.CashPiastres) }, new[] { T("بطاقة"), M(s.CardPiastres) },
            new[] { T("إنستاباي (المؤكد)"), M(s.InstaPayPiastres) }, new[] { T("محفظة (المؤكد)"), M(s.WalletPiastres) },
        }, new[] { T("الإجمالي"), M(s.CashPiastres + s.CardPiastres + s.InstaPayPiastres + s.WalletPiastres) },
            "الفرق عن صافي المبيعات = التحويلات غير المؤكدة وطلبات الدفع عند الاستلام التي لم تُسوَّ.");
        var types = Table("order_types", "حسب نوع الطلب", "بوقت البيع", money, new[]
        {
            new[] { T("استلام"), M(s.PickupPiastres) }, new[] { T("تيك أواي"), M(s.TakeawayPiastres) }, new[] { T("توصيل"), M(s.DeliveryPiastres) },
        }, new[] { T("الإجمالي"), M(s.PickupPiastres + s.TakeawayPiastres + s.DeliveryPiastres) });

        var shiftRows = new List<ReportCell[]>();
        for (var day = range.From; day <= range.To; day = day.AddDays(1))
            foreach (var sh in await _reports.GetShiftsForBusinessDateAsync(day, ct))
                shiftRows.Add(new[] { ReportCell.Day(day), T(sh.ShiftName), T(sh.OpenedByUserName), T(sh.Status == "Closed" ? "مغلقة" : "مفتوحة"),
                    N(sh.Sales.OrderCount), M(sh.Sales.NetSalesPiastres), M(sh.ExpensesTotalPiastres), M(sh.ExpectedCashPiastres), M(sh.ActualCashPiastres), M(sh.CashDifferencePiastres) });
        var shifts = Table("shifts", "حسب الوردية", "ورديات تاريخ يوم العمل، مبيعات كل وردية ومرتجعات من درجها",
            new[] { new ReportColumn("التاريخ", ReportCellKind.Date), new ReportColumn("الوردية", ReportCellKind.Text), new ReportColumn("فتحها", ReportCellKind.Text),
                    new ReportColumn("الحالة", ReportCellKind.Text), new ReportColumn("الطلبات", ReportCellKind.Count), new ReportColumn("صافي المبيعات", ReportCellKind.Money),
                    new ReportColumn("المصروفات", ReportCellKind.Money), new ReportColumn("النقدية المتوقعة", ReportCellKind.Money), new ReportColumn("المعدودة", ReportCellKind.Money),
                    new ReportColumn("الفرق", ReportCellKind.Money) }, shiftRows,
            note: "الوردية المفتوحة لا نقدية متوقعة ولا معدودة لها بعد (غير متاح).");

        var days = await _db.BusinessDays.AsNoTracking().Where(d => d.BusinessDate >= range.From && d.BusinessDate <= range.To)
            .OrderBy(d => d.BusinessDate).Select(d => new { d.Id, d.BusinessDate, d.Status }).ToListAsync(ct);
        var dayRows = new List<ReportCell[]>();
        foreach (var d in days)
        {
            var rec = await _reports.GetBusinessDayReconciliationAsync(d.Id, ct);
            dayRows.Add(new[] { ReportCell.Day(d.BusinessDate), T(d.Status switch { "Closed" => "مقفول", "Reopened" => "أُعيد فتحه", _ => "مفتوح" }),
                N(rec.Sales.OrderCount), M(rec.Sales.GrossSalesPiastres), M(rec.Sales.RefundsPiastres), M(rec.Sales.NetSalesPiastres), M(rec.ExpensesTotalPiastres) });
        }
        var businessDays = Table("business_days", "حسب يوم العمل", "أيام العمل التي يقع تاريخها في الفترة؛ الطلبات بيوم عملها (قد يمتد بعد منتصف الليل)",
            new[] { new ReportColumn("يوم العمل", ReportCellKind.Date), new ReportColumn("الحالة", ReportCellKind.Text), new ReportColumn("الطلبات", ReportCellKind.Count),
                    new ReportColumn("إجمالي المبيعات", ReportCellKind.Money), new ReportColumn("المرتجعات", ReportCellKind.Money), new ReportColumn("صافي المبيعات", ReportCellKind.Money),
                    new ReportColumn("المصروفات", ReportCellKind.Money) }, dayRows,
            note: "أساس هذا الجدول يوم العمل، وأساس الملخص الأيام التقويمية؛ قد يختلفان في الأيام الممتدة بعد منتصف الليل.");

        return Doc(m, "sales", "تقرير المبيعات", "الملخص بالأيام التقويمية (بوقت البيع)؛ الجداول موسومة بأساسها", summary,
            new[] { channels, types, shifts, businessDays },
            new[] { "المبيعة تُحسب في فترة إنشائها، والمرتجع في فترة دفعه." });
    }

    // ---------------------------------------------------------------- cash and treasury

    private async Task<ReportDocument> CashAsync(Meta m, BusinessDateRange range, CancellationToken ct)
    {
        var flow = await _reports.GetCashFlowAsync(range, ct);
        var accounts = Table("accounts", "حركة الحسابات", "بوقت حركة المال",
            new[] { new ReportColumn("الحساب", ReportCellKind.Text, 2), new ReportColumn("أول المدة", ReportCellKind.Money), new ReportColumn("الوارد", ReportCellKind.Money),
                    new ReportColumn("المنصرف", ReportCellKind.Money), new ReportColumn("آخر المدة", ReportCellKind.Money) },
            flow.Accounts.Select(a => new[] { T(ReportText.Account(a.AccountId)), M(a.OpeningPiastres), M(a.InflowPiastres), M(a.OutflowPiastres), M(a.ClosingPiastres) }),
            new[] { T("الإجمالي"), M(flow.Accounts.Sum(a => a.OpeningPiastres)), M(flow.Accounts.Sum(a => a.InflowPiastres)),
                    M(flow.Accounts.Sum(a => a.OutflowPiastres)), M(flow.Accounts.Sum(a => a.ClosingPiastres)) },
            "آخر المدة = أول المدة + الوارد − المنصرف لكل حساب؛ التحويل بين حسابين وارد لأحدهما ومنصرف من الآخر، فيظهر في الإجمالي مرتين.");
        var byType = Table("by_type", "حسب نوع الحركة", "بوقت حركة المال",
            new[] { new ReportColumn("الحساب", ReportCellKind.Text), new ReportColumn("النوع", ReportCellKind.Text, 2), new ReportColumn("الوارد", ReportCellKind.Money),
                    new ReportColumn("المنصرف", ReportCellKind.Money), new ReportColumn("العدد", ReportCellKind.Count) },
            flow.Accounts.SelectMany(a => a.Lines.Select(l => new[] { T(ReportText.Account(a.AccountId)), T(ReportText.MovementType(l.MovementType)), M(l.InflowPiastres), M(l.OutflowPiastres), N(l.MovementCount) })));

        var (start, end) = range.ToUtcRange();
        var movements = await _db.TreasuryMovements.AsNoTracking()
            .Where(x => x.Status == "Posted" && x.CreatedAtUtc >= start && x.CreatedAtUtc < end)
            .OrderBy(x => x.CreatedAtUtc).Take(MaxRows + 1)
            .Select(x => new { x.Id, x.CreatedAtUtc, x.Type, x.SourceAccountId, x.DestinationAccountId, x.AmountPiastres, x.ReferenceType, x.ReferenceId, x.CreatedBy, x.ApprovedBy, x.Reason })
            .ToListAsync(ct);
        var ledger = Table("movements", "كل حركات الخزينة", "بوقت حركة المال",
            new[] { new ReportColumn("الوقت", ReportCellKind.DateTime), new ReportColumn("النوع", ReportCellKind.Text, 2), new ReportColumn("من", ReportCellKind.Text),
                    new ReportColumn("إلى", ReportCellKind.Text), new ReportColumn("المبلغ", ReportCellKind.Money), new ReportColumn("سجّلها", ReportCellKind.Text),
                    new ReportColumn("اعتمدها", ReportCellKind.Text), new ReportColumn("السبب", ReportCellKind.Text, 3), new ReportColumn("المرجع", ReportCellKind.Text, 2) },
            movements.Select(x => new[] { ReportCell.At(x.CreatedAtUtc), T(ReportText.MovementType(x.Type)), T(ReportText.Account(x.SourceAccountId)),
                T(ReportText.Account(x.DestinationAccountId)), M(x.AmountPiastres), T(x.CreatedBy), T(x.ApprovedBy), T(x.Reason), T($"{x.ReferenceType}:{x.ReferenceId} ({x.Id})") }));

        var summary = new[]
        {
            new ReportKpiGroup("الأرصدة في نهاية الفترة", flow.Accounts.Select(a => new ReportKpi(ReportText.Account(a.AccountId), M(a.ClosingPiastres))).ToList()),
        };
        return Doc(m, "cash", "مطابقة النقدية وحركة الخزينة", "بوقت حركة المال (الحركات المرحّلة فقط)", summary, new[] { accounts, byType, ledger },
            new[] { "الرصيد يُحسب من الحركات المرحّلة وحدها؛ آخر المدة هو الرقم الذي يُطابق عليه الجرد الفعلي." });
    }

    // ---------------------------------------------------------------- expenses

    private async Task<ReportDocument> ExpensesAsync(Meta m, BusinessDateRange range, CancellationToken ct)
    {
        var e = await _reports.GetExpenseReportAsync(range, null, ct);
        var lines = Table("expenses", "تفصيل المصروفات", "بتاريخ يوم العمل",
            new[] { new ReportColumn("الوقت", ReportCellKind.DateTime), new ReportColumn("يوم العمل", ReportCellKind.Text), new ReportColumn("البند", ReportCellKind.Text),
                    new ReportColumn("الوصف", ReportCellKind.Text, 3), new ReportColumn("الحساب", ReportCellKind.Text), new ReportColumn("المبلغ", ReportCellKind.Money),
                    new ReportColumn("المرجع", ReportCellKind.Text, 2) },
            e.Expenses.OrderBy(x => x.CreatedAtUtc).Select(x => new[] { ReportCell.At(x.CreatedAtUtc), T(x.BusinessDate), T(x.Category), T(x.Description),
                T(ReportText.Account(x.PaymentAccountId)), M(x.AmountPiastres), T(x.ExpenseId) }),
            new[] { T("الإجمالي"), T(""), T(""), T(""), T(""), M(e.TotalPiastres), T("") },
            "قيد الإلغاء (exp-rev-…) سالب ومرتبط بأصله؛ الإجمالي صافٍ منه.");
        var byCategory = Table("categories", "حسب البند", "بتاريخ يوم العمل",
            new[] { new ReportColumn("البند", ReportCellKind.Text, 2), new ReportColumn("عدد القيود", ReportCellKind.Count), new ReportColumn("الصافي", ReportCellKind.Money) },
            e.Expenses.GroupBy(x => x.Category).OrderByDescending(g => g.Sum(x => x.AmountPiastres))
                .Select(g => new[] { T(g.Key), N(g.Count()), M(g.Sum(x => x.AmountPiastres)) }),
            new[] { T("الإجمالي"), N(e.Expenses.Count), M(e.TotalPiastres) });
        var summary = new[] { new ReportKpiGroup("المصروفات", new[]
        {
            new ReportKpi("صافي المصروفات", M(e.TotalPiastres)),
            new ReportKpi("قيود التصحيح والإلغاء", M(-e.Expenses.Where(x => x.AmountPiastres < 0).Sum(x => x.AmountPiastres)),
                Note: $"العدد: {e.Expenses.Count(x => x.AmountPiastres < 0)}"),
        }) };
        return Doc(m, "expenses", "تقرير المصروفات", "بتاريخ يوم العمل (لا تشمل مشتريات الخامات)", summary, new[] { byCategory, lines });
    }

    // ---------------------------------------------------------------- refunds and voids

    private async Task<ReportDocument> RefundsAsync(Meta m, BusinessDateRange range, CancellationToken ct)
    {
        var refunds = await _reports.GetRefundActivityAsync(range, null, ct);
        var voids = await _reports.GetVoidsAsync(range, ct);
        var refundTable = Table("refunds", "المرتجعات", "بوقت دفع المرتجع",
            new[] { new ReportColumn("الوقت", ReportCellKind.DateTime), new ReportColumn("الطلب", ReportCellKind.Text), new ReportColumn("المبلغ", ReportCellKind.Money),
                    new ReportColumn("الطريقة", ReportCellKind.Text), new ReportColumn("السبب", ReportCellKind.Text, 3), new ReportColumn("سجّله", ReportCellKind.Text),
                    new ReportColumn("اعتمده", ReportCellKind.Text), new ReportColumn("وقت البيع الأصلي", ReportCellKind.DateTime) },
            refunds.OrderBy(r => r.CreatedAtUtc).Select(r => new[] { ReportCell.At(r.CreatedAtUtc), T(r.OrderNumber ?? r.OrderId), M(r.AmountPiastres),
                T(ReportText.PaymentMethod(r.PaymentMethod)), T(r.Reason), T(r.CreatedBy), T(r.AuthorizedBy), ReportCell.At(r.OriginalSaleAtUtc) }),
            new[] { T("الإجمالي"), T(""), M(refunds.Sum(r => r.AmountPiastres)), T(""), T(""), T(""), T(""), T("") });
        var voidTable = Table("voids", "الطلبات الملغاة", "بوقت الإلغاء",
            new[] { new ReportColumn("الوقت", ReportCellKind.DateTime), new ReportColumn("الطلب", ReportCellKind.Text), new ReportColumn("النوع", ReportCellKind.Text, 2),
                    new ReportColumn("القيمة", ReportCellKind.Money), new ReportColumn("السبب", ReportCellKind.Text, 2), new ReportColumn("ألغاه", ReportCellKind.Text),
                    new ReportColumn("الطعام", ReportCellKind.Text), new ReportColumn("نقدية أُعيدت", ReportCellKind.Money) },
            voids.Voids.Select(v => new[] { ReportCell.At(v.CancelledAtUtc), T(v.OrderNumber), T(v.Kind switch
                { "electronic_unconfirmed" => "دفع إلكتروني لم يصل", "undelivered_cash_on_delivery" => "توصيل لم يتم", _ => "سبب آخر" }),
                M(v.TotalPiastres), T(v.Reason), T(v.CancelledBy), T(v.StockDisposition switch { "prepared" => "حُضّر - هدر", "not_prepared" => "أعيد للمخزون", _ => "غير مسجّل" }),
                M(v.ReturnedPiastres) }),
            new[] { T("الإجمالي"), T(""), T(""), M(voids.TotalPiastres), T(""), T(""), T(""), M(voids.Voids.Sum(v => v.ReturnedPiastres)) },
            "الطلب الملغي ليس مبيعة ولا يدخل في أي رقم مبيعات.");
        var summary = new[] { new ReportKpiGroup("المرتجعات والإلغاءات", new[]
        {
            new ReportKpi("المرتجعات المدفوعة", M(refunds.Sum(r => r.AmountPiastres)), Note: $"العدد: {refunds.Count}"),
            new ReportKpi("منها عن مبيعات قبل الفترة", M(refunds.Where(r => r.OriginalSaleAtUtc is { } s && BusinessClock.BusinessDate(s) < m.From).Sum(r => r.AmountPiastres))),
            new ReportKpi("قيمة الطلبات الملغاة", M(voids.TotalPiastres), Note: $"العدد: {voids.Count}"),
        }) };
        return Doc(m, "refunds", "المرتجعات والإلغاءات", "المرتجع بوقت دفعه، والإلغاء بوقت حدوثه", summary, new[] { refundTable, voidTable },
            new[] { "مرتجع مبيعة من فترة سابقة يظهر في فترة دفعه، ولا يغيّر الفترة السابقة." });
    }

    // ---------------------------------------------------------------- delivery

    private async Task<ReportDocument> DeliveryAsync(Meta m, BusinessDateRange range, CancellationToken ct)
    {
        var d = await _reports.GetDeliveryOutcomesReportAsync(range.From, range.To, ct);
        var (start, end) = range.ToUtcRange();
        var shortfalls = (await _shortfalls.ListAsync(ct)).Items.Where(i => i.DeliveredAtUtc >= start && i.DeliveredAtUtc < end).ToList();
        var settlements = await _db.DriverSettlements.AsNoTracking().Where(s => s.CreatedAtUtc >= start && s.CreatedAtUtc < end)
            .OrderBy(s => s.CreatedAtUtc).Take(MaxRows + 1).ToListAsync(ct);

        var summary = new[]
        {
            new ReportKpiGroup("نتائج التوصيل", new[]
            {
                new ReportKpi("تم التوصيل", N(d.DeliveredCount)), new ReportKpi("فشل", N(d.FailedCount)), new ReportKpi("رُدّ", N(d.ReturnedCount)),
                new ReportKpi("رسوم التوصيل للمُسلَّم", M(d.DeliveryFeesPiastres)),
                new ReportKpi("متوسط زمن التوصيل (دقيقة)", N(d.AverageDeliveryMinutes), d.AverageDeliveryMinutes is null ? "unavailable" : "actual"),
            }),
            new ReportKpiGroup("التسويات والفروق", new[]
            {
                new ReportKpi("نقدية سُلِّمت في التسويات", M(d.SettledCashPiastres), Note: $"عدد التسويات: {d.SettlementCount}"),
                new ReportKpi("عجز التسويات", M(d.ShortagesPiastres)), new ReportKpi("زيادة التسويات", M(d.OveragesPiastres)),
                new ReportKpi("فروق تحصيل عند التسليم", M(d.CollectionShortfallPiastres), Note: $"العدد: {d.CollectionShortfallCount}"),
                new ReportKpi("عهد لم تُسوَّ الآن", M(d.UnsettledCustodyExpectedPiastres), Note: $"العدد: {d.UnsettledCustodyCount}"),
            }),
        };
        var settlementTable = Table("settlements", "تسويات السائقين", "بوقت التسوية",
            new[] { new ReportColumn("الوقت", ReportCellKind.DateTime), new ReportColumn("السائق", ReportCellKind.Text), new ReportColumn("الطلبات", ReportCellKind.Count),
                    new ReportColumn("المحصّل", ReportCellKind.Money), new ReportColumn("أجر التوصيل", ReportCellKind.Money), new ReportColumn("المستحق", ReportCellKind.Money),
                    new ReportColumn("المُسلَّم", ReportCellKind.Money), new ReportColumn("الفرق", ReportCellKind.Money), new ReportColumn("استلمه", ReportCellKind.Text) },
            settlements.Select(s => new[] { ReportCell.At(s.CreatedAtUtc), T(s.DriverName ?? s.DriverId), N(s.OrderCount), M(s.CashCollectedPiastres),
                M(s.FeeEntitlementPiastres), M(s.ExpectedPiastres), M(s.ActualPiastres), M(s.DifferencePiastres), T(s.ReceivedBy) }),
            new[] { T("الإجمالي"), T(""), N(settlements.Sum(s => s.OrderCount)), M(settlements.Sum(s => s.CashCollectedPiastres)), M(settlements.Sum(s => s.FeeEntitlementPiastres)),
                    M(settlements.Sum(s => s.ExpectedPiastres)), M(settlements.Sum(s => s.ActualPiastres)), M(settlements.Sum(s => s.DifferencePiastres)), T("") });
        var shortfallTable = Table("shortfalls", "فروق التحصيل عند التسليم", "بوقت التسليم",
            new[] { new ReportColumn("وقت التسليم", ReportCellKind.DateTime), new ReportColumn("الطلب", ReportCellKind.Text), new ReportColumn("السائق", ReportCellKind.Text),
                    new ReportColumn("المستحق", ReportCellKind.Money), new ReportColumn("المحصّل", ReportCellKind.Money), new ReportColumn("الفرق", ReportCellKind.Money),
                    new ReportColumn("التصنيف", ReportCellKind.Text, 2), new ReportColumn("اعتمده", ReportCellKind.Text) },
            shortfalls.Select(s => new[] { ReportCell.At(s.DeliveredAtUtc), T(s.OrderNumber ?? s.OrderId), T(s.DriverName), M(s.ExpectedPiastres), M(s.CollectedPiastres),
                M(s.ShortfallPiastres), T(ShortfallLabel(s.Classification)), T(s.ApprovedBy) }),
            new[] { T("الإجمالي"), T(""), T(""), M(shortfalls.Sum(s => s.ExpectedPiastres)), M(shortfalls.Sum(s => s.CollectedPiastres)), M(shortfalls.Sum(s => s.ShortfallPiastres)), T(""), T("") },
            "الفرق غير المحسوم ليس خسارة مؤكدة ولا دينًا مؤكدًا حتى يُصنَّف.");
        return Doc(m, "delivery", "أداء التوصيل وتسويات السائقين", "نتائج التوصيل بوقت التسليم، والتسويات بوقتها", summary,
            new[] { settlementTable, shortfallTable }, new[] { "لا يتضمن التقرير بيانات العملاء الشخصية." });
    }

    private static string ShortfallLabel(string? classification) => classification switch
    {
        null => "غير محسوم",
        "customer_receivable" => "مديونية على العميل",
        "driver_liability" => "مسؤولية السائق",
        "approved_discount" => "خصم معتمد",
        "write_off" => "إعدام دين معتمد",
        _ => classification,
    };

    // ---------------------------------------------------------------- products

    private async Task<ReportDocument> ProductsAsync(Meta m, BusinessDateRange range, CancellationToken ct)
    {
        var products = await _reports.GetSoldProductsAsync(range.From, range.To, ct);
        var categories = await _reports.GetCategorySalesAsync(range.From, range.To, ct);
        var productTable = Table("products", "الأصناف المباعة", "بوقت البيع (الطلبات غير الملغاة)",
            new[] { new ReportColumn("الصنف", ReportCellKind.Text, 3), new ReportColumn("الفئة", ReportCellKind.Text, 2), new ReportColumn("الكمية", ReportCellKind.Count),
                    new ReportColumn("عدد الطلبات", ReportCellKind.Count), new ReportColumn("القيمة", ReportCellKind.Money), new ReportColumn("ساعة الذروة", ReportCellKind.Count) },
            products.OrderByDescending(p => p.TotalPiastres).Select(p => new[] { T(p.ProductName), T(p.CategoryName), N(p.Quantity), N(p.OrderCount), M(p.TotalPiastres), N(p.PeakHour) }),
            new[] { T("الإجمالي"), T(""), N(products.Sum(p => p.Quantity)), T(""), M(products.Sum(p => p.TotalPiastres)), T("") },
            "قيمة الصنف بسعره المسجَّل على الطلب وقت البيع؛ تغيير السعر لاحقًا لا يغيّرها. المرتجعات لا تُخصم هنا.");
        var categoryTable = Table("categories", "حسب الفئة", "بوقت البيع",
            new[] { new ReportColumn("الفئة", ReportCellKind.Text, 2), new ReportColumn("الكمية", ReportCellKind.Count), new ReportColumn("القيمة", ReportCellKind.Money) },
            categories.OrderByDescending(c => c.TotalPiastres).Select(c => new[] { T(c.CategoryName), N(c.Quantity), M(c.TotalPiastres) }),
            new[] { T("الإجمالي"), N(categories.Sum(c => c.Quantity)), M(categories.Sum(c => c.TotalPiastres)) });
        var summary = new[] { new ReportKpiGroup("الأصناف", new[]
        {
            new ReportKpi("عدد الأصناف المباعة", N(products.Count)),
            new ReportKpi("إجمالي القطع", N(products.Sum(p => p.Quantity))),
            new ReportKpi("قيمة الأصناف", M(products.Sum(p => p.TotalPiastres))),
        }) };
        return Doc(m, "products", "أداء الأصناف", "بوقت البيع", summary, new[] { categoryTable, productTable },
            new[] { "تقرير العروض لكل صنف غير متاح بعد (AUD-08)؛ قيمة العروض الإجمالية في تقرير المبيعات." });
    }

    // ---------------------------------------------------------------- financial exceptions

    private async Task<ReportDocument> ExceptionsAsync(Meta m, BusinessDateRange range, CancellationToken ct)
    {
        var report = await _owner.GetAsync(range, ct);
        var groups = report.Sections.Where(s => s.Key is "discrepancies" or "exceptions" or "receivables").Select(s => new ReportKpiGroup(s.Title,
            s.Kpis.Select(k => new ReportKpi(k.Label, k.AmountPiastres is not null || k.Count is null ? M(k.AmountPiastres) : N(k.Count), k.Status, k.Note)).ToList())).ToList();

        var shortfalls = (await _shortfalls.ListAsync(ct)).Items.Where(i => i.Classification is null).ToList();
        var handovers = await _handovers.ListUnconfirmedAsync(ct);
        var pending = await _reports.GetPendingElectronicAsync(ct);
        var shifts = await _db.Shifts.AsNoTracking()
            .Where(s => s.BusinessDate >= range.From && s.BusinessDate <= range.To && s.CashDifferencePiastres != null && s.CashDifferencePiastres != 0)
            .OrderBy(s => s.ClosedAtUtc)
            .Select(s => new { s.ShiftName, s.BusinessDate, s.ExpectedCashPiastres, s.ActualCashPiastres, s.CashDifferencePiastres, s.DifferenceAuthorizedBy, s.DifferenceReason })
            .ToListAsync(ct);

        var tables = new[]
        {
            Table("shortfalls", "فروق تحصيل غير محسومة (الآن)", "الوضع الآن",
                new[] { new ReportColumn("وقت التسليم", ReportCellKind.DateTime), new ReportColumn("الطلب", ReportCellKind.Text), new ReportColumn("السائق", ReportCellKind.Text),
                        new ReportColumn("الفرق", ReportCellKind.Money) },
                shortfalls.Select(s => new[] { ReportCell.At(s.DeliveredAtUtc), T(s.OrderNumber ?? s.OrderId), T(s.DriverName), M(s.ShortfallPiastres) }),
                new[] { T("الإجمالي"), T(""), T(""), M(shortfalls.Sum(s => s.ShortfallPiastres)) }),
            Table("handovers", "تسليمات نقدية غير مؤكدة أو مختلف عليها (الآن)", "الوضع الآن",
                new[] { new ReportColumn("الوردية", ReportCellKind.Text), new ReportColumn("يوم العمل", ReportCellKind.Date), new ReportColumn("سلّمها", ReportCellKind.Text),
                        new ReportColumn("المستلم", ReportCellKind.Text), new ReportColumn("المسلَّم", ReportCellKind.Money), new ReportColumn("المستلم فعلًا", ReportCellKind.Money),
                        new ReportColumn("الحالة", ReportCellKind.Text) },
                handovers.Select(h => new[] { T(h.ShiftName), ReportCell.Day(h.BusinessDate), T(h.ClosedBy), T(h.ReceiverName), M(h.HandedOverPiastres), M(h.ReceivedPiastres),
                    T(h.Status == "disputed" ? "مختلف عليه" : "بانتظار التأكيد") })),
            Table("pending_electronic", "تحويلات إلكترونية لم تُؤكد (الآن)", "الوضع الآن",
                new[] { new ReportColumn("الطلب", ReportCellKind.Text), new ReportColumn("الطريقة", ReportCellKind.Text), new ReportColumn("المبلغ", ReportCellKind.Money),
                        new ReportColumn("منذ (دقيقة)", ReportCellKind.Count) },
                pending.Payments.Select(p => new[] { T(p.OrderNumber), T(ReportText.PaymentMethod(p.Method)), M(p.AmountPiastres), N((long)p.AgeMinutes) }),
                new[] { T("الإجمالي"), T(""), M(pending.TotalPiastres), T("") }),
            Table("shift_differences", "فروق نقدية الورديات في الفترة", "ورديات تاريخ يوم العمل",
                new[] { new ReportColumn("الوردية", ReportCellKind.Text), new ReportColumn("يوم العمل", ReportCellKind.Date), new ReportColumn("المتوقع", ReportCellKind.Money),
                        new ReportColumn("المعدود", ReportCellKind.Money), new ReportColumn("الفرق", ReportCellKind.Money), new ReportColumn("اعتمده", ReportCellKind.Text),
                        new ReportColumn("السبب", ReportCellKind.Text, 2) },
                shifts.Select(s => new[] { T(s.ShiftName), ReportCell.Day(s.BusinessDate), M(s.ExpectedCashPiastres), M(s.ActualCashPiastres), M(s.CashDifferencePiastres),
                    T(s.DifferenceAuthorizedBy ?? "بلا اعتماد"), T(s.DifferenceReason) }),
                new[] { T("الإجمالي"), T(""), T(""), T(""), M(shifts.Sum(s => s.CashDifferencePiastres ?? 0)), T(""), T("") }),
        };
        return Doc(m, "exceptions", "تقرير الاستثناءات المالية", "الفروق والقوائم المفتوحة الآن، وفروق الورديات في الفترة", groups, tables,
            new[] { "البنود «الآن» قائمة مفتوحة حتى تُحسم مهما كان تاريخها." });
    }

    // ---------------------------------------------------------------- suppliers

    private async Task<ReportDocument> SuppliersAsync(Meta m, BusinessDateRange range, CancellationToken ct)
    {
        var aging = await _purchasing.GetPayablesAgingAsync(ct);
        var suppliers = await _purchasing.ListSuppliersAsync(ct);
        var (start, end) = range.ToUtcRange();
        var names = suppliers.ToDictionary(s => s.Id, s => s.Name);
        var purchases = await _db.Purchases.AsNoTracking().Where(p => p.CreatedAtUtc >= start && p.CreatedAtUtc < end).OrderBy(p => p.CreatedAtUtc)
            .Select(p => new { p.Id, p.InvoiceNumber, p.SupplierId, p.TotalPiastres, p.PaidPiastres, p.RemainingAmountPiastres, p.CreatedAtUtc, p.Status }).ToListAsync(ct);
        var payments = await _db.PurchasePayments.AsNoTracking().Where(p => p.CreatedAtUtc >= start && p.CreatedAtUtc < end).OrderBy(p => p.CreatedAtUtc)
            .Select(p => new { p.PurchaseId, p.SupplierId, p.AmountPiastres, p.PaymentAccountId, p.CreatedBy, p.CreatedAtUtc }).ToListAsync(ct);
        var credits = await _db.SupplierLedgerEntries.AsNoTracking()
            .Where(e => e.CreatedAtUtc >= start && e.CreatedAtUtc < end && (e.Type == "purchase_correction" || e.Type == "supplier_credit" || e.Type == "supplier_return"))
            .OrderBy(e => e.CreatedAtUtc).Select(e => new { e.SupplierId, e.PurchaseId, e.Type, e.CreditPiastres, e.Reason, e.CreatedBy, e.CreatedAtUtc }).ToListAsync(ct);

        var balances = Table("balances", "المستحق لكل مورد (الآن)", "الوضع الآن",
            new[] { new ReportColumn("المورد", ReportCellKind.Text, 2), new ReportColumn("إجمالي الفواتير", ReportCellKind.Money), new ReportColumn("المسدَّد", ReportCellKind.Money),
                    new ReportColumn("المستحق", ReportCellKind.Money) },
            suppliers.Where(s => s.OutstandingBalancePiastres != 0 || s.TotalInvoicedPiastres != 0).OrderByDescending(s => s.OutstandingBalancePiastres)
                .Select(s => new[] { T(s.Name), M(s.TotalInvoicedPiastres), M(s.TotalPaidPiastres), M(s.OutstandingBalancePiastres) }),
            new[] { T("الإجمالي"), M(suppliers.Sum(s => s.TotalInvoicedPiastres)), M(suppliers.Sum(s => s.TotalPaidPiastres)), M(suppliers.Sum(s => s.OutstandingBalancePiastres)) },
            "المستحق = الفواتير − السداد − التصحيحات والخصومات والمرتجعات، من دفتر الموردين.");
        var invoices = Table("aging", "الفواتير المستحقة وأعمارها (الآن)", "الوضع الآن",
            new[] { new ReportColumn("المورد", ReportCellKind.Text, 2), new ReportColumn("الفاتورة", ReportCellKind.Text), new ReportColumn("الاستلام", ReportCellKind.Text),
                    new ReportColumn("الاستحقاق", ReportCellKind.Text), new ReportColumn("العمر (يوم)", ReportCellKind.Count), new ReportColumn("الشريحة", ReportCellKind.Text),
                    new ReportColumn("المتبقي", ReportCellKind.Money) },
            aging.Invoices.Select(i => new[] { T(i.SupplierName), T(i.InvoiceNumber), T(i.ReceivedOnBusinessDate), T(i.DueDate ?? "بلا تاريخ"), N(i.AgeInDays), T(i.BucketLabel), M(i.RemainingPiastres) }),
            new[] { T("الإجمالي"), T(""), T(""), T(""), T(""), T(""), M(aging.TotalOutstandingPiastres) });
        var purchaseTable = Table("purchases", "مشتريات الفترة", "بوقت الاستلام",
            new[] { new ReportColumn("الوقت", ReportCellKind.DateTime), new ReportColumn("المورد", ReportCellKind.Text, 2), new ReportColumn("الفاتورة", ReportCellKind.Text),
                    new ReportColumn("الإجمالي", ReportCellKind.Money), new ReportColumn("المدفوع", ReportCellKind.Money), new ReportColumn("المتبقي الآن", ReportCellKind.Money) },
            purchases.Select(p => new[] { ReportCell.At(p.CreatedAtUtc), T(names.GetValueOrDefault(p.SupplierId, p.SupplierId)), T(p.InvoiceNumber ?? p.Id),
                M(p.TotalPiastres), M(p.PaidPiastres), M(p.RemainingAmountPiastres) }),
            new[] { T("الإجمالي"), T(""), T(""), M(purchases.Sum(p => p.TotalPiastres)), M(purchases.Sum(p => p.PaidPiastres)), M(purchases.Sum(p => p.RemainingAmountPiastres)) });
        var paymentTable = Table("payments", "سداد الموردين في الفترة", "بوقت السداد",
            new[] { new ReportColumn("الوقت", ReportCellKind.DateTime), new ReportColumn("المورد", ReportCellKind.Text, 2), new ReportColumn("الحساب", ReportCellKind.Text),
                    new ReportColumn("المبلغ", ReportCellKind.Money), new ReportColumn("سجّله", ReportCellKind.Text) },
            payments.Select(p => new[] { ReportCell.At(p.CreatedAtUtc), T(names.GetValueOrDefault(p.SupplierId, p.SupplierId)), T(ReportText.Account(p.PaymentAccountId)),
                M(p.AmountPiastres), T(p.CreatedBy) }),
            new[] { T("الإجمالي"), T(""), T(""), M(payments.Sum(p => p.AmountPiastres)), T("") });
        var creditTable = Table("credits", "تصحيحات وخصومات ومرتجعات الموردين في الفترة", "بوقت التسجيل",
            new[] { new ReportColumn("الوقت", ReportCellKind.DateTime), new ReportColumn("المورد", ReportCellKind.Text, 2), new ReportColumn("النوع", ReportCellKind.Text),
                    new ReportColumn("المبلغ", ReportCellKind.Money), new ReportColumn("السبب", ReportCellKind.Text, 2), new ReportColumn("سجّله", ReportCellKind.Text) },
            credits.Select(c => new[] { ReportCell.At(c.CreatedAtUtc), T(names.GetValueOrDefault(c.SupplierId, c.SupplierId)),
                T(c.Type switch { "purchase_correction" => "خطأ في الفاتورة", "supplier_return" => "مرتجع بضاعة", _ => "خصم من المورد" }), M(c.CreditPiastres), T(c.Reason), T(c.CreatedBy) }),
            new[] { T("الإجمالي"), T(""), T(""), M(credits.Sum(c => c.CreditPiastres)), T(""), T("") });
        var summary = new[] { new ReportKpiGroup("الموردون", new[]
        {
            new ReportKpi("إجمالي المستحق الآن", M(aging.TotalOutstandingPiastres), Note: $"فواتير: {aging.Invoices.Count}"),
            new ReportKpi("منه متأخر", M(aging.OverduePiastres), Note: $"فواتير: {aging.OverdueInvoiceCount}"),
            new ReportKpi("بلا تاريخ استحقاق", M(aging.UnscheduledPiastres), Note: $"فواتير: {aging.UnscheduledInvoiceCount}"),
            new ReportKpi("مشتريات الفترة", M(purchases.Sum(p => p.TotalPiastres))),
            new ReportKpi("سداد الفترة", M(payments.Sum(p => p.AmountPiastres))),
        }) };
        return Doc(m, "suppliers", "أرصدة الموردين والمشتريات", "الأرصدة الآن؛ المشتريات والسداد بوقتها في الفترة", summary,
            new[] { balances, invoices, purchaseTable, paymentTable, creditTable });
    }
}
