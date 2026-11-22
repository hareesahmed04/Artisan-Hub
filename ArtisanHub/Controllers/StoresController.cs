using ArtisanHub.Data;
using ArtisanHub.Models;
using ArtisanHub.Services;
using ArtisanHub.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.Controllers;

public class StoresController : Controller
{
    private readonly ArtisanHubContext _db;

    public StoresController(ArtisanHubContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? q, int? city)
    {
        var query = _db.Artists.Where(a => a.IsVerified && a.User.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(a => a.StoreName.Contains(term) || (a.Bio != null && a.Bio.Contains(term)));
        }
        if (city.HasValue) query = query.Where(a => a.CityID == city.Value);

        var vm = new StoreListVm
        {
            Q = q,
            City = city,
            Stores = await query.OrderBy(a => a.StoreName).ToStoreCards().ToListAsync(),
            Cities = await _db.Cities.Where(c => c.Artists.Any(a => a.IsVerified)).OrderBy(c => c.CityName).ToListAsync()
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var artist = await _db.Artists
            .Include(a => a.City)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.ArtistID == id);

        if (artist == null || !artist.IsVerified || !artist.User.IsActive) return NotFound();

        var sales = await _db.SalesByArtist.FirstOrDefaultAsync(s => s.ArtistID == id);
        var reviews = _db.Reviews.Where(r => r.Product.ArtistID == id);

        var vm = new StoreDetailsVm
        {
            Artist = artist,
            Products = await _db.Products.VisibleToShoppers()
                .Where(p => p.ArtistID == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToCards()
                .ToListAsync(),
            ReviewCount = await reviews.CountAsync(),
            AvgRating = await reviews.AverageAsync(r => (decimal?)r.Rating),
            ItemsSold = sales?.ItemsSold ?? 0,
            IsCustomer = User.IsInRole(RoleNames.Customer),
            IsAnonymous = User.Identity?.IsAuthenticated != true
        };
        return View(vm);
    }
}
