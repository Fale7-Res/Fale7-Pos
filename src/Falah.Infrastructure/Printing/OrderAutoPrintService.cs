namespace Falah.Infrastructure.Printing;

using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Infrastructure.Settings;
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

    public OrderAutoPrintService(CreatePrintJobsHandler createJobs, SettingsQueryService settings,
        ILogger<OrderAutoPrintService> log)
    {
        _createJobs = createJobs;
        _settings = settings;
        _log = log;
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
