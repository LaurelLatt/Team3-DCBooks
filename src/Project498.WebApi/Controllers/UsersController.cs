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
            return NotFound();
        }

        return user;
    }

    [HttpPost]
    public async Task<ActionResult<User>> AddUser(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Username) ||
            string.IsNullOrWhiteSpace(user.Password))
        {
            return BadRequest("Username and password are required");
        }

        if (await _context.Users.AnyAsync(u => u.Username == user.Username))
        {
            return BadRequest("Username already exists");
        }

        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
        {
            return BadRequest("Email already exists");
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditUser(int id, User user)
    {
        if (id != user.UserId) return BadRequest();

        _context.Entry(user).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Users.Any(u => u.UserId == id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<User>> Login([FromBody] User loginData)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(loginData.Username) ||
            string.IsNullOrWhiteSpace(loginData.Password))
        {
            return BadRequest("Username and password are required");
        }
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                u.Username == loginData.Username &&
                u.Password == loginData.Password);

        if (user == null)
        {
            return Unauthorized("Invalid username or password");
        }

        return Ok(user);
    }
}