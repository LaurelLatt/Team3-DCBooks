using System.Text.Json.Serialization;

namespace Project498.Mvc.Models;

public class User
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    public ICollection<Checkout> Checkouts { get; set; } = new List<Checkout>();
}
