using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Data;
using Project498.WebApi.Models;

namespace Project498.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComicsController(ComicsDbContext comicsDb) : ControllerBase
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "available",
        "checked_out"
    };

    [HttpGet]
    public async Task<IActionResult> GetComics(
        [FromQuery] string? title,
        [FromQuery] string? publisher,
        [FromQuery(Name = "issue_number")] int? issueNumber,
        [FromQuery(Name = "year_published")] int? yearPublished,
        [FromQuery] string? character)
    {
        IQueryable<Comic> query = comicsDb.Comics
            .Include(c => c.ComicCharacters)
            .ThenInclude(cc => cc.Character)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
        {
            var titleTerm = title.Trim().ToLower();
            query = query.Where(c => c.Title.ToLower().Contains(titleTerm));
        }

        if (!string.IsNullOrWhiteSpace(publisher))
        {
            var publisherTerm = publisher.Trim().ToLower();
            query = query.Where(c => c.Publisher.ToLower().Contains(publisherTerm));
        }

        if (issueNumber.HasValue)
        {
            query = query.Where(c => c.IssueNumber == issueNumber.Value);
        }

        if (yearPublished.HasValue)
        {
            query = query.Where(c => c.YearPublished == yearPublished.Value);
        }

        if (!string.IsNullOrWhiteSpace(character))
        {
            var characterTerm = character.Trim().ToLower();
            query = query.Where(c => c.ComicCharacters.Any(cc =>
                cc.Character.Name.ToLower().Contains(characterTerm) ||
                cc.Character.Alias.ToLower().Contains(characterTerm)));
        }

        var comics = await query
            .OrderBy(c => c.ComicId)
            .Select(c => new ComicResponse(
                c.ComicId,
                c.Title,
                c.IssueNumber,
                c.YearPublished,
                c.Publisher,
                c.Status,
                c.CheckedOutBy,
                c.ComicCharacters.Select(cc => cc.CharacterId).ToList(),
                c.ComicCharacters.Select(cc => cc.Character.Alias).ToList()
            ))
            .ToListAsync();

        return Ok(comics);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetComic(int id)
    {
        var comic = await comicsDb.Comics
            .Include(c => c.ComicCharacters)
            .ThenInclude(cc => cc.Character)
            .Where(c => c.ComicId == id)
            .Select(c => new ComicResponse(
                c.ComicId,
                c.Title,
                c.IssueNumber,
                c.YearPublished,
                c.Publisher,
                c.Status,
                c.CheckedOutBy,
                c.ComicCharacters.Select(cc => cc.CharacterId).ToList(),
                c.ComicCharacters.Select(cc => cc.Character.Alias).ToList()
            ))
            .FirstOrDefaultAsync();

        return comic is null
            ? NotFound(new ErrorResponse("COMIC_NOT_FOUND", $"Comic {id} was not found."))
            : Ok(comic);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddComic([FromBody] ComicUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Publisher))
        {
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", "Title and publisher are required."));
        }

        if (!AllowedStatuses.Contains(request.Status))
        {
            return BadRequest(new ErrorResponse("INVALID_STATUS", "Status must be available or checked_out."));
        }

        var comic = new Comic
        {
            Title = request.Title.Trim(),
            IssueNumber = request.IssueNumber,
            YearPublished = request.YearPublished,
            Publisher = request.Publisher.Trim(),
            Status = request.Status.ToLower(),
            CheckedOutBy = request.CheckedOutBy
        };

        comicsDb.Comics.Add(comic);
        await comicsDb.SaveChangesAsync();

        var linksResult = await UpsertCharacterLinks(comic.ComicId, request.CharacterIds);
        if (linksResult is not null)
        {
            return linksResult;
        }
        
        await comicsDb.SaveChangesAsync();

        return await GetComic(comic.ComicId);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditComic(int id, [FromBody] ComicUpsertRequest request)
    {
        var comic = await comicsDb.Comics.FindAsync(id);
        if (comic is null)
        {
            return NotFound(new ErrorResponse("COMIC_NOT_FOUND", $"Comic {id} was not found."));
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Publisher))
        {
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", "Title and publisher are required."));
        }

        if (!AllowedStatuses.Contains(request.Status))
        {
            return BadRequest(new ErrorResponse("INVALID_STATUS", "Status must be available or checked_out."));
        }

        comic.Title = request.Title.Trim();
        comic.IssueNumber = request.IssueNumber;
        comic.YearPublished = request.YearPublished;
        comic.Publisher = request.Publisher.Trim();
        comic.Status = request.Status.ToLower();
        comic.CheckedOutBy = request.CheckedOutBy;

        var linksResult = await UpsertCharacterLinks(id, request.CharacterIds);
        if (linksResult is not null)
        {
            return linksResult;
        }

        await comicsDb.SaveChangesAsync();
        return await GetComic(id);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComic(int id)
    {
        var comic = await comicsDb.Comics.FindAsync(id);
        if (comic is null)
        {
            return NotFound(new ErrorResponse("COMIC_NOT_FOUND", $"Comic {id} was not found."));
        }

        comicsDb.Comics.Remove(comic);
        await comicsDb.SaveChangesAsync();
        return NoContent();
    }

    private async Task<IActionResult?> UpsertCharacterLinks(int comicId, List<int>? characterIds)
    {
        if (characterIds is null)
        {
            return null;
        }

        var normalizedIds = characterIds.Distinct().ToList();
        var foundIds = await comicsDb.Characters
            .Where(c => normalizedIds.Contains(c.CharacterId))
            .Select(c => c.CharacterId)
            .ToListAsync();

        var missingIds = normalizedIds.Except(foundIds).ToList();
        if (missingIds.Count > 0)
        {
            return BadRequest(new ErrorResponse("CHARACTER_NOT_FOUND",
                $"Unknown character IDs: {string.Join(", ", missingIds)}"));
        }

        var existingLinks = comicsDb.ComicCharacters.Where(cc => cc.ComicId == comicId);
        comicsDb.ComicCharacters.RemoveRange(existingLinks);

        var newLinks = normalizedIds.Select(characterId => new ComicCharacter
        {
            ComicId = comicId,
            CharacterId = characterId
        });
        await comicsDb.ComicCharacters.AddRangeAsync(newLinks);

        return null;
    }
}

public record ComicUpsertRequest(
    string Title,
    int IssueNumber,
    int YearPublished,
    string Publisher,
    string Status,
    int? CheckedOutBy,
    List<int>? CharacterIds
);

public record ComicResponse(
    int ComicId,
    string Title,
    int IssueNumber,
    int YearPublished,
    string Publisher,
    string Status,
    int? CheckedOutBy,
    List<int> CharacterIds,
    List<string> CharacterNames
);

public record ErrorResponse(string Code, string Message);