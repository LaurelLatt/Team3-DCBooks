using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Data;
using Project498.WebApi.Models;
using Project498.WebApi.Controllers;

namespace Project498.WebApi.Tests;

public class UserControllerTests
{
    private AppDbContext CreateFreshDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
    
    private async Task<UsersController> CreateControllerWithSeededUser()
    {
        AppDbContext context = CreateFreshDbContext();

        // seed a default test user
        context.Users.Add(new User
        {
            Username = "johndoe",
            Email = "example@email.com",
            Password = "password123"
        });
        await context.SaveChangesAsync();

        return new UsersController(context);
    }

    [Fact]
    public async Task AddUser_ReturnsCreated_WhenValid()
    {
        UsersController controller = await CreateControllerWithSeededUser();
        
        User newUser = new User
        {
            Username = "newuser",
            Email = "new@email.com",
            Password = "password123!",
        };

        var result = await controller.AddUser(newUser);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedUser = Assert.IsType<User>(createdResult.Value);
        Assert.Equal("newuser", returnedUser.Username);
        Assert.Equal("new@email.com", returnedUser.Email);
    }

    [Theory]
    [InlineData(null, "email@email.com", "password123")]
    [InlineData("newuser", "email@email.com", null)]
    [InlineData(null, "email@email.com", null)]
    [InlineData("newuser", "email@email.com", "")]
    [InlineData("", "email@email.com", "password123")]
    [InlineData("", "email@email.com", "")]
    public async Task AddUser_ReturnsBadRequest_WhenUsernameOrPasswordMissing(string username, string email,
        string password)
    {
        UsersController controller = await CreateControllerWithSeededUser();

        User newUser = new User
        {
            Username = username,
            Email = email,
            Password = password
        };
        
        var result = await controller.AddUser(newUser);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Username and password are required", badRequest.Value);
    }

    [Fact]
    public async Task AddUser_ReturnsBadRequest_WhenUsernameExists()
    {
        UsersController controller = await CreateControllerWithSeededUser();

        User newUser = new User
        {
            Username = "johndoe",
            Email = "new@email.com",
            Password = "password123"
        };
        
        var result = await controller.AddUser(newUser);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Username already exists", badRequest.Value);
    }
    
    [Fact]
    public async Task AddUser_ReturnsBadRequest_WhenEmailExists()
    {
        UsersController controller = await CreateControllerWithSeededUser();

        User newUser = new User
        {
            Username = "newuser",
            Email = "example@email.com",
            Password = "password123"
        };
        
        var result = await controller.AddUser(newUser);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Email already exists", badRequest.Value);
    }

    [Fact]
    public async Task Login_ReturnsUser_WhenCredentialsValid()
    {
        UsersController controller = await CreateControllerWithSeededUser();
        
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
    [InlineData("johnDoe", "password123")]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsInvalid(string username, string password)
    {
        UsersController controller = await CreateControllerWithSeededUser();
        User loginData = new User
        {
            Username = username,
            Password = password
        };
        
        var result = await controller.Login(loginData);
        
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
    
    [Theory]
    [InlineData(null, "password123")]
    [InlineData("johndoe", null)]
    public async Task Login_ReturnsBadResponse_WhenFieldMissing(string username, string password)
    {
        UsersController controller = await CreateControllerWithSeededUser();

        User loginData = new User
        {
            Username = username,
            Password = password
        };
        
        var result = await controller.Login(loginData);
        
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}