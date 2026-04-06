using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Data;
using Project498.WebApi.Models;
using Project498.WebApi.Controllers;

namespace Project498.WebApi.Tests;

public class UserControllerTests
{
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
    
    [Fact]
    public async Task Login_ReturnsUser_WhenCredentialsValid()
    {
        AppDbContext context = GetDbContext();

        context.Users.Add(new User
        {
            Username = "johndoe",
            Password = "password123"
        });

        await context.SaveChangesAsync();

        UsersController controller = new UsersController(context);

        User loginData = new User
        {
            Username = "johndoe",
            Password = "password123"
        };

        var result = await controller.Login(loginData);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("johndoe", "password124")]
    [InlineData( "", "password123")]
    [InlineData("johndoe", "")]
    [InlineData("johnDoe", "password123")]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsInvalid(string username, string password)
    {
        AppDbContext context = GetDbContext();
        context.Users.Add(new User
            {
                Username = "johndoe",
                Password = "password123"
            }
        );
        
        await context.SaveChangesAsync();
        UsersController controller = new UsersController(context);

        User loginData = new User
        {
            Username = username,
            Password = password
        };
        
        var result = await controller.Login(loginData);
        
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
}