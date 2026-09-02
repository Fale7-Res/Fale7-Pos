namespace Falah.Domain.Orders;

public enum OrderType { DineIn, Takeaway, Delivery }

public enum OrderStatus { Pending, Preparing, Ready, Completed, Cancelled, Refunded }

public enum ElectronicPaymentStatus { Pending, Confirmed, Cancelled }
