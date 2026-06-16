using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        [DataContract]
        public sealed class ReceiptPrintData
        {
            [DataMember(Name = "printType", EmitDefaultValue = false)] public string PrintType { get; set; }
            [DataMember(Name = "title", EmitDefaultValue = false)] public string Title { get; set; }
            [DataMember(Name = "subtitle", EmitDefaultValue = false)] public string Subtitle { get; set; }
            [DataMember(Name = "copyStamp", EmitDefaultValue = false)] public string CopyStamp { get; set; }
            [DataMember(Name = "isCopy", EmitDefaultValue = false)] public bool IsCopy { get; set; }

            [DataMember(Name = "orderId", EmitDefaultValue = false)] public string OrderId { get; set; }
            [DataMember(Name = "backendOrderId", EmitDefaultValue = false)] public string BackendOrderId { get; set; }
            [DataMember(Name = "orderNo", EmitDefaultValue = false)] public int OrderNo { get; set; }
            [DataMember(Name = "shiftId", EmitDefaultValue = false)] public string ShiftId { get; set; }

            [DataMember(Name = "dateText", EmitDefaultValue = false)] public string DateText { get; set; }
            [DataMember(Name = "cashierName", EmitDefaultValue = false)] public string CashierName { get; set; }
            [DataMember(Name = "customerName", EmitDefaultValue = false)] public string CustomerName { get; set; }
            [DataMember(Name = "customerPhone", EmitDefaultValue = false)] public string CustomerPhone { get; set; }
            [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
            [DataMember(Name = "addressLine", EmitDefaultValue = false)] public string AddressLine { get; set; }
            [DataMember(Name = "driverName", EmitDefaultValue = false)] public string DriverName { get; set; }
            [DataMember(Name = "driverPhone", EmitDefaultValue = false)] public string DriverPhone { get; set; }
            [DataMember(Name = "notes", EmitDefaultValue = false)] public string Notes { get; set; }

            [DataMember(Name = "subtotal", EmitDefaultValue = false)] public decimal Subtotal { get; set; }
            [DataMember(Name = "deliveryFee", EmitDefaultValue = false)] public decimal DeliveryFee { get; set; }
            [DataMember(Name = "discount", EmitDefaultValue = false)] public decimal Discount { get; set; }
            [DataMember(Name = "total", EmitDefaultValue = false)] public decimal Total { get; set; }

            [DataMember(Name = "items", EmitDefaultValue = false)] public List<ReceiptPrintItemData> Items { get; set; }
            [DataMember(Name = "footerLines", EmitDefaultValue = false)] public List<string> FooterLines { get; set; }
            [DataMember(Name = "metadata", EmitDefaultValue = false)] public ReceiptPrintMetadataData Metadata { get; set; }
        }

        [DataContract]
        public sealed class ReceiptPrintItemData
        {
            [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Name = "qty", EmitDefaultValue = false)] public int Qty { get; set; }
            [DataMember(Name = "unitPrice", EmitDefaultValue = false)] public decimal UnitPrice { get; set; }
            [DataMember(Name = "lineTotal", EmitDefaultValue = false)] public decimal LineTotal { get; set; }
            [DataMember(Name = "notes", EmitDefaultValue = false)] public string Notes { get; set; }
            [DataMember(Name = "isDivider", EmitDefaultValue = false)] public bool IsDivider { get; set; }
        }

        [DataContract]
        public sealed class ReceiptPrintMetadataData
        {
            [DataMember(Name = "channel", EmitDefaultValue = false)] public string Channel { get; set; }
            [DataMember(Name = "source", EmitDefaultValue = false)] public string Source { get; set; }
            [DataMember(Name = "status", EmitDefaultValue = false)] public string Status { get; set; }
            [DataMember(Name = "layout", EmitDefaultValue = false)] public string Layout { get; set; }
            [DataMember(Name = "reference", EmitDefaultValue = false)] public string Reference { get; set; }
        }
    }
}
