namespace Project498.WebApi.Models;

public class ComicCharacter
{
    public int ComicId { get; set; }
    public int CharacterId { get; set; }

    public Comic Comic { get; set; } = null!;
    public Character Character { get; set; } = null!;
}