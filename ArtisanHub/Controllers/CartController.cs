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
public class CartController : Controller
{
    private readonly ArtisanHubContext _db;

    public CartController(ArtisanHubContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        return View(await BuildCartAsync(_db, User.GetUserId()));
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int quantity = 1, bool buyNow = false)
    {
        var userId = User.GetUserId();
        var product = await _db.Products.VisibleToShoppers().FirstOrDefaultAsync(p => p.ProductID == productId);
        if (product == null)
        {
            TempData["Error"] = "This product is not available.";
            return RedirectToAction("Index", "Products");
        }

        quantity = Math.Max(1, quantity);
        var item = await _db.CartItems.FindAsync(userId, productId);
        var wanted = (item?.Quantity ?? 0) + quantity;

        if (product.StockQty == 0)
        {
            TempData["Error"] = $"\"{product.Title}\" is out of stock.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        if (wanted > product.StockQty)
        {
            wanted = product.StockQty;
            TempData["Info"] = $"Only {product.StockQty} of \"{product.Title}\" are available, so your cart has the maximum.";
        }
        else
        {
            TempData["Success"] = $"\"{product.Title}\" was added to your cart.";
        }

        if (item == null)
        {
            _db.CartItems.Add(new CartItem { UserID = userId, ProductID = productId, Quantity = wanted, AddedAt = DateTime.Now });
        }
        else
        {
            item.Quantity = wanted;
        }
        await _db.SaveChangesAsync();

        return buyNow
            ? RedirectToAction("Index", "Checkout")
            : RedirectToAction("Details", "Products", new { id = productId });
    }

    [HttpPost]
    public async Task<IActionResult> Update(int productId, int quantity)
    {
        var userId = User.GetUserId();
        var item = await _db.CartItems.Include(c => c.Product).FirstOrDefaultAsync(c => c.UserID == userId && c.ProductID == productId);
        if (item == null) return RedirectToAction(nameof(Index));

        if (quantity <= 0)
        {
            _db.CartItems.Remove(item);
            TempData["Success"] = "Item removed from your cart.";
        }
        else
        {
            item.Quantity = Math.Min(quantity, Math.Max(1, item.Product.StockQty));
            if (quantity > item.Product.StockQty)
                TempData["Info"] = $"Only {item.Product.StockQty} of \"{item.Product.Title}\" are available.";
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int productId)
    {
        var userId = User.GetUserId();
        var item = await _db.CartItems.FindAsync(userId, productId);
        if (item != null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Item removed from your cart.";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Builds the cart grouped by artist (each artist ships their own parcel).</summary>
    public static async Task<CartVm> BuildCartAsync(ArtisanHubContext db, int userId)
    {
        var lines = await db.CartItems
            .Where(c => c.UserID == userId)
            .OrderBy(c => c.AddedAt)
            .Select(c => new
            {
                c.ProductID,
                c.Quantity,
                c.Product.Title,
                c.Product.Price,
                c.Product.StockQty,
                Available = c.Product.IsActive && c.Product.Artist.IsVerified,
                c.Product.ArtistID,
                c.Product.Artist.StoreName,
                CityName = c.Product.Artist.City.CityName,
                ImageUrl = c.Product.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.ImageID)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var cart = new CartVm
        {
            Groups = lines
                .GroupBy(l => new { l.ArtistID, l.StoreName, l.CityName })
                .Select(g => new CartGroupVm
                {
                    ArtistID = g.Key.ArtistID,
                    StoreName = g.Key.StoreName,
                    CityName = g.Key.CityName,
                    Lines = g.Select(l => new CartLineVm
                    {
                        ProductID = l.ProductID,
                        Title = l.Title,
                        Price = l.Price,
                        Quantity = l.Quantity,
                        StockQty = l.StockQty,
                        Available = l.Available,
                        ImageUrl = l.ImageUrl ?? CatalogExtensions.PlaceholderImage
                    }).ToList()
                })
                .ToList()
        };
        cart.Shipping = OrderService.ShippingFor(cart.Groups.Count);
        return cart;
    }
}
