using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WMS.API.Data;
using WMS.API.Models;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly WmsDbContext _db;
    public SuppliersController(WmsDbContext db) => _db = db;

    // GET /api/suppliers?search=
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null)
    {
        var query = _db.Suppliers.AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.Name.Contains(search) || s.Id.Contains(search));
        return Ok(await query.OrderBy(s => s.Name).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        return s == null ? NotFound(new { message = $"Không tìm thấy nhà cung cấp {id}" }) : Ok(s);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] SupplierDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var s = new Supplier {
            Id      = "S" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            Name    = dto.Name!,
            Phone   = dto.Phone,
            Email   = dto.Email,
            Address = dto.Address
        };
        _db.Suppliers.Add(s);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = s.Id }, s);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id, [FromBody] SupplierDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var s = await _db.Suppliers.FindAsync(id);
        if (s == null) return NotFound();
        s.Name = dto.Name!; s.Phone = dto.Phone;
        s.Email = dto.Email; s.Address = dto.Address;
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    // TC-18: Xóa NCC đang liên kết với SP → 409
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s == null) return NotFound();
        var hasProducts = await _db.Products.AnyAsync(p => p.SupplierId == id);
        if (hasProducts)
            return Conflict(new { message = "Không thể xóa — nhà cung cấp đang liên kết với sản phẩm" });
        _db.Suppliers.Remove(s);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class SupplierDto
{
    [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
    [MaxLength(200)] public string? Name { get; set; }
    [MaxLength(20)]  public string? Phone { get; set; }
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]  // TC-17
    [MaxLength(200)] public string? Email { get; set; }
    [MaxLength(500)] public string? Address { get; set; }
}
