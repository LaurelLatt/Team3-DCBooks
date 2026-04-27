using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Controllers;
using Project498.WebApi.Data;
using Project498.WebApi.Models;

namespace Project498.WebApi.Tests;

public class ComicsControllerTests
{
    private ComicsDbContext CreateFreshDbContext()
    {
        var options = new DbContextOptionsBuilder<ComicsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ComicsDbContext(options);
    }
    
    private async Task<(ComicsController controller, ComicsDbContext context)> 
        CreateComicsControllerWithSeedData()
    {
        var context = CreateFreshDbContext();

        context.Comics.AddRange(
            new Comic
            {
                ComicId = 1,
                Title = "Batman",
                IssueNumber = 1,
                YearPublished = 2020,
                Publisher = "DC",
                Status = "available"
            },
            new Comic
            {
                ComicId = 2,
                Title = "Wonder Woman",
                IssueNumber = 4,
                YearPublished = 2021,
                Publisher = "DC",
                Status = "available"
            },
            new Comic
            {
                ComicId = 3,
                Title = "Spider-Man",
                IssueNumber = 1,
                YearPublished = 2020,
                Publisher = "Marvel",
                Status = "available"
            }
        );

        await context.SaveChangesAsync();

        var controller = new ComicsController(context);
        return (controller, context);
    }
    
    private async Task<(ComicsController controller, ComicsDbContext context)> CreateControllerWithCharacters()
    {
        var context = CreateFreshDbContext();

        var character1 = new Character
        {
            CharacterId = 1,
            Name = "Bruce Wayne",
            Alias = "Batman"
        };

        var character2 = new Character
        {
            CharacterId = 2,
            Name = "Diana Prince",
            Alias = "Wonder Woman"
        };

        context.Characters.AddRange(character1, character2);

        context.Comics.Add(new Comic
        {
            ComicId = 1,
            Title = "Batman",
            IssueNumber = 1,
            YearPublished = 2020,
            Publisher = "DC",
            Status = "available"
        });

        await context.SaveChangesAsync();

        return (new ComicsController(context), context);
    }
    
    [Fact]
    public async Task GetComics_ReturnsAll_WhenNoFilters()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var result = await controller.GetComics(null, null, null, null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comics = Assert.IsType<List<ComicResponse>>(ok.Value);

        Assert.Equal(3, comics.Count);
    }
    
    [Fact]
    public async Task GetComics_FiltersByTitle()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var result = await controller.GetComics("batman", null, null, null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comics = Assert.IsType<List<ComicResponse>>(ok.Value);

        Assert.Single(comics);
        Assert.Equal("Batman", comics[0].Title);
    }
    
    [Fact]
    public async Task GetComics_FiltersByPublisher()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var result = await controller.GetComics(null, "marvel", null, null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comics = Assert.IsType<List<ComicResponse>>(ok.Value);

        Assert.Single(comics);
        Assert.Equal("Spider-Man", comics[0].Title);
    }
    
    [Fact]
    public async Task GetComics_FiltersByIssueNumber()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var result = await controller.GetComics(null, null, 1, null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comics = Assert.IsType<List<ComicResponse>>(ok.Value);

        Assert.Equal(2, comics.Count); // Batman + Spider-Man
    }
    
    [Fact]
    public async Task GetComics_FiltersByYear()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var result = await controller.GetComics(null, null, null, 2020, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comics = Assert.IsType<List<ComicResponse>>(ok.Value);

        Assert.Equal(2, comics.Count);
    }
    
    [Fact]
    public async Task GetComics_FiltersByCharacter()
    {
        var (controller, context) = await CreateControllerWithCharacters();

        // link Batman character to comic
        context.ComicCharacters.Add(new ComicCharacter
        {
            ComicId = 1,
            CharacterId = 1
        });

        await context.SaveChangesAsync();

        var result = await controller.GetComics(null, null, null, null, "batman");

        var ok = Assert.IsType<OkObjectResult>(result);
        var comics = Assert.IsType<List<ComicResponse>>(ok.Value);

        Assert.Single(comics);
        Assert.Equal("Batman", comics[0].Title);
    }
    
    [Fact]
    public async Task GetComics_FiltersByMultipleFields()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var result = await controller.GetComics("man", "dc", null, null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comics = Assert.IsType<List<ComicResponse>>(ok.Value);

        Assert.Equal(2, comics.Count); // Batman + Wonder Woman
    }
    
    
    [Fact]
    public async Task GetComic_ReturnsComic_WhenExists()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var result = await controller.GetComic(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comic = Assert.IsType<ComicResponse>(ok.Value);

        Assert.Equal("Batman", comic.Title);
    }
    
    [Fact]
    public async Task GetComic_ReturnsNotFound_WhenMissing()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var result = await controller.GetComic(999);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFound.Value);

        Assert.Equal("COMIC_NOT_FOUND", error.Code);
    }
    
    [Fact]
    public async Task AddComic_ReturnsBadRequest_WhenTitleMissing()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var request = new ComicUpsertRequest(
            "", 1, 2020, "DC", "available", null, null
        );

        var result = await controller.AddComic(request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(bad.Value);

        Assert.Equal("VALIDATION_ERROR", error.Code);
    }
    
    [Fact]
    public async Task AddComic_ReturnsBadRequest_WhenPublisherMissing()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var request = new ComicUpsertRequest(
            "Batman", 1, 2020, "", "available", null, null
        );

        var result = await controller.AddComic(request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(bad.Value);

        Assert.Equal("VALIDATION_ERROR", error.Code);
    }
    
    [Fact]
    public async Task AddComic_ReturnsBadRequest_WhenStatusInvalid()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var request = new ComicUpsertRequest(
            "Batman", 1, 2020, "DC", "invalid", null, null
        );

        var result = await controller.AddComic(request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(bad.Value);

        Assert.Equal("INVALID_STATUS", error.Code);
    }
    
    [Fact]
    public async Task AddComic_ReturnsBadRequest_WhenCharacterNotFound()
    {
        var (controller, _) = await CreateControllerWithCharacters();

        var request = new ComicUpsertRequest(
            "Batman", 1, 2020, "DC", "available", null, new List<int> { 999 }
        );

        var result = await controller.AddComic(request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(bad.Value);

        Assert.Equal("CHARACTER_NOT_FOUND", error.Code);
    }
    
    [Fact]
    public async Task AddComic_CreatesComic_WhenValid()
    {
        var (controller, context) = await CreateComicsControllerWithSeedData();

        var request = new ComicUpsertRequest(
            "The Flash", 1, 2022, "DC", "available", null, null
        );

        var result = await controller.AddComic(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comic = Assert.IsType<ComicResponse>(ok.Value);

        Assert.Equal("The Flash", comic.Title);

        // verify DB
        Assert.Equal(4, context.Comics.Count());
    }
    
    [Fact]
    public async Task AddComic_CreatesComic_WithCharacters()
    {
        var (controller, context) = await CreateControllerWithCharacters();

        var request = new ComicUpsertRequest(
            "Batman Returns",
            2,
            2022,
            "DC",
            "available",
            null,
            new List<int> { 1 } // existing character
        );

        var result = await controller.AddComic(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comic = Assert.IsType<ComicResponse>(ok.Value);

        Assert.Single(comic.CharacterIds);
        Assert.Equal(1, comic.CharacterIds[0]);

        // verify join table
        var links = context.ComicCharacters.ToList();
        Assert.Single(links);
    }
    
    [Fact]
    public async Task AddComic_RemovesDuplicateCharacterIds()
    {
        var (controller, context) = await CreateControllerWithCharacters();

        var request = new ComicUpsertRequest(
            "Batman",
            1,
            2022,
            "DC",
            "available",
            null,
            new List<int> { 1, 1, 1 }
        );

        var result = await controller.AddComic(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comic = Assert.IsType<ComicResponse>(ok.Value);

        Assert.Single(comic.CharacterIds);
    }
    
    [Fact]
    public async Task EditComic_ReturnsNotFound_WhenComicMissing()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var request = new ComicUpsertRequest(
            "New Title", 1, 2022, "DC", "available", null, null
        );

        var result = await controller.EditComic(999, request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFound.Value);

        Assert.Equal("COMIC_NOT_FOUND", error.Code);
    }
    
    [Theory]
    [InlineData("Batman", "")]
    [InlineData("", "DC")]
    [InlineData("", "")]
    public async Task EditComic_ReturnsBadRequest_WhenInvalidFields(string title, string publisher)
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var request = new ComicUpsertRequest(
            title, 1, 2022, publisher, "available", null, null
        );

        var result = await controller.EditComic(1, request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(bad.Value);
        
        Assert.Equal("VALIDATION_ERROR", error.Code);
    }
    
    [Fact]
    public async Task EditComic_ReturnsBadRequest_WhenStatusInvalid()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var request = new ComicUpsertRequest(
            "Batman", 1, 2022, "DC", "wrong", null, null
        );

        var result = await controller.EditComic(1, request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("INVALID_STATUS", ((ErrorResponse)bad.Value!).Code);
    }
    
    [Fact]
    public async Task EditComic_ReturnsBadRequest_WhenCharacterNotFound()
    {
        var (controller, _) = await CreateControllerWithCharacters();

        var request = new ComicUpsertRequest(
            "Batman", 1, 2022, "DC", "available", null, new List<int> { 999 }
        );

        var result = await controller.EditComic(1, request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("CHARACTER_NOT_FOUND", ((ErrorResponse)bad.Value!).Code);
    }
    
    [Fact]
    public async Task EditComic_UpdatesFields_WhenValid()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var request = new ComicUpsertRequest(
            "Updated Batman", 5, 2023, "DC", "checked_out", 10, null
        );

        var result = await controller.EditComic(1, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comic = Assert.IsType<ComicResponse>(ok.Value);

        Assert.Equal("Updated Batman", comic.Title);
        Assert.Equal(5, comic.IssueNumber);
        Assert.Equal("checked_out", comic.Status);
        Assert.Equal(10, comic.CheckedOutBy);
    }
    
    [Fact]
    public async Task EditComic_ReplacesCharacterLinks()
    {
        var (controller, context) = await CreateControllerWithCharacters();

        // initial link: comic 1 → character 1
        context.ComicCharacters.Add(new ComicCharacter
        {
            ComicId = 1,
            CharacterId = 1
        });

        await context.SaveChangesAsync();

        // update to character 2 instead
        var request = new ComicUpsertRequest(
            "Batman",
            1,
            2022,
            "DC",
            "available",
            null,
            new List<int> { 2 }
        );

        var result = await controller.EditComic(1, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comic = Assert.IsType<ComicResponse>(ok.Value);

        Assert.Single(comic.CharacterIds);
        Assert.Equal(2, comic.CharacterIds[0]);

        // verify DB: old link removed
        var links = context.ComicCharacters.ToList();
        Assert.Single(links);
        Assert.Equal(2, links[0].CharacterId);
    }
    
    [Fact]
    public async Task EditComic_ClearsCharacterLinks_WhenEmptyList()
    {
        var (controller, context) = await CreateControllerWithCharacters();

        context.ComicCharacters.Add(new ComicCharacter
        {
            ComicId = 1,
            CharacterId = 1
        });

        await context.SaveChangesAsync();

        var request = new ComicUpsertRequest(
            "Batman",
            1,
            2022,
            "DC",
            "available",
            null,
            new List<int>() // empty
        );

        var result = await controller.EditComic(1, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var comic = Assert.IsType<ComicResponse>(ok.Value);

        Assert.Empty(comic.CharacterIds);

        // DB should also be empty
        Assert.Empty(context.ComicCharacters);
    }
    
    [Fact]
    public async Task DeleteComic_ReturnsNotFound_WhenMissing()
    {
        var (controller, _) = await CreateComicsControllerWithSeedData();

        var result = await controller.DeleteComic(999);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFound.Value);
        
        Assert.Equal("COMIC_NOT_FOUND", error.Code);
    }
    
    [Fact]
    public async Task DeleteComic_RemovesComic_WhenExists()
    {
        var (controller, context) = await CreateComicsControllerWithSeedData();

        var result = await controller.DeleteComic(1);

        Assert.IsType<NoContentResult>(result);

        Assert.Empty(context.Comics.Where(c => c.ComicId == 1));
    }
    
    
}