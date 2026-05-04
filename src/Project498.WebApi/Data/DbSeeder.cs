using Project498.WebApi.Models;

namespace Project498.WebApi.Data;

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

    public static async Task SeedComicsAsync(ComicsDbContext comicsDb)
    {
        if (!comicsDb.Characters.Any())
        {
            comicsDb.Characters.AddRange(
                new Character { CharacterId = 1, Name = "Bruce Wayne", Alias = "Batman", Description = "The Dark Knight of Gotham City." },
                new Character { CharacterId = 2, Name = "Clark Kent", Alias = "Superman", Description = "The Man of Steel from Krypton." },
                new Character { CharacterId = 3, Name = "Diana Prince", Alias = "Wonder Woman", Description = "Amazonian warrior and princess." },
                new Character { CharacterId = 4, Name = "Barry Allen", Alias = "The Flash", Description = "The fastest man alive." },
                new Character { CharacterId = 5, Name = "Hal Jordan", Alias = "Green Lantern", Description = "Fearless member of the Green Lantern Corps." },
                new Character { CharacterId = 6, Name = "Arthur Curry", Alias = "Aquaman", Description = "King of Atlantis and ruler of the seas." },
                new Character { CharacterId = 7, Name = "Victor Stone", Alias = "Cyborg", Description = "Half-human, half-machine hero." },
                new Character { CharacterId = 8, Name = "Oliver Queen", Alias = "Green Arrow", Description = "Expert archer and vigilante." },
                new Character { CharacterId = 9, Name = "Billy Batson", Alias = "Shazam", Description = "Young boy who transforms into a powerful hero." },
                new Character { CharacterId = 10, Name = "John Constantine", Alias = "Constantine", Description = "Occult detective and master of dark arts." },
                new Character { CharacterId = 11, Name = "Dick Grayson", Alias = "Nightwing", Description = "Former Robin turned independent hero." },
                new Character { CharacterId = 12, Name = "Selina Kyle", Alias = "Catwoman", Description = "Skilled thief and occasional anti-hero." },
                new Character { CharacterId = 13, Name = "Joker", Alias = "Joker", Description = "Batman’s chaotic arch-nemesis." },
                new Character { CharacterId = 14, Name = "Harleen Quinzel", Alias = "Harley Quinn", Description = "Former psychiatrist turned chaotic anti-hero." },
                new Character { CharacterId = 15, Name = "Pamela Isley", Alias = "Poison Ivy", Description = "Eco-terrorist with control over plant life." },
                new Character { CharacterId = 16, Name = "Zatanna Zatara", Alias = "Zatanna", Description = "Powerful magician who casts spells backwards." },
                new Character { CharacterId = 17, Name = "Wally West", Alias = "The Flash", Description = "The third Flash and former Kid Flash." }
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
                new Comic { ComicId = 5, Title = "Green Lantern: Emerald Dawn", IssueNumber = 1, YearPublished = 1989, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 6, Title = "Batman: The Killing Joke", IssueNumber = 1, YearPublished = 1988, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 7, Title = "Batman: The Dark Knight Returns", IssueNumber = 1, YearPublished = 1986, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 8, Title = "Justice League: Origin", IssueNumber = 1, YearPublished = 2011, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 9, Title = "Aquaman: The Trench", IssueNumber = 1, YearPublished = 2012, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 10, Title = "Green Arrow: Year One", IssueNumber = 1, YearPublished = 2007, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 11, Title = "Flashpoint", IssueNumber = 1, YearPublished = 2011, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 12, Title = "Superman: Man of Steel", IssueNumber = 2, YearPublished = 1986, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 13, Title = "Shazam!: The New Beginning", IssueNumber = 1, YearPublished = 1987, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 14, Title = "Nightwing: Year One", IssueNumber = 101, YearPublished = 2005, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 15, Title = "John Constantine: Hellblazer", IssueNumber = 1, YearPublished = 2019, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 16, Title = "Gotham City Sirens", IssueNumber = 1, YearPublished = 2009, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 17, Title = "Justice League Dark", IssueNumber = 2, YearPublished = 2011, Publisher = "DC Comics", Status = "available", CheckedOutBy = null }
            );
            await comicsDb.SaveChangesAsync();
        }

        if (!comicsDb.ComicCharacters.Any())
        {
            comicsDb.ComicCharacters.AddRange(
                new ComicCharacter { ComicId = 1, CharacterId = 1 },
                new ComicCharacter { ComicId = 2, CharacterId = 2 },
                new ComicCharacter { ComicId = 3, CharacterId = 3 },
                new ComicCharacter { ComicId = 4, CharacterId = 17 },
                new ComicCharacter { ComicId = 5, CharacterId = 5 },
                new ComicCharacter { ComicId = 1, CharacterId = 4 },
                new ComicCharacter { ComicId = 6, CharacterId = 1 },   
                new ComicCharacter { ComicId = 6, CharacterId = 13 },
                new ComicCharacter { ComicId = 7, CharacterId = 1 },  
                new ComicCharacter { ComicId = 7, CharacterId = 13 },
                new ComicCharacter { ComicId = 8, CharacterId = 1 },  
                new ComicCharacter { ComicId = 8, CharacterId = 2 },  
                new ComicCharacter { ComicId = 8, CharacterId = 3 },  
                new ComicCharacter { ComicId = 8, CharacterId = 4 },  
                new ComicCharacter { ComicId = 8, CharacterId = 5 },  
                new ComicCharacter { ComicId = 8, CharacterId = 6 },  
                new ComicCharacter { ComicId = 9, CharacterId = 6 }, 
                new ComicCharacter { ComicId = 10, CharacterId = 8 }, 
                new ComicCharacter { ComicId = 11, CharacterId = 4 }, 
                new ComicCharacter { ComicId = 12, CharacterId = 2 },
                new ComicCharacter { ComicId = 13, CharacterId = 9 }, 
                new ComicCharacter { ComicId = 14, CharacterId = 11 }, 
                new ComicCharacter { ComicId = 1, CharacterId = 12 },
                new ComicCharacter { ComicId = 15, CharacterId = 10 },
                new ComicCharacter { ComicId = 16, CharacterId = 14 },
                new ComicCharacter { ComicId = 16, CharacterId = 15 },
                new ComicCharacter { ComicId = 16, CharacterId = 12 },
                new ComicCharacter { ComicId = 17, CharacterId = 16 },
                new ComicCharacter { ComicId = 17, CharacterId = 10 }
            );
            await comicsDb.SaveChangesAsync();
        }
    }
}