using ArtisanHub.Data;
using ArtisanHub.Helpers;
using ArtisanHub.Models;
using ArtisanHub.Services;
using ArtisanHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.Controllers;

/// <summary>The artist's panel: dashboard, products, shipments, custom requests, sales and store profile.</summary>
[Authorize(Roles = RoleNames.Artist)]
public class SellerController : Controller
{
    private readonly ArtisanHubContext _db;
    private readonly OrderService _orders;
    private readonly ImageStorage _images;

    public SellerController(ArtisanHubContext db, OrderService orders, ImageStorage images)
    {
        _db = db;
        _orders = orders;
        _images = images;
    }

    public IActionResult Index() => RedirectToAction(nameof(Dashboard));

    // ------------------------------------------------------------------ dashboard

    public async Task<IActionResult> Dashboard()
    {
        var artist = await CurrentArtistAsync();
        if (artist == null) return Forbid();

        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var sold = _db.OrderItems.Where(oi => oi.Product.ArtistID == artist.ArtistID && oi.Order.Status != OrderStatus.Cancelled);
        var reviews = _db.Reviews.Where(r => r.Product.ArtistID == artist.ArtistID);

        var vm = new SellerDashboardVm
        {
            Artist = artist,
            SalesThisMonth = await sold.Where(oi => oi.Order.OrderDate >= monthStart).SumAsync(oi => (decimal?)(oi.Quantity * oi.UnitPrice)) ?? 0,
            TotalSales = await sold.SumAsync(oi => (decimal?)(oi.Quantity * oi.UnitPrice)) ?? 0,
            OrdersThisMonth = await sold.Where(oi => oi.Order.OrderDate >= monthStart).Select(oi => oi.OrderID).Distinct().CountAsync(),
            ActiveProducts = await _db.Products.CountAsync(p => p.ArtistID == artist.ArtistID && p.IsActive),
            AwaitingShipment = await _db.Shipments.CountAsync(s => s.ArtistID == artist.ArtistID && s.Status == ShipmentStatus.Preparing),
            PendingRequests = await _db.CustomRequests.CountAsync(r => r.ArtistID == artist.ArtistID && r.Status == RequestStatus.Pending),
            ReviewCount = await reviews.CountAsync(),
            AvgRating = await reviews.AverageAsync(r => (decimal?)r.Rating),
            LowStock = await _db.Products
                .Where(p => p.ArtistID == artist.ArtistID && p.IsActive && p.StockQty <= 3)
                .OrderBy(p => p.StockQty)
                .ToListAsync(),
            RecentShipments = (await LoadShipmentsAsync(artist.ArtistID, null)).Take(5).ToList()
        };
        return View(vm);
    }

    // ------------------------------------------------------------------ products

    public async Task<IActionResult> Products()
    {
        var artist = await CurrentArtistAsync();
        if (artist == null) return Forbid();

        var rows = await _db.Products
            .Where(p => p.ArtistID == artist.ArtistID)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new SellerProductRowVm
            {
                ProductID = p.ProductID,
                Title = p.Title,
                CategoryName = p.Category.CategoryName,
                Price = p.Price,
                StockQty = p.StockQty,
                IsActive = p.IsActive,
                IsCustomizable = p.IsCustomizable,
                UnitsSold = p.OrderItems.Where(oi => oi.Order.Status != OrderStatus.Cancelled).Sum(oi => (int?)oi.Quantity) ?? 0,
                ImageUrl = p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.ImageID)
                    .Select(i => i.ImageUrl).FirstOrDefault() ?? CatalogExtensions.PlaceholderImage
            })
            .ToListAsync();

        ViewData["Artist"] = artist;
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> CreateProduct()
    {
        var artist = await CurrentArtistAsync();
        if (artist == null) return Forbid();

        var vm = new ProductFormVm { IsActive = true, StockQty = 1 };
        await FillCategoriesAsync(vm);
        return View("ProductForm", vm);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(ProductFormVm vm)
    {
        var artist = await CurrentArtistAsync();
        if (artist == null) return Forbid();

        ValidateUploads(vm);
        if (!ModelState.IsValid)
        {
            await FillCategoriesAsync(vm);
            return View("ProductForm", vm);
        }

        var product = new Product
        {
            ArtistID = artist.ArtistID,
            CategoryID = vm.CategoryID!.Value,
            Title = vm.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
            Price = vm.Price!.Value,
            StockQty = vm.StockQty!.Value,
            IsCustomizable = vm.IsCustomizable,
            IsActive = vm.IsActive,
            CreatedAt = DateTime.Now
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        await AddImagesAsync(product.ProductID, vm, hasPrimary: false);

        TempData["Success"] = artist.IsVerified
            ? $"\"{product.Title}\" is now listed in your store."
            : $"\"{product.Title}\" was saved. It will appear in the shop once your store is verified.";
        return RedirectToAction(nameof(Products));
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int id)
    {
        var artist = await CurrentArtistAsync();
        if (artist == null) return Forbid();

        var product = await _db.Products.Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductID == id && p.ArtistID == artist.ArtistID);
        if (product == null) return NotFound();

        var vm = new ProductFormVm
        {
            ProductID = product.ProductID,
            Title = product.Title,
            CategoryID = product.CategoryID,
            Description = product.Description,
            Price = product.Price,
            StockQty = product.StockQty,
            IsCustomizable = product.IsCustomizable,
            IsActive = product.IsActive,
            ExistingImages = product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.ImageID).ToList()
        };
        await FillCategoriesAsync(vm);
        return View("ProductForm", vm);
    }

    [HttpPost]
    public async Task<IActionResult> EditProduct(int id, ProductFormVm vm)
    {
        var artist = await CurrentArtistAsync();
        if (artist == null) return Forbid();

        var product = await _db.Products.Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductID == id && p.ArtistID == artist.ArtistID);
        if (product == null) return NotFound();

        ValidateUploads(vm);
        if (!ModelState.IsValid)
        {
            vm.ProductID = id;
            vm.ExistingImages = product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.ImageID).ToList();
            await FillCategoriesAsync(vm);
            return View("ProductForm", vm);
        }

        product.Title = vm.Title.Trim();
        product.CategoryID = vm.CategoryID!.Value;
        product.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
        product.Price = vm.Price!.Value;
        product.StockQty = vm.StockQty!.Value;
        product.IsCustomizable = vm.IsCustomizable;
        product.IsActive = vm.IsActive;
        await _db.SaveChangesAsync();

        await AddImagesAsync(product.ProductID, vm, hasPrimary: product.Images.Any(i => i.IsPrimary));

        TempData["Success"] = $"\"{product.Title}\" has been updated.";
        return RedirectToAction(nameof(EditProduct), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleProduct(int id)
    {
        var artistId = User.GetArtistId();
        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductID == id && p.ArtistID == artistId);
        if (product == null) return NotFound();

        product.IsActive = !product.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = product.IsActive ? $"\"{product.Title}\" is visible in the shop again." : $"\"{product.Title}\" is now hidden from the shop.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    public async Task<IActionResult> SetPrimaryImage(int imageId)
    {
        var artistId = User.GetArtistId();
        var image = await _db.ProductImages.Include(i => i.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(i => i.ImageID == imageId && i.Product.ArtistID == artistId);
        if (image == null) return NotFound();

        foreach (var other in image.Product.Images) other.IsPrimary = other.ImageID == imageId;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Main photo updated.";
        return RedirectToAction(nameof(EditProduct), new { id = image.ProductID });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteImage(int imageId)
    {
        var artistId = User.GetArtistId();
        var image = await _db.ProductImages.Include(i => i.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(i => i.ImageID == imageId && i.Product.ArtistID == artistId);
        if (image == null) return NotFound();

        var productId = image.ProductID;
        var wasPrimary = image.IsPrimary;
        _db.ProductImages.Remove(image);
        if (wasPrimary)
        {
            var next = image.Product.Images.Where(i => i.ImageID != imageId).OrderBy(i => i.ImageID).FirstOrDefault();
            if (next != null) next.IsPrimary = true;
        }
        await _db.SaveChangesAsync();
        _images.DeleteIfUploaded(image.ImageUrl);

        TempData["Success"] = "Photo removed.";
        return RedirectToAction(nameof(EditProduct), new { id = productId });
    }

    // ------------------------------------------------------------------ orders / shipments

    public async Task<IActionResult> Orders(string? status = null)
    {
        var artist = await CurrentArtistAsync();
        if (artist == null) return Forbid();

        ViewData["Status"] = status;
        ViewData["Artist"] = artist;
        return View(await LoadShipmentsAsync(artist.ArtistID, status));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateShipment(int shipmentId, string status, string? courierName, string? trackingNo, string? returnStatus)
    {
        var artistId = User.GetArtistId();
        if (artistId == null) return Forbid();

        var error = await _orders.UpdateShipmentAsync(artistId.Value, shipmentId, status, courierName, trackingNo);
        if (error != null) TempData["Error"] = error;
        else TempData["Success"] = $"Shipment updated to \"{status}\".";
        return RedirectToAction(nameof(Orders), new { status = returnStatus });
    }

    // ------------------------------------------------------------------ custom requests

    public async Task<IActionResult> Requests()
    {
        var artist = await CurrentArtistAsync();
        if (artist == null) return Forbid();

        var requests = await _db.CustomRequests
            .Include(r => r.User)
            .Include(r => r.Category)
            .Where(r => r.ArtistID == artist.ArtistID)
            .OrderBy(r => r.Status == RequestStatus.Pending ? 0 : r.Status == RequestStatus.Accepted ? 1 : r.Status == RequestStatus.Quoted ? 2 : 3)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewData["Artist"] = artist;
        return View(requests);
    }

    [HttpPost]
    public async Task<IActionResult> QuoteRequest(int id, decimal? quotedPrice)
    {
        var request = await FindOwnRequestAsync(id);
        if (request == null) return NotFound();

        if (request.Status != RequestStatus.Pending && request.Status != RequestStatus.Quoted)
        {
            TempData["Error"] = "This request can no longer be quoted.";
        }
        else if (quotedPrice == null || quotedPrice <= 0 || quotedPrice > 10000000)
        {
            TempData["Error"] = "Enter a quote between Rs. 1 and Rs. 10,000,000.";
        }
        else
        {
            request.QuotedPrice = quotedPrice;
            request.Status = RequestStatus.Quoted;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Your quote has been sent to the customer.";
        }
        return RedirectToAction(nameof(Requests));
    }

    [HttpPost]
    public async Task<IActionResult> RejectRequest(int id)
    {
        var request = await FindOwnRequestAsync(id);
        if (request == null) return NotFound();

        if (request.Status == RequestStatus.Pending || request.Status == RequestStatus.Quoted)
        {
            request.Status = RequestStatus.Rejected;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Request declined.";
        }
        return RedirectToAction(nameof(Requests));
    }

    [HttpPost]
    public async Task<IActionResult> CompleteRequest(int id)
    {
        var request = await FindOwnRequestAsync(id);
        if (request == null) return NotFound();

        if (request.Status == RequestStatus.Accepted)
        {
            request.Status = RequestStatus.Completed;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Marked as completed. Great work!";
        }
        return RedirectToAction(nameof(Requests));
    }

    // ------------------------------------------------------------------ sales report

    public async Task<IActionResult> Sales()
    {
        var artist = await CurrentArtistAsync();
        if (artist == null) return Forbid();

        var since = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
        var rows = await _db.OrderItems
            .Where(oi => oi.Product.ArtistID == artist.ArtistID && oi.Order.Status != OrderStatus.Cancelled)
            .Select(oi => new { oi.Order.OrderDate, oi.Quantity, oi.UnitPrice, oi.Product.Title })
            .ToListAsync();

        var months = Enumerable.Range(0, 6)
            .Select(i => since.AddMonths(i))
            .Select(m => new AmountBarVm
            {
                Label = m.ToString("MMM yyyy"),
                Amount = rows.Where(r => r.OrderDate.Year == m.Year && r.OrderDate.Month == m.Month).Sum(r => r.Quantity * r.UnitPrice),
                Count = rows.Where(r => r.OrderDate.Year == m.Year && r.OrderDate.Month == m.Month).Sum(r => r.Quantity)
            })
            .ToList();

        var vm = new SellerSalesVm
        {
            Summary = await _db.SalesByArtist.FirstOrDefaultAsync(s => s.ArtistID == artist.ArtistID),
            SalesThisMonth = months.Last().Amount,
            Months = months,
            TopProducts = rows
                .GroupBy(r => r.Title)
                .Select(g => new AmountBarVm { Label = g.Key, Amount = g.Sum(r => r.Quantity * r.UnitPrice), Count = g.Sum(r => r.Quantity) })
                .OrderByDescending(x => x.Amount)
                .Take(6)
                .ToList()
        };

        ViewData["Artist"] = artist;
        return View(vm);
    }

    // ------------------------------------------------------------------ store profile

    [HttpGet]
    public async Task<IActionResult> Store()
    {
        var artist = await CurrentArtistAsync();
        if (artist == null) return Forbid();

        var vm = new StoreProfileVm
        {
            StoreName = artist.StoreName,
            CityID = artist.CityID,
            Bio = artist.Bio,
            IsVerified = artist.IsVerified,
            Cities = await CityOptionsAsync()
        };
        ViewData["Artist"] = artist;
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Store(StoreProfileVm vm)
    {
        var artist = await CurrentArtistAsync();
        if (artist == null) return Forbid();

        var name = vm.StoreName?.Trim() ?? string.Empty;
        if (await _db.Artists.AnyAsync(a => a.StoreName == name && a.ArtistID != artist.ArtistID))
            ModelState.AddModelError(nameof(vm.StoreName), "Another store already uses this name.");

        if (!ModelState.IsValid)
        {
            vm.IsVerified = artist.IsVerified;
            vm.Cities = await CityOptionsAsync();
            ViewData["Artist"] = artist;
            return View(vm);
        }

        artist.StoreName = name;
        artist.CityID = vm.CityID!.Value;
        artist.Bio = string.IsNullOrWhiteSpace(vm.Bio) ? null : vm.Bio.Trim();
        await _db.SaveChangesAsync();

        TempData["Success"] = "Store profile saved.";
        return RedirectToAction(nameof(Store));
    }

    // ------------------------------------------------------------------ helpers

    private async Task<Artist?> CurrentArtistAsync()
    {
        var userId = User.GetUserId();
        return await _db.Artists
            .Include(a => a.City)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserID == userId);
    }

    private async Task<CustomRequest?> FindOwnRequestAsync(int id)
    {
        var artistId = User.GetArtistId();
        return await _db.CustomRequests.FirstOrDefaultAsync(r => r.RequestID == id && r.ArtistID == artistId);
    }

    private async Task<List<SellerShipmentVm>> LoadShipmentsAsync(int artistId, string? status)
    {
        var query = _db.Shipments.Where(s => s.ArtistID == artistId && s.Order.Status != OrderStatus.Cancelled);
        if (!string.IsNullOrEmpty(status)) query = query.Where(s => s.Status == status);

        return await query
            .OrderBy(s => s.Status == ShipmentStatus.Preparing ? 0 : s.Status == ShipmentStatus.Shipped ? 1 : 2)
            .ThenByDescending(s => s.Order.OrderDate)
            .Select(s => new SellerShipmentVm
            {
                ShipmentID = s.ShipmentID,
                OrderID = s.OrderID,
                OrderDate = s.Order.OrderDate,
                CustomerName = s.Order.User.FullName,
                CustomerPhone = s.Order.User.Phone,
                AddressLine = s.Order.Address.AddressLine,
                CityName = s.Order.Address.City.CityName,
                PostalCode = s.Order.Address.PostalCode,
                Status = s.Status,
                CourierName = s.CourierName,
                TrackingNo = s.TrackingNo,
                ShippedAt = s.ShippedAt,
                DeliveredAt = s.DeliveredAt,
                OrderState = s.Order.Status,
                Method = s.Order.Payment != null ? s.Order.Payment.Method : string.Empty,
                PaymentState = s.Order.Payment != null ? s.Order.Payment.Status : string.Empty,
                Lines = s.Order.Items
                    .Where(i => i.Product.ArtistID == artistId)
                    .Select(i => new SellerShipmentLineVm
                    {
                        ProductID = i.ProductID,
                        Title = i.Product.Title,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        ImageUrl = i.Product.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.ImageID)
                            .Select(x => x.ImageUrl).FirstOrDefault() ?? CatalogExtensions.PlaceholderImage
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    private void ValidateUploads(ProductFormVm vm)
    {
        if (vm.Uploads == null) return;
        foreach (var file in vm.Uploads.Where(f => f.Length > 0))
        {
            var error = ImageStorage.Validate(file);
            if (error != null) ModelState.AddModelError(nameof(vm.Uploads), error);
        }

        if (!string.IsNullOrWhiteSpace(vm.ImageUrl)
            && !vm.ImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !vm.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && !vm.ImageUrl.StartsWith("/", StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(vm.ImageUrl), "Image URLs must start with http://, https:// or /.");
        }
    }

    private async Task AddImagesAsync(int productId, ProductFormVm vm, bool hasPrimary)
    {
        var urls = new List<string>();
        if (vm.Uploads != null)
        {
            foreach (var file in vm.Uploads.Where(f => f.Length > 0))
            {
                urls.Add(await _images.SaveProductImageAsync(file));
            }
        }
        if (!string.IsNullOrWhiteSpace(vm.ImageUrl)) urls.Add(vm.ImageUrl.Trim());

        foreach (var url in urls)
        {
            _db.ProductImages.Add(new ProductImage { ProductID = productId, ImageUrl = url, IsPrimary = !hasPrimary });
            hasPrimary = true;
        }

        if (urls.Count > 0) await _db.SaveChangesAsync();
    }

    private async Task FillCategoriesAsync(ProductFormVm vm)
    {
        vm.Categories = await _db.Categories
            .OrderBy(c => c.CategoryName)
            .Select(c => new SelectListItem(c.CategoryName, c.CategoryID.ToString()))
            .ToListAsync();
    }

    private async Task<IEnumerable<SelectListItem>> CityOptionsAsync()
        => await _db.Cities
            .OrderBy(c => c.CityName)
            .Select(c => new SelectListItem(c.CityName + ", " + c.Province, c.CityID.ToString()))
            .ToListAsync();
}
