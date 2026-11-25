namespace Falah.Infrastructure.Printing;

using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Turns a committed sale into the tickets it needs, automatically.
///
/// This exists on the SERVER, not in a station, because a kitchen ticket must not depend on the
/// client surviving the moment after payment. Before this, the cashier app created no print job at
/// all - an order was taken on screen and the kitchen never heard about it - while the Call Center
/// app did it for itself. Two independent implementations of "print the order" is exactly how one
/// of them came to be missing, so there is now one.
///
/// Three properties are load-bearing:
///
/// It runs AFTER the sale is committed and OUTSIDE its transaction. A printer, a queue, or a
/// settings read must never be able to roll back money that was already taken.
///
/// It never throws. Every failure is logged and swallowed. The worst outcome of a printing problem
/// is a missing ticket that an operator can reprint from Order Lookup; losing the paid order itself
/// would be immeasurably worse.
///
/// Its idempotency key is derived from the checkout's own key, so a retried checkout that returns
/// the same order returns the same tickets instead of printing the kitchen a second copy.
/// </summary>
public sealed class OrderAutoPrintService
{
    private readonly CreatePrintJobsHandler _createJobs;
    private readonly SettingsQueryService _settings;
    private readonly ILogger<OrderAutoPrintService> _log;
    private readonly Falah.Infrastructure.Persistence.FalahDbContext _db;

    public OrderAutoPrintService(CreatePrintJobsHandler createJobs, SettingsQueryService settings,
        ILogger<OrderAutoPrintService> log, Falah.Infrastructure.Persistence.FalahDbContext db)
    {
        _createJobs = createJobs;
        _settings = settings;
        _log = log;
        _db = db;
    }

    /// <summary>PRT-0016 (owner decision 2026-11-22): the transfer was confirmed, so the kitchen ticket
    /// held for it is queued now. Keyed on the order, and CreatePrintJobsHandler queues only routes this
    /// order has no first print for, so a retried confirmation never prints the kitchen a second copy.
    /// Never fails the confirmation; a missing ticket stays printable from Order Lookup.</summary>
    public async Task<bool> TryReleaseHeldKitchenTicketsAsync(string orderId, string actorUserId, CancellationToken ct = default)
    {
        try
        {
            var settings = await _settings.GetAsync(ct);
            if (!settings.AutoPrintPreparationTicket || !settings.HoldPreparationUntilPaymentConfirmed) return true;
            await _createJobs.HandleAsync(new CreatePrintJobsRequest(
                RequestId: $"confirmed-{orderId}", OrderId: orderId, CustomerRoute: null,
                IsReprint: false, ActorUserId: actorUserId, IncludeKitchenRoutes: true), ct);
            return true;
        }
        catch (Falah.Domain.Common.DomainException)
        {
            return true;   // nothing to prepare (no kitchen item): not a failure
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Releasing the held kitchen ticket failed for order {OrderId}; the confirmation itself is unaffected.", orderId);
            return false;
        }
    }

    /// <summary>Workflow redesign (owner, 2026-11-24): a pickup's customer took the bag - collected and paid, or
    /// handed over prepaid - and the customer's receipt prints on the till now (the kitchen papers printed when the
    /// order was taken). A document of its own (PickupReceipt), so the till's earlier "first print" of the order does
    /// not swallow it. Keyed on the order: a retried collection or handover - a replay included - queues it once.
    /// Never fails the handover; the receipt stays printable from Order Lookup.</summary>
    public async Task<bool> TryPrintPickupReceiptAsync(string orderId, string actorUserId, CancellationToken ct = default)
    {
        try
        {
            var settings = await _settings.GetAsync(ct);
            if (!settings.AutoPrintReceipt) return true;
            var jobId = $"printjob-pickup-receipt-{orderId}";
            if (await _db.PrintJobs.AsNoTracking().AnyAsync(j => j.Id == jobId, ct)) return true;
            var order = await _db.Orders.AsNoTracking().Where(o => o.Id == orderId).Select(o => new { o.OrderNumber }).FirstAsync(ct);
            var actor = await _db.Users.AsNoTracking().Where(u => u.Id == actorUserId).Select(u => u.Name).FirstOrDefaultAsync(ct);
            _db.PrintJobs.Add(new Falah.Infrastructure.Persistence.Entities.PrintJobEntity
            {
                Id = jobId, CreateRequestId = $"pickup-receipt-{orderId}", OrderId = orderId, OrderNumber = order.OrderNumber,
                Route = nameof(Falah.Domain.Printing.PrintRoute.Receipt), Status = nameof(Falah.Domain.Printing.PrintJobStatus.Pending),
                AttemptCount = 0, IsReprint = false, CreatedBy = actor ?? actorUserId, CreatedAtUtc = DateTimeOffset.UtcNow,
                DocumentKind = nameof(Falah.Domain.Printing.PrintDocumentKind.PickupReceipt),
            });
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateException)
            {
                // Queued by a sibling (a retried handover arriving at the same instant) is not a failure.
                if (!await _db.PrintJobs.AsNoTracking().AnyAsync(j => j.Id == jobId, ct)) throw;
            }
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Printing the pickup receipt failed for order {OrderId}; the handover itself is unaffected.", orderId);
            return false;
        }
    }

    /// <returns>True when the tickets were queued, or when the restaurant has deliberately turned
    /// automatic printing off. False only when something actually failed - the caller passes that
    /// on to the station so a missing ticket is visible rather than silent.</returns>
    public async Task<bool> TryPrintAsync(CheckoutOrderRequest request, CheckoutOrderResult result, CancellationToken ct = default)
    {
        try
        {
            var settings = await _settings.GetAsync(ct);

            // A station that did not say which customer ticket it prints gets the till's receipt -
            // the Call Center is the only caller that says otherwise, and it says so explicitly.
            // Workflow redesign (owner, 2026-11-24): a pickup prints its kitchen papers now - the station's copy is the
            // "تحضير طلب" slip (PrintRules.ComposeReceiptTicket) - and the customer's receipt when it is handed over
            // (TryPrintPickupReceiptAsync), paid in advance or not.
            var customerRoute = settings.AutoPrintReceipt
                ? string.IsNullOrWhiteSpace(request.CustomerPrintRoute) ? "Receipt" : request.CustomerPrintRoute
                : null;

            // An electronic transfer the customer says they sent, that nobody has confirmed. Whether
            // the kitchen starts on it is a decision about how this restaurant runs - plenty start
            // immediately and plenty do not - so it is a setting, and it defaults to what this
            // system has always done. The customer's receipt prints either way; they are standing
            // there. When the transfer is confirmed the ticket can be sent from Order Lookup.
            var awaitingPayment = result.ElectronicPaymentStatus == "pending"
                                  || result.ElectronicPaymentStatus == "Pending";
            var kitchenRoutes = settings.AutoPrintPreparationTicket
                                && !(awaitingPayment && settings.HoldPreparationUntilPaymentConfirmed);

            if (awaitingPayment && settings.HoldPreparationUntilPaymentConfirmed && settings.AutoPrintPreparationTicket)
                _log.LogInformation(
                    "Preparation ticket for order {OrderId} held until the electronic transfer is confirmed.",
                    result.OrderId);

            // Both switches off means the restaurant has deliberately turned automatic printing off.
            // Calling the handler with nothing to print would be an error, so this is where it stops.
            if (customerRoute is null && !kitchenRoutes) return true;

            await _createJobs.HandleAsync(new CreatePrintJobsRequest(
                RequestId: $"auto-{request.CheckoutRequestId}",
                OrderId: result.OrderId,
                CustomerRoute: customerRoute,
                IsReprint: false,
                ActorUserId: request.CashierId,
                IncludeKitchenRoutes: kitchenRoutes), ct);
            return true;
        }
        catch (Exception ex)
        {
            // Deliberately broad. Nothing this method can hit is worth failing a paid order over,
            // and the order's tickets remain reprintable from Order Lookup either way.
            _log.LogError(ex, "Automatic printing failed for order {OrderNumber} ({OrderId}); the sale itself is unaffected.",
                result.OrderNumber, result.OrderId);
            return false;
        }
    }
}
