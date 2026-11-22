using ArtisanHub.Data;
using ArtisanHub.Services;
using ArtisanHub.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.Controllers;

public class HomeController : Controller
{
    private readonly ArtisanHubContext _db;

    public HomeController(ArtisanHubContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var visible = _db.Products.VisibleToShoppers();

        // Top makers come from the vw_SalesByArtist view.
        var topArtistIds = await _db.SalesByArtist
            .Where(s => s.IsVerified)
            .OrderByDescending(s => s.TotalSales)
            .Select(s => s.ArtistID)
            .Take(4)
            .ToListAsync();

        var makers = await _db.Artists
            .Where(a => topArtistIds.Contains(a.ArtistID))
            .ToStoreCards()
            .ToListAsync();

        var vm = new HomeVm
        {
            Categories = await _db.Categories
                .OrderBy(c => c.CategoryID)
                .Select(c => new CategoryTileVm
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    ProductCount = c.Products.Count(p => p.IsActive && p.Artist.IsVerified)
                })
                .ToListAsync(),
            NewArrivals = await visible
                .OrderByDescending(p => p.CreatedAt)
                .ToCards()
                .Take(8)
                .ToListAsync(),
            TopRated = await visible
                .Where(p => p.Reviews.Any())
                .OrderByDescending(p => p.Reviews.Average(r => (double)r.Rating))
                .ThenByDescending(p => p.Reviews.Count())
                .ToCards()
                .Take(4)
                .ToListAsync(),
            Makers = makers.OrderBy(m => topArtistIds.IndexOf(m.ArtistID)).ToList(),
            Cities = await _db.Cities
                .Where(c => c.Artists.Any(a => a.IsVerified))
                .OrderBy(c => c.CityName)
                .ToListAsync(),
            ArtistCount = await _db.Artists.CountAsync(a => a.IsVerified),
            ProductCount = await visible.CountAsync(),
        };
        vm.CityCount = vm.Cities.Count;

        return View(vm);
    }

    public IActionResult About() => View();

    [Route("Home/Status/{code:int}")]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return code == 404 ? View("NotFound") : View("Error");
    }

    public IActionResult Error() => View();
}
