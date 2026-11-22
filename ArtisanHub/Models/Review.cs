namespace ArtisanHub.Models;

public class Review
{
    public int ReviewID { get; set; }
    public int ProductID { get; set; }
    public int UserID { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime ReviewDate { get; set; } = DateTime.Now;

    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
}
