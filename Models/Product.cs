using System.ComponentModel.DataAnnotations;
namespace SuperMarket.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public int Stock { get; set; }
        public int MinStock { get; set; } = 5;
        public string Unit { get; set; } = "قطعة";
        public bool IsActive { get; set; } = true;
    }
}
