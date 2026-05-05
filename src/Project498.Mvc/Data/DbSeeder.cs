using Project498.Mvc.Models;

namespace Project498.Mvc.Data;

/// <summary>
/// Seeds the app database (users) with a demo account on first run.
/// Comics seeding is handled by Project498.WebApi.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAppAsync(AppDbContext db)
    {
        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                FirstName = "Demo",
                LastName = "Demoson",
                Username = "demo",
                Email = "demo@demo.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Demo123")
            });

            await db.SaveChangesAsync();
        }
    }
}
