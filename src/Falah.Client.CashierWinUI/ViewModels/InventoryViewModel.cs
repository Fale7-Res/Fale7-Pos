using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Inventory;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The storeroom: what is there, what it is worth, what moved it, and what a physical count found.
///
/// Balance is never stored or computed here - it is derived server-side from the movement log, so
/// this screen reads it rather than tracking it. The write actions stay deliberately distinct: a
/// COUNT ADJUSTMENT records what a human actually found on the shelf (the server trusts the observed
/// direction and quantity, because it cannot re-count a physical shelf the way it can recompute
/// cash), while WASTE records a known loss valued at the last purchase cost. Merging them would
/// destroy the difference between "we miscounted" and "we threw food away", which are different
/// corrective actions.
///
/// Value follows cost, and "unknown cost is not zero" holds here too: an item nobody has priced has
/// an unknown value, is excluded from the storeroom total, and is counted separately so the total
/// is never read as the whole truth.
/// </summary>
public partial class InventoryViewModel : ObservableObject
{
    private readonly AppSession _session;

    public InventoryViewModel(AppSession session) => _session = session;
    private readonly RetainedRequest<RecordCountAdjustmentRequest> _countRequest = new("inventory.count-adjustment");
    private readonly RetainedRequest<RecordWasteRequest> _wasteRequest = new("inventory.waste");
    public bool CanEditAction => !IsBusy && !_countRequest.HasPending && !_wasteRequest.HasPending;
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditAction));
        _session.SetManagementOperationPending(this, !CanEditAction);
    }

    public ObservableCollection<InventoryOverviewRowDto> Items { get; } = new();

    [ObservableProperty] private InventoryOverviewDto? _overview;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _searchQuery = "";
    [ObservableProperty] private bool _lowStockOnly;

    private readonly List<InventoryOverviewRowDto> _allItems = new();

    public bool HasNoItems => !IsLoading && LoadError is null && Items.Count == 0;
    public bool CanManage => _session.HasPermission("inventory_manage");
    public bool CanViewValue => _session.HasPermission("reports_view");

    partial void OnSearchQueryChanged(string value) => ApplyFilter();
    partial void OnLowStockOnlyChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        Items.Clear();
        IEnumerable<InventoryOverviewRowDto> source = _allItems.Where(i => i.Active);
        if (LowStockOnly) source = source.Where(i => i.IsLowStock);
        if (!string.IsNullOrWhiteSpace(SearchQuery))
            source = source.Where(i => i.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase)
                                    || i.Category.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase));
        foreach (var i in source) Items.Add(i);
        OnPropertyChanged(nameof(HasNoItems));
        OnPropertyChanged(nameof(LowStockCount));
        if (IsCounting) RebuildCountSheet();
    }

    public int LowStockCount => _allItems.Count(i => i.Active && i.IsLowStock);

    // The three figures that belong above the list.
    public string StockValueDisplay =>
        Overview is { } o ? CashInput.Format(o.KnownStockValuePiastres) + " ج" : "—";

    /// <summary>Said out loud rather than left for the reader to infer from a total that looks
    /// precise: some of the shelf has no price, so the value above is a floor, not the whole.</summary>
    public string StockValueCaveat =>
        Overview is not { } o || o.ItemsWithoutCostHoldingStockCount == 0 ? ""
        : $"لا تشمل {o.ItemsWithoutCostHoldingStockCount} صنفًا برصيد بلا تكلفة معروفة";

    public bool HasStockValueCaveat => StockValueCaveat.Length > 0;

    public string LowStockDisplay => Overview is { } o ? o.LowStockCount.ToString() : "—";

    /// <summary>Stock below zero is not "low", it is impossible - the kitchen consumed what was
    /// never recorded as received. Said separately from the low-stock count because the action is
    /// different: enter the missing purchases, do not go and order more.</summary>
    public string NegativeBalanceWarning =>
        Overview is not { NegativeBalanceCount: > 0 } o ? ""
        : $"{o.NegativeBalanceCount} صنف رصيده بالسالب - يعني اتصرف من غير ما يتسجّل استلامه. سجّل فواتير الشراء الناقصة، وقيمة المخزون فوق مش صح من غيرها.";

    public bool HasNegativeBalances => NegativeBalanceWarning.Length > 0;
    public string ItemsWithoutCostDisplay => Overview is { } o ? o.ItemsWithoutCostCount.ToString() : "—";

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            var actor = _session.CurrentUser!.Id;
            var overview = await _session.Api.GetInventoryOverviewAsync(actor);
            if (!overview.Success || overview.Value is null)
            {
                LoadError = overview.Error ?? "تعذر تحميل المخزون.";
                return;
            }
            Overview = overview.Value;
            _allItems.Clear();
            _allItems.AddRange(overview.Value.Items);
            ApplyFilter();

            var movements = await _session.Api.GetStockMovementsAsync(limit: 60);
            if (!movements.Success || movements.Value is null) { LoadError = movements.Error ?? "تعذر تحميل حركات المخزون."; return; }
            RecentMovements.Clear();
            foreach (var m in movements.Value) RecentMovements.Add(new MovementRow { Movement = m, ItemName = NameOf(m.InventoryItemId), Unit = UnitOf(m.InventoryItemId) });

            if (SelectedItem is not null) await LoadDetailAsync();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoItems));
            OnPropertyChanged(nameof(StockValueDisplay));
            OnPropertyChanged(nameof(StockValueCaveat));
            OnPropertyChanged(nameof(HasStockValueCaveat));
            OnPropertyChanged(nameof(LowStockDisplay));
            OnPropertyChanged(nameof(ItemsWithoutCostDisplay));
            OnPropertyChanged(nameof(NegativeBalanceWarning));
            OnPropertyChanged(nameof(HasNegativeBalances));
        }
    }

    private string NameOf(string itemId) => _allItems.FirstOrDefault(i => i.Id == itemId)?.Name ?? itemId;
    private string UnitOf(string itemId) => _allItems.FirstOrDefault(i => i.Id == itemId)?.Unit ?? "";

    // ================================ movements ================================

    /// <summary>One movement, said in words: what happened, to how much, who did it and why. The
    /// screen used to fetch these and then show none of them.</summary>
    public sealed class MovementRow
    {
        public required StockMovementDto Movement { get; init; }
        public required string ItemName { get; init; }
        public required string Unit { get; init; }

        public string TypeLabel => Movement.Type switch
        {
            "PurchaseReceipt" => "استلام شراء",
            "ManualReceipt" => "إدخال يدوي",
            "Usage" => "استهلاك مبيعات",
            "Waste" => "فاقد",
            "Spoilage" => "تلف تخزين",
            "Breakage" => "كسر",
            "CountAdjustment" => "تسوية جرد",
            _ => Movement.Type,
        };

        public bool IsIn => Movement.Direction == "In";

        public string QuantityDisplay =>
            (IsIn ? "+" : "−") + MilliUnits.Format(Movement.QuantityMilliUnits) + " " + Unit;

        public string ValueDisplay =>
            Movement.LossValuePiastres is { } loss ? CashInput.Format(loss) + " ج"
            : Movement.UnitCostPiastres is { } unit ? CashInput.Format(unit) + " ج / " + Unit
            : "—";

        public string WhenDisplay => Movement.CreatedAtUtc.ToLocalTime().ToString("MM/dd HH:mm");
        public string ActorDisplay => Movement.ApprovedBy is { Length: > 0 } approver
            ? $"{Movement.CreatedBy} · اعتماد {approver}"
            : Movement.CreatedBy;

        /// <summary>The business document behind the movement - which sale, which purchase - so a
        /// number on the shelf can be traced back to the thing that moved it.</summary>
        public string ReferenceDisplay => Movement.ReferenceType switch
        {
            "order" => "طلب " + Movement.ReferenceId,
            "purchase" => "فاتورة " + Movement.ReferenceId,
            _ => string.IsNullOrWhiteSpace(Movement.ReferenceId) ? "" : Movement.ReferenceType + " " + Movement.ReferenceId,
        };

        public string Reason => Movement.Reason;
    }

    public ObservableCollection<MovementRow> RecentMovements { get; } = new();
    public ObservableCollection<MovementRow> DetailMovements { get; } = new();

    // ================================ item detail ================================

    [ObservableProperty] private InventoryOverviewRowDto? _selectedItem;
    [ObservableProperty] private InventoryItemDetailDto? _detail;
    [ObservableProperty] private bool _isDetailLoading;
    [ObservableProperty] private string? _detailError;

    public bool HasSelection => SelectedItem is not null;
    public bool HasDetail => Detail is not null && !IsDetailLoading;

    partial void OnSelectedItemChanged(InventoryOverviewRowDto? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        _ = LoadDetailAsync();
    }

    partial void OnDetailChanged(InventoryItemDetailDto? value)
    {
        DetailMovements.Clear();
        foreach (var m in value?.Movements ?? Array.Empty<StockMovementRowDto>())
            DetailMovements.Add(new MovementRow
            {
                Movement = new StockMovementDto(m.InventoryItemId, m.Type, m.Direction, m.QuantityMilliUnits,
                    m.UnitCostPiastres, m.LossValuePiastres, m.ReferenceType, m.ReferenceId, m.Reason,
                    m.CreatedBy, m.ApprovedBy, m.CreatedAtUtc),
                ItemName = m.ItemName,
                Unit = m.Unit,
            });

        OnPropertyChanged(nameof(HasDetail));
        OnPropertyChanged(nameof(DetailBalanceDisplay));
        OnPropertyChanged(nameof(DetailValueDisplay));
        OnPropertyChanged(nameof(DetailUnitCostDisplay));
        OnPropertyChanged(nameof(DetailCostSourceLabel));
        OnPropertyChanged(nameof(DetailCostAsOfDisplay));
        OnPropertyChanged(nameof(DetailMinimumDisplay));
        OnPropertyChanged(nameof(DetailPurchasedDisplay));
        OnPropertyChanged(nameof(DetailUsageDisplay));
        OnPropertyChanged(nameof(DetailWasteDisplay));
        OnPropertyChanged(nameof(DetailCountAdjustmentDisplay));
        OnPropertyChanged(nameof(DetailVarianceDisplay));
        OnPropertyChanged(nameof(DetailVarianceNote));
        OnPropertyChanged(nameof(HasDetailVariance));
        OnPropertyChanged(nameof(HasNoDetailMovements));
    }

    public string DetailBalanceDisplay => Detail is { } d ? $"{MilliUnits.Format(d.BalanceMilliUnits)} {d.Unit}" : "—";
    public string DetailMinimumDisplay => Detail is { } d ? $"{MilliUnits.Format(d.MinimumMilliUnits)} {d.Unit}" : "—";
    public string DetailValueDisplay => Detail?.StockValuePiastres is { } v ? CashInput.Format(v) + " ج" : "—";
    public string DetailUnitCostDisplay =>
        Detail is { } d && d.UnitCostPiastres is { } c ? $"{CashInput.Format(c)} ج / {d.Unit}" : "—";

    public string DetailCostSourceLabel => Detail?.CostSource switch
    {
        "Purchase" => "فاتورة شراء",
        "Estimated" => "تقديري",
        "Prep" => "خلطة تجهيز",
        null => "",
        _ => "غير معروف",
    };

    public string DetailCostAsOfDisplay =>
        Detail?.CostAsOfUtc is { } at ? $"آخر شراء {at.ToLocalTime():yyyy/MM/dd}" : "";

    public string DetailPurchasedDisplay => Detail is { } d ? $"{MilliUnits.Format(d.Totals.PurchasedMilliUnits)} {d.Unit}" : "—";
    public string DetailUsageDisplay => Detail is { } d ? $"{MilliUnits.Format(d.Totals.UsageMilliUnits)} {d.Unit}" : "—";
    public string DetailWasteDisplay => Detail is { } d ? $"{MilliUnits.Format(d.Totals.WasteMilliUnits)} {d.Unit}" : "—";
    public string DetailCountAdjustmentDisplay => Detail is not { } d ? "—"
        : (d.Totals.CountAdjustmentNetMilliUnits >= 0 ? "+" : "−")
          + MilliUnits.Format(Math.Abs(d.Totals.CountAdjustmentNetMilliUnits)) + " " + d.Unit;

    public bool HasDetailVariance => Detail is { } d && d.Variance.VarianceMilliUnits != 0;

    public string DetailVarianceDisplay => Detail is not { } d ? "—"
        : $"{MilliUnits.Format(Math.Abs(d.Variance.VarianceMilliUnits))} {d.Unit}";

    /// <summary>Two neutral signals, never collapsed into an accusation. A gap usually means a
    /// recipe changed, is not attached yet, or a loss was never logged.</summary>
    public string DetailVarianceNote => Detail is not { } d ? ""
        : d.Variance.VarianceMilliUnits == 0 ? "المسجّل يطابق ما تقوله الوصفات."
        : d.Variance.VarianceMilliUnits > 0
            ? $"الوصفات تقول إنه اتصرف {MilliUnits.Format(d.Variance.TheoreticalUsageMilliUnits)} {d.Unit}، والمسجّل {MilliUnits.Format(d.Variance.RecordedConsumptionMilliUnits)} {d.Unit} - الفرق غير مفسَّر."
            : $"المسجّل {MilliUnits.Format(d.Variance.RecordedConsumptionMilliUnits)} {d.Unit} أكثر مما تقوله الوصفات ({MilliUnits.Format(d.Variance.TheoreticalUsageMilliUnits)} {d.Unit}).";

    public bool HasNoDetailMovements => Detail is not null && DetailMovements.Count == 0;

    [RelayCommand]
    public async Task LoadDetailAsync()
    {
        if (SelectedItem is not { } row) { Detail = null; return; }
        if (!CanViewValue) { DetailError = "لا تملك صلاحية عرض تكلفة المخزون."; Detail = null; return; }

        IsDetailLoading = true;
        DetailError = null;
        OnPropertyChanged(nameof(HasDetail));
        try
        {
            var result = await _session.Api.GetInventoryItemDetailAsync(row.Id, _session.CurrentUser!.Id);
            if (!result.Success || result.Value is null)
            {
                DetailError = result.Error ?? "تعذر تحميل تفاصيل الصنف.";
                Detail = null;
                return;
            }
            Detail = result.Value;
        }
        finally
        {
            IsDetailLoading = false;
            OnPropertyChanged(nameof(HasDetail));
        }
    }

    [RelayCommand]
    private void CloseDetail() => SelectedItem = null;

    // ================================ count sheet ================================

    /// <summary>One line of a physical count. The counted figure stays as typed until submission so
    /// a half-entered "0." never becomes a zero behind the counter's back.</summary>
    public sealed partial class CountLine : ObservableObject
    {
        public required InventoryOverviewRowDto Item { get; init; }
        public required Action Changed { get; init; }

        [ObservableProperty] private string _countedText = "";
        [ObservableProperty] private string? _result;

        partial void OnCountedTextChanged(string value)
        {
            OnPropertyChanged(nameof(VarianceDisplay));
            OnPropertyChanged(nameof(VarianceValueDisplay));
            OnPropertyChanged(nameof(HasVariance));
            OnPropertyChanged(nameof(IsShort));
            Changed();
        }

        public string Name => Item.Name;
        public string SystemDisplay => $"{MilliUnits.Format(Item.BalanceMilliUnits)} {Item.Unit}";

        public long? CountedMilliUnits =>
            MilliUnits.TryParse(CountedText, out var counted) && CountedText.Trim().Length > 0 ? counted : null;

        public long? VarianceMilliUnits =>
            CountedMilliUnits is { } counted ? counted - Item.BalanceMilliUnits : null;

        public bool HasVariance => VarianceMilliUnits is { } v && v != 0;
        public bool IsShort => VarianceMilliUnits is { } v && v < 0;

        public string VarianceDisplay =>
            VarianceMilliUnits is not { } v ? ""
            : v == 0 ? "مطابق"
            : (v > 0 ? "زيادة " : "عجز ") + MilliUnits.Format(Math.Abs(v)) + " " + Item.Unit;

        /// <summary>What the difference is worth. Blank - never zero - when the item has no cost
        /// basis, because a shortage of something unpriced is a real shortage of unknown value.</summary>
        public string VarianceValueDisplay =>
            VarianceMilliUnits is not { } v || v == 0 ? ""
            : Item.UnitCostPiastres is not { } cost ? "قيمة غير معروفة"
            : CashInput.Format(Math.Abs(v) * cost / 1000) + " ج";
    }

    public ObservableCollection<CountLine> CountSheet { get; } = new();

    [ObservableProperty] private bool _isCounting;
    [ObservableProperty] private string _countSheetReason = "";
    [ObservableProperty] private string _countSheetManagerPin = "";
    [ObservableProperty] private string? _countSheetError;
    [ObservableProperty] private string? _countSheetSummary;

    private string _countSheetId = "";

    public bool CountSheetNeedsApproval => !CanManage;

    public int CountSheetDifferenceCount => CountSheet.Count(l => l.HasVariance);
    public int CountSheetEnteredCount => CountSheet.Count(l => l.CountedMilliUnits is not null);

    public string CountSheetProgressLabel =>
        $"تم إدخال {CountSheetEnteredCount} من {CountSheet.Count} · فروق: {CountSheetDifferenceCount}";

    /// <summary>The value the sheet is about to move, and how much of it cannot be valued.</summary>
    public string CountSheetValueLabel
    {
        get
        {
            var priced = CountSheet.Where(l => l.HasVariance && l.Item.UnitCostPiastres is not null).ToList();
            var unpriced = CountSheet.Count(l => l.HasVariance && l.Item.UnitCostPiastres is null);
            if (priced.Count == 0 && unpriced == 0) return "";

            var net = priced.Sum(l => l.VarianceMilliUnits!.Value * l.Item.UnitCostPiastres!.Value / 1000);
            var text = $"أثر القيمة: {(net >= 0 ? "+" : "−")}{CashInput.Format(Math.Abs(net))} ج";
            return unpriced > 0 ? text + $" (بدون {unpriced} صنفًا بلا تكلفة معروفة)" : text;
        }
    }

    [RelayCommand]
    private void StartCountSheet()
    {
        if (!CanEditAction) return;
        _countSheetId = $"sheet-{Guid.NewGuid():N}";
        CountSheetReason = "";
        CountSheetManagerPin = "";
        CountSheetError = null;
        CountSheetSummary = null;
        IsCounting = true;
        SelectedItem = null;
        RebuildCountSheet();
        OnPropertyChanged(nameof(CountSheetNeedsApproval));
    }

    [RelayCommand]
    private void CancelCountSheet()
    {
        if (!CanEditAction) return;
        IsCounting = false;
        CountSheet.Clear();
        CountSheetError = null;
    }

    private void RebuildCountSheet()
    {
        // Only the rows currently in view: a count is done shelf by shelf, and the search box is how
        // the counter picks the shelf. Lines already typed keep their figures.
        var typed = CountSheet.ToDictionary(l => l.Item.Id, l => l.CountedText);
        CountSheet.Clear();
        foreach (var item in Items)
            CountSheet.Add(new CountLine
            {
                Item = item,
                Changed = OnCountSheetChanged,
                CountedText = typed.GetValueOrDefault(item.Id, ""),
            });
        OnCountSheetChanged();
    }

    private void OnCountSheetChanged()
    {
        OnPropertyChanged(nameof(CountSheetDifferenceCount));
        OnPropertyChanged(nameof(CountSheetEnteredCount));
        OnPropertyChanged(nameof(CountSheetProgressLabel));
        OnPropertyChanged(nameof(CountSheetValueLabel));
    }

    [RelayCommand]
    private async Task SubmitCountSheetAsync()
    {
        if (IsBusy || !IsCounting) return;

        var differing = CountSheet.Where(l => l.HasVariance).ToList();
        if (differing.Count == 0)
        {
            CountSheetError = "لا يوجد فرق لتسجيله. اكتب الكميات المعدودة أولًا.";
            return;
        }
        if (string.IsNullOrWhiteSpace(CountSheetReason))
        {
            CountSheetError = "سبب الجرد إلزامي.";
            return;
        }

        IsBusy = true;
        CountSheetError = null;
        try
        {
            // One approval for the sheet, not one per line - the manager is authorising this count,
            // and asking for the PIN once per shelf item would train everyone to hand it over.
            var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                CountSheetNeedsApproval, CountSheetManagerPin, "inventory_manage", CountSheetReason,
                e => CountSheetError = e);
            if (!ok) return;

            var posted = 0;
            var failed = 0;
            foreach (var line in differing)
            {
                var difference = line.VarianceMilliUnits!.Value;
                // Derived from the sheet and the item, so resubmitting after a failure replays the
                // same request rather than posting a second adjustment for the same shelf.
                var requestId = $"{_countSheetId}-{line.Item.Id}";
                var result = await _session.Api.RecordCountAdjustmentAsync(new RecordCountAdjustmentRequest(
                    requestId, line.Item.Id, Math.Abs(difference), difference > 0 ? "In" : "Out",
                    CountSheetReason, _session.CurrentUser!.Id, approvalId, approverId));

                if (result.Success && result.Value is not null) { line.Result = "تم"; posted++; }
                else { line.Result = result.Error ?? "فشل"; failed++; }
            }

            CountSheetSummary = failed == 0
                ? $"تم تسجيل {posted} تسوية جرد."
                : $"تم تسجيل {posted} تسوية، وفشل {failed} - أعد الإرسال، لن تتكرر التسويات الناجحة.";

            if (failed == 0)
            {
                IsCounting = false;
                CountSheet.Clear();
                ActionNote = CountSheetSummary;
            }
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    // ================================ single-item count ================================

    [ObservableProperty] private InventoryOverviewRowDto? _countTarget;
    [ObservableProperty] private string _countActualText = "";
    [ObservableProperty] private string _countReason = "";
    [ObservableProperty] private string _countManagerPin = "";
    [ObservableProperty] private string? _countError;
    public bool IsCountOpen => CountTarget is not null;
    partial void OnCountTargetChanged(InventoryOverviewRowDto? value) => OnPropertyChanged(nameof(IsCountOpen));

    private string? _countRequestId;

    public bool CountNeedsApproval => !CanManage;

    /// <summary>What the correction will post, shown before it is submitted: the server records the
    /// human's observed difference, so the operator should see the difference they are about to
    /// assert rather than discovering it afterwards in the movement log.</summary>
    public string CountDifferenceLabel
    {
        get
        {
            if (CountTarget is null || !MilliUnits.TryParse(CountActualText, out var actual)) return "";
            var diff = actual - CountTarget.BalanceMilliUnits;
            if (diff == 0) return "لا يوجد فرق عن رصيد النظام.";
            var direction = diff > 0 ? "زيادة" : "عجز";
            return $"{direction} {MilliUnits.Format(Math.Abs(diff))} {CountTarget.Unit}";
        }
    }

    partial void OnCountActualTextChanged(string value) => OnPropertyChanged(nameof(CountDifferenceLabel));

    [RelayCommand]
    private void OpenCount(InventoryOverviewRowDto item)
    {
        if (!CanEditAction) return;
        CountError = null;
        CountActualText = MilliUnits.Format(item.BalanceMilliUnits);
        CountReason = "";
        CountManagerPin = "";
        _countRequestId = $"cnt-{Guid.NewGuid():N}";
        CountTarget = item;
        OnPropertyChanged(nameof(CountNeedsApproval));
        OnPropertyChanged(nameof(CountDifferenceLabel));
    }

    [RelayCommand]
    private void CloseCount() { if (CanEditAction) CountTarget = null; }

    [RelayCommand]
    private async Task SubmitCountAsync()
    {
        if (IsBusy || _wasteRequest.HasPending) return;
        if (_countRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompleteCountAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        if (CountTarget is null) return;
        if (!MilliUnits.TryParse(CountActualText, out var actual))
        {
            CountError = "أدخل الكمية المعدودة بشكل صحيح.";
            return;
        }
        if (string.IsNullOrWhiteSpace(CountReason))
        {
            CountError = "سبب التسوية إلزامي.";
            return;
        }

        var difference = actual - CountTarget.BalanceMilliUnits;
        if (difference == 0)
        {
            CountError = "لا يوجد فرق لتسجيله.";
            return;
        }

        IsBusy = true;
        CountError = null;
        try
        {
            var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                CountNeedsApproval, CountManagerPin, "inventory_manage", CountReason, e => CountError = e);
            if (!ok) return;

            await CompleteCountAsync(new RecordCountAdjustmentRequest(
                _countRequestId!, CountTarget.Id, Math.Abs(difference), difference > 0 ? "In" : "Out",
                CountReason, _session.CurrentUser!.Id, approvalId, approverId));
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteCountAsync(RecordCountAdjustmentRequest request)
    {
        CountError = null;
        var result = await _countRequest.SendAsync(request, r => _session.Api.RecordCountAdjustmentAsync(r));
        if (!result.Success || result.Value is null)
        {
            CountError = _countRequest.FailureMessage(result.Error);
            return;
        }
        ActionNote = "تم تسجيل تسوية الجرد.";
        CountTarget = null;
        _countRequestId = null;
        await LoadAsync();
    }

    // ================================ waste ================================

    [ObservableProperty] private InventoryOverviewRowDto? _wasteTarget;
    [ObservableProperty] private string _wasteQuantityText = "";
    [ObservableProperty] private string _wasteType = "Waste";
    [ObservableProperty] private string _wasteReason = "";
    [ObservableProperty] private string _wasteManagerPin = "";
    [ObservableProperty] private string? _wasteError;
    public bool IsWasteOpen => WasteTarget is not null;
    partial void OnWasteTargetChanged(InventoryOverviewRowDto? value) => OnPropertyChanged(nameof(IsWasteOpen));

    private string? _wasteRequestId;

    public bool WasteNeedsApproval => !CanManage;

    [RelayCommand]
    private void OpenWaste(InventoryOverviewRowDto item)
    {
        if (!CanEditAction) return;
        WasteError = null;
        WasteQuantityText = "";
        WasteType = "Waste";
        WasteReason = "";
        WasteManagerPin = "";
        _wasteRequestId = $"wst-{Guid.NewGuid():N}";
        WasteTarget = item;
        OnPropertyChanged(nameof(WasteNeedsApproval));
    }

    [RelayCommand]
    private void CloseWaste() { if (CanEditAction) WasteTarget = null; }

    [RelayCommand]
    private void SetWasteType(string type) { if (CanEditAction) WasteType = type; }

    [RelayCommand]
    private async Task SubmitWasteAsync()
    {
        if (IsBusy || _countRequest.HasPending) return;
        if (_wasteRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompleteWasteAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        if (WasteTarget is null) return;
        if (!MilliUnits.TryParse(WasteQuantityText, out var quantity) || quantity <= 0)
        {
            WasteError = "أدخل الكمية التالفة بشكل صحيح.";
            return;
        }
        if (string.IsNullOrWhiteSpace(WasteReason))
        {
            WasteError = "سبب الفاقد إلزامي.";
            return;
        }

        IsBusy = true;
        WasteError = null;
        try
        {
            var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                WasteNeedsApproval, WasteManagerPin, "inventory_manage", WasteReason, e => WasteError = e);
            if (!ok) return;

            await CompleteWasteAsync(new RecordWasteRequest(
                _wasteRequestId!, WasteTarget.Id, quantity, WasteType, WasteReason,
                _session.CurrentUser!.Id, approvalId, approverId));
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteWasteAsync(RecordWasteRequest request)
    {
        WasteError = null;
        var result = await _wasteRequest.SendAsync(request, r => _session.Api.RecordWasteAsync(r));
        if (!result.Success || result.Value is null)
        {
            WasteError = _wasteRequest.FailureMessage(result.Error);
            return;
        }

        // "Unknown cost is not zero": an item with no purchase history posts the loss with a
        // quantity but no value, and the message must not imply a value was calculated.
        ActionNote = result.Value.LossValuePiastres is { } loss
            ? $"تم تسجيل الفاقد بقيمة {CashInput.Format(loss)} ج."
            : "تم تسجيل الفاقد (لا توجد تكلفة شراء سابقة لتقييمه).";
        WasteTarget = null;
        _wasteRequestId = null;
        await LoadAsync();
    }

    private async Task<(string? ApprovalId, string? ApproverUserId, bool Ok)> ResolveApprovalAsync(
        bool needsApproval, string managerPin, string permission, string reason, Action<string> onError)
    {
        if (!needsApproval) return (null, null, true);
        if (managerPin.Length < 4)
        {
            onError("أدخل رمز المدير المخوّل باعتماد هذا الإجراء.");
            return (null, null, false);
        }
        // One call, not two: the server checks the PIN and decides who approved. Verifying it
        // here and then naming the manager made the check a formality anything could skip.
        var approval = await _session.Api.IssueManagerApprovalAsync(
            new Falah.Application.Approvals.IssueManagerApprovalRequest(managerPin, permission, reason));
        if (!approval.Success || approval.Value is null)
        {
            onError(approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل باعتماد هذا الإجراء.");
            return (null, null, false);
        }

        return (approval.Value.ApprovalId, approval.Value.ApproverUserId, true);
    }
}
