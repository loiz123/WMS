using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.API.Models;

/// <summary>
/// Chi tiết từng dòng trong phiếu kiểm kho
/// Lưu số lượng hệ thống (system) và số lượng thực đếm (actual)
/// </summary>
public class StockTakeItem
{
    [Key]
    public int Id { get; set; }  // Auto-increment

    // Khóa ngoại → STOCK_TAKES
    [Required]
    [MaxLength(30)]
    public string StockTakeId { get; set; } = "";
    public StockTake? StockTake { get; set; }

    // Khóa ngoại → PRODUCTS
    [Required]
    [MaxLength(20)]
    public string ProductId { get; set; } = "";
    public Product? Product { get; set; }

    /// Tồn kho hệ thống TẠI THỜI ĐIỂM tạo phiếu kiểm (snapshot)
    public int SystemQuantity { get; set; }

    /// Số lượng thực tế nhân viên đếm được (nhập tay)
    public int ActualQuantity { get; set; }

    /// Chênh lệch = Thực tế - Hệ thống (âm = thiếu, dương = thừa)
    [NotMapped]
    public int Difference => ActualQuantity - SystemQuantity;

    [MaxLength(300)]
    public string? Note { get; set; }  // Ghi chú cho từng dòng
}
