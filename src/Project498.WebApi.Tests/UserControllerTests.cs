using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Data;
using Project498.WebApi.Models;
using Project498.WebApi.Controllers;
using Project498.WebApi.Models.DTOs;

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
            FirstName = "John",
            LastName = "Doe",
            Username = "johndoe",
            Email = "example@email.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password123")
        });
        await context.SaveChangesAsync();

        return new UsersController(context);
    }
    
    [Fact]
    public async Task GetUser_ReturnsUser_WhenExists()
    {
        UsersController controller = await CreateControllerWithSeededUser();

        var result = await controller.GetUser(1);

        var user = Assert.IsType<User>(result.Value);
        Assert.Equal("johndoe", user.Username);
    }
    
    [Fact]
    public async Task GetUser_ReturnsNotFound_WhenMissing()
    {
        AppDbContext context = CreateFreshDbContext();
        UsersController controller = new UsersController(context);

        var result = await controller.GetUser(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddUser_ReturnsCreated_WhenValid()
    {
        UsersController controller = await CreateControllerWithSeededUser();
        
        User newUser = new User
        {
            Username = "newuser",
            Email = "new@email.com",
            Password = "password123"
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
    
    [Fact]
    public async Task EditUser_UpdatesUserFields()
    {
        AppDbContext context = CreateFreshDbContext();

        context.Users.Add(new User { UserId = 1, FirstName = "Old" });
        await context.SaveChangesAsync();

        UsersController controller = new UsersController(context);

        UpdateUserDto dto = new UpdateUserDto { FirstName = "New" };

        var result = await controller.EditUser(1, dto);

        var user = await context.Users.FindAsync(1);

        Assert.Equal("New", user.FirstName);
        Assert.IsType<NoContentResult>(result);
    }
    
    [Fact]
    public async Task EditUser_ReturnsNotFound_WhenMissing()
    {
        AppDbContext context = CreateFreshDbContext();
        UsersController controller = new UsersController(context);

        var result = await controller.EditUser(999, new UpdateUserDto());

        Assert.IsType<NotFoundObjectResult>(result);
    }
    
    [Fact]
    public async Task DeleteUser_RemovesUser()
    {
        AppDbContext context = CreateFreshDbContext();

        context.Users.Add(new User { UserId = 1 });
        await context.SaveChangesAsync();

        var controller = new UsersController(context);

        var result = await controller.DeleteUser(1);

        Assert.Null(await context.Users.FindAsync(1));
        Assert.IsType<NoContentResult>(result);
    }
    
    [Fact]
    public async Task DeleteUser_ReturnsNotFound_WhenMissing()
    {
        AppDbContext context = CreateFreshDbContext();
        UsersController controller = new UsersController(context);

        var result = await controller.DeleteUser(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }
    
    [Fact]
    public async Task CheckUsername_ReturnsTrue_WhenExists()
    {
        UsersController controller = await CreateControllerWithSeededUser();

        var result = await controller.CheckUsername("johndoe");

        var ok = Assert.IsType<OkObjectResult>(result);

        var value = ok.Value;
        var existsProperty = value.GetType().GetProperty("exists");
        var exists = (bool)existsProperty.GetValue(value);

        Assert.True(exists);
    }
    
    [Fact]
    public async Task CheckUsername_ReturnsFalse_WhenMissing()
    {
        UsersController controller = await CreateControllerWithSeededUser();

        var result = await controller.CheckUsername("randomUsername");

        var ok = Assert.IsType<OkObjectResult>(result);

        var value = ok.Value;
        var existsProperty = value.GetType().GetProperty("exists");
        var exists = (bool)existsProperty.GetValue(value);

        Assert.False(exists);
    }
    
    [Fact]
    public async Task CheckEmail_ReturnsTrue_WhenExists()
    {
        UsersController controller = await CreateControllerWithSeededUser();

        var result = await controller.CheckEmail("example@email.com");

        var ok = Assert.IsType<OkObjectResult>(result);
        
        var value = ok.Value;
        var existsProperty = value.GetType().GetProperty("exists");
        var exists = (bool)existsProperty.GetValue(value);

        Assert.True(exists);
    }
    
    [Fact]
    public async Task CheckEmail_ReturnsFalse_WhenMissing()
    {
        UsersController controller = await CreateControllerWithSeededUser();

        var result = await controller.CheckEmail("newemail@email.com");

        var ok = Assert.IsType<OkObjectResult>(result);
        
        var value = ok.Value;
        var existsProperty = value.GetType().GetProperty("exists");
        var exists = (bool)existsProperty.GetValue(value);

        Assert.False(exists);
    }
    
    [Fact]
    public async Task ChangePassword_UpdatesPassword_WhenValid()
    {
        AppDbContext context = CreateFreshDbContext();

        context.Users.Add(new User
        {
            UserId = 1,
            Password = BCrypt.Net.BCrypt.HashPassword("oldpass")
        });

        await context.SaveChangesAsync();

        UsersController controller = new UsersController(context);

        ChangePasswordDto dto = new ChangePasswordDto
        {
            UserId = 1,
            OldPassword = "oldpass",
            NewPassword = "newpass",
            ConfirmPassword = "newpass"
        };

        var result = await controller.ChangePassword(dto);

        var ok = Assert.IsType<OkObjectResult>(result);

        var user = await context.Users.FindAsync(1);
        Assert.Equal(ok.Value, "Password updated");
        Assert.True(BCrypt.Net.BCrypt.Verify("newpass", user.Password));
    }
    
    [Fact]
    public async Task ChangePassword_ReturnsBadRequest_WhenOldPasswordWrong()
    {
        UsersController controller = await CreateControllerWithSeededUser();

        ChangePasswordDto dto = new ChangePasswordDto
        {
            UserId = 1,
            OldPassword = "wrong",
            NewPassword = "newpass",
            ConfirmPassword = "newpass"
        };

        var result = await controller.ChangePassword(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Incorrect old password", badRequest.Value);
    }
    
    [Fact]
    public async Task ChangePassword_ReturnsBadRequest_WhenNewPasswordMismatch()
    {
        UsersController controller = await CreateControllerWithSeededUser();

        ChangePasswordDto dto = new ChangePasswordDto
        {
            UserId = 1,
            OldPassword = "password123",
            NewPassword = "new1",
            ConfirmPassword = "new2"
        };

        var result = await controller.ChangePassword(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Passwords do not match", badRequest.Value);
    }
    
    [Fact]
    public async Task ChangePassword_ReturnsNotFound_WhenUserMissing()
    {
        var context = CreateFreshDbContext();
        var controller = new UsersController(context);

        var dto = new ChangePasswordDto { UserId = 999 };

        var result = await controller.ChangePassword(dto);

        Assert.IsType<NotFoundResult>(result);
    }
}