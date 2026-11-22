namespace ArtisanHub.Models;

/// <summary>Composite key (UserID, ProductID).</summary>
public class CartItem
{
    public int UserID { get; set; }
    public int ProductID { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;

    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
