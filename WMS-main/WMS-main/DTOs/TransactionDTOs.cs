using System.ComponentModel.DataAnnotations;

namespace WMS.API;

public class TransactionCreateDto
{
    [Required]
    [RegularExpression("^(Import|Export)$", ErrorMessage = "Type phải là Import hoặc Export")]
    public string Type { get; set; } = "";

    [Required(ErrorMessage = "Chọn sản phẩm")]
    public string ProductId { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải ≥ 1")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải ≥ 0")]
    public decimal UnitPrice { get; set; }

    [MaxLength(500)] public string? Note { get; set; }
}