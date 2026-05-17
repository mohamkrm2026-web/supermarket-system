namespace SuperMarket.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; } = DateTime.Now;
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = "نقدي";
        public List<SaleItem> Items { get; set; } = new();
    }

    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }
}
