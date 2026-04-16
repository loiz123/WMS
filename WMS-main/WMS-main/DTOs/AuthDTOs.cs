using System.ComponentModel.DataAnnotations;

namespace WMS.API;

public class LoginRequest
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string Token     { get; set; } = "";
    public int    ExpiresIn { get; set; }  // giây (86400 = 24h)
    public string Role     { get; set; } = "";
    public string FullName { get; set; } = "";
    public string UserId   { get; set; } = "";
}

public class CreateUserDto
{
    [Required] [MaxLength(100)] public string Username { get; set; } = "";
    [Required] [MinLength(6, ErrorMessage = "Mật khẩu ≥ 6 ký tự")]
    public string Password { get; set; } = "";
    [MaxLength(200)] public string? FullName { get; set; }
    [Required] public string Role { get; set; } = "Staff";
}