using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.API.Models;

public class TransactionItem
{
    [Key]
    public int Id { get; set; }  // Auto-increment

    // Khóa ngoại → TRANSACTIONS
    [MaxLength(30)]
    public string TransactionId { get; set; } = "";
    public Transaction? Transaction { get; set; }

    // Khóa ngoại → PRODUCTS
    [MaxLength(20)]
    public string ProductId { get; set; } = "";
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    // Lưu đơn giá TẠI THỜI ĐIỂM giao dịch (không bị ảnh hưởng nếu giá sau đó thay đổi)
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    // Thành tiền (computed, không lưu DB)
    [NotMapped]
    public decimal TotalPrice => Quantity * UnitPrice;
}