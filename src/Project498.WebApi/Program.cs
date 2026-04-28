using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Data;
using Project498.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// ComicsDbContext is the only database this service owns.
// AppDbContext (users, checkouts) lives in Project498.Mvc.
builder.Services.AddDbContext<ComicsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ComicsConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var comicsDb = scope.ServiceProvider.GetRequiredService<ComicsDbContext>();
    await comicsDb.Database.EnsureCreatedAsync();
    await DbSeeder.SeedComicsAsync(comicsDb);

    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Project498 Backend API");
    });
}

app.UseHttpsRedirection();

// Validate the service-to-service API key on every inbound request.
// This replaces user JWT authentication — the backend has no concept of user identity.
// Only Project498.Mvc is trusted to call this API.
// See: Project498.WebApi.Middleware.ApiKeyMiddleware
// See: Project498.WebApi.Constants.ApiKeyConstants
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

app.Run();
