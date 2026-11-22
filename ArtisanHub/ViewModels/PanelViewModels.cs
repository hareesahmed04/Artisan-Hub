using System.ComponentModel.DataAnnotations;
using ArtisanHub.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArtisanHub.ViewModels;

// ----------------------------- Seller (artist) panel -----------------------------

public class SellerShipmentLineVm
{
    public int ProductID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class SellerShipmentVm
{
    public int ShipmentID { get; set; }
    public int OrderID { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CourierName { get; set; }
    public string? TrackingNo { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string OrderState { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string PaymentState { get; set; } = string.Empty;
    public List<SellerShipmentLineVm> Lines { get; set; } = new();
    public decimal Subtotal => Lines.Sum(l => l.Quantity * l.UnitPrice);
}

public class SellerDashboardVm
{
    public Artist Artist { get; set; } = null!;
    public decimal SalesThisMonth { get; set; }
    public decimal TotalSales { get; set; }
    public int OrdersThisMonth { get; set; }
    public int ActiveProducts { get; set; }
    public int AwaitingShipment { get; set; }
    public int PendingRequests { get; set; }
    public decimal? AvgRating { get; set; }
    public int ReviewCount { get; set; }
    public List<Product> LowStock { get; set; } = new();
    public List<SellerShipmentVm> RecentShipments { get; set; } = new();
}

public class SellerProductRowVm
{
    public int ProductID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQty { get; set; }
    public bool IsActive { get; set; }
    public bool IsCustomizable { get; set; }
    public int UnitsSold { get; set; }
}

public class ProductFormVm
{
    public int? ProductID { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a category."), Display(Name = "Category")]
    public int? CategoryID { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required, Range(1, 10000000, ErrorMessage = "Price must be between Rs. 1 and Rs. 10,000,000."), Display(Name = "Price (Rs.)")]
    public decimal? Price { get; set; }

    [Required, Range(0, 100000, ErrorMessage = "Stock must be between 0 and 100,000."), Display(Name = "Units in stock")]
    public int? StockQty { get; set; }

    [Display(Name = "I take custom orders for this item")]
    public bool IsCustomizable { get; set; }

    [Display(Name = "Visible in the shop")]
    public bool IsActive { get; set; } = true;

    [StringLength(500), Display(Name = "Or add an image by URL")]
    public string? ImageUrl { get; set; }

    [ValidateNever]
    public List<IFormFile>? Uploads { get; set; }

    [ValidateNever]
    public List<ProductImage> ExistingImages { get; set; } = new();

    [ValidateNever]
    public IEnumerable<SelectListItem> Categories { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class StoreProfileVm
{
    [Required, StringLength(100), Display(Name = "Store name")]
    public string StoreName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a city."), Display(Name = "City")]
    public int? CityID { get; set; }

    [StringLength(1000), Display(Name = "About your store")]
    public string? Bio { get; set; }

    public bool IsVerified { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> Cities { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class AmountBarVm
{
    public string Label { get; set; } = string.Empty;
    public string? Note { get; set; }
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class SellerSalesVm
{
    public SalesByArtist? Summary { get; set; }
    public decimal SalesThisMonth { get; set; }
    public List<AmountBarVm> Months { get; set; } = new();
    public List<AmountBarVm> TopProducts { get; set; } = new();
}

// ----------------------------- Admin panel -----------------------------

public class AdminOrderRowVm
{
    public int OrderID { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal Total { get; set; }
    public string Method { get; set; } = string.Empty;
    public string PaymentState { get; set; } = string.Empty;
    public int ShipmentCount { get; set; }
}

public class AdminDashboardVm
{
    public int Customers { get; set; }
    public int VerifiedArtists { get; set; }
    public int PendingArtists { get; set; }
    public int ActiveProducts { get; set; }
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public int OpenRequests { get; set; }
    public List<AdminOrderRowVm> RecentOrders { get; set; } = new();
    public List<Artist> ArtistsToVerify { get; set; } = new();
    public List<SalesByCategory> CategorySales { get; set; } = new();
}

public class AdminArtistRowVm
{
    public int ArtistID { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; }
    public bool IsVerified { get; set; }
    public int ProductCount { get; set; }
    public decimal TotalSales { get; set; }
    public string? Bio { get; set; }
}

public class CategoryFormVm
{
    public int? CategoryID { get; set; }

    [Required, StringLength(60), Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }
}

public class AdminCategoryRowVm
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
    public int RequestCount { get; set; }
}

public class AdminUserRowVm
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public string? StoreName { get; set; }
    public int OrderCount { get; set; }
}

public class AdminReportsVm
{
    public List<SalesByArtist> ByArtist { get; set; } = new();
    public List<SalesByCategory> ByCategory { get; set; } = new();
    public List<SalesByCity> ByCity { get; set; } = new();
    public decimal GrandTotal { get; set; }
    public int ItemsSold { get; set; }
}
