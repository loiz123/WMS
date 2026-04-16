using System.ComponentModel.DataAnnotations;

namespace WMS.API;

// ── Tạo phiếu kiểm kho mới ──────────────────────────────────────
public class StockTakeCreateDto
{
    [MaxLength(200)]
    public string? Note { get; set; }

    /// Nếu không truyền → hệ thống tự lấy toàn bộ sản phẩm
    /// Nếu truyền → chỉ kiểm các sản phẩm trong danh sách này
    public List<string>? ProductIds { get; set; }
}

// ── Nhập số lượng thực tế cho 1 dòng ────────────────────────────
public class StockTakeItemUpdateDto
{
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng thực tế phải ≥ 0")]
    public int ActualQuantity { get; set; }

    [MaxLength(300)]
    public string? Note { get; set; }
}

// ── Duyệt phiếu kiểm (Admin confirm) ────────────────────────────
public class StockTakeApproveDto
{
    /// true = duyệt và điều chỉnh tồn kho
    /// false = hủy phiếu
    public bool Approve { get; set; }

    [MaxLength(200)]
    public string? Note { get; set; }
}
