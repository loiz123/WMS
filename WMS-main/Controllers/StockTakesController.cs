using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;

namespace WMS.API.Controllers;

/// <summary>
/// Kiểm kho (Physical Inventory / Stock Take)
///
/// Luồng nghiệp vụ:
///   1. Nhân viên/Admin tạo phiếu → Status = "Draft"
///      POST /api/stocktakes
///
///   2. Nhân viên nhập số lượng thực đếm cho từng dòng
///      PUT  /api/stocktakes/{id}/items/{itemId}
///
///   3. Admin duyệt → Status = "Completed", hệ thống tự điều chỉnh tồn kho
///      POST /api/stocktakes/{id}/approve  { approve: true }
///
///   4. Hoặc Admin hủy → Status = "Cancelled" (không chạm tồn kho)
///      POST /api/stocktakes/{id}/approve  { approve: false }
/// </summary>
[ApiController]
[Route("api/stocktakes")]
[Authorize]
public class StockTakesController : ControllerBase
{
  
    private readonly WmsDbContext _db;
    public StockTakesController(WmsDbContext db) => _db = db;

    // ────────────────────────────────────────────────────────────────
    // GET /api/stocktakes?status=Draft&page=1&pageSize=20
    // Danh sách tất cả phiếu kiểm kho
    // ────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20)
    {
        var query = _db.StockTakes
            .Include(s => s.Creator)
            .Include(s => s.Approver)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new {
                s.Id, s.Status, s.Note,
                s.CreatedAt, s.ApprovedAt,
                CreatedBy   = s.Creator  != null ? s.Creator.FullName  : s.CreatedBy,
                ApprovedBy  = s.Approver != null ? s.Approver.FullName : s.ApprovedBy,
                ItemCount   = s.Items.Count
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    // ────────────────────────────────────────────────────────────────
    // GET /api/stocktakes/{id}
    // Chi tiết 1 phiếu kiểm kho (bao gồm tất cả dòng)
    // ────────────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var st = await _db.StockTakes
            .Include(s => s.Creator)
            .Include(s => s.Approver)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (st == null) return NotFound(new { message = $"Không tìm thấy phiếu kiểm {id}" });

        return Ok(new {
            st.Id, st.Status, st.Note,
            st.CreatedAt, st.ApprovedAt,
            CreatedBy  = st.Creator  != null ? st.Creator.FullName  : st.CreatedBy,
            ApprovedBy = st.Approver != null ? st.Approver.FullName : st.ApprovedBy,
            Items = st.Items.Select(i => new {
                i.Id, i.ProductId,
                ProductName    = i.Product?.Name ?? "",
                ProductUnit    = i.Product?.Unit ?? "",
                i.SystemQuantity,
                i.ActualQuantity,
                Difference     = i.ActualQuantity - i.SystemQuantity,
                i.Note
            })
        });
    }

    // ────────────────────────────────────────────────────────────────
    // POST /api/stocktakes
    // Tạo phiếu kiểm kho mới (snapshot tồn kho hiện tại)
    // ────────────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StockTakeCreateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Lấy danh sách sản phẩm cần kiểm
        IQueryable<Product> productQuery = _db.Products;
        if (dto.ProductIds != null && dto.ProductIds.Any())
            productQuery = productQuery.Where(p => dto.ProductIds.Contains(p.Id));

        var products = await productQuery.ToListAsync();
        if (!products.Any())
            return BadRequest(new { message = "Không có sản phẩm nào để kiểm kho" });

        // Tạo phiếu kiểm
        var stockTake = new StockTake
        {
            Id        = "ST" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            Note      = dto.Note,
            Status    = "Draft",
            CreatedBy = userId,
        };

        // Snapshot tồn kho TẠI THỜI ĐIỂM này (ActualQuantity mặc định = SystemQuantity)
        foreach (var p in products)
        {
            stockTake.Items.Add(new StockTakeItem {
                ProductId      = p.Id,
                SystemQuantity = p.Quantity,
                ActualQuantity = p.Quantity   // Nhân viên sẽ sửa lại
            });
        }

        _db.StockTakes.Add(stockTake);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = stockTake.Id }, new {
            stockTake.Id, stockTake.Status,
            Message   = $"Đã tạo phiếu kiểm kho với {products.Count} sản phẩm"
        });
    }

    // ────────────────────────────────────────────────────────────────
    // PUT /api/stocktakes/{id}/items/{itemId}
    // Nhân viên nhập số lượng thực tế đếm được
    // ────────────────────────────────────────────────────────────────
    [HttpPut("{id}/items/{itemId:int}")]
    public async Task<IActionResult> UpdateItem(
        string id, int itemId,
        [FromBody] StockTakeItemUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var st = await _db.StockTakes.FindAsync(id);
        if (st == null) return NotFound(new { message = "Không tìm thấy phiếu kiểm" });

        if (st.Status != "Draft")
            return BadRequest(new { message = $"Phiếu đã {st.Status}, không thể chỉnh sửa" });

        var item = await _db.StockTakeItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.StockTakeId == id);
        if (item == null) return NotFound(new { message = "Không tìm thấy dòng kiểm kho" });

        item.ActualQuantity = dto.ActualQuantity;
        item.Note           = dto.Note;

        await _db.SaveChangesAsync();

        return Ok(new {
            item.Id, item.ProductId,
            item.SystemQuantity, item.ActualQuantity,
            Difference = item.ActualQuantity - item.SystemQuantity,
            item.Note
        });
    }

    // ────────────────────────────────────────────────────────────────
    // POST /api/stocktakes/{id}/approve
    // Admin duyệt hoặc hủy phiếu kiểm kho
    // Nếu duyệt → điều chỉnh tồn kho ATOMIC
    // ────────────────────────────────────────────────────────────────
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(string id, [FromBody] StockTakeApproveDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var st = await _db.StockTakes
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (st == null) return NotFound(new { message = "Không tìm thấy phiếu kiểm" });

        if (st.Status != "Draft")
            return BadRequest(new { message = $"Phiếu đã {st.Status}, không thể xử lý lại" });

        if (!dto.Approve)
        {
            // Hủy phiếu — không điều chỉnh tồn kho
            st.Status     = "Cancelled";
            st.ApprovedBy = adminId;
            st.ApprovedAt = DateTime.UtcNow;
            if (dto.Note != null) st.Note = dto.Note;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã hủy phiếu kiểm kho", id = st.Id });
        }

        // ── DUYỆT: Điều chỉnh tồn kho ATOMIC ──
        await using var dbTx = await _db.Database.BeginTransactionAsync();
        try
        {
            var adjustments = new List<object>();

            foreach (var item in st.Items)
            {
                var diff = item.ActualQuantity - item.SystemQuantity;
                if (diff == 0) continue;   // Không có chênh lệch → bỏ qua

                if (item.Product == null) continue;

                var oldQty = item.Product.Quantity;
                // Áp dụng chênh lệch: tồn mới = tồn hiện tại + (thực tế - snapshot)
                // Dùng snapshot để tính delta, không phải tồn hiện tại (tránh double-count)
                item.Product.Quantity += diff;
                item.Product.UpdatedAt = DateTime.UtcNow;

                adjustments.Add(new {
                    item.ProductId,
                    ProductName = item.Product.Name,
                    OldQty      = oldQty,
                    NewQty      = item.Product.Quantity,
                    Difference  = diff
                });
            }

            st.Status     = "Completed";
            st.ApprovedBy = adminId;
            st.ApprovedAt = DateTime.UtcNow;
            if (dto.Note != null) st.Note = dto.Note;

            await _db.SaveChangesAsync();
            await dbTx.CommitAsync();

            return Ok(new {
                message     = $"Đã duyệt phiếu kiểm kho. Điều chỉnh {adjustments.Count} sản phẩm.",
                id          = st.Id,
                adjustments
            });
        }
        catch (Exception ex)
        {
            await dbTx.RollbackAsync();
            return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    // ────────────────────────────────────────────────────────────────
    // GET /api/stocktakes/{id}/differences
    // Xem nhanh danh sách các dòng có chênh lệch (khác 0)
    // ────────────────────────────────────────────────────────────────
    [HttpGet("{id}/differences")]
    public async Task<IActionResult> GetDifferences(string id)
    {
        var items = await _db.StockTakeItems
            .Include(i => i.Product)
            .Where(i => i.StockTakeId == id && i.ActualQuantity != i.SystemQuantity)
            .Select(i => new {
                i.Id, i.ProductId,
                ProductName    = i.Product != null ? i.Product.Name : "",
                ProductUnit    = i.Product != null ? i.Product.Unit : "",
                i.SystemQuantity,
                i.ActualQuantity,
                Difference     = i.ActualQuantity - i.SystemQuantity,
                i.Note
            })
            .ToListAsync();

        return Ok(new { count = items.Count, items });
    }
}
