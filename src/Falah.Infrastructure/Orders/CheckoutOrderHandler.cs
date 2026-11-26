namespace Falah.Infrastructure.Orders;

using Falah.Application.Orders;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Pricing;
using Falah.Domain.Shifts;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The full Checkout/Create-Order use case: the one Application-layer orchestration this phase
/// implements end to end. Composes the pure Domain rules (CheckoutRules, PricingRules,
/// AuthorizationPolicy) with EF Core persistence, inside ONE database transaction, so an order,
/// its payment movement(s), and its idempotency journal entry are committed together or not at
/// all - there is no window in which a half-written order can exist.
/// </summary>
public sealed class CheckoutOrderHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly MenuPricingService _pricing;
    private readonly InventoryDeductionService _inventory;
    private readonly IAuditLog _auditLog;

    public CheckoutOrderHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, MenuPricingService pricing, InventoryDeductionService inventory, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _pricing = pricing;
        _inventory = inventory;
        _auditLog = auditLog;
    }

    public async Task<CheckoutOrderResult> HandleAsync(CheckoutOrderRequest request, CancellationToken ct = default)
    {
        // Fast idempotent-replay path first, no transaction needed for a pure read: a same-task
        // retry or a network retry that already succeeded gets back the exact same order.
        // QA-03 (audit 2026-11-23): checked BEFORE the replay lookup - an empty id is no key at all, and
        // looking it up made a second, different sale return the first one and never be recorded.
        InputRules.AssertRequestId(request.CheckoutRequestId);
        var existing = await FindExistingAsync(request.CheckoutRequestId, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
            ?? throw new ShiftNotOpenException("الوردية غير موجودة.");
        if (shift.Status != nameof(ShiftStatus.Open))
            throw new ShiftNotOpenException("يجب فتح وردية قبل تسجيل أي عملية بيع.");

        var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
            ?? throw new InvalidOperationException("الكاشير غير موجود.");
        var cashier = ToActor(cashierUser);

        var now0 = DateTimeOffset.UtcNow;
        // Owner, 2026-11-25: an offer may be limited to order types and stations - both known before pricing.
        var station = PricingRules.StationOf(request.CustomerPrintRoute);
        var resolvedItems = await _pricing.ResolveAsync(request.Items, now0, OrderEnumMapping.ParseOrderType(request.OrderType), station, ct);
        var items = resolvedItems.Select(r => r.Item).ToList();

        var taxRuleEntity = await _db.TaxRules.AsNoTracking().FirstOrDefaultAsync(t => t.Enabled, ct);
        var taxRule = taxRuleEntity is null ? null
            : new TaxRule(taxRuleEntity.Id, taxRuleEntity.Name, taxRuleEntity.RateBasisPoints,
                Enum.TryParse<TaxMode>(taxRuleEntity.Mode, true, out var savedMode) && Enum.IsDefined(savedMode) ? savedMode : throw new DomainException("نوع الضريبة المحفوظ غير صالح؛ راجع إعدادات الضرائب."), taxRuleEntity.Enabled);

        var discount = Money.FromPiastres(request.DiscountPiastres);
        var deliveryFee = Money.FromPiastres(request.DeliveryFeePiastres);
        var compAmount = Money.FromPiastres(request.CompAmountPiastres);
        var amountPaid = Money.FromPiastres(request.AmountPaidPiastres);
        var orderType = OrderEnumMapping.ParseOrderType(request.OrderType);
        var paymentMethod = OrderEnumMapping.ParsePaymentMethod(request.PaymentMethod);
        var splits = request.SplitPayments?
            .Select(s => new PaymentSplit(OrderEnumMapping.ParsePaymentMethod(s.Method), Money.FromPiastres(s.AmountPiastres)))
            .ToList();

        // Ported from checkout()'s own re-validation in lib/store.tsx: a delivery order without a
        // complete customer/address is rejected here too, not just at the pre-checkout "save
        // delivery details" form step - Call Center (and any future client) submits straight to
        // this endpoint with no such form gate of its own, so the server must be the one place this
        // is actually enforced.
        string? composedDeliveryAddress = null;
        if (orderType == OrderType.Delivery)
        {
            var missing = DeliveryRules.FirstMissingDeliveryField(
                request.CustomerPhone, request.CustomerName, request.DeliveryServiceId, request.DeliveryArea, request.DeliveryStreet);
            if (missing is not null) throw new DomainException(missing);

            composedDeliveryAddress = DeliveryRules.ComposeDeliveryAddress(
                request.DeliveryArea, request.DeliveryStreet, request.DeliveryBuilding, request.DeliveryFloor,
                request.DeliveryApartment, request.DeliveryLandmark);
        }
        else if (orderType == OrderType.Pickup)
        {
            // Enforced here for the same reason delivery is, spelled out above: the POS form is not
            // the only way an order reaches this endpoint, so a pickup with nobody's name on it must
            // be refused by the server rather than by whichever client happens to have a form.
            var missing = DeliveryRules.FirstMissingPickupField(request.CustomerPhone, request.CustomerName);
            if (missing is not null) throw new DomainException(missing);
        }

        // QA-11 (audit 2026-11-23): customer details print on tickets and fit their columns.
        InputRules.AssertMaxLength(request.CustomerName?.Trim(), InputRules.MaxNameLength, "اسم العميل");
        InputRules.AssertMaxLength(request.CustomerPhone?.Trim(), 30, "رقم هاتف العميل");

        // AUD-01: the fee (and the service name frozen with it) is the configured zone's, never the client's.
        DeliveryRules.DeliveryServiceTerms? serviceTerms = null;
        string? deliveryServiceName = null;
        if (orderType == OrderType.Delivery)
        {
            var service = await _db.DeliveryServices.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.DeliveryServiceId, ct);
            if (service is not null)
            {
                serviceTerms = new DeliveryRules.DeliveryServiceTerms(service.Active, Money.FromPiastres(service.FeePiastres ?? 0));
                deliveryServiceName = service.Name;
            }
        }
        deliveryFee = DeliveryRules.ResolveDeliveryFee(orderType, deliveryFee, serviceTerms);

        CheckoutRules.AssertRequestedPickupTime(orderType, request.RequestedPickupAtUtc, DateTimeOffset.UtcNow);

        // Ported from checkout() in lib/store.tsx: 'pos' is the base gate on EVERY checkout,
        // unconditionally, before any comp logic runs at all - a comped checkout is still a
        // checkout. A real gap found during the legacy-parity audit: this used to be checked only
        // in the no-comp branch, so a comped order needed just orders_comp and silently skipped the
        // base 'pos' requirement entirely.
        if (!AuthorizationPolicy.HasPermission(cashier, "pos"))
            throw new DomainException("ليس لديك صلاحية تسجيل عملية بيع.");

        // Ported from store.tsx's own discount gate: a manual discount &gt; 0 requires orders_discount
        // - previously enforced nowhere server-side in this port despite the field being fully live.
        if (discount.IsPositive && !AuthorizationPolicy.HasPermission(cashier, "orders_discount"))
            throw new DomainException("ليس لديك صلاحية تطبيق خصم يدوي.");

        // Owner, 2026-11-18: orders_discount covers a discount up to the Settings limit (10% of the
        // items unless changed). Above it a manager's approval is spent on the order, like a comp's -
        // before this, orders_discount alone could take 100% off a sale a comp of which needed a manager.
        string? discountApprovedByName = null;
        if (discount.IsPositive)
        {
            var itemsValue = CheckoutRules.ComputeSubtotal(items);
            if (discount > itemsValue) throw new DomainException("الخصم لا يمكن أن يتجاوز قيمة الأصناف.");
            var limit = await _db.Settings.AsNoTracking()
                .Where(s => s.Id == SettingsEntity.SingletonId)
                .Select(s => s.MaxDiscountWithoutApprovalBasisPoints)
                .FirstOrDefaultAsync(ct) ?? PricingRules.DefaultMaxDiscountWithoutApprovalBasisPoints;
            if (PricingRules.DiscountNeedsApproval(discount, itemsValue, limit)
                && AuthorizationPolicy.CanIssueApproval(cashier, "orders_discount"))
            {
                // The person the limit asks for is at the till: they could issue this approval to
                // themselves with their own PIN, so they are named as its authoriser instead.
                discountApprovedByName = cashierUser.Name;
            }
            else if (PricingRules.DiscountNeedsApproval(discount, itemsValue, limit))
            {
                if (string.IsNullOrEmpty(request.DiscountApprovalId) || string.IsNullOrEmpty(request.DiscountApproverUserId))
                    throw new DomainException($"خصم أكبر من {limit / 100m:0.##}% من قيمة الأصناف يتطلب اعتماد مدير.");
                var discountApprover = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.DiscountApproverUserId, ct)
                    ?? throw new DomainException("المعتمد غير موجود.");
                AuthorizationPolicy.ValidateApproval(_approvalRegistry, "orders_discount",
                    new ManagerApproval(request.DiscountApprovalId, discountApprover.Id, discountApprover.Name,
                        discountApprover.Role, DateTimeOffset.UtcNow, "orders_discount", null), cashier.Id);
                discountApprovedByName = discountApprover.Name;
            }
        }

        string? compApprovedByName = null;
        if (compAmount.IsPositive)
        {
            var preCompSubtotal = CheckoutRules.ComputeSubtotal(items);
            var preCompTaxable = preCompSubtotal - discount;
            if (preCompTaxable.IsNegative) preCompTaxable = Money.Zero;
            var orderValueBeforeComp = TaxEngine.Calculate(preCompTaxable, taxRule).Final + deliveryFee;
            PricingRules.AssertCompAmount(compAmount, orderValueBeforeComp);
            PricingRules.AssertCompReasonCode(request.CompReasonCode);

            if (string.IsNullOrEmpty(request.CompApprovalId) || string.IsNullOrEmpty(request.CompApproverUserId))
                throw new DomainException("الضيافة تتطلب اعتماد مدير.");
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CompApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            // The approval is looked up in the persisted registry (EfApprovalRegistry), so it survives
            // a Server restart between the manager granting it and this checkout spending it. It is
            // single-use, must match the orders_comp action, and only a role allowed to issue it can.
            //
            // Validated for EVERY cashier, including one who holds orders_comp: the order and the
            // audit trail name this approver, so the approval has to be real. Holding the permission
            // used to skip the check entirely and record whatever approver the request named.
            var approval = new ManagerApproval(request.CompApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "orders_comp", request.CompReason);
            AuthorizationPolicy.ValidateApproval(_approvalRegistry, "orders_comp", approval, cashier.Id);
            compApprovedByName = approverUser.Name;
        }

        var plan = CheckoutRules.BuildPlan(orderType, paymentMethod, items, discount, taxRule,
            deliveryFee, compAmount, amountPaid, splits, request.WalletProvider, request.PayOnCollection);

        // Owner, 2026-11-25: a gift is earned by what the customer actually pays (with or without the delivery fee, as
        // the offer says) - the highest tier reached, its product and size, no more than its quantity. The gift lines
        // are 0, so they never count towards their own threshold.
        foreach (var gift in resolvedItems.Where(r => r.GiftRule is not null).GroupBy(r => r.GiftRule!.Id))
        {
            var rule = gift.First().GiftRule!;
            var basis = PricingRules.GiftBasis(rule, plan.PayableTotal, plan.DeliveryFee);
            var tier = PricingRules.GiftTierFor(rule, basis)
                ?? throw new DomainException($"الطلب ما وصلش لمبلغ هدية «{rule.Name}» ({rule.GiftTiers.Min(t => t.Threshold.Piastres) / 100m:0.##} ج). شيل الهدية.");
            var lines = gift.Select(r => r.Item).ToList();
            if (lines.Any(l => l.ProductId != tier.ProductId || (tier.VariantId is not null && l.VariantId != tier.VariantId)))
                throw new DomainException($"هدية «{rule.Name}» للطلب ده {tier.Quantity} من صنف تاني - حدّث الهدية.");
            if (lines.Sum(l => l.Quantity) > tier.Quantity)
                throw new DomainException($"هدية «{rule.Name}» للطلب ده {tier.Quantity} بس.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await Falah.Infrastructure.BusinessDay.FinancialAdmission.AcquireAsync(_db, ct);
            var admissionReplay = await FindExistingAsync(request.CheckoutRequestId, ct);
            if (admissionReplay is not null) return ToResult(admissionReplay, wasReplayed: true);
            var businessDayId = await Falah.Infrastructure.BusinessDay.BusinessDayGate.RequireOpenAsync(_db, ct);
            var currentShift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct);
            if (currentShift?.Status != nameof(ShiftStatus.Open))
                throw new ShiftNotOpenException("يجب فتح وردية قبل تسجيل أي عملية بيع.");
            // Owner, 2026-11-25: an offer's daily / overall limit, counted inside the admission lock every sale takes,
            // so two tills cannot both sell its last pieces.
            var limited = resolvedItems.Select(r => r.OfferRule ?? r.GiftRule).Where(r => r is { MaxPerDay: not null } or { MaxTotal: not null })
                .DistinctBy(r => r!.Id).Select(r => r!).ToList();
            if (limited.Count > 0)
            {
                var sold = await Falah.Infrastructure.Pricing.OfferUsage.SoldAsync(_db, limited, now0, ct);
                foreach (var rule in limited)
                {
                    var here = resolvedItems.Where(r => (r.OfferRule ?? r.GiftRule)?.Id == rule.Id).Sum(r => r.Item.Quantity);
                    var s = sold.GetValueOrDefault(rule.Id);
                    if (rule.MaxPerDay is { } day && s.Today + here > day)
                        throw new DomainException($"العرض «{rule.Name}» فاضل منه {Math.Max(0, day - s.Today)} قطعة النهارده بس.");
                    if (rule.MaxTotal is { } total && s.Total + here > total)
                        throw new DomainException($"العرض «{rule.Name}» فاضل منه {Math.Max(0, total - s.Total)} قطعة بس.");
                }
            }
            var saleKey = $"sale-{request.CheckoutRequestId}";
            if (!plan.DefersAllMoney)
            {
                var journal = new EfFinancialJournal(_db);
                if (!journal.TryStart(saleKey, "sale"))
                    throw new CheckoutConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");
            }

            var sequence = await OrderSequenceService.AllocateNextAsync(_db, ct);
            var orderId = $"ord-{Guid.NewGuid():N}";
            var orderNumber = FinancialRules.FormatOrderNumber(sequence);
            var now = DateTimeOffset.UtcNow;

            // Server-resolved, never trusted from the client (request.BusinessDayId is accepted on
            // the wire only for source-compat and otherwise ignored) - see BusinessDayGate.
            // Admission above rechecks the open day and shift inside this transaction.

            var order = Order.Create(plan, orderId, orderNumber, sequence, request.CashierId, request.ShiftId,
                items, request.CheckoutRequestId, businessDayId, now,
                request.CustomerName, request.CustomerPhone, composedDeliveryAddress, request.DeliveryArea, discount,
                compAmount.IsPositive ? request.CompReasonCode : null,
                compApprovedByName, compAmount.IsPositive ? request.CompApprovalId : null,
                taxRule?.RateBasisPoints, taxRule?.Mode.ToString(), taxRule?.Name);

            var orderEntity = ToEntity(order);
            // Structured delivery breakdown - kept off the Order Domain aggregate entirely (no
            // business rule of Order's own reads them, they are pure display/navigation data for a
            // driver/receipt/Call Center), set directly on the persistence row exactly like
            // Delivery's own side-effect row below.
            orderEntity.DeliveryStreet = request.DeliveryStreet;
            orderEntity.DeliveryBuilding = request.DeliveryBuilding;
            orderEntity.DeliveryFloor = request.DeliveryFloor;
            orderEntity.DeliveryApartment = request.DeliveryApartment;
            orderEntity.DeliveryLandmark = request.DeliveryLandmark;
            orderEntity.DeliveryNotes = request.DeliveryNotes;
            orderEntity.DeliveryServiceId = request.DeliveryServiceId;
            orderEntity.DeliveryServiceName = orderType == OrderType.Delivery ? deliveryServiceName : request.DeliveryServiceName;

            // No order type has a table any more - DineIn was the only one, and it is gone. The
            // column and the request field stay so the orders written before the change still read
            // back with the table they were served at; nothing new is ever written into it.
            orderEntity.TableNumber = null;
            orderEntity.RequestedPickupAtUtc = orderType == OrderType.Pickup ? request.RequestedPickupAtUtc : null;
            // UI-0025: registered and sent to the kitchen now; paid, and only then a sale, at collection.
            orderEntity.AwaitingCollection = plan.PayOnCollection;

            _db.Orders.Add(orderEntity);

            if (compAmount.IsPositive)
            {
                _auditLog.Append(new AuditEvent(
                    $"audit-{Guid.NewGuid():N}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
                    "ضيافة على طلب", "order", $"ضيافة {compAmount.Piastres} قرش على طلب {orderNumber} - {request.CompReason}",
                    "critical", now, ShiftId: request.ShiftId, Reason: request.CompReason, AuthorizedBy: compApprovedByName,
                    ReferenceId: orderId));
            }

            // A confirmed audit gap: a manual discount is gated by orders_discount above but, unlike
            // its sibling comp branch, was never written to the audit trail - a discounted sale and a
            // full-price sale looked identical in "سجل المراجعة". Id is deterministic on orderId (not
            // a random Guid) so this line is naturally idempotent-safe with the rest of this handler:
            // a retry that reaches this point only does so once, since FindExistingAsync's replay
            // short-circuit at the top of HandleAsync returns before this code runs again.
            if (discount.IsPositive)
            {
                _auditLog.Append(new AuditEvent(
                    $"audit-discount-{orderId}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
                    "خصم يدوي على طلب", "order", $"خصم {discount.Piastres} قرش على طلب {orderNumber}",
                    discountApprovedByName is null ? "normal" : "warning", now, ShiftId: request.ShiftId,
                    AuthorizedBy: discountApprovedByName, ReferenceId: orderId));
            }

            // Ported from registerDeliveryOrder in the reference app: every delivery-type order gets
            // exactly one Delivery row, created here rather than by a separate call, since this
            // system has no other path that produces a delivery order. Starts 'New' - dispatch
            // cannot assign a driver until it's explicitly marked 'ready' (POST /api/delivery/{id}/
            // ready), matching Falah.Domain.Delivery.DeliveryTransitions' own tested contract.
            if (order.OrderType == OrderType.Delivery)
            {
                _db.Deliveries.Add(new DeliveryEntity
                {
                    Id = $"delivery-{orderId}", OrderId = orderId, Status = nameof(DeliveryStatus.New),
                    ShiftId = order.ShiftId, CreatedBy = cashierUser.Name, CreatedAtUtc = now,
                });

                // Owner, 2026-11-17: a delivery order prints ONE paper into the restaurant when it is
                // taken (the station's own customer copy - the call center's prep slip) and the
                // customer's full receipt when the driver collects it (DriverDispatch). The separate
                // DeliveryOrder ticket that used to be queued here came out beside the prep slip, so
                // two papers printed at once; it is no longer queued. The route and its composer stay,
                // so an older job already on record can still be printed or reprinted.
            }

            // Stock leaves the kitchen the moment the order is made, independent of whether the
            // money settles now or later (cash-on-delivery, a pending electronic transfer) - same
            // unconditional placement as the current app's checkout flow.
            await _inventory.DeductForOrderAsync(orderId, request.OrderType, resolvedItems, cashierUser.Name, now, ct);

            if (!plan.DefersAllMoney)
            {
                if (paymentMethod == PaymentMethod.Multi && splits is not null)
                {
                    var idx = 0;
                    foreach (var split in splits)
                    {
                        if (split.Method is PaymentMethod.InstaPay or PaymentMethod.Wallet) { idx++; continue; }
                        var movement = TreasuryMovementFactory.BuildSaleMovement($"tm-{Guid.NewGuid():N}",
                            $"op-{Guid.NewGuid():N}", $"{request.CheckoutRequestId}-{idx}", orderId,
                            split.Method, split.Amount, businessDayId, request.ShiftId, cashierUser.Name, now);
                        _db.TreasuryMovements.Add(ToEntity(movement));
                        idx++;
                    }
                }
                else
                {
                    var movement = TreasuryMovementFactory.BuildSaleMovement($"tm-{Guid.NewGuid():N}",
                        $"op-{Guid.NewGuid():N}", request.CheckoutRequestId, orderId,
                        paymentMethod, plan.PayableTotal, businessDayId, request.ShiftId, cashierUser.Name, now);
                    _db.TreasuryMovements.Add(ToEntity(movement));
                }
                new EfFinancialJournal(_db).Complete(saleKey);
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToResult(orderEntity, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or CheckoutConflictException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await FindExistingAsync(request.CheckoutRequestId, ct);
            if (replay is not null) return ToResult(replay, wasReplayed: true);
            throw new CheckoutConflictException(
                "تعذر إتمام العملية بسبب تعارض في مفتاح منع التكرار، ولا يوجد طلب سابق لإعادته - يحتاج مراجعة يدوية.");
        }
    }

    private async Task<OrderEntity?> FindExistingAsync(string checkoutRequestId, CancellationToken ct) =>
        await _db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.CheckoutRequestId == checkoutRequestId, ct);

    private static Actor ToActor(UserEntity user) => OrderMapper.ToActor(user);

    private static OrderEntity ToEntity(Order order) => OrderMapper.ToEntity(order);

    private static TreasuryMovementEntity ToEntity(TreasuryMovement movement) => OrderMapper.ToEntity(movement);

    private static CheckoutOrderResult ToResult(OrderEntity entity, bool wasReplayed) => new(
        entity.Id, entity.OrderNumber, entity.SequenceNumber, entity.TotalPiastres, entity.AmountPaidPiastres,
        entity.ChangeDuePiastres, entity.Status, entity.IsPaid, entity.ElectronicPaymentStatus, wasReplayed,
        AwaitingCollection: entity.AwaitingCollection);
}
