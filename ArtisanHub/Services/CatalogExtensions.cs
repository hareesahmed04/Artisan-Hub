using ArtisanHub.Models;
using ArtisanHub.ViewModels;

namespace ArtisanHub.Services;

/// <summary>Reusable LINQ pieces for the shop pages.</summary>
public static class CatalogExtensions
{
    public const string PlaceholderImage = "/images/placeholder.svg";

    /// <summary>Only active products of verified, active artists are shown to shoppers.</summary>
    public static IQueryable<Product> VisibleToShoppers(this IQueryable<Product> query)
        => query.Where(p => p.IsActive && p.Artist.IsVerified && p.Artist.User.IsActive);

    public static IQueryable<ProductCardVm> ToCards(this IQueryable<Product> query)
        => query.Select(p => new ProductCardVm
        {
            ProductID = p.ProductID,
            Title = p.Title,
            Price = p.Price,
            StockQty = p.StockQty,
            IsCustomizable = p.IsCustomizable,
            ArtistID = p.ArtistID,
            StoreName = p.Artist.StoreName,
            CityName = p.Artist.City.CityName,
            CategoryName = p.Category.CategoryName,
            ImageUrl = p.Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.ImageID)
                .Select(i => i.ImageUrl)
                .FirstOrDefault() ?? PlaceholderImage,
            ReviewCount = p.Reviews.Count(),
            AvgRating = p.Reviews.Average(r => (decimal?)r.Rating)
        });

    public static IQueryable<StoreCardVm> ToStoreCards(this IQueryable<Artist> query)
        => query.Select(a => new StoreCardVm
        {
            ArtistID = a.ArtistID,
            StoreName = a.StoreName,
            CityName = a.City.CityName,
            Bio = a.Bio,
            JoinDate = a.JoinDate,
            ProductCount = a.Products.Count(p => p.IsActive),
            ReviewCount = a.Products.SelectMany(p => p.Reviews).Count(),
            AvgRating = a.Products.SelectMany(p => p.Reviews).Average(r => (decimal?)r.Rating)
        });
}
