using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Data;
using Project498.WebApi.Models;

namespace Project498.WebApi.Controllers;

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

    [HttpPut("{id}")]
    public async Task<IActionResult> EditUser(int id, User user)
    {
        if (id != user.UserId)
        {
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", "Route id must match user id."));
        }

        _context.Entry(user).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Users.Any(u => u.UserId == id))
            {
                return NotFound(new ErrorResponse("USER_NOT_FOUND", $"User {id} was not found."));
            }

            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
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
}