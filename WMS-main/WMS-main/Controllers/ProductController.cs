using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API;
using WMS.API.Models;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]  // Mọi endpoint đều cần đăng nhập
public class ProductsController : ControllerBase
{
    private readonly WmsDbContext _db;
    public ProductsController(WmsDbContext db) => _db = db;

    /// GET /api/products?search=laptop&category=Điện tử&stock=low&page=1&pageSize=20
    /// FR-008: Tìm kiếm + lọc theo danh mục + lọc theo tồn kho
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search   = null,
        [FromQuery] string? category = null,
        [FromQuery] string? stock    = null,  // "low" | "ok"
        [FromQuery] int page          = 1,
        [FromQuery] int pageSize      = 20)
    {
        var query = _db.Products
            .Include(p => p.Supplier)
            .AsQueryable();

        // Tìm theo tên hoặc mã sản phẩm
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.ToLower().Contains(search.ToLower()) ||
                p.Id.ToLower().Contains(search.ToLower()));

        // Lọc theo danh mục
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        // Lọc theo tình trạng tồn kho (FR-008)
        if (stock == "low")
            query = query.Where(p => p.Quantity <= p.MinQuantity);
        else if (stock == "ok")
            query = query.Where(p => p.Quantity > p.MinQuantity);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new {
                p.Id, p.Name, p.Category, p.Unit,
                p.Quantity, p.MinQuantity, p.Price,
                p.SupplierId,
                SupplierName = p.Supplier != null ? p.Supplier.Name : null,
                IsLowStock   = p.Quantity <= p.MinQuantity,
                p.CreatedAt, p.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// GET /api/products/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var p = await _db.Products
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (p == null) return NotFound(new { message = $"Không tìm thấy sản phẩm {id}" });
        return Ok(p);
    }

    /// POST /api/products — chỉ Admin (FR-005)
    [HttpPost]
    [Authorize(Roles = "Admin")]  // TC-04: Staff gọi → 403
    public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var product = new Product
        {
            // Tự sinh mã SP theo format P + yyyyMMddHHmmss (FR-005)
            Id          = "P" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            Name        = dto.Name.Trim(),
            Category    = dto.Category.Trim(),
            Unit        = dto.Unit.Trim(),
            Quantity    = dto.Quantity,
            MinQuantity = dto.MinQuantity,
            Price       = dto.Price,
            SupplierId  = string.IsNullOrEmpty(dto.SupplierId) ? null : dto.SupplierId
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// PUT /api/products/{id} — chỉ Admin, không sửa mã SP (FR-006)
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id, [FromBody] ProductUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();

        // Cập nhật tất cả trường trừ Id và CreatedAt (FR-006)
        p.Name        = dto.Name.Trim();
        p.Category    = dto.Category.Trim();
        p.Unit        = dto.Unit.Trim();
        p.MinQuantity = dto.MinQuantity;
        p.Price       = dto.Price;
        p.SupplierId  = string.IsNullOrEmpty(dto.SupplierId) ? null : dto.SupplierId;
        p.UpdatedAt   = DateTime.UtcNow;
        // Lưu ý: KHÔNG cập nhật Quantity ở đây — chỉ cập nhật qua Transaction

        await _db.SaveChangesAsync();
        return Ok(p);
    }

    /// DELETE /api/products/{id} — chỉ Admin, kiểm tra có giao dịch không (FR-007)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();

        // Kiểm tra SP đã có giao dịch chưa (TC-08: FR-007)
        var hasTransactions = await _db.TransactionItems
            .AnyAsync(i => i.ProductId == id);

        if (hasTransactions)
            return Conflict(new {
                message = "Không thể xóa sản phẩm đã có giao dịch"
            }); // 409 Conflict

        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent(); // 204
    }

    /// GET /api/products/low-stock — lấy danh sách cảnh báo (FR-009)
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock()
    {
        var items = await _db.Products
            .Where(p => p.Quantity <= p.MinQuantity)
            .OrderBy(p => p.Quantity)
            .Select(p => new {
                p.Id, p.Name, p.Category, p.Unit,
                p.Quantity, p.MinQuantity,
                Deficit = p.MinQuantity - p.Quantity
            })
            .ToListAsync();

        return Ok(new { count = items.Count, items });
    }

    /// GET /api/products/categories — lấy danh sách danh mục (cho dropdown)
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var cats = await _db.Products
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
        return Ok(cats);
    }
}