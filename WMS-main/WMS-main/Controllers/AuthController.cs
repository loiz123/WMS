using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.API;
using WMS.API.Services;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) => _auth = auth;

    /// POST /api/auth/login
    /// TC-01: đúng thông tin → 200 + JWT
    /// TC-02: sai mật khẩu → 401
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _auth.LoginAsync(req.Username, req.Password);

        if (result == null)
            return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không đúng" });

        return Ok(result);
    }

    /// POST /api/auth/logout — hủy token phía client
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // JWT là stateless — server không lưu token
        // Logout thực sự bằng cách xóa token ở localStorage frontend
        return Ok(new { message = "Đã đăng xuất thành công" });
    }

    /// GET /api/auth/me — lấy thông tin user hiện tại từ token
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new {
            UserId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            Username = User.Identity?.Name,
            Role     = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
        });
    }
}