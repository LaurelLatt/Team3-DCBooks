using Project498.Mvc.Constants;

namespace Project498.Mvc.Models;

public class Checkout
{
    public int CheckoutId { get; set; }
    public int UserId { get; set; }
    public int ComicId { get; set; }

    /// <summary><see cref="ComicSourceConstants"/> — <c>dc</c> or <c>marvel</c>.</summary>
    public string ComicSource { get; set; } = ComicSourceConstants.Dc;
    public DateTime CheckoutDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
