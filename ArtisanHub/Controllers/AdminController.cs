using ArtisanHub.Data;
using ArtisanHub.Helpers;
using ArtisanHub.Models;
using ArtisanHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.Controllers;

/// <summary>Admin panel: verify artists, manage categories and users, view orders and reports.</summary>
[Authorize(Roles = RoleNames.Admin)]
public class AdminController : Controller
{
    private readonly ArtisanHubContext _db;

    public AdminController(ArtisanHubContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var liveOrders = _db.OrderTotals.Where(t => t.Status != OrderStatus.Cancelled);

        var vm = new AdminDashboardVm
        {
            Customers = await _db.Users.CountAsync(u => u.Role.RoleName == RoleNames.Customer),
            VerifiedArtists = await _db.Artists.CountAsync(a => a.IsVerified),
            PendingArtists = await _db.Artists.CountAsync(a => !a.IsVerified),
            ActiveProducts = await _db.Products.CountAsync(p => p.IsActive && p.Artist.IsVerified),
            Orders = await _db.Orders.CountAsync(),
            Revenue = await liveOrders.SumAsync(t => (decimal?)t.TotalAmount) ?? 0,
            RevenueThisMonth = await liveOrders.Where(t => t.OrderDate >= monthStart).SumAsync(t => (decimal?)t.TotalAmount) ?? 0,
            OpenRequests = await _db.CustomRequests.CountAsync(r => r.Status == RequestStatus.Pending || r.Status == RequestStatus.Quoted),
            RecentOrders = (await LoadOrdersAsync(null)).Take(6).ToList(),
            ArtistsToVerify = await _db.Artists
                .Include(a => a.User)
                .Include(a => a.City)
                .Where(a => !a.IsVerified)
                .OrderBy(a => a.JoinDate)
                .ToListAsync(),
            CategorySales = await _db.SalesByCategory.OrderByDescending(s => s.TotalSales).ToListAsync()
        };
        return View(vm);
    }

    // ------------------------------------------------------------------ artists

    public async Task<IActionResult> Artists(string? filter = null)
    {
        var sales = await _db.SalesByArtist.ToDictionaryAsync(s => s.ArtistID, s => s.TotalSales);
        var query = _db.Artists.AsQueryable();
        if (filter == "pending") query = query.Where(a => !a.IsVerified);
        if (filter == "verified") query = query.Where(a => a.IsVerified);

        var rows = await query
            .OrderBy(a => a.IsVerified).ThenBy(a => a.StoreName)
            .Select(a => new AdminArtistRowVm
            {
                ArtistID = a.ArtistID,
                StoreName = a.StoreName,
                OwnerName = a.User.FullName,
                Email = a.User.Email,
                CityName = a.City.CityName,
                JoinDate = a.JoinDate,
                IsVerified = a.IsVerified,
                ProductCount = a.Products.Count(),
                Bio = a.Bio
            })
            .ToListAsync();

        foreach (var row in rows)
        {
            row.TotalSales = sales.TryGetValue(row.ArtistID, out var total) ? total : 0;
        }

        ViewData["Filter"] = filter;
        return View(rows);
    }

    [HttpPost]
    public async Task<IActionResult> VerifyArtist(int id, bool verify, string? filter)
    {
        var artist = await _db.Artists.FindAsync(id);
        if (artist == null) return NotFound();

        artist.IsVerified = verify;
        await _db.SaveChangesAsync();
        TempData["Success"] = verify
            ? $"\"{artist.StoreName}\" is verified. Its products are now visible in the shop."
            : $"\"{artist.StoreName}\" is no longer verified and has been hidden from the shop.";
        return RedirectToAction(nameof(Artists), new { filter });
    }

    // ------------------------------------------------------------------ categories

    public async Task<IActionResult> Categories()
    {
        var rows = await _db.Categories
            .OrderBy(c => c.CategoryName)
            .Select(c => new AdminCategoryRowVm
            {
                CategoryID = c.CategoryID,
                CategoryName = c.CategoryName,
                Description = c.Description,
                ProductCount = c.Products.Count(),
                RequestCount = c.CustomRequests.Count()
            })
            .ToListAsync();
        return View(rows);
    }

    [HttpPost]
    public async Task<IActionResult> SaveCategory(CategoryFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                                ?? "Please check the category details.";
            return RedirectToAction(nameof(Categories));
        }

        var name = vm.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.CategoryName == name && c.CategoryID != (vm.CategoryID ?? 0)))
        {
            TempData["Error"] = $"A category called \"{name}\" already exists.";
            return RedirectToAction(nameof(Categories));
        }

        if (vm.CategoryID.HasValue)
        {
            var category = await _db.Categories.FindAsync(vm.CategoryID.Value);
            if (category == null) return NotFound();
            category.CategoryName = name;
            category.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
            TempData["Success"] = $"Category \"{name}\" updated.";
        }
        else
        {
            _db.Categories.Add(new Category
            {
                CategoryName = name,
                Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim()
            });
            TempData["Success"] = $"Category \"{name}\" added.";
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();

        var inUse = await _db.Products.AnyAsync(p => p.CategoryID == id)
                    || await _db.CustomRequests.AnyAsync(r => r.CategoryID == id);
        if (inUse)
        {
            TempData["Error"] = $"\"{category.CategoryName}\" still has products or requests, so it cannot be deleted.";
            return RedirectToAction(nameof(Categories));
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Category \"{category.CategoryName}\" deleted.";
        return RedirectToAction(nameof(Categories));
    }

    // ------------------------------------------------------------------ users

    public async Task<IActionResult> Users(string? role = null, string? q = null)
    {
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrEmpty(role)) query = query.Where(u => u.Role.RoleName == role);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(u => u.FullName.Contains(term) || u.Email.Contains(term));
        }

        var rows = await query
            .OrderBy(u => u.Role.RoleName).ThenBy(u => u.FullName)
            .Select(u => new AdminUserRowVm
            {
                UserID = u.UserID,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                RoleName = u.Role.RoleName,
                CreatedAt = u.CreatedAt,
                IsActive = u.IsActive,
                StoreName = u.Artist != null ? u.Artist.StoreName : null,
                OrderCount = u.Orders.Count()
            })
            .ToListAsync();

        ViewData["Role"] = role;
        ViewData["Q"] = q;
        return View(rows);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleUser(int id, string? role, string? q)
    {
        if (id == User.GetUserId())
        {
            TempData["Error"] = "You cannot deactivate your own admin account.";
            return RedirectToAction(nameof(Users), new { role, q });
        }

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = user.IsActive
            ? $"{user.FullName}'s account is active again."
            : $"{user.FullName}'s account has been deactivated.";
        return RedirectToAction(nameof(Users), new { role, q });
    }

    // ------------------------------------------------------------------ orders & reports

    public async Task<IActionResult> Orders(string? status = null)
    {
        ViewData["Status"] = status;
        return View(await LoadOrdersAsync(status));
    }

    public async Task<IActionResult> Reports()
    {
        var vm = new AdminReportsVm
        {
            ByArtist = await _db.SalesByArtist.OrderByDescending(s => s.TotalSales).ToListAsync(),
            ByCategory = await _db.SalesByCategory.OrderByDescending(s => s.TotalSales).ToListAsync(),
            ByCity = await _db.SalesByCity.Where(s => s.ArtistCount > 0).OrderByDescending(s => s.TotalSales).ToListAsync()
        };
        vm.GrandTotal = vm.ByCategory.Sum(s => s.TotalSales);
        vm.ItemsSold = vm.ByCategory.Sum(s => s.ItemsSold);
        return View(vm);
    }

    private async Task<List<AdminOrderRowVm>> LoadOrdersAsync(string? status)
    {
        var totals = _db.OrderTotals.AsQueryable();
        if (!string.IsNullOrEmpty(status)) totals = totals.Where(t => t.Status == status);

        var list = await totals.OrderByDescending(t => t.OrderDate).ToListAsync();
        var ids = list.Select(t => t.OrderID).ToList();

        var extra = await _db.Orders
            .Where(o => ids.Contains(o.OrderID))
            .Select(o => new
            {
                o.OrderID,
                CustomerName = o.User.FullName,
                Method = o.Payment != null ? o.Payment.Method : string.Empty,
                PaymentState = o.Payment != null ? o.Payment.Status : string.Empty,
                ShipmentCount = o.Shipments.Count()
            })
            .ToDictionaryAsync(x => x.OrderID);

        var rows = new List<AdminOrderRowVm>();
        foreach (var t in list)
        {
            var row = new AdminOrderRowVm
            {
                OrderID = t.OrderID,
                OrderDate = t.OrderDate,
                Status = t.Status,
                ItemCount = t.ItemCount,
                Total = t.TotalAmount
            };
            if (extra.TryGetValue(t.OrderID, out var e))
            {
                row.CustomerName = e.CustomerName;
                row.Method = e.Method;
                row.PaymentState = e.PaymentState;
                row.ShipmentCount = e.ShipmentCount;
            }
            rows.Add(row);
        }
        return rows;
    }
}
