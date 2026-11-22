using ArtisanHub.Data;
using ArtisanHub.Helpers;
using ArtisanHub.Models;
using ArtisanHub.Services;
using ArtisanHub.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.Controllers;

public class ProductsController : Controller
{
    private const int PageSize = 12;
    private readonly ArtisanHubContext _db;

    public ProductsController(ArtisanHubContext db)
    {
        _db = db;
    }

    // GET /Products?q=vase&category=1&city=2&min=1000&max=5000&sort=price-asc&page=1
    public async Task<IActionResult> Index(string? q, int? category, int? city, decimal? min, decimal? max,
        bool customOnly = false, string sort = "newest", int page = 1)
    {
        var query = _db.Products.VisibleToShoppers();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Title.Contains(term)
                                     || (p.Description != null && p.Description.Contains(term))
                                     || p.Artist.StoreName.Contains(term)
                                     || p.Category.CategoryName.Contains(term));
        }

        if (category.HasValue) query = query.Where(p => p.CategoryID == category.Value);
        if (city.HasValue) query = query.Where(p => p.Artist.CityID == city.Value);
        if (min.HasValue) query = query.Where(p => p.Price >= min.Value);
        if (max.HasValue) query = query.Where(p => p.Price <= max.Value);
        if (customOnly) query = query.Where(p => p.IsCustomizable);

        query = sort switch
        {
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            "rating" => query.OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0)
                             .ThenByDescending(p => p.Reviews.Count()),
            "popular" => query.OrderByDescending(p => p.OrderItems.Sum(oi => (int?)oi.Quantity) ?? 0),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var vm = new ProductListVm
        {
            Q = q,
            Category = category,
            City = city,
            Min = min,
            Max = max,
            CustomOnly = customOnly,
            Sort = sort,
            Page = page,
            TotalPages = totalPages,
            TotalCount = total,
            Products = await query.ToCards().Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(),
            Categories = await _db.Categories.OrderBy(c => c.CategoryName).ToListAsync(),
            Cities = await _db.Cities.OrderBy(c => c.CityName).ToListAsync()
        };
        vm.CategoryName = vm.Categories.FirstOrDefault(c => c.CategoryID == category)?.CategoryName;

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products
            .Include(p => p.Artist).ThenInclude(a => a.City)
            .Include(p => p.Artist).ThenInclude(a => a.User)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductID == id);

        if (product == null) return NotFound();

        var isOwner = User.GetArtistId() == product.ArtistID;
        var isVisible = product.IsActive && product.Artist.IsVerified && product.Artist.User.IsActive;
        if (!isVisible && !isOwner && !User.IsInRole(RoleNames.Admin)) return NotFound();

        var rating = await _db.ProductRatings.FirstOrDefaultAsync(r => r.ProductID == id);
        var reviews = await _db.Reviews
            .Include(r => r.User)
            .Where(r => r.ProductID == id)
            .OrderByDescending(r => r.ReviewDate)
            .ToListAsync();

        var vm = new ProductDetailsVm
        {
            Product = product,
            Images = product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.ImageID).ToList(),
            AvgRating = rating?.AvgRating,
            ReviewCount = rating?.ReviewCount ?? 0,
            Reviews = reviews,
            IsAnonymous = User.Identity?.IsAuthenticated != true,
            IsCustomer = User.IsInRole(RoleNames.Customer),
            Related = await _db.Products.VisibleToShoppers()
                .Where(p => p.CategoryID == product.CategoryID && p.ProductID != product.ProductID)
                .OrderByDescending(p => p.CreatedAt)
                .ToCards()
                .Take(4)
                .ToListAsync()
        };

        foreach (var review in reviews)
        {
            if (review.Rating >= 1 && review.Rating <= 5) vm.RatingCounts[review.Rating - 1]++;
        }

        if (vm.IsCustomer)
        {
            var userId = User.GetUserId();
            vm.MyReview = reviews.FirstOrDefault(r => r.UserID == userId);
            vm.CanReview = await HasReceivedAsync(userId, product);
            vm.InCart = await _db.CartItems
                .Where(c => c.UserID == userId && c.ProductID == id)
                .Select(c => c.Quantity)
                .FirstOrDefaultAsync();
        }

        return View(vm);
    }

    private Task<bool> HasReceivedAsync(int userId, Product product)
        => _db.OrderItems.AnyAsync(oi =>
            oi.ProductID == product.ProductID &&
            oi.Order.UserID == userId &&
            oi.Order.Shipments.Any(s => s.ArtistID == product.ArtistID && s.Status == ShipmentStatus.Delivered));
}
