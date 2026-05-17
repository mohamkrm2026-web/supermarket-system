using System.ComponentModel.DataAnnotations;
namespace SuperMarket.Models
{
    public class Customer
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public decimal TotalPurchases { get; set; }
        public int Points { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
