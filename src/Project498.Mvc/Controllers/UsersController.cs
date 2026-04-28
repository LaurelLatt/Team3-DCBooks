using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.Mvc.Data;
using Project498.Mvc.Models;
using Project498.Mvc.Models.DTOs;

namespace Project498.Mvc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new ErrorResponse("USER_NOT_FOUND", $"User {id} was not found."));
        }

        return user;
    }

    [HttpPost]
    public async Task<ActionResult<User>> AddUser(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Username) ||
            string.IsNullOrWhiteSpace(user.Password))
        {
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", "Username and password are required."));
        }

        if (await _context.Users.AnyAsync(u => u.Username == user.Username))
        {
            return Conflict(new ErrorResponse("USERNAME_EXISTS", "Username already exists."));
        }

        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
        {
            return Conflict(new ErrorResponse("EMAIL_EXISTS", "Email already exists."));
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditUser(int id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new ErrorResponse("USER_NOT_FOUND", $"User {id} was not found."));
        }

        user.FirstName = dto.FirstName ?? user.FirstName;
        user.LastName = dto.LastName ?? user.LastName;
        user.Username = dto.Username ?? user.Username;
        user.Email = dto.Email ?? user.Email;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new ErrorResponse("USER_NOT_FOUND", $"User {id} was not found."));
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("check-username")]
    public async Task<IActionResult> CheckUsername(string username)
    {
        bool exists = await _context.Users.AnyAsync(u => u.Username == username);
        return Ok(new { exists });
    }

    [HttpGet("check-email")]
    public async Task<IActionResult> CheckEmail(string email)
    {
        bool exists = await _context.Users.AnyAsync(u => u.Email == email);
        return Ok(new { exists });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);

        if (user == null)
            return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.Password))
            return BadRequest(new ErrorResponse("INVALID_OLD_PASSWORD", "The current password is incorrect."));

        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest(new ErrorResponse("PASSWORD_MISMATCH", "New password and confirmation do not match."));

        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Password updated." });
    }
}
