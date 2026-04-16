using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Services;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]  // Toàn bộ controller chỉ Admin
public class UsersController : ControllerBase
{
    private readonly WmsDbContext _db;
    private readonly AuthService _auth;
    public UsersController(WmsDbContext db, AuthService auth)
    {
        _db = db; _auth = auth;
    }

    // GET /api/users
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null)
    {
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.Username.Contains(search) ||
                                    (u.FullName != null && u.FullName.Contains(search)));
        return Ok(await query
            .OrderBy(u => u.Username)
            .Select(u => new { u.Id, u.Username, u.FullName, u.Role, u.CreatedAt })
            .ToListAsync());
    }

    // POST /api/users — Tạo tài khoản mới (FR-004)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var user = await _auth.CreateUserAsync(dto);
            return CreatedAtAction(nameof(GetAll), new {
                user.Id, user.Username, user.FullName, user.Role, user.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE /api/users/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        // Không cho xóa chính mình
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (u.Id == currentUserId)
            return BadRequest(new { message = "Không thể xóa tài khoản đang đăng nhập" });
        _db.Users.Remove(u);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
