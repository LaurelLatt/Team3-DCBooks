namespace Project498.WebApi.Models;

public class Character
{
    public int CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<ComicCharacter> ComicCharacters { get; set; } = new List<ComicCharacter>();
}