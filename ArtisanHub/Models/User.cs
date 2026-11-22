namespace ArtisanHub.Models;

/// <summary>Every account (admin, artist or customer) signs in through this table.</summary>
public class User
{
    public int UserID { get; set; }
    public int RoleID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    public Role Role { get; set; } = null!;
    public Artist? Artist { get; set; }
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<CustomRequest> CustomRequests { get; set; } = new List<CustomRequest>();
}
