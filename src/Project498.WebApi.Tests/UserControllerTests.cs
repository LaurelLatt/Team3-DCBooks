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
        var error = Assert.IsType<ErrorResponse>(badRequest.Value);
        Assert.Equal("VALIDATION_ERROR", error.Code);
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
        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(conflict.Value);
        Assert.Equal("USERNAME_EXISTS", error.Code);
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
        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(conflict.Value);
        Assert.Equal("EMAIL_EXISTS", error.Code);
    }

    [Fact]
    public async Task AddUser_HashesPasswordBeforeSave()
    {
        UsersController controller = await CreateControllerWithSeededUser();
        User newUser = new User
        {
            Username = "hashme",
            Email = "hashme@email.com",
            Password = "password123!"
        };

        var result = await controller.AddUser(newUser);
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedUser = Assert.IsType<User>(createdResult.Value);

        Assert.NotEqual("password123!", returnedUser.Password);
        Assert.True(BCrypt.Net.BCrypt.Verify("password123!", returnedUser.Password));
    }
}