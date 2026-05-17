using System.ComponentModel.DataAnnotations;

namespace SuperMarket.Models
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        [Required] public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "كاشير"; // Admin | كاشير | مدير مخزن
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }
    }
}
