using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.API.Models;

public class Product
{
    [Key]
    [MaxLength(20)]
    public string Id { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = "";

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = "";

    public int     Quantity    { get; set; }  // Tồn kho hiện tại
    public int     MinQuantity { get; set; }  // Ngưỡng cảnh báo

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    // Khóa ngoại → SUPPLIERS
    [MaxLength(20)]
    public string? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation: 1 product có nhiều transaction items
    public ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();

    // Computed property: cảnh báo tồn thấp (FR-009)
    [NotMapped]
    public bool IsLowStock => Quantity <= MinQuantity;
}