using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Project498.WebApi.Data;
using Project498.WebApi.Models;

namespace Project498.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password) ||
            string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.FirstName) ||
            string.IsNullOrWhiteSpace(user.LastName))
        {
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", "All registration fields are required."));
        }

        if (await _context.Users.AnyAsync(u => u.Username == user.Username))
        {
            return BadRequest(new ErrorResponse("USERNAME_EXISTS", "Username already exists."));
        }

        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
        {
            return BadRequest(new ErrorResponse("EMAIL_EXISTS", "Email already exists."));
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { message = "User registered successfully." });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = _context.Users.SingleOrDefault(u => u.Username == request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return Unauthorized(new ErrorResponse("INVALID_CREDENTIALS", "Invalid username or password."));

        var token = GenerateToken(user);
        return Ok(new { access_token = token, token });
    }
    
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var token = Request.Headers["Authorization"]
            .ToString()
            .Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
            return BadRequest(new ErrorResponse("NO_TOKEN", "Token is required."));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var revokedToken = new RevokedToken
        {
            Token = token,
            Expiration = jwt.ValidTo
        };

        _context.RevokedTokens.Add(revokedToken);
        _context.SaveChanges();

        return Ok(new { message = "Logged out successfully." });
    }

    private string GenerateToken(User user)
    {
        var secret = _configuration["Jwt:Secret"]!;
        var expiresInHours = int.Parse(_configuration["Jwt:ExpiresInHours"]!);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("user_id", user.UserId.ToString()),
            new Claim("username", user.Username)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiresInHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RevokedToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}