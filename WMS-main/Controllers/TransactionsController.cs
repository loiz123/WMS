using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API;
using WMS.API.Models;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly WmsDbContext _db;
    public TransactionsController(WmsDbContext db) => _db = db;

    /// GET /api/transactions?type=Import&page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? type     = null,
        [FromQuery] string? search   = null,
        [FromQuery] int    page     = 1,
        [FromQuery] int    pageSize = 20)
    {
        var query = _db.Transactions
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .Include(t => t.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(type))
            query = query.Where(t => t.Type == type);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(t =>
                t.Id.Contains(search) ||
                t.Items.Any(i => i.Product != null && i.Product.Name.Contains(search)));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// GET /api/transactions/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var tx = await _db.Transactions
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tx == null) return NotFound();
        return Ok(tx);
    }

    /// POST /api/transactions — Tạo phiếu nhập/xuất (ATOMIC)
    /// TC-09: Nhập SL=10 → tồn tăng 10 ✓
    /// TC-10: Nhập SL=0 → 400 ✓
    /// TC-12: Xuất SL ≤ tồn → OK ✓
    /// TC-13: Xuất SL > tồn → 400 ✓
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransactionCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Lấy userId từ JWT token (không cần truyền từ client)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Tìm sản phẩm
        var product = await _db.Products.FindAsync(dto.ProductId);
        if (product == null)
            return NotFound(new { message = $"Không tìm thấy sản phẩm {dto.ProductId}" });

        // Kiểm tra tồn kho khi xuất (TC-13 — FR-011)
        if (dto.Type == "Export" && dto.Quantity > product.Quantity)
            return BadRequest(new {
                message = $"Không đủ hàng trong kho. Tồn hiện tại: {product.Quantity} {product.Unit}"
            });

        // ── ATOMIC: dùng database transaction để đảm bảo tính toàn vẹn ──
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Bước 1: Tạo phiếu giao dịch
            var tx = new Transaction
            {
                Id              = "TX" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                Type            = dto.Type,
                UserId          = userId,
                Note            = dto.Note,
                TransactionDate = DateTime.UtcNow,
            };

            // Bước 2: Thêm item vào phiếu
            tx.Items.Add(new TransactionItem {
                ProductId = dto.ProductId,
                Quantity  = dto.Quantity,
                UnitPrice = dto.UnitPrice > 0 ? dto.UnitPrice : product.Price
            });

            // Bước 3: Cập nhật tồn kho
            if (dto.Type == "Import")
                product.Quantity += dto.Quantity;  // Nhập → cộng
            else
                product.Quantity -= dto.Quantity;  // Xuất → trừ

            product.UpdatedAt = DateTime.UtcNow;

            // Bước 4: Lưu cả 2 thay đổi
            _db.Transactions.Add(tx);
            await _db.SaveChangesAsync();

            // Bước 5: Commit — chỉ khi KHÔNG có lỗi
            await dbTransaction.CommitAsync();

            return CreatedAtAction(nameof(GetById), new { id = tx.Id }, new {
                tx.Id, tx.Type,
                ProductName = product.Name,
                dto.Quantity,
                UnitPrice   = dto.UnitPrice > 0 ? dto.UnitPrice : product.Price,
                TotalPrice  = dto.Quantity * (dto.UnitPrice > 0 ? dto.UnitPrice : product.Price),
                NewStock    = product.Quantity,
                tx.Note
            });
        }
        catch (Exception ex)
        {
            // Rollback nếu có bất kỳ lỗi nào → dữ liệu không bị sai
            await dbTransaction.RollbackAsync();
            return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
        }
    }
}