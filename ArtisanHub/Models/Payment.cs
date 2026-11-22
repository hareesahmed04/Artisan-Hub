namespace ArtisanHub.Models;

/// <summary>One payment per order (UNIQUE OrderID).</summary>
public class Payment
{
    public int PaymentID { get; set; }
    public int OrderID { get; set; }
    public string Method { get; set; } = PaymentMethod.COD;
    public decimal Amount { get; set; }
    public string Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }
    public string? TransactionRef { get; set; }

    public Order Order { get; set; } = null!;
}
