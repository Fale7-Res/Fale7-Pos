namespace Falah.Infrastructure.Delivery;

using Falah.Application.Delivery;
using Falah.Domain.Delivery;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class DeliveryQueryService
{
    private readonly FalahDbContext _db;
    public DeliveryQueryService(FalahDbContext db) => _db = db;

    public Task<List<DriverDto>> ListActiveDriversAsync(CancellationToken ct = default) =>
        _db.Drivers.AsNoTracking().Where(d => d.Active).Select(d => new DriverDto(d.Id, d.Name, d.Active)).ToListAsync(ct);

    /// <summary>Management view, including deactivated drivers - distinct from the dispatch-facing
    /// ListActiveDriversAsync above, same active-vs-all split this codebase already uses for
    /// Categories/Products.</summary>
    public Task<List<DriverDto>> ListAllDriversAsync(CancellationToken ct = default) =>
        _db.Drivers.AsNoTracking().Select(d => new DriverDto(d.Id, d.Name, d.Active)).ToListAsync(ct);

    public Task<List<DeliveryServiceDto>> ListActiveDeliveryServicesAsync(CancellationToken ct = default) =>
        _db.DeliveryServices.AsNoTracking().Where(s => s.Active)
            .Select(s => new DeliveryServiceDto(s.Id, s.Name, s.FeePiastres, s.Active, s.CreatedBy, s.CreatedAtUtc)).ToListAsync(ct);

    public Task<List<DeliveryServiceDto>> ListAllDeliveryServicesAsync(CancellationToken ct = default) =>
        _db.DeliveryServices.AsNoTracking()
            .Select(s => new DeliveryServiceDto(s.Id, s.Name, s.FeePiastres, s.Active, s.CreatedBy, s.CreatedAtUtc)).ToListAsync(ct);

    public Task<List<DeliveryDto>> ListReadyForDispatchAsync(CancellationToken ct = default) =>
        ListByStatusAsync(nameof(DeliveryStatus.Ready), ct);

    /// <summary>Every delivery still in play, whatever its stage - the read a dispatch board needs.
    /// ListReadyForDispatchAsync alone cannot drive that screen: a delivery is created as `New` at
    /// checkout and only becomes `Ready` when someone marks it so, which nobody could do while the
    /// only list available excluded `New` entirely. Terminal states (Delivered/Failed/Returned/
    /// Settled/Cancelled) are excluded - they belong to reporting and settlement, not to the live
    /// board. Read-only: composes exactly the same DeliveryDto the sibling queries already return,
    /// adds no new business rule and writes nothing.</summary>
    public Task<List<DeliveryDto>> ListDispatchBoardAsync(CancellationToken ct = default) =>
        QueryToDtos(_db.Deliveries.AsNoTracking().Where(d =>
            d.Status == nameof(DeliveryStatus.New) ||
            d.Status == nameof(DeliveryStatus.Preparing) ||
            d.Status == nameof(DeliveryStatus.Ready) ||
            d.Status == nameof(DeliveryStatus.Assigned) ||
            d.Status == nameof(DeliveryStatus.HandedToDriver) ||
            d.Status == nameof(DeliveryStatus.OutForDelivery)), ct);

    public Task<List<DeliveryDto>> ListActiveForDriverAsync(string driverId, CancellationToken ct = default) =>
        QueryToDtos(_db.Deliveries.AsNoTracking().Where(d => d.DriverId == driverId
            && (d.Status == nameof(DeliveryStatus.Assigned) || d.Status == nameof(DeliveryStatus.HandedToDriver) || d.Status == nameof(DeliveryStatus.OutForDelivery))), ct);

    private Task<List<DeliveryDto>> ListByStatusAsync(string status, CancellationToken ct) =>
        QueryToDtos(_db.Deliveries.AsNoTracking().Where(d => d.Status == status), ct);

    private async Task<List<DeliveryDto>> QueryToDtos(IQueryable<Persistence.Entities.DeliveryEntity> query, CancellationToken ct)
    {
        var deliveries = await query.OrderBy(d => d.CreatedAtUtc).ToListAsync(ct);
        var orderIds = deliveries.Select(d => d.OrderId).ToList();
        var orders = (await _db.Orders.AsNoTracking().Where(o => orderIds.Contains(o.Id)).ToListAsync(ct)).ToDictionary(o => o.Id);
        var driverIds = deliveries.Where(d => d.DriverId is not null).Select(d => d.DriverId!).Distinct().ToList();
        var drivers = (await _db.Drivers.AsNoTracking().Where(d => driverIds.Contains(d.Id)).ToListAsync(ct)).ToDictionary(d => d.Id);

        return deliveries.Select(d =>
        {
            var order = orders[d.OrderId];
            var facts = DeliveryFactsComposer.Compose(d, order);
            var driverName = d.DriverId is not null && drivers.TryGetValue(d.DriverId, out var driver) ? driver.Name : d.DriverNameAtHandover;
            return new DeliveryDto(d.Id, order.Id, order.OrderNumber, d.Status, d.DriverId, driverName,
                facts.AmountToCollect.Piastres, facts.OrderTotal.Piastres, order.PaymentMethod, order.IsPaid, d.ShiftId, d.CreatedAtUtc,
                order.CustomerName, order.CustomerPhone, order.DeliveryArea, order.DeliveryAddress, order.ElectronicPaymentStatus);
        }).ToList();
    }

    public async Task<DeliveryDto> GetOneAsync(string deliveryId, CancellationToken ct = default)
    {
        var delivery = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deliveryId, ct)
            ?? throw new InvalidOperationException("طلب الدليفري غير موجود.");
        var order = await _db.Orders.AsNoTracking().FirstAsync(o => o.Id == delivery.OrderId, ct);
        var facts = DeliveryFactsComposer.Compose(delivery, order);
        string? driverName = null;
        if (delivery.DriverId is not null)
        {
            var driver = await _db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == delivery.DriverId, ct);
            driverName = driver?.Name ?? delivery.DriverNameAtHandover;
        }
        return new DeliveryDto(delivery.Id, order.Id, order.OrderNumber, delivery.Status, delivery.DriverId, driverName,
            facts.AmountToCollect.Piastres, facts.OrderTotal.Piastres, order.PaymentMethod, order.IsPaid, delivery.ShiftId, delivery.CreatedAtUtc,
            order.CustomerName, order.CustomerPhone, order.DeliveryArea, order.DeliveryAddress, order.ElectronicPaymentStatus);
    }

    public async Task<List<DeliveryEventDto>> GetTimelineAsync(string deliveryId, CancellationToken ct = default) =>
        await _db.DeliveryEvents.AsNoTracking().Where(e => e.DeliveryId == deliveryId).OrderBy(e => e.CreatedAtUtc)
            .Select(e => new DeliveryEventDto(e.Type, e.Reason, e.AmountPiastres, e.Actor, e.Approver, e.CreatedAtUtc))
            .ToListAsync(ct);

    /// <summary>Ported from driverAccountView + driverOperationalStatus in
    /// lib/delivery-domain.mjs. Read-only (no locks) - reuses the exact same
    /// DeliveryRules.BuildDriverSettlementScope aggregation SettleDriverCustodyHandler's own
    /// mutation path uses, so a caller previewing "can this driver be settled?" always sees the
    /// identical scope the settlement command itself would compute.</summary>
    public async Task<DriverAccountDto> GetDriverAccountAsync(string driverId, CancellationToken ct = default)
    {
        _ = await _db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == driverId, ct)
            ?? throw new InvalidOperationException("السائق غير موجود.");

        var deliveryEntities = await _db.Deliveries.AsNoTracking()
            .Where(d => d.DriverId == driverId && d.Status != nameof(DeliveryStatus.Settled) && d.Status != nameof(DeliveryStatus.Cancelled))
            .ToListAsync(ct);
        var orderIds = deliveryEntities.Select(d => d.OrderId).ToList();
        var orders = orderIds.Count == 0 ? new List<OrderEntity>() : await _db.Orders.AsNoTracking().Where(o => orderIds.Contains(o.Id)).ToListAsync(ct);
        var ordersById = orders.ToDictionary(o => o.Id);
        var facts = deliveryEntities.Select(d => DeliveryFactsComposer.Compose(d, ordersById[d.OrderId])).ToList();

        var custodyEntities = await _db.DriverCustodies.AsNoTracking()
            .Where(c => c.DriverId == driverId && c.Status != nameof(CustodyStatus.Settled)).ToListAsync(ct);
        var custodies = custodyEntities.Select(DeliveryFactsComposer.ToDriverCustody).ToList();

        var scope = DeliveryRules.BuildDriverSettlementScope(driverId, facts, custodies);
        var cashCustodyPiastres = scope.UnsettledCustodies.Sum(c => c.Collected.Piastres);
        var failedOrReturnedUnsettled = scope.ResolvedDeliveries.Count(d => d.Status != DeliveryStatus.Delivered);

        // No separate "trip" object exists in this per-order model (see DriverOperationalStatus's
        // own doc comment) - status is derived directly from the driver's own active/resolved
        // delivery statuses instead of a trip.status field.
        var operationalStatus =
            custodies.Any(c => c.Collected.IsPositive) ? DriverOperationalStatus.SettlementPending
            : scope.ActiveDeliveries.Any(d => d.Status == DeliveryStatus.OutForDelivery) ? DriverOperationalStatus.Out
            : scope.ResolvedDeliveries.Any(d => d.Status == DeliveryStatus.Returned) ? DriverOperationalStatus.Returning
            : scope.ActiveDeliveries.Count > 0 ? DriverOperationalStatus.Assigned
            : DriverOperationalStatus.Available;

        var driverSettlements = await _db.DriverSettlements.AsNoTracking()
            .Where(s => s.DriverId == driverId).OrderByDescending(s => s.CreatedAtUtc).ToListAsync(ct);

        return new DriverAccountDto(
            driverId, operationalStatus, DescribeOperationalStatus(operationalStatus),
            scope.ActiveDeliveries.Count, scope.ResolvedDeliveries.Count, failedOrReturnedUnsettled,
            scope.GrossOrderValue.Piastres, scope.PrepaidValue.Piastres, cashCustodyPiastres,
            CanSettle: scope.ActiveDeliveries.Count == 0 && scope.ResolvedDeliveries.Count > 0,
            BlockingOrders: scope.ActiveDeliveries.Select(d => new BlockingOrderDto(d.Id, d.OrderId, d.OrderNumber ?? d.OrderId, d.Status.ToString())).ToList(),
            AllTimeSettledOrderCount: driverSettlements.Sum(s => s.OrderCount),
            AllTimeSettledCashPiastres: driverSettlements.Sum(s => s.ActualPiastres),
            LastSettlementAtUtc: driverSettlements.Count == 0 ? null : driverSettlements[0].CreatedAtUtc);
    }

    /// <summary>One driver's figures for "today / last 7 days / month / custom period" - reuses the
    /// exact same Deliveries/DriverCustodies/DriverSettlements rows GetDriverAccountAsync and
    /// GetDeliveryOutcomesReportAsync already read, scoped to one driver and a date range instead of
    /// everyone/all-time. See DriverStatisticsDto's own doc comment for which fields stay all-time
    /// on purpose (current custody, last settlement).</summary>
    public async Task<DriverStatisticsDto> GetDriverStatisticsAsync(
        string driverId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        var driver = await _db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == driverId, ct)
            ?? throw new InvalidOperationException("السائق غير موجود.");

        bool InRange(DateTimeOffset? t) => t is { } value && (from is null || value >= from) && (to is null || value < to);

        var deliveries = await _db.Deliveries.AsNoTracking().Where(d => d.DriverId == driverId).ToListAsync(ct);
        var assigned = deliveries.Where(d => InRange(d.AssignedAtUtc)).ToList();
        var delivered = deliveries.Where(d => InRange(d.DeliveredAtUtc)).ToList();
        var failed = deliveries.Count(d => InRange(d.FailedAtUtc));
        var returned = deliveries.Count(d => InRange(d.ReturnedAtUtc));

        var deliveredOrderIds = delivered.Select(d => d.OrderId).ToList();
        var orderValue = deliveredOrderIds.Count == 0 ? 0
            : await _db.Orders.AsNoTracking().Where(o => deliveredOrderIds.Contains(o.Id)).SumAsync(o => (long?)o.TotalPiastres, ct) ?? 0;
        var cashCollected = delivered.Sum(d => d.CollectedAmountPiastres ?? 0);

        var custody = await _db.DriverCustodies.AsNoTracking()
            .Where(c => c.DriverId == driverId && c.Status != nameof(CustodyStatus.Settled))
            .SumAsync(c => (long?)c.CollectedPiastres, ct) ?? 0;

        var settlements = await _db.DriverSettlements.AsNoTracking().Where(s => s.DriverId == driverId).ToListAsync(ct);
        var settlementsInRange = settlements.Where(s => InRange(s.CreatedAtUtc)).ToList();

        var durations = delivered
            .Where(d => d.OutForDeliveryAtUtc is not null && d.DeliveredAtUtc is not null)
            .Select(d => (d.DeliveredAtUtc!.Value - d.OutForDeliveryAtUtc!.Value).TotalMinutes)
            .Where(m => m >= 0).ToList();
        long? averageMinutes = durations.Count == 0 ? null : (long)Math.Round(durations.Average());

        var lastOrderAtUtc = deliveries.Where(d => d.AssignedAtUtc is not null)
            .Select(d => d.AssignedAtUtc!.Value).DefaultIfEmpty().Max();

        return new DriverStatisticsDto(
            driverId, driver.Name, assigned.Count, delivered.Count, failed, returned,
            orderValue, cashCollected, custody,
            settlementsInRange.Count,
            settlementsInRange.Sum(s => Math.Max(0, -s.DifferencePiastres)),
            settlementsInRange.Sum(s => Math.Max(0, s.DifferencePiastres)),
            averageMinutes,
            lastOrderAtUtc == default ? null : lastOrderAtUtc,
            settlements.Count == 0 ? null : settlements.Max(s => s.CreatedAtUtc));
    }

    /// <summary>The status in words. DriverOperationalStatus serialises as a bare integer, so a
    /// screen binding straight to it showed "0" to a supervisor. Written once here rather than
    /// re-derived in every client that displays a driver.</summary>
    private static string DescribeOperationalStatus(Falah.Application.Delivery.DriverOperationalStatus status) => status switch
    {
        Falah.Application.Delivery.DriverOperationalStatus.Available => "متاح",
        Falah.Application.Delivery.DriverOperationalStatus.Out => "بالخارج",
        Falah.Application.Delivery.DriverOperationalStatus.Returning => "في طريق العودة",
        Falah.Application.Delivery.DriverOperationalStatus.Assigned => "مُسند إليه طلب",
        Falah.Application.Delivery.DriverOperationalStatus.SettlementPending => "بانتظار التسوية",
        _ => status.ToString(),
    };
}
