namespace Falah.Infrastructure.Orders;

using Falah.Application.Orders;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
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

    public CheckoutOrderHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, MenuPricingService pricing, InventoryDeductionService inventory)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _pricing = pricing;
        _inventory = inventory;
    }

    public async Task<CheckoutOrderResult> HandleAsync(CheckoutOrderRequest request, CancellationToken ct = default)
    {
        // Fast idempotent-replay path first, no transaction needed for a pure read: a same-task
        // retry or a network retry that already succeeded gets back the exact same order.
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
        var resolvedItems = await _pricing.ResolveAsync(request.Items, now0, ct);
        var items = resolvedItems.Select(r => r.Item).ToList();

        var taxRuleEntity = await _db.TaxRules.AsNoTracking().FirstOrDefaultAsync(t => t.Enabled, ct);
        var taxRule = taxRuleEntity is null ? null
            : new TaxRule(taxRuleEntity.Id, taxRuleEntity.Name, taxRuleEntity.RateBasisPoints,
                taxRuleEntity.Mode == "Inclusive" ? TaxMode.Inclusive : TaxMode.Exclusive, taxRuleEntity.Enabled);

        var discount = Money.FromPiastres(request.DiscountPiastres);
        var deliveryFee = Money.FromPiastres(request.DeliveryFeePiastres);
        var compAmount = Money.FromPiastres(request.CompAmountPiastres);
        var amountPaid = Money.FromPiastres(request.AmountPaidPiastres);
        var orderType = OrderEnumMapping.ParseOrderType(request.OrderType);
        var paymentMethod = OrderEnumMapping.ParsePaymentMethod(request.PaymentMethod);
        var splits = request.SplitPayments?
            .Select(s => new PaymentSplit(OrderEnumMapping.ParsePaymentMethod(s.Method), Money.FromPiastres(s.AmountPiastres)))
            .ToList();

        if (compAmount.IsPositive)
        {
            var preCompSubtotal = CheckoutRules.ComputeSubtotal(items);
            var preCompTaxable = preCompSubtotal - discount;
            if (preCompTaxable.IsNegative) preCompTaxable = Money.Zero;
            var orderValueBeforeComp = TaxEngine.Calculate(preCompTaxable, taxRule).Final + deliveryFee;
            PricingRules.AssertCompAmount(compAmount, orderValueBeforeComp);

            if (string.IsNullOrEmpty(request.CompApprovalId) || string.IsNullOrEmpty(request.CompApproverUserId))
                throw new DomainException("الضيافة تتطلب اعتماد مدير.");
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CompApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            // NOTE: the approval registry is in-memory only in this slice (does not survive a
            // Server restart) - see the TODO in docs/PROGRESS.md. Comp authorization is otherwise
            // fully enforced (single-use, action-matched, role-gated issuance).
            var approval = new ManagerApproval(request.CompApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "orders_comp", request.CompReason);
            AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, cashier, "orders_comp", approval, requireApproval: true);
        }
        else if (!AuthorizationPolicy.HasPermission(cashier, "pos"))
        {
            throw new DomainException("ليس لديك صلاحية تسجيل عملية بيع.");
        }

        var plan = CheckoutRules.BuildPlan(orderType, paymentMethod, items, discount, taxRule,
            deliveryFee, compAmount, amountPaid, splits, request.WalletProvider);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
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

            var order = Order.Create(plan, orderId, orderNumber, sequence, request.CashierId, request.ShiftId,
                items, request.CheckoutRequestId, request.BusinessDayId, now,
                request.CustomerName, request.CustomerPhone, request.DeliveryAddress, request.DeliveryArea);

            var orderEntity = ToEntity(order);
            _db.Orders.Add(orderEntity);

            // Stock leaves the kitchen the moment the order is made, independent of whether the
            // money settles now or later (cash-on-delivery, a pending electronic transfer) - same
            // unconditional placement as the current app's checkout flow.
            await _inventory.DeductForOrderAsync(orderId, resolvedItems, cashierUser.Name, now, ct);

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
                            split.Method, split.Amount, request.BusinessDayId, request.ShiftId, cashierUser.Name, now);
                        _db.TreasuryMovements.Add(ToEntity(movement));
                        idx++;
                    }
                }
                else
                {
                    var movement = TreasuryMovementFactory.BuildSaleMovement($"tm-{Guid.NewGuid():N}",
                        $"op-{Guid.NewGuid():N}", request.CheckoutRequestId, orderId,
                        paymentMethod, plan.PayableTotal, request.BusinessDayId, request.ShiftId, cashierUser.Name, now);
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
        entity.ChangeDuePiastres, entity.Status, entity.IsPaid, entity.ElectronicPaymentStatus, wasReplayed);
}
