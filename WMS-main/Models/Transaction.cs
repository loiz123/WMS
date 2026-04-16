using System.ComponentModel.DataAnnotations;

namespace WMS.API.Models;

public class Transaction
{
    [Key]
    [MaxLength(30)]
    public string Id { get; set; } = "";

    [Required]
    [MaxLength(10)]
    public string Type { get; set; } = "";  // "Import" | "Export"

    // Khóa ngoại → APP_USERS
    [MaxLength(20)]
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;

    // Navigation: 1 transaction có nhiều items (Master-Detail)
    public ICollection<TransactionItem> Items { get; set; } = new List<TransactionItem>();
}