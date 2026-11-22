using ArtisanHub.Data;
using ArtisanHub.Helpers;
using ArtisanHub.Models;
using ArtisanHub.Services;
using ArtisanHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.Controllers;

[Authorize(Roles = RoleNames.Customer)]
public class OrdersController : Controller
{
    private readonly ArtisanHubContext _db;
    private readonly OrderService _orders;

    public OrdersController(ArtisanHubContext db, OrderService orders)
    {
        _db = db;
        _orders = orders;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();

        // Totals come from the vw_OrderTotals view.
        var totals = await _db.OrderTotals
            .Where(t => t.UserID == userId)
            .OrderByDescending(t => t.OrderDate)
            .ToListAsync();

        var orderIds = totals.Select(t => t.OrderID).ToList();
        var methods = await _db.Payments
            .Where(p => orderIds.Contains(p.OrderID))
            .ToDictionaryAsync(p => p.OrderID, p => p.Method);

        var thumbs = await _db.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderID))
            .Select(oi => new
            {
                oi.OrderID,
                Url = oi.Product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.ImageID)
                    .Select(i => i.ImageUrl).FirstOrDefault()
            })
            .ToListAsync();

        var vm = totals.Select(t => new OrderListItemVm
        {
            OrderID = t.OrderID,
            OrderDate = t.OrderDate,
            Status = t.Status,
            ItemCount = t.ItemCount,
            Total = t.TotalAmount,
            Method = methods.TryGetValue(t.OrderID, out var m) ? m : string.Empty,
            Thumbnails = thumbs.Where(x => x.OrderID == t.OrderID)
                .Select(x => x.Url ?? CatalogExtensions.PlaceholderImage)
                .Take(4)
                .ToList()
        }).ToList();

        return View(vm);
    }

    public async Task<IActionResult> Details(int id, bool placed = false)
    {
        var userId = User.GetUserId();
        var order = await _db.Orders
            .Include(o => o.Address).ThenInclude(a => a.City)
            .Include(o => o.Payment)
            .Include(o => o.Shipments).ThenInclude(s => s.Artist).ThenInclude(a => a.City)
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

        if (order == null) return NotFound();

        var deliveredArtists = order.Shipments
            .Where(s => s.Status == ShipmentStatus.Delivered)
            .Select(s => s.ArtistID)
            .ToHashSet();

        var productIds = order.Items.Select(i => i.ProductID).ToList();
        var reviewed = await _db.Reviews
            .Where(r => r.UserID == userId && productIds.Contains(r.ProductID))
            .Select(r => r.ProductID)
            .ToListAsync();

        var itemsTotal = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        var vm = new OrderDetailsVm
        {
            Order = order,
            ItemsTotal = itemsTotal,
            Total = itemsTotal + order.ShippingFee,
            CanCancel = OrderService.CanCancel(order),
            JustPlaced = placed,
            ReviewableProductIds = order.Items
                .Where(i => deliveredArtists.Contains(i.Product.ArtistID))
                .Select(i => i.ProductID)
                .ToHashSet(),
            ReviewedProductIds = reviewed.ToHashSet(),
            Images = order.Items.ToDictionary(
                i => i.ProductID,
                i => i.Product.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.ImageID)
                        .Select(x => x.ImageUrl).FirstOrDefault() ?? CatalogExtensions.PlaceholderImage)
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var error = await _orders.CancelOrderAsync(User.GetUserId(), id);
        if (error != null) TempData["Error"] = error;
        else TempData["Success"] = $"Order #{id} has been cancelled. Any online payment will be refunded.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
