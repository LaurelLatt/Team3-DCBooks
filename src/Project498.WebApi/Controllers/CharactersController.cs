using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Data;
using Project498.WebApi.Models;

namespace Project498.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CharactersController(ComicsDbContext comicsDb) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCharacters()
    {
        var characters = await comicsDb.Characters
            .OrderBy(c => c.CharacterId)
            .ToListAsync();
        return Ok(characters);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCharacter(int id)
    {
        var character = await comicsDb.Characters.FindAsync(id);
        return character is null
            ? NotFound(new ErrorResponse("CHARACTER_NOT_FOUND", $"Character {id} was not found."))
            : Ok(character);
    }

    [HttpPost]
    public async Task<IActionResult> AddCharacter([FromBody] Character request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", "Character name is required."));
        }

        var character = new Character
        {
            Name = request.Name.Trim(),
            Alias = request.Alias?.Trim() ?? string.Empty,
            Description = request.Description?.Trim() ?? string.Empty
        };

        comicsDb.Characters.Add(character);
        await comicsDb.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCharacter), new { id = character.CharacterId }, character);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditCharacter(int id, [FromBody] Character request)
    {
        var character = await comicsDb.Characters.FindAsync(id);
        if (character is null)
        {
            return NotFound(new ErrorResponse("CHARACTER_NOT_FOUND", $"Character {id} was not found."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", "Character name is required."));
        }

        character.Name = request.Name.Trim();
        character.Alias = request.Alias?.Trim() ?? string.Empty;
        character.Description = request.Description?.Trim() ?? string.Empty;
        await comicsDb.SaveChangesAsync();
        return Ok(character);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCharacter(int id)
    {
        var character = await comicsDb.Characters.FindAsync(id);
        if (character is null)
        {
            return NotFound(new ErrorResponse("CHARACTER_NOT_FOUND", $"Character {id} was not found."));
        }

        comicsDb.Characters.Remove(character);
        await comicsDb.SaveChangesAsync();
        return NoContent();
    }
}