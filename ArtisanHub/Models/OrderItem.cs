namespace ArtisanHub.Models;

/// <summary>Composite key (OrderID, ProductID). UnitPrice keeps the price at the time of sale.</summary>
public class OrderItem
{
    public int OrderID { get; set; }
    public int ProductID { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
