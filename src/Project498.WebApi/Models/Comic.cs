namespace Project498.WebApi.Models;

public class Comic
{
    public int ComicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int IssueNumber { get; set; }
    public int YearPublished { get; set; }
    public string Publisher { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? CheckedOutBy { get; set; }

    public ICollection<ComicCharacter> ComicCharacters { get; set; } = new List<ComicCharacter>();
}