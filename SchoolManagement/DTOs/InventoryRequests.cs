namespace SchoolManagement.DTOs
{
    public class PurchaseOrderRequest { public int SchoolId { get; set; } public int VendorId { get; set; } public string? PoNumber { get; set; } public DateTime PurchaseDate { get; set; } public string? InvoiceNumber { get; set; } public List<PurchaseItemRequest> Items { get; set; } = new(); }
    public class PurchaseItemRequest { public int ProductId { get; set; } public int? ProductVariantId { get; set; } public decimal Quantity { get; set; } public decimal Rate { get; set; } public decimal GstPercent { get; set; } public decimal Discount { get; set; } }
    public class StudentOrderRequest { public int SchoolId { get; set; } public int AcademicSessionId { get; set; } public int StudentId { get; set; } public List<StudentOrderItemRequest> Items { get; set; } = new(); }
    public class StudentOrderItemRequest { public int ProductId { get; set; } public int? ProductVariantId { get; set; } public decimal Quantity { get; set; } public decimal? UnitPrice { get; set; } public decimal Discount { get; set; } }
    public class InventoryReturnRequest { public int SchoolId { get; set; } public int StudentOrderId { get; set; } public string Reason { get; set; } = ""; public List<InventoryReturnItemRequest> Items { get; set; } = new(); }
    public class InventoryReturnItemRequest { public int ProductId { get; set; } public int? ProductVariantId { get; set; } public decimal Quantity { get; set; } public string Condition { get; set; } = "Good"; public bool Restock { get; set; } = true; }
}
