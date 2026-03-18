namespace Project498.WebApi.Models;

public class Checkout
{
    public int CheckoutId { get; set; }
    public int UserId { get; set; }
    public int ComicId { get; set; }
    public DateTime CheckoutDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}