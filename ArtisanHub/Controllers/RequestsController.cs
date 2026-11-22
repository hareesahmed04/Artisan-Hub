using ArtisanHub.Data;
using ArtisanHub.Helpers;
using ArtisanHub.Models;
using ArtisanHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.Controllers;

/// <summary>Customer side of custom art requests.</summary>
[Authorize(Roles = RoleNames.Customer)]
public class RequestsController : Controller
{
    private readonly ArtisanHubContext _db;

    public RequestsController(ArtisanHubContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        var requests = await _db.CustomRequests
            .Include(r => r.Artist).ThenInclude(a => a.City)
            .Include(r => r.Category)
            .Where(r => r.UserID == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return View(requests);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? artistId, int? productId)
    {
        var vm = new CustomRequestFormVm { ArtistID = artistId };

        if (productId.HasValue)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductID == productId.Value);
            if (product != null)
            {
                vm.ArtistID = product.ArtistID;
                vm.CategoryID = product.CategoryID;
                vm.ProductTitle = product.Title;
                vm.Description = $"I would like a custom version of \"{product.Title}\". ";
            }
        }

        await FillOptionsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomRequestFormVm vm)
    {
        if (vm.ArtistID.HasValue && !await _db.Artists.AnyAsync(a => a.ArtistID == vm.ArtistID && a.IsVerified))
            ModelState.AddModelError(nameof(vm.ArtistID), "Please choose an artist from the list.");

        if (vm.RequiredBy.HasValue && vm.RequiredBy.Value.Date <= DateTime.Today)
            ModelState.AddModelError(nameof(vm.RequiredBy), "Please choose a date in the future.");

        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(vm);
            return View(vm);
        }

        _db.CustomRequests.Add(new CustomRequest
        {
            UserID = User.GetUserId(),
            ArtistID = vm.ArtistID!.Value,
            CategoryID = vm.CategoryID,
            Description = vm.Description.Trim(),
            Budget = vm.Budget,
            RequiredBy = vm.RequiredBy?.Date,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Your request has been sent. The artist will reply with a quote.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Customer accepts or declines the artist's quote.</summary>
    [HttpPost]
    public async Task<IActionResult> Respond(int id, bool accept)
    {
        var userId = User.GetUserId();
        var request = await _db.CustomRequests.FirstOrDefaultAsync(r => r.RequestID == id && r.UserID == userId);
        if (request == null) return NotFound();

        if (request.Status != RequestStatus.Quoted)
        {
            TempData["Error"] = "There is no quote to respond to on this request.";
            return RedirectToAction(nameof(Index));
        }

        request.Status = accept ? RequestStatus.Accepted : RequestStatus.Rejected;
        await _db.SaveChangesAsync();
        TempData["Success"] = accept
            ? "Quote accepted! The artist will start working on your piece."
            : "You declined the quote.";
        return RedirectToAction(nameof(Index));
    }

    private async Task FillOptionsAsync(CustomRequestFormVm vm)
    {
        vm.Artists = await _db.Artists
            .Where(a => a.IsVerified && a.User.IsActive)
            .OrderBy(a => a.StoreName)
            .Select(a => new SelectListItem(a.StoreName + " (" + a.City.CityName + ")", a.ArtistID.ToString()))
            .ToListAsync();
        vm.Categories = await _db.Categories
            .OrderBy(c => c.CategoryName)
            .Select(c => new SelectListItem(c.CategoryName, c.CategoryID.ToString()))
            .ToListAsync();
    }
}
