namespace ArtisanHub.Models;

/// <summary>A customer asks an artist to make a custom piece; the artist quotes a price.</summary>
public class CustomRequest
{
    public int RequestID { get; set; }
    public int UserID { get; set; }
    public int ArtistID { get; set; }
    public int? CategoryID { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? Budget { get; set; }
    public DateTime? RequiredBy { get; set; }
    public decimal? QuotedPrice { get; set; }
    public string Status { get; set; } = RequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public User User { get; set; } = null!;
    public Artist Artist { get; set; } = null!;
    public Category? Category { get; set; }
}
