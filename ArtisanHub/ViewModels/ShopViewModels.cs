using ArtisanHub.Models;

namespace ArtisanHub.ViewModels;

public class ProductCardVm
{
    public int ProductID { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQty { get; set; }
    public bool IsCustomizable { get; set; }
    public int ArtistID { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int ReviewCount { get; set; }
    public decimal? AvgRating { get; set; }
}

public class StoreCardVm
{
    public int ArtistID { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public DateTime JoinDate { get; set; }
    public int ProductCount { get; set; }
    public int ReviewCount { get; set; }
    public decimal? AvgRating { get; set; }
}

public class CategoryTileVm
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
}

public class HomeVm
{
    public List<CategoryTileVm> Categories { get; set; } = new();
    public List<ProductCardVm> NewArrivals { get; set; } = new();
    public List<ProductCardVm> TopRated { get; set; } = new();
    public List<StoreCardVm> Makers { get; set; } = new();
    public List<City> Cities { get; set; } = new();
    public int ArtistCount { get; set; }
    public int ProductCount { get; set; }
    public int CityCount { get; set; }
}

public class ProductListVm
{
    public string? Q { get; set; }
    public int? Category { get; set; }
    public int? City { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public bool CustomOnly { get; set; }
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public List<ProductCardVm> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<City> Cities { get; set; } = new();
    public string? CategoryName { get; set; }
}

public class ProductDetailsVm
{
    public Product Product { get; set; } = null!;
    public List<ProductImage> Images { get; set; } = new();
    public decimal? AvgRating { get; set; }
    public int ReviewCount { get; set; }
    public List<Review> Reviews { get; set; } = new();
    public List<ProductCardVm> Related { get; set; } = new();
    public bool IsCustomer { get; set; }
    public bool IsAnonymous { get; set; }
    public bool CanReview { get; set; }
    public Review? MyReview { get; set; }
    public int InCart { get; set; }
    public int[] RatingCounts { get; set; } = new int[5];
}

public class StoreListVm
{
    public string? Q { get; set; }
    public int? City { get; set; }
    public List<StoreCardVm> Stores { get; set; } = new();
    public List<City> Cities { get; set; } = new();
}

public class StoreDetailsVm
{
    public Artist Artist { get; set; } = null!;
    public List<ProductCardVm> Products { get; set; } = new();
    public decimal? AvgRating { get; set; }
    public int ReviewCount { get; set; }
    public int ItemsSold { get; set; }
    public bool IsCustomer { get; set; }
    public bool IsAnonymous { get; set; }
}
