using Microsoft.AspNetCore.Mvc;

namespace Project498.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComicsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetComics()
    {
        return Ok("coming soon");
    }

    [HttpGet("{id}")]
    public IActionResult GetComic(int id)
    {
        return Ok("coming soon");
    }

    [HttpPost]
    public IActionResult AddComic()
    {
        return Ok("coming soon");
    }

    [HttpPut("{id}")]
    public IActionResult EditComic(int id)
    {
        return Ok("coming soon");
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteComic(int id)
    {
        return Ok("coming soon");
    }
}