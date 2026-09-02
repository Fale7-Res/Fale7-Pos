namespace Falah.Domain.Orders;

using Falah.Domain.Common;
using Falah.Domain.Financial;

/// <summary>An InstaPay/wallet leg is only a CLAIM until verified - no Treasury movement posts for
/// it at checkout, and the order stays operationally frozen (no kitchen ticket, no delivery
/// dispatch) until someone confirms receipt. Mirrors the electronicPayment sub-object in the
/// current app's Order type.</summary>
public sealed record ElectronicPayment(PaymentMethod Method, string? Provider, Money Amount, ElectronicPaymentStatus Status);
