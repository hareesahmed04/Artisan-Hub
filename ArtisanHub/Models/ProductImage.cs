namespace ArtisanHub.Models;

public class ProductImage
{
    public int ImageID { get; set; }
    public int ProductID { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }

    public Product Product { get; set; } = null!;
}
