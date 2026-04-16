using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.API.Models;

/// <summary>
/// Phiếu kiểm kho (Stock Take Session)
/// Mỗi lần kiểm kho tạo 1 phiếu, ghi nhận ngày giờ và người thực hiện
/// </summary>
public class StockTake
{
    [Key]
    [MaxLength(30)]
    public string Id { get; set; } = "";          // Format: ST + yyyyMMddHHmmss

    [MaxLength(200)]
    public string? Note { get; set; }             // Ghi chú phiếu kiểm

    // "Draft" | "Completed" | "Cancelled"
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Draft";

    // Khóa ngoại → APP_USERS (người tạo phiếu)
    [Required]
    [MaxLength(20)]
    public string CreatedBy { get; set; } = "";
    public AppUser? Creator { get; set; }

    // Người duyệt phiếu (Admin confirm)
    [MaxLength(20)]
    public string? ApprovedBy { get; set; }
    public AppUser? Approver { get; set; }

    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }

    // Navigation: 1 phiếu kiểm có nhiều dòng chi tiết
    public ICollection<StockTakeItem> Items { get; set; } = new List<StockTakeItem>();
}
