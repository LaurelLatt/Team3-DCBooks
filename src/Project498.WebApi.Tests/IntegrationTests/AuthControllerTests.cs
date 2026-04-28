using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Project498.Mvc.Controllers;
using Project498.Mvc.Data;
using Project498.Mvc.Models;

namespace Project498.WebApi.Tests.IntegrationTests;

public class AuthControllerTests
{
    private AppDbContext CreateFreshDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
    
    private async Task<(AuthController controller, AppDbContext context)> CreateAuthControllerWithSeededUser()
    {
        var context = CreateFreshDbContext();

        context.Users.Add(new User
        {
            FirstName = "John",
            LastName = "Doe",
            Username = "johndoe",
            Email = "example@email.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password123")
        });

        await context.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Jwt:Secret", "this_is_a_super_long_test_secret_key_12345"},
                {"Jwt:ExpiresInHours", "1"}
            })
            .Build();

        var controller = new AuthController(context, config);

        return (controller, context);
    }
    
    [Fact]
    public async Task Register_ReturnsOk_WhenSuccessful()
    {
        var (controller, _) = await CreateAuthControllerWithSeededUser();

        var user = new User
        {
            FirstName = "A",
            LastName = "B",
            Username = "newuser",
            Email = "new@email.com",
            Password = "password"
        };

        var result = await controller.Register(user);

        Assert.IsType<OkObjectResult>(result);
    }
    
    [Fact]
    public async Task Register_SavesUser_WhenSuccessful()
    {
        var (controller, context) = await CreateAuthControllerWithSeededUser();

        var user = new User
        {
            FirstName = "A",
            LastName = "B",
            Username = "newuser",
            Email = "new@email.com",
            Password = "password"
        };

        await controller.Register(user);

        var savedUser = await context.Users
            .FirstOrDefaultAsync(u => u.Username == "newuser");

        Assert.NotNull(savedUser);
    }
    
    [Fact]
    public async Task Register_CreatesUser_WithHashedPassword()
    {
        var (controller, context) = await CreateAuthControllerWithSeededUser();

        var user = new User
        {
            FirstName = "A",
            LastName = "B",
            Username = "newuser",
            Email = "new@email.com",
            Password = "password"
        };

        await controller.Register(user);

        var savedUser = await context.Users
            .FirstOrDefaultAsync(u => u.Username == "newuser");

        Assert.NotNull(savedUser);
        Assert.NotEqual("password", savedUser.Password);
    }
    
    [Theory]
    [InlineData("first", "last", "newuser", "new@email.com", "")]
    [InlineData("first", "last", "newuser", "", "password")]
    [InlineData("first", "last", "", "new@email.com", "password")]
    [InlineData("first", "", "newuser", "new@email.com", "password")]
    [InlineData("", "last", "newuser", "new@email.com", "password")]
    [InlineData("", "", "", "", "")]
    [InlineData(null, null, null, null, null)]
    public async Task Register_ReturnsBadRequest_WhenFieldsMissing(string first, string last,  string username, string email, string password)
    {
        var (controller, _) = await CreateAuthControllerWithSeededUser();

        var user = new User
        {
            FirstName = first,
            LastName = last,
            Username = username, 
            Email = email,
            Password = password
        };

        var result = await controller.Register(user);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        var error = Assert.IsType<ErrorResponse>(badRequest.Value);
        Assert.Equal("VALIDATION_ERROR", error.Code);
    }
    
    [Fact]
    public async Task Register_ReturnsConflict_WhenUsernameExists()
    {
        var (controller, _) = await CreateAuthControllerWithSeededUser();

        var user = new User
        {
            FirstName = "Jane",
            LastName = "Doe",
            Username = "johndoe", // already seeded
            Email = "new@email.com",
            Password = "password"
        };

        var result = await controller.Register(user);

        var conflict = Assert.IsType<ConflictObjectResult>(result);

        var error = Assert.IsType<ErrorResponse>(conflict.Value);
        Assert.Equal("USERNAME_EXISTS", error.Code);
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenEmailExists()
    {
        var (controller, _) = await CreateAuthControllerWithSeededUser();

        var user = new User
        {
            FirstName = "Jane",
            LastName = "Doe",
            Username = "newuser",
            Email = "example@email.com", // already seeded
            Password = "password"
        };

        var result = await controller.Register(user);

        var conflict = Assert.IsType<ConflictObjectResult>(result);

        var error = Assert.IsType<ErrorResponse>(conflict.Value);
        Assert.Equal("EMAIL_EXISTS", error.Code);
    }
    
    [Fact]
    public async Task Login_ReturnsToken_WhenValidCredentials()
    {
        var (controller, _) = await CreateAuthControllerWithSeededUser();

        var request = new LoginRequest
        {
            Username = "johndoe",
            Password = "password123"
        };

        var result = await controller.Login(request);

        var okResult = Assert.IsType<OkObjectResult>(result);

        var value = okResult.Value;

        var accessTokenProp = value.GetType().GetProperty("access_token");
        var tokenValue = accessTokenProp?.GetValue(value, null);

        Assert.NotNull(tokenValue);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIncorrect()
    {
        var (controller, _) = await CreateAuthControllerWithSeededUser();

        var request = new LoginRequest
        {
            Username = "johndoe",
            Password = "wrongpassword"
        };

        var result = await controller.Login(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Theory]
    [InlineData("wrongusername")]
    [InlineData("Johndoe")]
    public async Task Login_ReturnsUnauthorized_WhenUsernameIncorrect(string username)
    {
        var (controller, _) = await CreateAuthControllerWithSeededUser();

        var request = new LoginRequest
        {
            Username = username,
            Password = "password123"
        };

        var result = await controller.Login(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsToken_WithCorrectClaims()
    {
        var (controller, _) = await CreateAuthControllerWithSeededUser();

        var request = new LoginRequest
        {
            Username = "johndoe",
            Password = "password123"
        };

        var result = await controller.Login(request);
        var okResult = Assert.IsType<OkObjectResult>(result);

        // extract token using reflection
        var value = okResult.Value;
        var tokenProp = value.GetType().GetProperty("access_token");
        var token = tokenProp?.GetValue(value)?.ToString();

        Assert.NotNull(token);

        // decode JWT
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // verify claims
        var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "user_id");
        var usernameClaim = jwt.Claims.FirstOrDefault(c => c.Type == "username");

        Assert.NotNull(userIdClaim);
        Assert.NotNull(usernameClaim);

        Assert.Equal("johndoe", usernameClaim.Value);
    }
}