using System.ComponentModel.DataAnnotations;
namespace SuperMarket.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
    }

    public class PurchaseOrder
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        public string Status { get; set; } = "معلق"; // معلق، مستلم، ملغي
        public decimal Total { get; set; }
        public string? Notes { get; set; }
        public List<PurchaseOrderItem> Items { get; set; } = new();
    }

    public class PurchaseOrderItem
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Total { get; set; }
    }
}
