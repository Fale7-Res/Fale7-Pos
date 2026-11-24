namespace Falah.Infrastructure.Reports;

using Falah.Application.Reports;
using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The owner's daily financial report (P5) and the two reconciliations behind it (P1). It owns no figure that another
/// screen already computes: sales, VAT, profitability, obligations and attention items come from the owner dashboard
/// (itself composed from the report queries), so this report can never disagree with it. What it adds is:
///
/// - the sales bridge: the period's own orders followed to where their money is now (through the treasury at the
///   sale or on confirmation, settled by a driver, held by a driver, not collected yet, a pending transfer, or a
///   collection shortfall by classification), with a residual that must be zero;
/// - the money flow: every posted movement of the period classified (MoneyFlowRules), so internal transfers,
///   corrections, reimbursements and funding are shown for what they are and only customer money counts as
///   collected, with an opening + in − out = closing check;
/// - a definition, basis and source for every KPI, and "unavailable" rather than zero for what cannot be known.
/// </summary>
public sealed class OwnerFinancialReportQueryService
{
    private readonly FalahDbContext _db;
    private readonly OwnerDashboardQueryService _dashboard;

    public OwnerFinancialReportQueryService(FalahDbContext db, OwnerDashboardQueryService dashboard)
    {
        _db = db;
        _dashboard = dashboard;
    }

    public async Task<OwnerFinancialReportDto> GetAsync(BusinessDateRange range, CancellationToken ct = default)
    {
        var (start, end) = range.ToUtcRange();
        var dashboard = await _dashboard.GetAsync(range, ct);
        var bridge = await BuildSalesBridgeAsync(start, end, dashboard.Cash.NetSalesPiastres, dashboard.Leakage.RefundsPiastres, ct);
        var flow = await BuildMoneyFlowAsync(start, end, ct);

        var sections = new List<FinancialKpiSectionDto>
        {
            Sales(dashboard),
            Collections(flow, bridge),
            Balances(dashboard.Cash.TreasuryNow),
            await DriversAsync(dashboard, ct),
            await ReceivablesAsync(dashboard, bridge, ct),
            Liabilities(dashboard),
            await ExpensesAsync(range, ct),
            await DiscrepanciesAsync(range, dashboard, ct),
            await ExceptionsAsync(dashboard, flow, ct),
            Profitability(dashboard.Profitability),
        };

        return new OwnerFinancialReportDto(
            range.From.ToString("yyyy-MM-dd"), range.To.ToString("yyyy-MM-dd"), DateTimeOffset.UtcNow,
            sections, bridge, flow, TimingNotes(bridge));
    }

    // ---------------------------------------------------------------- reconciliations

    private async Task<SalesBridgeDto> BuildSalesBridgeAsync(DateTimeOffset start, DateTimeOffset end, long netSales, long refundsMade, CancellationToken ct)
    {
        var cancelled = OrderStatus.Cancelled.ToWireString();
        var entities = await _db.Orders.AsNoTracking().Include(o => o.Items).Include(o => o.Refunds)
            .Where(o => o.Status != cancelled && !o.AwaitingCollection && o.CreatedAtUtc >= start && o.CreatedAtUtc < end)
            .ToListAsync(ct);
        var orders = entities.Select(OrderMapper.ToDomain).ToList();
        var ids = orders.Select(o => o.Id).ToList();

        var collections = await _db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Type == "sale_collection" && m.Status == nameof(MovementStatus.Posted) && ids.Contains(m.ReferenceId))
            .Select(m => new { m.AmountPiastres, m.CreatedAtUtc }).ToListAsync(ct);

        var codIds = orders.Where(o => CheckoutRules.IsCashOnDelivery(o.OrderType, o.PaymentMethod, o.Total)).Select(o => o.Id).ToHashSet();
        var custodies = await (from c in _db.DriverCustodies.AsNoTracking()
                               join d in _db.Deliveries.AsNoTracking() on c.DeliveryId equals d.Id
                               join r in _db.CollectionShortfallResolutions.AsNoTracking() on d.Id equals r.DeliveryId into rs
                               from r in rs.DefaultIfEmpty()
                               where codIds.Contains(c.OrderId)
                               select new { c.OrderId, c.ExpectedPiastres, c.CollectedPiastres, c.Status, d.DeliveredAtUtc,
                                   Classification = r == null ? null : r.Classification })
            .ToListAsync(ct);

        long codSettled = 0, codHeld = 0, codNotYet = 0, unresolved = 0, receivable = 0, realised = 0, above = 0;
        foreach (var order in orders.Where(o => codIds.Contains(o.Id)))
        {
            var custody = custodies.FirstOrDefault(c => c.OrderId == order.Id);
            if (custody is null || custody.DeliveredAtUtc is null) { codNotYet += order.Total.Piastres; continue; }
            var due = custody.ExpectedPiastres;
            var got = Math.Min(custody.CollectedPiastres, due);
            above += Math.Max(0, custody.CollectedPiastres - due);
            if (custody.Status == nameof(CustodyStatus.Settled)) codSettled += got; else codHeld += got;
            var gap = due - got;
            if (gap > 0)
            {
                if (custody.Classification is null) unresolved += gap;
                else if (CollectionShortfallRules.IsReceivable(custody.Classification)) receivable += gap;
                else realised += gap;
            }
            // A custody whose amount due is not the order's total is a disagreement the residual must show.
            codNotYet += order.Total.Piastres - due;
        }

        var gross = orders.Sum(o => o.Total.Piastres);
        var throughTreasury = collections.Sum(m => m.AmountPiastres);
        var pending = orders.Sum(o => o.ElectronicPayment is { Status: ElectronicPaymentStatus.Pending } leg ? leg.Amount.Piastres : 0);
        var residual = gross - (throughTreasury + codSettled + codHeld + codNotYet + pending + unresolved + receivable + realised);

        var earlier = await (from m in _db.TreasuryMovements.AsNoTracking()
                             join o in _db.Orders.AsNoTracking() on m.ReferenceId equals o.Id
                             where m.Type == "sale_collection" && m.Status == nameof(MovementStatus.Posted)
                                 && m.CreatedAtUtc >= start && m.CreatedAtUtc < end && o.CreatedAtUtc < start
                             select m.AmountPiastres).SumAsync(x => (long?)x, ct) ?? 0;

        return new SalesBridgeDto(gross, throughTreasury, codSettled, codHeld, codNotYet, pending, unresolved, receivable, realised,
            residual, above, refundsMade, netSales, collections.Where(m => m.CreatedAtUtc >= end).Sum(m => m.AmountPiastres), earlier);
    }

    private async Task<MoneyFlowSummaryDto> BuildMoneyFlowAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var posted = _db.TreasuryMovements.AsNoTracking().Where(m => m.Status == nameof(MovementStatus.Posted));
        static long Net(IEnumerable<(string? Source, string? Destination, long Amount)> rows) =>
            rows.Sum(m => (m.Destination is not null ? m.Amount : 0) - (m.Source is not null ? m.Amount : 0));

        var before = (await posted.Where(m => m.CreatedAtUtc < start)
            .Select(m => new { m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres }).ToListAsync(ct))
            .Select(m => (m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres)).ToList();
        var during = await posted.Where(m => m.CreatedAtUtc >= start && m.CreatedAtUtc < end)
            .Select(m => new { m.Type, m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres }).ToListAsync(ct);

        var lines = during
            .GroupBy(m => MoneyFlowRules.Classify(m.Type))
            .Select(g => new MoneyFlowLineDto(
                g.Key.ToString(), FlowLabel(g.Key),
                g.Where(m => m.SourceAccountId is null && m.DestinationAccountId is not null).Sum(m => m.AmountPiastres),
                g.Where(m => m.SourceAccountId is not null && m.DestinationAccountId is null).Sum(m => m.AmountPiastres),
                g.Where(m => m.SourceAccountId is not null && m.DestinationAccountId is not null).Sum(m => m.AmountPiastres),
                g.Count(), MoneyFlowRules.IsCustomerReceipt(g.Key)))
            .OrderBy(l => l.Category, StringComparer.Ordinal)
            .ToList();

        var receipts = during.Where(m => MoneyFlowRules.IsCustomerReceipt(MoneyFlowRules.Classify(m.Type)) && m.DestinationAccountId is not null).ToList();
        var opening = Net(before);
        var inflow = lines.Sum(l => l.InflowPiastres);
        var outflow = lines.Sum(l => l.OutflowPiastres);
        var closing = opening + inflow - outflow;
        var atEnd = Net(before.Concat(during.Select(m => (m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres))));

        return new MoneyFlowSummaryDto(lines,
            receipts.Sum(m => m.AmountPiastres),
            receipts.Where(m => MoneyFlowRules.IsCashAccount(m.DestinationAccountId!)).Sum(m => m.AmountPiastres),
            receipts.Where(m => !MoneyFlowRules.IsCashAccount(m.DestinationAccountId!)).Sum(m => m.AmountPiastres),
            lines.Sum(l => l.MovedInternallyPiastres), opening, inflow, outflow, closing, closing - atEnd,
            during.Count(m => MoneyFlowRules.Classify(m.Type) == MoneyFlowCategory.Unclassified));
    }

    // ---------------------------------------------------------------- sections

    private const string Movement = "movement";
    private const string Sale = "sale";
    private const string Now = "now";

    private static FinancialKpiDto Actual(string key, string label, long amount, string definition, string basis, string sources, int? count = null, string? note = null) =>
        new(key, label, amount, count, "actual", definition, basis, sources, note);

    private static FinancialKpiSectionDto Sales(OwnerDashboardDto d) => new("sales", "المبيعات", new[]
    {
        Actual("net_sales", "صافي المبيعات", d.Cash.NetSalesPiastres,
            "إجمالي الطلبات غير الملغاة المنشأة في الفترة بقيمتها الكاملة، ناقص المرتجعات المدفوعة في الفترة (أيًا كان تاريخ بيعها). شامل ضريبة القيمة المضافة.",
            Sale, "Orders (CreatedAtUtc, Status≠cancelled) + OrderRefunds (CreatedAtUtc)", d.Cash.OrderCount),
        Actual("vat", "ضريبة القيمة المضافة المحصلة (صافي)", d.Cash.NetSalesPiastres - d.Profitability.NetSalesPiastres,
            "الضريبة المسجلة على كل طلب وقت البيع، ناقص أثر المرتجعات. مستحقة للدولة وليست إيرادًا.", Sale, "Orders.TaxAmount, OrderRefunds"),
        Actual("net_sales_ex_vat", "صافي المبيعات بدون الضريبة", d.Profitability.NetSalesPiastres,
            "صافي المبيعات ناقص الضريبة. الأساس الذي تُحسب عليه الربحية.", Sale, "Orders, OrderRefunds"),
    });

    private static FinancialKpiSectionDto Collections(MoneyFlowSummaryDto flow, SalesBridgeDto bridge)
    {
        long Of(MoneyFlowCategory c, bool outflow = false) =>
            flow.Lines.Where(l => l.Category == c.ToString()).Sum(l => outflow ? l.OutflowPiastres : l.InflowPiastres);
        return new("collections", "التحصيل الفعلي (المال الذي دخل)", new[]
        {
            Actual("customer_receipts", "إجمالي ما دفعه العملاء ودخل حسابات المطعم", flow.CustomerReceiptsPiastres,
                "كل مبلغ من عميل وصل فعلًا إلى حساب من حسابات المطعم في الفترة: تحصيل البيع (نقدي/بطاقة/تحويل مؤكد) + ما سلّمه السائقون. لا يشمل التحويلات الداخلية ولا التصحيحات ولا الإيداعات ولا استرداد المصروفات.",
                Movement, "TreasuryMovements: sale_collection, driver_settlement (Posted)"),
            Actual("customer_receipts_cash", "منها نقدًا (الدرج والخزينة)", flow.CustomerReceiptsCashPiastres,
                "الجزء الذي دخل الدرج أو الخزينة الرئيسية.", Movement, "TreasuryMovements → tre-drawer, tre-main"),
            Actual("customer_receipts_electronic", "منها إلكترونيًا (بنك، إنستاباي، محفظة)", flow.CustomerReceiptsElectronicPiastres,
                "الجزء الذي دخل البنك أو إنستاباي أو المحفظة، بعد تأكيده فقط. التحويل غير المؤكد ليس تحصيلًا.", Movement, "TreasuryMovements → tre-bank, tre-instapay, tre-wallet"),
            Actual("driver_remittances", "منها ما سلّمه السائقون", Of(MoneyFlowCategory.DriverRemittance),
                "النقدية التي سلّمها السائقون عند التسوية في الفترة، بعد خصم أجر التوصيل الذي احتفظوا به.", Movement, "TreasuryMovements: driver_settlement"),
            Actual("refunds_paid", "مرتجعات دُفعت للعملاء", Of(MoneyFlowCategory.CustomerRefund, outflow: true),
                "مبالغ خرجت فعلًا للعملاء في الفترة (مرتجع، أو نقدية طلب ملغي أُعيدت).", Movement, "TreasuryMovements: refund, void_return"),
            Actual("sales_collected_so_far", "من مبيعات الفترة: ما وصل للمطعم حتى الآن", bridge.ReceivedThroughTreasuryPiastres + bridge.CodSettledPiastres,
                "من طلبات الفترة نفسها: ما دخل الخزينة عند البيع أو التأكيد، وما سوّاه السائقون. الفرق عن إجمالي ما دفعه العملاء فرق توقيت (انظر الملاحظات).",
                Sale, "Orders + TreasuryMovements(sale_collection) + DriverCustodies(Settled)"),
        });
    }

    private static FinancialKpiSectionDto Balances(TreasuryBalancesDto t) => new("balances", "الأرصدة الآن", new[]
    {
        Actual("balance_drawer", "درج الكاشير", t.DrawerPiastres, "رصيد الدرج من كل الحركات المرحّلة حتى الآن.", Now, "TreasuryMovements (tre-drawer)"),
        Actual("balance_main", "الخزينة الرئيسية", t.MainPiastres, "رصيد الخزينة الرئيسية الآن.", Now, "TreasuryMovements (tre-main)"),
        Actual("balance_cash_total", "إجمالي النقدية", t.DrawerPiastres + t.MainPiastres, "الدرج + الخزينة الرئيسية.", Now, "TreasuryMovements"),
        Actual("balance_bank", "البنك", t.BankPiastres, "رصيد البنك الآن.", Now, "TreasuryMovements (tre-bank)"),
        Actual("balance_instapay", "إنستاباي", t.InstaPayPiastres, "رصيد إنستاباي الآن (المؤكد فقط).", Now, "TreasuryMovements (tre-instapay)"),
        Actual("balance_wallet", "المحفظة", t.WalletPiastres, "رصيد المحفظة الآن (المؤكد فقط).", Now, "TreasuryMovements (tre-wallet)"),
        Actual("balance_electronic_total", "إجمالي الأرصدة الإلكترونية", t.BankPiastres + t.InstaPayPiastres + t.WalletPiastres,
            "البنك + إنستاباي + المحفظة.", Now, "TreasuryMovements"),
    });

    private async Task<FinancialKpiSectionDto> DriversAsync(OwnerDashboardDto d, CancellationToken ct)
    {
        var held = await (from c in _db.DriverCustodies.AsNoTracking()
                          join del in _db.Deliveries.AsNoTracking() on c.DeliveryId equals del.Id
                          where c.Status != nameof(CustodyStatus.Settled) && del.DeliveredAtUtc != null
                          select c.CollectedPiastres).ToListAsync(ct);
        var drivers = await _db.DriverCustodies.AsNoTracking().Where(c => c.Status != nameof(CustodyStatus.Settled))
            .Select(c => c.DriverId).Distinct().CountAsync(ct);
        return new("drivers", "السائقون", new[]
        {
            Actual("cash_held_by_drivers", "نقدية مع السائقين الآن", held.Sum(),
                "ما حصّله السائقون من العملاء في توصيلات تمت ولم يُسوَّ بعد. مال المطعم لكنه ليس في حساباته.", Now,
                "DriverCustodies (Status≠Settled) + Deliveries (DeliveredAtUtc)", held.Count),
            Actual("unsettled_custodies", "عهد لم تُسوَّ (بقيمتها المستحقة)", d.Attention.UnsettledCustodyPiastres,
                "قيمة كل عهدة سائق مفتوحة بما فيها التوصيلات التي لم تُسلَّم بعد.", Now, "DriverCustodies (Status≠Settled)",
                d.Attention.UnsettledCustodyCount, $"عدد السائقين: {drivers}"),
            Actual("driver_compensation", "أجر التوصيل المستحق للسائقين عن مبيعات الفترة", d.Profitability.DriverCompensationPiastres,
                "رسوم التوصيل للطلبات التي سُلِّمت فعلًا من مبيعات الفترة. يُدفع للسائق باحتفاظه به من التحصيل أو من الدرج.", Sale,
                "Orders.DeliveryFeePiastres + Deliveries (Delivered, not Returned)", note: d.Profitability.DriverCompensationPendingCount > 0
                    ? $"{d.Profitability.DriverCompensationPendingCount} توصيلة لم تُحسم بعد بأجر {d.Profitability.DriverCompensationPendingPiastres / 100m:0.##} ج - غير محسوب."
                    : null),
        });
    }

    private async Task<FinancialKpiSectionDto> ReceivablesAsync(OwnerDashboardDto d, SalesBridgeDto bridge, CancellationToken ct)
    {
        var shortfalls = await (from del in _db.Deliveries.AsNoTracking()
                                join c in _db.DriverCustodies.AsNoTracking() on del.Id equals c.DeliveryId
                                join r in _db.CollectionShortfallResolutions.AsNoTracking() on del.Id equals r.DeliveryId into rs
                                from r in rs.DefaultIfEmpty()
                                where del.DeliveredAtUtc != null && c.CollectedPiastres < c.ExpectedPiastres
                                select new { Gap = c.ExpectedPiastres - c.CollectedPiastres, Classification = r == null ? null : r.Classification })
            .ToListAsync(ct);
        var unresolved = shortfalls.Where(s => s.Classification is null).ToList();
        var customer = shortfalls.Where(s => s.Classification == CollectionShortfallRules.CustomerReceivable).ToList();
        var driver = shortfalls.Where(s => s.Classification == CollectionShortfallRules.DriverLiability).ToList();
        return new("receivables", "مبالغ مستحقة للمطعم لم تصل بعد", new[]
        {
            Actual("pending_electronic", "تحويلات إلكترونية لم تُؤكد", d.Attention.PendingElectronicPiastres,
                "مبالغ قال العميل إنه حوّلها ولم يؤكدها أحد بعد. ادعاء وليس مالًا.", Now, "Orders (ElectronicPaymentStatus=pending)",
                d.Attention.PendingElectronicOrderCount),
            Actual("cod_not_collected", "طلبات دفع عند الاستلام لم تُحصَّل (من مبيعات الفترة)", bridge.CodNotYetCollectedPiastres,
                "طلبات توصيل نقدية من الفترة لم يُسجَّل تسليمها بعد: المال ما زال مع العميل.", Sale, "Orders + Deliveries (DeliveredAtUtc null)"),
            Actual("shortfall_customer_receivable", "فروق تحصيل مصنفة مديونية على العملاء", customer.Sum(s => s.Gap),
                "عجز تحصيل عند التسليم صنّفه مدير بدليل على أنه دين على العميل.", Now, "DriverCustodies + CollectionShortfallResolutions", customer.Count),
            Actual("shortfall_driver_liability", "فروق تحصيل مصنفة مديونية على السائقين", driver.Sum(s => s.Gap),
                "عجز تحصيل صنّفه مدير بدليل على أنه مسؤولية السائق. لا يُخصم تلقائيًا من تسويته (قرار المالك).", Now,
                "DriverCustodies + CollectionShortfallResolutions", driver.Count),
            new("shortfall_unresolved", "فروق تحصيل غير محسومة", unresolved.Sum(s => s.Gap), unresolved.Count, unresolved.Count == 0 ? "actual" : "partial",
                "عجز تحصيل عند التسليم لم يصنّفه أحد بعد. ليس خسارة مؤكدة وليس دينًا مؤكدًا - يحتاج قرارًا.", Now,
                "DriverCustodies (Collected<Expected) without CollectionShortfallResolutions",
                unresolved.Count == 0 ? null : "لا يدخل في الربحية حتى يُصنَّف."),
        });
    }

    private static FinancialKpiSectionDto Liabilities(OwnerDashboardDto d) => new("liabilities", "التزامات على المطعم", new[]
    {
        Actual("supplier_outstanding", "مستحق للموردين", d.Obligations.SupplierOutstandingPiastres,
            "الفواتير ناقص السداد ناقص تصحيحات وخصومات ومرتجعات الموردين، من دفتر الموردين.", Now, "SupplierLedgerEntries", d.Obligations.SuppliersWithBalanceCount),
        Actual("supplier_overdue", "منها متأخر عن موعده", d.Obligations.OverduePayablesPiastres,
            "الفواتير التي تجاوزت تاريخ استحقاقها وما زال عليها مبلغ.", Now, "Purchases (DueDate, RemainingAmount)", d.Obligations.OverduePayableCount,
            d.Obligations.UnscheduledPayableCount > 0 ? $"{d.Obligations.UnscheduledPayableCount} فاتورة بلا تاريخ استحقاق ({d.Obligations.UnscheduledPayablesPiastres / 100m:0.##} ج) لا تُعد متأخرة." : null),
        Actual("refund_liability", "مستحق للعملاء: طلبات مدفوعة لم تصل ولم تُرد", d.Attention.PaidUndeliveredOwedPiastres,
            "طلبات توصيل مدفوعة مسبقًا فشل توصيلها أو رُدّت ولم يُرد مبلغها بالكامل.", Now, "Deliveries (Failed/Returned) + Orders (IsPaid, RefundAmount)",
            d.Attention.PaidUndeliveredOrderCount),
        Actual("employee_balances", "أرصدة مستحقة للعاملين", d.Obligations.EmployeeBalancesOwedPiastres,
            "صافي ما يستحقه العاملون في دفاترهم ولم يُصرف.", Now, "EmployeeLedgerEntries", d.Obligations.EmployeesWithBalanceCount),
    });

    private async Task<FinancialKpiSectionDto> ExpensesAsync(BusinessDateRange range, CancellationToken ct)
    {
        var rows = await _db.Expenses.AsNoTracking()
            .Where(e => e.BusinessDate >= range.From && e.BusinessDate <= range.To)
            .Select(e => new { e.AmountPiastres }).ToListAsync(ct);
        var corrections = rows.Where(r => r.AmountPiastres < 0).ToList();
        return new("expenses", "المصروفات", new[]
        {
            Actual("expenses_net", "صافي المصروفات", rows.Sum(r => r.AmountPiastres),
                "المصروفات المسجلة في الفترة ناقص قيود إلغائها (خطأ تسجيل أو مبلغ عاد نقدًا). لا تشمل مشتريات الخامات.", "business_date",
                "Expenses (BusinessDate)", rows.Count(r => r.AmountPiastres > 0)),
            Actual("expense_corrections", "منها قيود إلغاء وتصحيح", -corrections.Sum(r => r.AmountPiastres),
                "قيود إلغاء مرتبطة بمصروف أصلي. لا تُعد دخلًا ولا تحصيلًا.", "business_date", "Expenses (exp-rev-*)", corrections.Count),
        });
    }

    private async Task<FinancialKpiSectionDto> DiscrepanciesAsync(BusinessDateRange range, OwnerDashboardDto d, CancellationToken ct)
    {
        var shifts = await _db.Shifts.AsNoTracking()
            .Where(s => s.BusinessDate >= range.From && s.BusinessDate <= range.To)
            .Select(s => new { s.Status, s.HandedOverPiastres, s.HandoverReceiptStatus, s.HandoverReceivedPiastres })
            .ToListAsync(ct);
        var handedOver = shifts.Where(s => s.Status == "Closed" && (s.HandedOverPiastres ?? 0) > 0).ToList();
        var pending = handedOver.Where(s => s.HandoverReceiptStatus == "pending").ToList();
        var disputed = handedOver.Where(s => s.HandoverReceiptStatus == "disputed").ToList();
        var notRecorded = handedOver.Count(s => s.HandoverReceiptStatus is null);
        var openShifts = shifts.Count(s => s.Status != "Closed");
        string? openNote = openShifts > 0 ? $"{openShifts} وردية ما زالت مفتوحة ولم تُعد نقديتها - فروقها غير معروفة بعد." : null;
        return new("discrepancies", "فروق النقدية", new[]
        {
            new FinancialKpiDto("shift_shortage", "عجز الورديات", d.Attention.ShiftShortagePiastres, null, openShifts > 0 ? "partial" : "actual",
                "مجموع العجز بين النقدية المعدودة والمتوقعة عند إغلاق ورديات الفترة.", "shift", "Shifts.CashDifference (<0)", openNote),
            new FinancialKpiDto("shift_overage", "زيادة الورديات", d.Attention.ShiftOveragePiastres, null, openShifts > 0 ? "partial" : "actual",
                "مجموع الزيادة بين المعدود والمتوقع عند الإغلاق.", "shift", "Shifts.CashDifference (>0)", openNote),
            new FinancialKpiDto("unauthorized_discrepancies", "فروق فوق الحد بلا اعتماد", null, d.Attention.UnauthorizedShiftDiscrepancyCount, "actual",
                "عدد الورديات التي تجاوز فرقها الحد المسموح دون اعتماد مدير.", "shift", "Shifts (DifferenceAuthorizedBy null)"),
            new FinancialKpiDto("handover_pending", "نقدية سُلِّمت ولم يؤكد المستلم استلامها", pending.Sum(s => s.HandedOverPiastres!.Value), pending.Count,
                "actual", "نقدية إغلاق ورديات الفترة نُقلت للخزينة الرئيسية ولم يؤكد مستلمها استلامها برمزه أو من جلسته.", "shift",
                "Shifts (HandoverReceiptStatus=pending)",
                notRecorded > 0 ? $"{notRecorded} تسليم قبل تفعيل تأكيد الاستلام - غير مؤكد ولا مرفوض." : null),
            Actual("handover_disputed", "فرق بين المسلَّم والمستلم", disputed.Sum(s => (s.HandoverReceivedPiastres ?? 0) - s.HandedOverPiastres!.Value),
                "ما عدّه المستلم ناقص ما سلّمه الكاشير، لكل تسليم مختلف عليه. سالب = نقص. لا يُرحَّل تلقائيًا (قرار المالك).", "shift",
                "Shifts (HandoverReceiptStatus=disputed)", disputed.Count),
        });
    }

    private async Task<FinancialKpiSectionDto> ExceptionsAsync(OwnerDashboardDto d, MoneyFlowSummaryDto flow, CancellationToken ct)
    {
        var unresolved = await (from del in _db.Deliveries.AsNoTracking()
                                join c in _db.DriverCustodies.AsNoTracking() on del.Id equals c.DeliveryId
                                where del.DeliveredAtUtc != null && c.CollectedPiastres < c.ExpectedPiastres
                                    && !_db.CollectionShortfallResolutions.Any(r => r.DeliveryId == del.Id)
                                select c.Id).CountAsync(ct);
        var pendingHandovers = await _db.Shifts.AsNoTracking()
            .CountAsync(s => s.Status == "Closed" && (s.HandoverReceiptStatus == "pending" || s.HandoverReceiptStatus == "disputed"), ct);
        var incompleteJournals = await _db.FinancialJournalEntries.AsNoTracking().CountAsync(j => j.Status != "Complete", ct);
        var items = unresolved + pendingHandovers + incompleteJournals + flow.UnclassifiedMovementCount + d.Attention.CloseBlockerCount;
        return new("exceptions", "استثناءات مالية مفتوحة", new[]
        {
            new FinancialKpiDto("open_financial_exceptions", "عدد البنود المالية التي تحتاج قرارًا", null, items, "actual",
                "فروق تحصيل غير مصنفة + تسليمات نقدية غير مؤكدة أو مختلف عليها + عمليات مالية معلقة + حركات غير مصنفة + موانع قفل اليوم.",
                Now, "CollectionShortfallResolutions, Shifts, FinancialJournalEntries, TreasuryMovements, BusinessDayCloseEvaluator",
                $"فروق تحصيل: {unresolved} - تسليمات: {pendingHandovers} - عمليات معلقة: {incompleteJournals} - حركات غير مصنفة: {flow.UnclassifiedMovementCount} - موانع القفل: {d.Attention.CloseBlockerCount}"),
            new FinancialKpiDto("control_exceptions_open", "استثناءات مركز التحكم المفتوحة", null, d.Attention.OpenExceptionCount, "actual",
                "استثناءات سُجّلت لها حالة مفتوحة في مركز التحكم.", Now, "ControlExceptionStates (Open)"),
        });
    }

    private static FinancialKpiSectionDto Profitability(ProfitabilityDto p)
    {
        var missing = p.IsFoodCostComplete ? null : $"تكلفة {p.SoldLineCount - p.KnownCostLineCount} من {p.SoldLineCount} بند مباع غير معروفة - لا تُحسب الربحية.";
        var shortfallNote = p.DeliveryShortfallUnresolvedPiastres > 0
            ? $"فروق تحصيل غير محسومة {p.DeliveryShortfallUnresolvedPiastres / 100m:0.##} ج غير مخصومة."
            : null;
        return new("profit", "الربحية (تقديرية)", new[]
        {
            new FinancialKpiDto("gross_profit_estimate", "مجمل الربح التقديري", p.GrossProfitPiastres, null, p.GrossProfitPiastres is null ? "unavailable" : "estimate",
                "صافي المبيعات بدون الضريبة ناقص تكلفة الطعام المقدرة (الوصفات الحالية × آخر سعر شراء). تقدير وليس ربحًا فعليًا.", Sale,
                "Orders, Recipes, StockMovements(PurchaseReceipt)", Note: missing),
            new FinancialKpiDto("operating_result_estimate", "النتيجة التشغيلية التقديرية", p.OperatingResultPiastres, null, p.OperatingResultPiastres is null ? "unavailable" : "estimate",
                "مجمل الربح ناقص المصروفات والأجور وأجر التوصيل وفروق التحصيل المعتمدة كخصم أو إعدام. تقدير، وتُستبعد منه الفروق غير المحسومة.",
                Sale, "Profitability (owner dashboard)", Note: string.Join(" ", new[] { missing, shortfallNote }.Where(n => n is not null)) is { Length: > 0 } n ? n : null),
        });
    }

    private static IReadOnlyList<string> TimingNotes(SalesBridgeDto b) => new[]
    {
        "المبيعات تُحسب بوقت إنشاء الطلب، والمرتجع بوقت دفعه، والتحصيل بوقت دخول المال الحساب؛ لذلك يختلف تحصيل اليوم عن مبيعاته.",
        $"تحصيل في هذه الفترة لمبيعات فترات سابقة (تأكيد تحويلات قديمة): {b.ReceivedInPeriodForEarlierSalesPiastres / 100m:0.##} ج.",
        $"من مبيعات هذه الفترة وصل بعد نهايتها: {b.ReceivedAfterPeriodPiastres / 100m:0.##} ج.",
        "تسوية السائق تُحسب بوقتها لا بوقت الطلب؛ ما زال مع السائقين أو لم يُحصَّل يظهر في الجسر أعلاه.",
        "الوردية ويوم العمل: الورديات تُنسب لتاريخ يوم العمل، وقد يمتد يوم العمل بعد منتصف الليل؛ هذا التقرير بالأيام التقويمية (القاهرة).",
    };

    private static string FlowLabel(MoneyFlowCategory c) => c switch
    {
        MoneyFlowCategory.CustomerCollection => "تحصيل من العملاء",
        MoneyFlowCategory.DriverRemittance => "تسليم السائقين",
        MoneyFlowCategory.CustomerRefund => "مرتجعات للعملاء",
        MoneyFlowCategory.Expense => "مصروفات",
        MoneyFlowCategory.ExpenseCorrection => "تصحيح مصروف مسجل بالخطأ",
        MoneyFlowCategory.ExpenseReimbursement => "مبلغ مصروف عاد نقدًا",
        MoneyFlowCategory.SupplierPayment => "سداد موردين",
        MoneyFlowCategory.Payroll => "رواتب وسلف",
        MoneyFlowCategory.DriverPay => "أجر سائقين من الدرج",
        MoneyFlowCategory.OwnerDrawing => "سحب المالك",
        MoneyFlowCategory.ExternalFunding => "إيداع من خارج النشاط",
        MoneyFlowCategory.InternalTransfer => "تحويلات داخلية (لا تُحسب)",
        MoneyFlowCategory.CashCountDifference => "فروق جرد النقدية",
        MoneyFlowCategory.EntryCorrection => "تصحيح حركة خزينة",
        _ => "حركات غير مصنفة",
    };
}
