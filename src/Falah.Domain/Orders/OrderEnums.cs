namespace Falah.Domain.Orders;

/// <summary>
/// <c>Takeaway</c> is the walk-in who orders and waits. <c>Pickup</c> is the customer who ordered
/// ahead and comes to collect, so it is the one non-delivery type that carries a name and phone -
/// without them nobody at the counter knows whose bag is whose. It replaced the old DineIn: this
/// restaurant has no hall, and a table number was the only thing DineIn ever collected.
/// </summary>
public enum OrderType { Pickup, Takeaway, Delivery }

public enum OrderStatus { Pending, Preparing, Ready, Completed, Cancelled, Refunded }

public enum ElectronicPaymentStatus { Pending, Confirmed, Cancelled }
