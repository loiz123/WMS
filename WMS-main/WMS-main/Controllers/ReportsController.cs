using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly WmsDbContext _db;
    public ReportsController(WmsDbContext db) => _db = db;

    // GET /api/reports/dashboard — 4 KPI cards + giao dịch gần nhất
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var today    = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var totalProducts = await _db.Products.CountAsync();
        var totalValue    = await _db.Products.SumAsync(p => (decimal)p.Quantity * p.Price);
        var todayTx       = await _db.Transactions.CountAsync(t =>
                                t.CreatedAt >= today && t.CreatedAt < tomorrow);
        var lowStockCount = await _db.Products.CountAsync(p => p.Quantity <= p.MinQuantity);

        var recentTx = await _db.Transactions
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new {
                t.Id, t.Type, t.CreatedAt, t.Note,
                UserName = t.User != null ? t.User.FullName : null,
                Items = t.Items.Select(i => new {
                    i.Quantity, i.UnitPrice,
                    ProductName = i.Product != null ? i.Product.Name : null
                })
            })
            .ToListAsync();

        var lowStockItems = await _db.Products
            .Where(p => p.Quantity <= p.MinQuantity)
            .OrderBy(p => p.Quantity)
            .Take(10)
            .Select(p => new { p.Id, p.Name, p.Quantity, p.MinQuantity, p.Unit })
            .ToListAsync();

        return Ok(new {
            totalProducts, totalValue, todayTx, lowStockCount,
            recentTx, lowStockItems
        });
    }

    // GET /api/reports/statistics — Tổng hợp nhập/xuất
    [HttpGet("statistics")]
    public async Task<IActionResult> Statistics()
    {
        var allItems = await _db.TransactionItems
            .Include(i => i.Transaction)
            .ToListAsync();

        var importItems = allItems.Where(i => i.Transaction?.Type == "Import").ToList();
        var exportItems = allItems.Where(i => i.Transaction?.Type == "Export").ToList();

        var top5 = await _db.Products
            .OrderByDescending(p => (decimal)p.Quantity * p.Price)
            .Take(5)
            .Select(p => new { p.Name, p.Quantity, p.Price, Value = (decimal)p.Quantity * p.Price })
            .ToListAsync();

        return Ok(new {
            importCount      = importItems.Select(i => i.TransactionId).Distinct().Count(),
            importTotalValue = importItems.Sum(i => i.Quantity * i.UnitPrice),
            exportCount      = exportItems.Select(i => i.TransactionId).Distinct().Count(),
            exportTotalValue = exportItems.Sum(i => i.Quantity * i.UnitPrice),
            currentStockValue = await _db.Products.SumAsync(p => (decimal)p.Quantity * p.Price),
            top5ByValue = top5
        });
    }

    // GET /api/reports/low-stock — danh sách cảnh báo tồn thấp
    [HttpGet("low-stock")]
    public async Task<IActionResult> LowStock()
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
}
