using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WMS.API.Data;
using WMS.API;
using WMS.API.Models;

namespace WMS.API.Services;

public class AuthService
{
    private readonly WmsDbContext _db;
    private readonly IConfiguration _cfg;

    public AuthService(WmsDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    // Đăng nhập: kiểm tra username + bcrypt password → trả JWT (FR-001)
    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        // Tìm user theo username (case-sensitive)
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        // Nếu không tìm thấy hoặc password sai → return null (FR-001)
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        var token = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token     = token,
            ExpiresIn = 86400,  // 24h = 24 × 3600 giây
            Role      = user.Role,
            FullName  = user.FullName ?? user.Username,
            UserId    = user.Id
        };
    }

    // Tạo JWT token với claims: id, username, role (để RBAC hoạt động)
    private string GenerateJwtToken(AppUser user)
    {
        var secretKey = _cfg["JwtSettings:SecretKey"]!;
        var key       = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds     = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),       // lấy UserId từ token
            new Claim(ClaimTypes.Name,           user.Username),
            new Claim(ClaimTypes.Role,           user.Role),    // "Admin" | "Staff"
            new Claim("fullName",               user.FullName ?? ""),
        };

        var expiresHours = int.Parse(_cfg["JwtSettings:ExpiresHours"] ?? "24");

        var token = new JwtSecurityToken(
            issuer:             _cfg["JwtSettings:Issuer"],
            audience:           _cfg["JwtSettings:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(expiresHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Tạo tài khoản mới (hash password bằng bcrypt)
    public async Task<AppUser> CreateUserAsync(CreateUserDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại");

        var user = new AppUser
        {
            Id           = "U" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            Username     = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),  // hash bcrypt
            Role         = dto.Role,
            FullName     = dto.FullName,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }
}