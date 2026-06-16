using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fale7_POS
{
    [DataContract]
    public sealed class PosShiftRecord
    {
        [DataMember(Name = "id")]                  public string Id                { get; set; }
        [DataMember(Name = "branch_id")]           public string BranchId          { get; set; }
        [DataMember(Name = "status")]              public string Status            { get; set; }
        [DataMember(Name = "opened_at")]           public string OpenedAt          { get; set; }
        [DataMember(Name = "closed_at")]           public string ClosedAt          { get; set; }
        [DataMember(Name = "opened_by_user_id")]   public string OpenedByUserId    { get; set; }
        [DataMember(Name = "opening_cash")]        public decimal OpeningCash      { get; set; }
        [DataMember(Name = "closing_cash")]        public decimal? ClosingCash     { get; set; }
        [DataMember(Name = "total_cash_orders")]   public decimal TotalCashOrders  { get; set; }
        [DataMember(Name = "total_card_orders")]   public decimal TotalCardOrders  { get; set; }
        [DataMember(Name = "note")]                public string Note             { get; set; }
    }

    [DataContract]
    public sealed class ActiveShiftRpcResult
    {
        [DataMember(Name = "ok")]           public bool          Ok           { get; set; }
        [DataMember(Name = "error")]        public string        Error        { get; set; }
        [DataMember(Name = "shift")]        public PosShiftRecord Shift       { get; set; }
        [DataMember(Name = "orderCount")]   public int           OrderCount   { get; set; }
        [DataMember(Name = "totalRevenue")] public decimal       TotalRevenue { get; set; }
    }

    [DataContract]
    public sealed class OpenShiftRpcResult
    {
        [DataMember(Name = "ok")]           public bool   Ok          { get; set; }
        [DataMember(Name = "error")]        public string Error       { get; set; }
        [DataMember(Name = "shiftId")]      public string ShiftId     { get; set; }
        [DataMember(Name = "alreadyOpen")]  public bool   AlreadyOpen { get; set; }
    }

    [DataContract]
    public sealed class CloseShiftRpcResult
    {
        [DataMember(Name = "ok")]           public bool    Ok          { get; set; }
        [DataMember(Name = "error")]        public string  Error       { get; set; }
        [DataMember(Name = "totalCash")]    public decimal TotalCash   { get; set; }
        [DataMember(Name = "totalCard")]    public decimal TotalCard   { get; set; }
        [DataMember(Name = "closingCash")]  public decimal ClosingCash { get; set; }
        [DataMember(Name = "difference")]   public decimal Difference  { get; set; }
    }

    public enum PrintType
    {
        Kitchen,
        Customer,
        Pickup,
        ShiftReport
    }

    public sealed class OrderPrintData
    {
        public string OrderId { get; set; }
        public string BackendOrderId { get; set; }
        public int OrderNo { get; set; }
        public string OrderType { get; set; }
        public string DateText { get; set; }
        public string CashierName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string District { get; set; }
        public string AddressBlock { get; set; }
        public string AddressStreet { get; set; }
        public string AddressBuilding { get; set; }
        public string AddressApartment { get; set; }
        public string AddressFloor { get; set; }
        public string AddressNote { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public int KitchenPrintCount { get; set; }
        public List<PrintLineItem> Items { get; set; } = new List<PrintLineItem>();
    }

    public sealed class PrintLineItem
    {
        public string Name { get; set; }
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public string Notes { get; set; }
    }

    [DataContract]
    public sealed class KitchenPrintRpcResult
    {
        [DataMember(Name = "ok")] public bool Ok { get; set; }
        [DataMember(Name = "error")] public string Error { get; set; }
        [DataMember(Name = "printCount")] public int PrintCount { get; set; }
        [DataMember(Name = "isReprint")] public bool IsReprint { get; set; }
    }

    [DataContract]
    public sealed class OrderNumberRpcResult
    {
        [DataMember(Name = "ok")] public bool Ok { get; set; }
        [DataMember(Name = "error")] public string Error { get; set; }
        [DataMember(Name = "orderNumber")] public long OrderNumber { get; set; }
    }

    [DataContract]
    public sealed class OrderLockRpcResult
    {
        [DataMember(Name = "ok")] public bool Ok { get; set; }
        [DataMember(Name = "locked")] public bool Locked { get; set; }
        [DataMember(Name = "renewed")] public bool Renewed { get; set; }
        [DataMember(Name = "error")] public string Error { get; set; }
        [DataMember(Name = "message")] public string Message { get; set; }
    }
}
