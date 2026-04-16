using System.ComponentModel.DataAnnotations;

namespace WMS.API;

public class ProductCreateDto
{
    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
    [MaxLength(200)] public string Name { get; set; } = "";

    [Required(ErrorMessage = "Danh mục là bắt buộc")]
    [MaxLength(100)] public string Category { get; set; } = "";

    [Required] [MaxLength(20)] public string Unit { get; set; } = "";

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải ≥ 0")]
    public int Quantity { get; set; }

    [Range(0, int.MaxValue)] public int MinQuantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải ≥ 0")]
    public decimal Price { get; set; }

    public string? SupplierId { get; set; }
}

public class ProductUpdateDto : ProductCreateDto { }  // Kế thừa, giống hệt