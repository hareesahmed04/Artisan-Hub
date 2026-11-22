namespace ArtisanHub.Models;

public class Address
{
    public int AddressID { get; set; }
    public int UserID { get; set; }
    public int CityID { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public bool IsDefault { get; set; }

    public User User { get; set; } = null!;
    public City City { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
