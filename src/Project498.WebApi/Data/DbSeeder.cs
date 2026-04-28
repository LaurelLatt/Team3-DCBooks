using Project498.WebApi.Models;

namespace Project498.WebApi.Data;

/// <summary>
/// Seeds the comics database with initial characters, comics, and character links
/// for development and first-run environments.
/// App DB seeding (users) is handled by Project498.Mvc.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedComicsAsync(ComicsDbContext comicsDb)
    {
        if (!comicsDb.Characters.Any())
        {
            comicsDb.Characters.AddRange(
                new Character { CharacterId = 1, Name = "Bruce Wayne", Alias = "Batman", Description = "The Dark Knight of Gotham City." },
                new Character { CharacterId = 2, Name = "Clark Kent", Alias = "Superman", Description = "The Man of Steel from Krypton." },
                new Character { CharacterId = 3, Name = "Diana Prince", Alias = "Wonder Woman", Description = "Amazonian warrior and princess." },
                new Character { CharacterId = 4, Name = "Barry Allen", Alias = "The Flash", Description = "The fastest man alive." },
                new Character { CharacterId = 5, Name = "Hal Jordan", Alias = "Green Lantern", Description = "Fearless member of the Green Lantern Corps." }
            );
            await comicsDb.SaveChangesAsync();
        }

        if (!comicsDb.Comics.Any())
        {
            comicsDb.Comics.AddRange(
                new Comic { ComicId = 1, Title = "Batman: Year One", IssueNumber = 1, YearPublished = 1987, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 2, Title = "Superman: Man of Steel", IssueNumber = 1, YearPublished = 1986, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 3, Title = "Wonder Woman: Gods and Mortals", IssueNumber = 1, YearPublished = 1987, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 4, Title = "The Flash: Born to Run", IssueNumber = 1, YearPublished = 1994, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 5, Title = "Green Lantern: Emerald Dawn", IssueNumber = 1, YearPublished = 1989, Publisher = "DC Comics", Status = "available", CheckedOutBy = null }
            );
            await comicsDb.SaveChangesAsync();
        }

        if (!comicsDb.ComicCharacters.Any())
        {
            comicsDb.ComicCharacters.AddRange(
                new ComicCharacter { ComicId = 1, CharacterId = 1 },
                new ComicCharacter { ComicId = 2, CharacterId = 2 },
                new ComicCharacter { ComicId = 3, CharacterId = 3 },
                new ComicCharacter { ComicId = 4, CharacterId = 4 },
                new ComicCharacter { ComicId = 5, CharacterId = 5 },
                new ComicCharacter { ComicId = 1, CharacterId = 4 },
                new ComicCharacter { ComicId = 2, CharacterId = 1 }
            );
            await comicsDb.SaveChangesAsync();
        }
    }
}
