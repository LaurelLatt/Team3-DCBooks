using Microsoft.AspNetCore.Mvc;

namespace Project498.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetCheckouts()
    {
        return Ok("coming soon");
    }

    [HttpGet("{id}")]
    public IActionResult GetCheckout(int id)
    {
        return Ok("coming soon");
    }

    [HttpPost]
    public IActionResult Checkout()
    {
        return Ok("coming soon");
    }

    [HttpPut("{id}")]
    public IActionResult Return(int id)
    {
        return Ok("coming soon");
    }
}