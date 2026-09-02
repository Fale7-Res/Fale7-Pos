namespace Falah.Domain.Delivery;

using Falah.Domain.Common;

public enum DeliveryStatus { New, Preparing, Ready, Assigned, HandedToDriver, OutForDelivery, Delivered, Failed, Returned, Settled, Cancelled }

/// <summary>Ported 1:1 from DELIVERY_TRANSITIONS in lib/delivery-domain.mjs. 'Assigned ->
/// OutForDelivery' is the direct path: assignment IS custody/physical receipt now, so no separate
/// handover confirmation is required. 'Assigned -> HandedToDriver -> OutForDelivery' is kept only
/// so older persisted trips already mid-handover keep working.</summary>
public static class DeliveryTransitions
{
    private static readonly IReadOnlyDictionary<DeliveryStatus, DeliveryStatus[]> Allowed = new Dictionary<DeliveryStatus, DeliveryStatus[]>
    {
        [DeliveryStatus.New] = new[] { DeliveryStatus.Preparing, DeliveryStatus.Ready, DeliveryStatus.Cancelled },
        [DeliveryStatus.Preparing] = new[] { DeliveryStatus.Ready, DeliveryStatus.Cancelled },
        [DeliveryStatus.Ready] = new[] { DeliveryStatus.Assigned, DeliveryStatus.Cancelled },
        [DeliveryStatus.Assigned] = new[] { DeliveryStatus.HandedToDriver, DeliveryStatus.Ready, DeliveryStatus.OutForDelivery },
        [DeliveryStatus.HandedToDriver] = new[] { DeliveryStatus.OutForDelivery },
        [DeliveryStatus.OutForDelivery] = new[] { DeliveryStatus.Delivered, DeliveryStatus.Failed, DeliveryStatus.Returned },
        [DeliveryStatus.Delivered] = new[] { DeliveryStatus.Settled },
        [DeliveryStatus.Failed] = new[] { DeliveryStatus.Returned },
        [DeliveryStatus.Returned] = new[] { DeliveryStatus.Settled },
        [DeliveryStatus.Settled] = Array.Empty<DeliveryStatus>(),
        [DeliveryStatus.Cancelled] = Array.Empty<DeliveryStatus>(),
    };

    public static void AssertTransition(DeliveryStatus from, DeliveryStatus to)
    {
        if (!Allowed.TryGetValue(from, out var next) || !next.Contains(to))
            throw new DomainException($"انتقال حالة الدليفري غير مسموح: {from} → {to}");
    }
}
