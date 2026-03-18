using Microsoft.AspNetCore.Mvc;

namespace Project498.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CharactersController : ControllerBase
{
    [HttpGet]
    public IActionResult GetCharacters()
    {
        return Ok("coming soon");
    }

    [HttpGet("{id}")]
    public IActionResult GetCharacter(int id)
    {
        return Ok("coming soon");
    }

    [HttpPost]
    public IActionResult AddCharacter()
    {
        return Ok("coming soon");
    }

    [HttpPut("{id}")]
    public IActionResult EditCharacter(int id)
    {
        return Ok("coming soon");
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteCharacter(int id)
    {
        return Ok("coming soon");
    }
}