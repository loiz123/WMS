using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WMS.API.Models;

public class AppUser
{
    [Key]
    [MaxLength(20)]
    public string Id { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = "";  // UNIQUE (đặt trong DbContext)

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = "";  // bcrypt hash

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "Staff";  // "Admin" | "Staff"

    [MaxLength(200)]
    public string? FullName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation: 1 user có nhiều transactions
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}