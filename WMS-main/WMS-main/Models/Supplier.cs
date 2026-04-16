using System.ComponentModel.DataAnnotations;

namespace WMS.API.Models;

public class Supplier
{
    [Key]
    [MaxLength(20)]
    public string Id { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation: 1 supplier có nhiều products
    public ICollection<Product> Products { get; set; } = new List<Product>();
}