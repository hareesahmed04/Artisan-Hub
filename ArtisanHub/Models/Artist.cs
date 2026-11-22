namespace ArtisanHub.Models;

/// <summary>A seller's store. Linked one-to-one with a User account.</summary>
public class Artist
{
    public int ArtistID { get; set; }
    public int UserID { get; set; }
    public int CityID { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.Today;
    public bool IsVerified { get; set; }

    public User User { get; set; } = null!;
    public City City { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<CustomRequest> CustomRequests { get; set; } = new List<CustomRequest>();
}
